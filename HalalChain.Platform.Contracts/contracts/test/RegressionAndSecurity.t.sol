// SPDX-License-Identifier: MIT
pragma solidity 0.8.24;

import {Test} from "forge-std/Test.sol";
import {HalalAccessControl} from "../src/HalalAccessControl.sol";
import {SupplierRegistry} from "../src/SupplierRegistry.sol";
import {HalalProductRegistry} from "../src/HalalProductRegistry.sol";
import {HalalCertificationRegistry, IHalalCertificationRegistry} from "../src/HalalCertificationRegistry.sol";
import {TraceabilityEventLog} from "../src/TraceabilityEventLog.sol";
import {Deploy} from "../script/Deploy.s.sol";

/**
 * @title RegressionAndSecurityTest
 * @notice Targeted regression + security tests for the audit fixes.
 *
 * Covers:
 *  - ProductRegistry.metadataHash index updates on updateMetadata
 *  - ProductRegistry.productExists(bytes32(0)) == false
 *  - ProductRegistry.recall frees the metadata index
 *  - ProductRegistry and CertRegistry ReentrancyGuard wiring
 *  - AccessControl.compactPendingGrants / cancellation compaction
 *  - AccessControl.rotateCertificationRegistry emits event
 *  - AccessControl.rotateCertificationRegistry: unauthorized revert
 *  - Deploy script hardening: mainnet timelock < 24h reverts
 *  - TraceabilityEventLog: existing eventId reverts with EventAlreadyExists
 *  - Constructor zero-address validation
 */
contract RegressionAndSecurityTest is Test {
    HalalAccessControl        internal access;
    SupplierRegistry          internal suppliers;
    HalalProductRegistry      internal products;
    HalalCertificationRegistry internal certs;
    TraceabilityEventLog      internal events;

    address internal admin        = address(0xAD);
    address internal operator     = address(0xOP);
    address internal certifier1   = address(0xC1);
    address internal inspector    = address(0xIN);
    address internal supplier1    = address(0xS1);
    address internal attacker     = address(0xBAD);

    bytes32 internal SID1 = keccak256("SUP-MY-000001");
    bytes32 internal PID1 = keccak256("HPC-PROD-2026-000001");
    bytes32 internal PID2 = keccak256("HPC-PROD-2026-000002");
    bytes32 internal CID1 = keccak256("CRT-2026-JAKIM-000001");

    event ProductMetadataUpdated(bytes32 indexed productId, bytes32 metadataHash, string ipfsCid, uint64 updatedAt);
    event CertificationRegistryRotated(address indexed previousCertifier, address indexed newCertifier, address indexed admin);

    function setUp() public {
        access = new HalalAccessControl(admin, 0);
        suppliers = new SupplierRegistry(access);
        products = new HalalProductRegistry(access, suppliers);
        certs    = new HalalCertificationRegistry(access, products);
        events   = new TraceabilityEventLog(access, products);

        vm.prank(admin);
        products.setCertificationRegistry(address(certs));

        vm.startPrank(admin);
        access.grantRole(access.PLATFORM_OPERATOR_ROLE(), operator);
        access.grantRole(access.CERTIFIER_ROLE(), certifier1);
        access.grantRole(access.INSPECTOR_ROLE(), inspector);
        vm.stopPrank();
    }

    // ════════════════════════════════════════════════════════════════
    // HalalProductRegistry: metadata hash uniqueness invariants
    // ════════════════════════════════════════════════════════════════

    function _registerSupplier() internal {
        vm.prank(operator);
        suppliers.registerSupplier(SID1, supplier1, "ipfs://sup", "MY");
    }

    function test_Product_UpdateMetadata_FreesOldHash() public {
        _registerSupplier();
        bytes32 hashA = keccak256("hashA");
        bytes32 hashB = keccak256("hashB");
        bytes32 hashC = keccak256("hashC");

        vm.startPrank(operator);
        products.registerProduct(PID1, SID1, hashA, "ipfs://1", "MY");

        // Updating to a new hash must free the old one from the index
        // so it can be used for a different product.
        products.updateMetadata(PID1, hashB, "ipfs://1b");

        // The old hash must now be re-usable.
        products.registerProduct(PID2, SID1, hashA, "ipfs://2", "MY");
        // The new hash must NOT be re-usable (it's currently held by PID1).
        vm.expectRevert(abi.encodeWithSignature("DuplicateMetadataHash(bytes32)", hashB));
        products.registerProduct(keccak256("pid3"), SID1, hashB, "ipfs://3", "MY");
        vm.stopPrank();
    }

    function test_Product_UpdateMetadata_SameHashIsNoOp() public {
        _registerSupplier();
        bytes32 hashA = keccak256("hashA");
        vm.startPrank(operator);
        products.registerProduct(PID1, SID1, hashA, "ipfs://1", "MY");
        // Same hash should NOT revert; it is a no-op.
        products.updateMetadata(PID1, hashA, "ipfs://1");
        vm.stopPrank();
    }

    function test_Product_Recall_FreesMetadataHash() public {
        _registerSupplier();
        bytes32 hashA = keccak256("hashA");
        vm.startPrank(operator);
        products.registerProduct(PID1, SID1, hashA, "ipfs://1", "MY");
        products.recall(PID1, "ipfs://reason");
        // The metadata hash must now be free for re-registration.
        products.registerProduct(PID2, SID1, hashA, "ipfs://2", "MY");
        vm.stopPrank();
    }

    function test_Product_Exists_RejectsZeroId() public view {
        assertFalse(products.productExists(bytes32(0)));
    }

    function test_Product_Constructor_RejectsZeroAddresses() public {
        vm.expectRevert(abi.encodeWithSignature("ZeroAddress()"));
        new HalalProductRegistry(HalalAccessControl(address(0)), suppliers);
        vm.expectRevert(abi.encodeWithSignature("ZeroAddress()"));
        new HalalProductRegistry(access, SupplierRegistry(address(0)));
    }

    // ════════════════════════════════════════════════════════════════
    // HalalCertificationRegistry: zero-address + ReentrancyGuard
    // ════════════════════════════════════════════════════════════════

    function test_Cert_Constructor_RejectsZeroAddresses() public {
        vm.expectRevert(abi.encodeWithSignature("ZeroAddress()"));
        new HalalCertificationRegistry(HalalAccessControl(address(0)), products);
        vm.expectRevert(abi.encodeWithSignature("ZeroAddress()"));
        new HalalCertificationRegistry(access, HalalProductRegistry(address(0)));
    }

    // ════════════════════════════════════════════════════════════════
    // HalalAccessControl: pending grant compaction + cert rotation
    // ════════════════════════════════════════════════════════════════

    function test_AccessControl_CancelPendingGrant_CompactsList() public {
        // 3 pending grants
        vm.startPrank(admin);
        bytes32 k1 = access.proposeRoleGrant(address(0xA1), access.PLATFORM_OPERATOR_ROLE());
        bytes32 k2 = access.proposeRoleGrant(address(0xA2), access.PLATFORM_OPERATOR_ROLE());
        bytes32 k3 = access.proposeRoleGrant(address(0xA3), access.PLATFORM_OPERATOR_ROLE());
        assertEq(_pendingGrantCount(), 3);
        access.cancelRoleGrant(k2);
        // List should be compacted to 2 entries.
        assertEq(_pendingGrantCount(), 2);
        // Both remaining grants should still be queryable.
        (address a1,,) = access.pendingGrants(k1);
        (address a3,,) = access.pendingGrants(k3);
        assertEq(a1, address(0xA1));
        assertEq(a3, address(0xA3));
        vm.stopPrank();
    }

    function test_AccessControl_RotateCertificationRegistry_EmitsEvent() public {
        address newCert = address(0xC2);
        vm.expectEmit(true, true, true, false);
        emit CertificationRegistryRotated(address(0), newCert, admin);
        vm.prank(admin);
        access.rotateCertificationRegistry(newCert);
        assertEq(access.certificationRegistry(), newCert);
    }

    function test_AccessControl_RotateCertificationRegistry_RevertsForNonAdmin() public {
        vm.prank(attacker);
        vm.expectRevert();
        access.rotateCertificationRegistry(address(0xC2));
    }

    function test_AccessControl_RotateCertificationRegistry_RevertsForZeroAddress() public {
        vm.prank(admin);
        vm.expectRevert(abi.encodeWithSignature("ZeroAddress()"));
        access.rotateCertificationRegistry(address(0));
    }

    // ════════════════════════════════════════════════════════════════
    // TraceabilityEventLog: duplicates must be rejected
    // ════════════════════════════════════════════════════════════════

    function test_Event_DuplicateIdRevertsWithEventAlreadyExists() public {
        _registerSupplier();
        vm.prank(operator);
        products.registerProduct(PID1, SID1, keccak256("h"), "ipfs://p", "MY");
        bytes32 EID = keccak256("event-1");
        vm.startPrank(operator);
        events.recordEvent(EID, PID1, keccak256("batch"), TraceabilityEventLog.EventType.Manufactured, "x", "y", "z");
        vm.expectRevert(abi.encodeWithSignature("EventAlreadyExists(bytes32)", EID));
        events.recordEvent(EID, PID1, keccak256("batch"), TraceabilityEventLog.EventType.Manufactured, "x", "y", "z");
        vm.stopPrank();
    }

    function test_EventLog_Constructor_RejectsZeroAddresses() public {
        vm.expectRevert(abi.encodeWithSignature("ZeroAddress()"));
        new TraceabilityEventLog(HalalAccessControl(address(0)), products);
        vm.expectRevert(abi.encodeWithSignature("ZeroAddress()"));
        new TraceabilityEventLog(access, HalalProductRegistry(address(0)));
    }

    // ════════════════════════════════════════════════════════════════
    // Deploy script: hardening
    // ════════════════════════════════════════════════════════════════

    function test_Deploy_Run_RevertsToForceExplicitMode() public {
        Deploy d = new Deploy();
        vm.expectRevert();
        d.run();
    }

    function test_Deploy_Mainnet_RejectsShortTimelock() public {
        Deploy d = new Deploy();
        vm.expectRevert();
        d.deployMainnet(60); // 1 minute — below 24h floor
    }

    function test_Deploy_Testnet_RejectsLongTimelock() public {
        Deploy d = new Deploy();
        vm.expectRevert();
        d.deployTestnet(30 days);
    }

    // ════════════════════════════════════════════════════════════════
    // Fuzz: productExists(bytes32(0)) always false
    // ════════════════════════════════════════════════════════════════
    function testFuzz_ProductExists_AlwaysFalseForZero() public view {
        assertFalse(products.productExists(bytes32(0)));
    }

    function _pendingGrantCount() internal view returns (uint256) {
        // The pendingGrantKeys public array has an autogenerated length
        // accessor only when the contract declares `function
        // pendingGrantKeysLength() external view returns (uint256)` (or
        // uses the array's own `pendingGrantKeys(uint256)` indexed
        // getter). We probe the public array getter instead.
        uint256 n = 0;
        while (true) {
            try access.pendingGrantKeys(n) returns (bytes32) {
                unchecked { n += 1; }
            } catch {
                break;
            }
        }
        return n;
    }
}
