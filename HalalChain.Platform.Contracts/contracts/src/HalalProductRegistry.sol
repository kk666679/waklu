// SPDX-License-Identifier: MIT
pragma solidity 0.8.24;

import {HalalAccessControl} from "./HalalAccessControl.sol";
import {SupplierRegistry} from "./SupplierRegistry.sol";
import {ReentrancyGuard} from "@openzeppelin/contracts/utils/ReentrancyGuard.sol";

/**
 * @title IHalalProductRegistry
 * @notice EIP-165 marker for the product registry.
 */
interface IHalalProductRegistry {
    enum ProductStatus { Pending, Verified, Suspended, Recalled }

    struct Product {
        bytes32 productId;        // stable off-chain-issued ID, e.g. HPC-PROD-2026-000123
        bytes32 supplierId;        // reference to SupplierRegistry
        bytes32 metadataHash;      // SHA-256 of (ipfsCid || supplierId || productId || name || batch)
        string  ipfsCid;           // IPFS CID of product metadata JSON
        string  jurisdiction;      // ISO-3166-1 alpha-2 (cached for cheap reads)
        bytes32 currentCertId;     // pointer to the active HalalCertificationRegistry cert (0 = none)
        uint64  registeredAt;
        uint64  updatedAt;
        ProductStatus status;
        bool    exists;
    }

    event ProductRegistered(
        bytes32 indexed productId, bytes32 indexed supplierId, bytes32 metadataHash, string ipfsCid, uint64 registeredAt
    );
    event ProductCurrentCertUpdated(bytes32 indexed productId, bytes32 indexed certId);
    event ProductStatusChanged(bytes32 indexed productId, ProductStatus status, uint64 updatedAt);
    event ProductRecalled(bytes32 indexed productId, string reasonCid, uint64 recalledAt);
    event ProductMetadataUpdated(bytes32 indexed productId, bytes32 metadataHash, string ipfsCid, uint64 updatedAt);
}

/**
 * @title HalalProductRegistry
 * @notice Per-product provenance. The product's "verified" state is
 *         derived from the active certification pointer
 *         (currentCertId) — there is no separate "isVerified" boolean.
 *         Only PLATFORM_OPERATOR_ROLE may register products. The
 *         certification registry cross-calls setCurrentCertificate
 *         to update the pointer atomically with cert issuance/revocation.
 *
 *         Implements checks-effects-interactions: every external call
 *         happens AFTER state has been mutated, and the registry uses
 *         ReentrancyGuard for an extra layer of defence-in-depth.
 */
contract HalalProductRegistry is IHalalProductRegistry, ReentrancyGuard {
    HalalAccessControl public immutable access;
    SupplierRegistry public immutable suppliers;

    mapping(bytes32 => Product) private _products;
    mapping(bytes32 => bool)    private _metadataHashes; // prevent duplicate registrations with the same hash

    error ProductNotFound(bytes32 productId);
    error ProductAlreadyRegistered(bytes32 productId);
    error DuplicateMetadataHash(bytes32 metadataHash);
    error SupplierNotActive(bytes32 supplierId);
    error InvalidStatusTransition(ProductStatus from, ProductStatus to);
    error ZeroBytes32();
    error ZeroAddress();
    error NotCertificationRegistry(address caller, address expected);

    modifier onlyOperator() {
        access._checkRole(access.PLATFORM_OPERATOR_ROLE(), msg.sender);
        _;
    }

    /// @notice Only the CertificationRegistry may call setCurrentCertificate.
    modifier onlyCertificationRegistry() {
        if (msg.sender != certifier) revert NotCertificationRegistry(msg.sender, certifier);
        _;
    }

    address public certifier;

    constructor(HalalAccessControl access_, SupplierRegistry suppliers_) {
        if (address(access_) == address(0) || address(suppliers_) == address(0)) revert ZeroAddress();
        access = access_;
        suppliers = suppliers_;
    }

    /// @notice One-time, post-deploy: the deployer (DEFAULT_ADMIN) registers
    ///         the CertificationRegistry so it can call setCurrentCertificate.
    function setCertificationRegistry(address certifier_) external {
        access._checkRole(access.DEFAULT_ADMIN_ROLE(), msg.sender);
        if (certifier_ == address(0)) revert ZeroAddress();
        certifier = certifier_;
    }

    function registerProduct(
        bytes32 productId,
        bytes32 supplierId,
        bytes32 metadataHash,
        string calldata ipfsCid,
        string calldata jurisdiction
    ) external onlyOperator nonReentrant returns (uint64 registeredAt) {
        if (productId == bytes32(0) || supplierId == bytes32(0) || metadataHash == bytes32(0)) {
            revert ZeroBytes32();
        }
        if (_products[productId].exists) revert ProductAlreadyRegistered(productId);
        if (_metadataHashes[metadataHash]) revert DuplicateMetadataHash(metadataHash);

        // Verify the supplier is active
        SupplierRegistry.SupplierStatus status = suppliers.getSupplier(supplierId).status;
        if (status != SupplierRegistry.SupplierStatus.Active) revert SupplierNotActive(supplierId);

        registeredAt = uint64(block.timestamp);
        _products[productId] = Product({
            productId: productId,
            supplierId: supplierId,
            metadataHash: metadataHash,
            ipfsCid: ipfsCid,
            jurisdiction: jurisdiction,
            currentCertId: bytes32(0),
            registeredAt: registeredAt,
            updatedAt: registeredAt,
            status: ProductStatus.Pending,
            exists: true
        });
        _metadataHashes[metadataHash] = true;
        emit ProductRegistered(productId, supplierId, metadataHash, ipfsCid, registeredAt);
    }

    /// @notice Called by HalalCertificationRegistry when a new cert is issued or revoked.
    function setCurrentCertificate(bytes32 productId, bytes32 certId) external onlyCertificationRegistry nonReentrant {
        Product storage p = _products[productId];
        if (!p.exists) revert ProductNotFound(productId);
        p.currentCertId = certId;
        p.updatedAt = uint64(block.timestamp);
        // A non-zero cert pointer auto-promotes the product to Verified
        if (certId != bytes32(0) && p.status == ProductStatus.Pending) {
            p.status = ProductStatus.Verified;
            emit ProductStatusChanged(productId, ProductStatus.Verified, p.updatedAt);
        }
        // A zero cert pointer (revocation) demotes to Pending (if currently Verified)
        if (certId == bytes32(0) && p.status == ProductStatus.Verified) {
            p.status = ProductStatus.Pending;
            emit ProductStatusChanged(productId, ProductStatus.Pending, p.updatedAt);
        }
        emit ProductCurrentCertUpdated(productId, certId);
    }

    function setStatus(bytes32 productId, ProductStatus newStatus) external onlyOperator {
        Product storage p = _products[productId];
        if (!p.exists) revert ProductNotFound(productId);
        ProductStatus old = p.status;
        if (old == newStatus) return;
        if (old == ProductStatus.Recalled) revert InvalidStatusTransition(old, newStatus);
        p.status = newStatus;
        p.updatedAt = uint64(block.timestamp);
        emit ProductStatusChanged(productId, newStatus, p.updatedAt);
    }

    function recall(bytes32 productId, string calldata reasonCid) external onlyOperator {
        Product storage p = _products[productId];
        if (!p.exists) revert ProductNotFound(productId);
        // Free the previously-unique metadata hash so a re-registration
        // of the same product (e.g. after recall closure) is possible.
        if (p.metadataHash != bytes32(0)) {
            delete _metadataHashes[p.metadataHash];
        }
        p.status = ProductStatus.Recalled;
        p.currentCertId = bytes32(0);
        p.updatedAt = uint64(block.timestamp);
        emit ProductStatusChanged(productId, ProductStatus.Recalled, p.updatedAt);
        emit ProductRecalled(productId, reasonCid, p.updatedAt);
    }

    function updateMetadata(
        bytes32 productId,
        bytes32 metadataHash,
        string calldata ipfsCid
    ) external onlyOperator {
        Product storage p = _products[productId];
        if (!p.exists) revert ProductNotFound(productId);
        if (metadataHash == bytes32(0)) revert ZeroBytes32();
        // The new hash must be globally unique unless it is the same
        // as the one currently on the product (a no-op metadata update).
        if (metadataHash != p.metadataHash) {
            if (_metadataHashes[metadataHash]) revert DuplicateMetadataHash(metadataHash);
            _metadataHashes[metadataHash] = true;
            // Free the old hash from the uniqueness index so a future
            // re-registration or a different product can use it.
            if (p.metadataHash != bytes32(0)) {
                delete _metadataHashes[p.metadataHash];
            }
        }
        p.metadataHash = metadataHash;
        p.ipfsCid = ipfsCid;
        p.updatedAt = uint64(block.timestamp);
        emit ProductMetadataUpdated(productId, metadataHash, ipfsCid, p.updatedAt);
    }

    function getProduct(bytes32 productId) external view returns (Product memory) {
        Product memory p = _products[productId];
        if (!p.exists) revert ProductNotFound(productId);
        return p;
    }

    /// @notice Returns true only when a real product is registered at ``productId``.
    ///         ``bytes32(0)`` is NEVER a valid product ID.
    function productExists(bytes32 productId) external view returns (bool) {
        if (productId == bytes32(0)) return false;
        return _products[productId].exists;
    }

    function supportsInterface(bytes4 iid) external pure returns (bool) {
        return iid == type(IHalalProductRegistry).interfaceId || iid == 0x01ffc9a7;
    }
}
