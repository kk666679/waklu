// SPDX-License-Identifier: MIT
pragma solidity 0.8.24;

import {Test} from "forge-std/Test.sol";
import {HalalAccessControl} from "../src/HalalAccessControl.sol";
import {SupplierRegistry} from "../src/SupplierRegistry.sol";
import {HalalProductRegistry} from "../src/HalalProductRegistry.sol";
import {HalalCertificationRegistry, IHalalCertificationRegistry} from "../src/HalalCertificationRegistry.sol";
import {TraceabilityEventLog} from "../src/TraceabilityEventLog.sol";

/**
 * @title HalalPlatformTest
 * @notice End-to-end Foundry tests for the HalalChain platform.
 *         Covers: role access control, supplier lifecycle, product
 *         registration, certificate issuance/revocation, cross-contract
 *         invariants, traceability events, and event emission.
 */
contract HalalPlatformTest is Test {
    HalalAccessControl        internal access;
    SupplierRegistry          internal suppliers;
    HalalProductRegistry      internal products;
    HalalCertificationRegistry internal certs;
    TraceabilityEventLog      internal events;

    address internal admin        = address(0xAD);
    address internal operator     = address(0xOP);
    address internal certifier1   = address(0xC1); // JAKIM-style
    address internal certifier2   = address(0xC2); // MUI-style
    address internal inspector    = address(0xIN);
    address internal supplier1    = address(0xS1);
    address internal supplier2    = address(0xS2);
    address internal attacker     = address(0xBAD);

    bytes32 internal SID1 = keccak256("SUP-MY-000001");
    bytes32 internal SID2 = keccak256("SUP-MY-000002");
    bytes32 internal PID1 = keccak256("HPC-PROD-2026-000001");
    bytes32 internal PID2 = keccak256("HPC-PROD-2026-000002");
    bytes32 internal CID1 = keccak256("CRT-2026-JAKIM-000001");
    bytes32 internal BID1 = keccak256("BATCH-2026-001");

    // EIP-1967 / OZ v5: AccessControl._checkRole reverts with this custom error
    bytes32 internal constant ACCESS_CONTROL_DEFAULT_ADMIN = 0x00;

    function setUp() public {
        // timelock=0 on testnet for fast iteration
        access = new HalalAccessControl(admin, 0);
        suppliers = new SupplierRegistry(access);
        products = new HalalProductRegistry(access, suppliers);
        certs    = new HalalCertificationRegistry(access, products);
        events   = new TraceabilityEventLog(access, products);

        // Wire the cert registry as the authorised caller on the product registry
        vm.prank(admin);
        products.setCertificationRegistry(address(certs));

        // Grant roles
        vm.startPrank(admin);
        access.grantRole(access.PLATFORM_OPERATOR_ROLE(), operator);
        access.grantRole(access.CERTIFIER_ROLE(), certifier1);
        access.grantRole(access.CERTIFIER_ROLE(), certifier2);
        access.grantRole(access.INSPECTOR_ROLE(), inspector);
        vm.stopPrank();
    }

    // ════════════════════════════════════════════════════════════════
    //  HalalAccessControl
    // ════════════════════════════════════════════════════════════════
    function test_AccessControl_AdminHasDefaultRole() public view {
        assertTrue(access.hasRole(access.DEFAULT_ADMIN_ROLE(), admin));
    }

    function test_AccessControl_OperatorRoleGranted() public view {
        assertTrue(access.hasRole(access.PLATFORM_OPERATOR_ROLE(), operator));
    }

    function test_AccessControl_AttackerHasNoRole() public view {
        assertFalse(access.hasRole(access.PLATFORM_OPERATOR_ROLE(), attacker));
        assertFalse(access.hasRole(access.CERTIFIER_ROLE(), attacker));
    }

    function test_AccessControl_PauseAndUnpause() public {
        // Pauser is admin by default (constructor grants it)
        vm.prank(admin);
        access.pause();
        assertTrue(access.paused());
        vm.prank(admin);
        access.unpause();
        assertFalse(access.paused());
    }

    function test_AccessControl_AttackerCannotPause() public {
        vm.prank(attacker);
        vm.expectRevert();
        access.pause();
    }

    // ════════════════════════════════════════════════════════════════
    //  SupplierRegistry
    // ════════════════════════════════════════════════════════════════
    function test_Supplier_Register_Success() public {
        vm.prank(operator);
        suppliers.registerSupplier(SID1, supplier1, "ipfs://supplier1", "MY");
        SupplierRegistry.Supplier memory s = suppliers.getSupplier(SID1);
        assertTrue(s.exists);
        assertEq(s.wallet, supplier1);
        assertEq(uint(s.status), uint(SupplierRegistry.SupplierStatus.Active));
        assertEq(s.jurisdiction, "MY");
    }

    function test_Supplier_Register_RevertsIfNotOperator() public {
        vm.prank(attacker);
        vm.expectRevert();
        suppliers.registerSupplier(SID1, supplier1, "ipfs://supplier1", "MY");
    }

    function test_Supplier_Register_RevertsIfDuplicate() public {
        vm.startPrank(operator);
        suppliers.registerSupplier(SID1, supplier1, "ipfs://supplier1", "MY");
        vm.expectRevert(abi.encodeWithSignature("SupplierAlreadyRegistered(bytes32)", SID1));
        suppliers.registerSupplier(SID1, supplier1, "ipfs://supplier1", "MY");
        vm.stopPrank();
    }

    function test_Supplier_Register_RevertsIfWalletAlreadyLinked() public {
        vm.startPrank(operator);
        suppliers.registerSupplier(SID1, supplier1, "ipfs://supplier1", "MY");
        vm.expectRevert();
        suppliers.registerSupplier(SID2, supplier1, "ipfs://supplier2", "MY");
        vm.stopPrank();
    }

    function test_Supplier_SuspendAndRevoke() public {
        vm.prank(operator);
        suppliers.registerSupplier(SID1, supplier1, "ipfs://supplier1", "MY");

        vm.prank(operator);
        suppliers.setStatus(SID1, SupplierRegistry.SupplierStatus.Suspended);
        assertEq(uint(suppliers.getSupplier(SID1).status), uint(SupplierRegistry.SupplierStatus.Suspended));

        vm.prank(operator);
        suppliers.setStatus(SID1, SupplierRegistry.SupplierStatus.Active);

        vm.prank(operator);
        suppliers.setStatus(SID1, SupplierRegistry.SupplierStatus.Revoked);
        assertEq(uint(suppliers.getSupplier(SID1).status), uint(SupplierRegistry.SupplierStatus.Revoked));
        // Revoked frees the wallet
        assertEq(suppliers.getSupplierIdByWallet(supplier1), bytes32(0));
    }

    function test_Supplier_WalletRotation_TwoStep() public {
        address newWallet = address(0xNEW);
        vm.prank(operator);
        suppliers.registerSupplier(SID1, supplier1, "ipfs://supplier1", "MY");

        vm.prank(supplier1);
        suppliers.proposeWalletRotation(SID1, newWallet);
        assertEq(suppliers.getSupplier(SID1).proposedWallet, newWallet);

        // Current wallet still in effect
        assertEq(suppliers.getSupplier(SID1).wallet, supplier1);

        // Attacker cannot accept
        vm.prank(attacker);
        vm.expectRevert();
        suppliers.acceptWalletRotation(SID1);

        // New wallet accepts
        vm.prank(newWallet);
        suppliers.acceptWalletRotation(SID1);
        assertEq(suppliers.getSupplier(SID1).wallet, newWallet);
    }

    function test_Supplier_ProposeRotationRejectsLinkedWallet() public {
        vm.startPrank(operator);
        suppliers.registerSupplier(SID1, supplier1, "ipfs://supplier1", "MY");
        suppliers.registerSupplier(SID2, supplier2, "ipfs://supplier2", "MY");
        vm.stopPrank();

        vm.prank(supplier1);
        vm.expectRevert();
        suppliers.proposeWalletRotation(SID1, supplier2); // supplier2 already linked
    }

    // ════════════════════════════════════════════════════════════════
    //  HalalProductRegistry
    // ════════════════════════════════════════════════════════════════
    function _registerSupplier(bytes32 sid, address wallet) internal {
        vm.prank(operator);
        suppliers.registerSupplier(sid, wallet, "ipfs://x", "MY");
    }

    function test_Product_Register_Success() public {
        _registerSupplier(SID1, supplier1);
        bytes32 metadataHash = keccak256("meta1");

        vm.prank(operator);
        products.registerProduct(PID1, SID1, metadataHash, "ipfs://product1", "MY");

        HalalProductRegistry.Product memory p = products.getProduct(PID1);
        assertTrue(p.exists);
        assertEq(p.supplierId, SID1);
        assertEq(p.metadataHash, metadataHash);
        assertEq(uint(p.status), uint(HalalProductRegistry.ProductStatus.Pending));
    }

    function test_Product_Register_RevertsIfSupplierSuspended() public {
        _registerSupplier(SID1, supplier1);
        vm.prank(operator);
        suppliers.setStatus(SID1, SupplierRegistry.SupplierStatus.Suspended);

        vm.prank(operator);
        vm.expectRevert(abi.encodeWithSignature("SupplierNotActive(bytes32)", SID1));
        products.registerProduct(PID1, SID1, keccak256("h"), "ipfs://p", "MY");
    }

    function test_Product_Register_RevertsIfDuplicateMetadataHash() public {
        _registerSupplier(SID1, supplier1);
        bytes32 hash = keccak256("dup");

        vm.startPrank(operator);
        products.registerProduct(PID1, SID1, hash, "ipfs://p1", "MY");
        vm.expectRevert(abi.encodeWithSignature("DuplicateMetadataHash(bytes32)", hash));
        products.registerProduct(PID2, SID1, hash, "ipfs://p2", "MY");
        vm.stopPrank();
    }

    function test_Product_Register_RevertsIfNotOperator() public {
        _registerSupplier(SID1, supplier1);
        vm.prank(attacker);
        vm.expectRevert();
        products.registerProduct(PID1, SID1, keccak256("h"), "ipfs://p", "MY");
    }

    function test_Product_Recall() public {
        _registerSupplier(SID1, supplier1);
        vm.prank(operator);
        products.registerProduct(PID1, SID1, keccak256("h"), "ipfs://p", "MY");

        vm.prank(operator);
        products.recall(PID1, "ipfs://reason");
        assertEq(uint(products.getProduct(PID1).status), uint(HalalProductRegistry.ProductStatus.Recalled));
        assertEq(products.getProduct(PID1).currentCertId, bytes32(0));
    }

    // ════════════════════════════════════════════════════════════════
    //  HalalCertificationRegistry
    // ════════════════════════════════════════════════════════════════
    function _registerProduct(bytes32 pid, bytes32 sid) internal {
        _registerSupplier(sid, supplier1);
        vm.prank(operator);
        products.registerProduct(pid, sid, keccak256(abi.encodePacked(pid, "h")), "ipfs://p", "MY");
    }

    function test_Cert_Issue_Success_PromotesProductToVerified() public {
        _registerProduct(PID1, SID1);

        uint64 expires = uint64(block.timestamp) + 365 days;
        vm.prank(certifier1);
        certs.issueCertificate(CID1, PID1, "ipfs://cert1", expires, "ipfs://scope1", "MY");

        IHalalCertificationRegistry.Certificate memory c = certs.getCertificate(CID1);
        assertTrue(c.exists);
        assertEq(c.certifier, certifier1);
        assertEq(uint(c.status), uint(IHalalCertificationRegistry.CertificateStatus.Active));

        // Cross-call: product promoted to Verified
        assertEq(products.getProduct(PID1).currentCertId, CID1);
        assertEq(uint(products.getProduct(PID1).status), uint(HalalProductRegistry.ProductStatus.Verified));
    }

    function test_Cert_Issue_RevertsIfNotCertifier() public {
        _registerProduct(PID1, SID1);
        vm.prank(attacker);
        vm.expectRevert();
        certs.issueCertificate(CID1, PID1, "ipfs://c", uint64(block.timestamp) + 100, "ipfs://s", "MY");
    }

    function test_Cert_Issue_RevertsIfInvalidExpiry() public {
        _registerProduct(PID1, SID1);
        vm.prank(certifier1);
        vm.expectRevert(abi.encodeWithSignature("InvalidExpiry(uint64,uint64)", uint64(block.timestamp), uint64(block.timestamp)));
        certs.issueCertificate(CID1, PID1, "ipfs://c", uint64(block.timestamp), "ipfs://s", "MY");
    }

    function test_Cert_Revoke_ByIssuingCertifier_ClearsPointer() public {
        _registerProduct(PID1, SID1);
        uint64 expires = uint64(block.timestamp) + 365 days;
        vm.prank(certifier1);
        certs.issueCertificate(CID1, PID1, "ipfs://c", expires, "ipfs://s", "MY");
        assertEq(products.getProduct(PID1).currentCertId, CID1);

        vm.prank(certifier1);
        certs.revokeCertificate(CID1, "ipfs://reason");
        assertEq(uint(certs.getCertificate(CID1).status), uint(IHalalCertificationRegistry.CertificateStatus.Revoked));
        assertEq(products.getProduct(PID1).currentCertId, bytes32(0));
        assertEq(uint(products.getProduct(PID1).status), uint(HalalProductRegistry.ProductStatus.Pending));
    }

    function test_Cert_Revoke_ByOperator_Emergency() public {
        _registerProduct(PID1, SID1);
        uint64 expires = uint64(block.timestamp) + 365 days;
        vm.prank(certifier1);
        certs.issueCertificate(CID1, PID1, "ipfs://c", expires, "ipfs://s", "MY");

        // Operator (not the issuing certifier) revokes
        vm.prank(operator);
        certs.revokeCertificate(CID1, "ipfs://emergency");
        assertEq(uint(certs.getCertificate(CID1).status), uint(IHalalCertificationRegistry.CertificateStatus.Revoked));
    }

    function test_Cert_Revoke_ByAttacker_Reverts() public {
        _registerProduct(PID1, SID1);
        uint64 expires = uint64(block.timestamp) + 365 days;
        vm.prank(certifier1);
        certs.issueCertificate(CID1, PID1, "ipfs://c", expires, "ipfs://s", "MY");

        vm.prank(attacker);
        vm.expectRevert();
        certs.revokeCertificate(CID1, "ipfs://hack");
    }

    function test_Cert_Revoke_IsIdempotent() public {
        _registerProduct(PID1, SID1);
        uint64 expires = uint64(block.timestamp) + 365 days;
        vm.prank(certifier1);
        certs.issueCertificate(CID1, PID1, "ipfs://c", expires, "ipfs://s", "MY");
        vm.prank(certifier1);
        certs.revokeCertificate(CID1, "ipfs://r");
        // Second revoke is a no-op (no revert)
        vm.prank(certifier1);
        certs.revokeCertificate(CID1, "ipfs://r2");
        assertEq(uint(certs.getCertificate(CID1).status), uint(IHalalCertificationRegistry.CertificateStatus.Revoked));
    }

    function test_Cert_Expire_DemotesProduct() public {
        _registerProduct(PID1, SID1);
        uint64 expires = uint64(block.timestamp) + 100;
        vm.prank(certifier1);
        certs.issueCertificate(CID1, PID1, "ipfs://c", expires, "ipfs://s", "MY");

        // Move time forward
        vm.warp(expires + 1);

        vm.prank(certifier1);
        certs.expireCertificate(CID1);
        assertEq(uint(certs.getCertificate(CID1).status), uint(IHalalCertificationRegistry.CertificateStatus.Expired));
        assertEq(products.getProduct(PID1).currentCertId, bytes32(0));
    }

    function test_Cert_IsValid_HonoursExpiry() public {
        _registerProduct(PID1, SID1);
        uint64 expires = uint64(block.timestamp) + 100;
        vm.prank(certifier1);
        certs.issueCertificate(CID1, PID1, "ipfs://c", expires, "ipfs://s", "MY");
        assertTrue(certs.isValid(CID1));

        vm.warp(expires + 1);
        assertFalse(certs.isValid(CID1));
    }

    function test_Cert_TwoCertsForSameProduct_TracksCurrent() public {
        _registerProduct(PID1, SID1);
        bytes32 CID2 = keccak256("CRT-renewal");

        uint64 expires1 = uint64(block.timestamp) + 30 days;
        vm.prank(certifier1);
        certs.issueCertificate(CID1, PID1, "ipfs://c1", expires1, "ipfs://s1", "MY");
        assertEq(products.getProduct(PID1).currentCertId, CID1);

        // After CID1 expires, certifier2 issues a renewal
        vm.warp(expires1 + 1);
        vm.prank(certifier1);
        certs.expireCertificate(CID1);
        assertEq(products.getProduct(PID1).currentCertId, bytes32(0));

        uint64 expires2 = uint64(block.timestamp) + 365 days;
        vm.prank(certifier2);
        certs.issueCertificate(CID2, PID1, "ipfs://c2", expires2, "ipfs://s2", "ID");
        assertEq(products.getProduct(PID1).currentCertId, CID2);
        assertEq(certs.getCurrentCertForProduct(PID1).certId, CID2);
        assertEq(certs.getCertHistoryForProduct(PID1).length, 2);
    }

    // ════════════════════════════════════════════════════════════════
    //  TraceabilityEventLog
    // ════════════════════════════════════════════════════════════════
    function test_Event_RecordByOperator() public {
        _registerProduct(PID1, SID1);
        bytes32 EID = keccak256("event-1");
        vm.prank(operator);
        events.recordEvent(EID, PID1, BID1, TraceabilityEventLog.EventType.Manufactured, "ipfs://loc", "ipfs://ev", "ipfs://notes");
        assertTrue(events.getEvent(EID).exists);
        assertEq(events.totalEvents(), 1);
    }

    function test_Event_RecordByInspector() public {
        _registerProduct(PID1, SID1);
        bytes32 EID = keccak256("event-inspect");
        vm.prank(inspector);
        events.recordEvent(EID, PID1, BID1, TraceabilityEventLog.EventType.Inspected, "ipfs://loc", "ipfs://ev", "");
        assertTrue(events.getEvent(EID).exists);
    }

    function test_Event_RecordByAttacker_Reverts() public {
        _registerProduct(PID1, SID1);
        vm.prank(attacker);
        vm.expectRevert();
        events.recordEvent(keccak256("e"), PID1, BID1, TraceabilityEventLog.EventType.Manufactured, "x", "y", "z");
    }

    function test_Event_RecordForUnknownProduct_Reverts() public {
        vm.prank(operator);
        vm.expectRevert(abi.encodeWithSignature("ProductNotFound(bytes32)", PID1));
        events.recordEvent(keccak256("e"), PID1, BID1, TraceabilityEventLog.EventType.Manufactured, "x", "y", "z");
    }

    // ════════════════════════════════════════════════════════════════
    //  EIP-165
    // ════════════════════════════════════════════════════════════════
    function test_EIP165_SupplierRegistry() public view {
        assertTrue(suppliers.supportsInterface(0x01ffc9a7)); // ERC165
    }
    function test_EIP165_ProductRegistry() public view {
        assertTrue(products.supportsInterface(0x01ffc9a7));
    }
    function test_EIP165_CertRegistry() public view {
        assertTrue(certs.supportsInterface(0x01ffc9a7));
    }
    function test_EIP165_EventLog() public view {
        assertTrue(events.supportsInterface(0x01ffc9a7));
    }

    // ════════════════════════════════════════════════════════════════
    //  Fuzz: metadata hash uniqueness for arbitrary product IDs
    // ════════════════════════════════════════════════════════════════
    function testFuzz_DuplicateMetadataHashRejected(uint256 seed) public {
        _registerSupplier(SID1, supplier1);
        bytes32 pidA = keccak256(abi.encode("prodA", seed));
        bytes32 pidB = keccak256(abi.encode("prodB", seed));
        bytes32 hash  = keccak256(abi.encode(seed));

        vm.startPrank(operator);
        products.registerProduct(pidA, SID1, hash, "ipfs://a", "MY");
        vm.expectRevert(abi.encodeWithSignature("DuplicateMetadataHash(bytes32)", hash));
        products.registerProduct(pidB, SID1, hash, "ipfs://b", "MY");
        vm.stopPrank();
    }
}
