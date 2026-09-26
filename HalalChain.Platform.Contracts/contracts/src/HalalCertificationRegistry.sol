// SPDX-License-Identifier: MIT
pragma solidity 0.8.24;

import {HalalAccessControl} from "./HalalAccessControl.sol";
import {HalalProductRegistry, IHalalProductRegistry} from "./HalalProductRegistry.sol";
import {ReentrancyGuard} from "@openzeppelin/contracts/utils/ReentrancyGuard.sol";

/**
 * @title IHalalCertificationRegistry
 * @notice EIP-165 marker for the certification registry.
 */
interface IHalalCertificationRegistry {
    enum CertificateStatus { Active, Revoked, Expired }

    struct Certificate {
        bytes32 certId;            // unique cert ID, e.g. CRT-2026-JAKIM-000123
        bytes32 productId;         // pointer to HalalProductRegistry
        address certifier;         // the certifier's wallet (has CERTIFIER_ROLE)
        string  documentCid;        // IPFS CID of the certificate body (PDF/image)
        uint64  issuedAt;
        uint64  expiresAt;
        string  scopeCid;           // IPFS CID of the cert scope (product line, facility, etc.)
        string  country;            // ISO-3166-1 alpha-2 of the certifying body
        CertificateStatus status;
        bool    exists;
    }

    event CertificateIssued(
        bytes32 indexed certId, bytes32 indexed productId, address indexed certifier,
        string documentCid, uint64 issuedAt, uint64 expiresAt, string country
    );
    event CertificateRevoked(bytes32 indexed certId, string reasonCid, uint64 revokedAt);
    event CertificateExpired(bytes32 indexed certId, uint64 expiredAt);
}

/**
 * @title HalalCertificationRegistry
 * @notice Per-certificate record. Only the issuing certifier (or
 *         PLATFORM_OPERATOR_ROLE in an emergency) may revoke.
 *         Cross-calls HalalProductRegistry.setCurrentCertificate so
 *         the product's "verified" state updates atomically with the
 *         cert pointer.
 */
contract HalalCertificationRegistry is IHalalCertificationRegistry, ReentrancyGuard {
    HalalAccessControl public immutable access;
    HalalProductRegistry public immutable products;

    mapping(bytes32 => Certificate) private _certs;
    mapping(bytes32 => bytes32[]) private _productCertHistory; // productId => list of certIds (chronological)
    mapping(bytes32 => uint256)    private _productCurrentIndex; // productId => index into _productCertHistory for current cert

    error CertNotFound(bytes32 certId);
    error CertAlreadyExists(bytes32 certId);
    error ProductNotFound(bytes32 productId);
    error NotCertifier(address caller, address expected);
    error ExpiredCert(uint64 expiresAt, uint64 nowTs);
    error InvalidExpiry(uint64 issuedAt, uint64 expiresAt);
    error ZeroBytes32();
    error ZeroAddress();

    modifier onlyCertifier() {
        access.requireRole(access.CERTIFIER_ROLE(), msg.sender);
        _;
    }

    modifier onlyOperator() {
        access.requireRole(access.PLATFORM_OPERATOR_ROLE(), msg.sender);
        _;
    }

    constructor(HalalAccessControl access_, HalalProductRegistry products_) {
        if (address(access_) == address(0) || address(products_) == address(0)) revert ZeroAddress();
        access = access_;
        products = products_;
    }

    /// @notice Issue a certificate for a product. Sets it as the product's current cert.
    function issueCertificate(
        bytes32 certId,
        bytes32 productId,
        string calldata documentCid,
        uint64 expiresAt,
        string calldata scopeCid,
        string calldata country
    ) external onlyCertifier nonReentrant returns (uint64 issuedAt) {
        if (certId == bytes32(0) || productId == bytes32(0)) revert ZeroBytes32();
        if (_certs[certId].exists) revert CertAlreadyExists(certId);

        IHalalProductRegistry.Product memory product = products.getProduct(productId);
        if (!product.exists) revert ProductNotFound(productId);

        issuedAt = uint64(block.timestamp);
        if (expiresAt <= issuedAt) revert InvalidExpiry(issuedAt, expiresAt);

        _certs[certId] = Certificate({
            certId: certId,
            productId: productId,
            certifier: msg.sender,
            documentCid: documentCid,
            issuedAt: issuedAt,
            expiresAt: expiresAt,
            scopeCid: scopeCid,
            country: country,
            status: CertificateStatus.Active,
            exists: true
        });

        // Append to history and set as current
        _productCertHistory[productId].push(certId);
        _productCurrentIndex[productId] = _productCertHistory[productId].length - 1;

        // Cross-call: point the product at this cert
        products.setCurrentCertificate(productId, certId);

        emit CertificateIssued(certId, productId, msg.sender, documentCid, issuedAt, expiresAt, country);
    }

    /// @notice Revoke a certificate. The issuing certifier OR the platform
    ///         operator (emergency) may revoke. Cross-calls to clear the
    ///         product's current cert pointer.
    function revokeCertificate(bytes32 certId, string calldata reasonCid) external nonReentrant {
        Certificate storage c = _certs[certId];
        if (!c.exists) revert CertNotFound(certId);
        if (c.status != CertificateStatus.Active) {
            // Idempotent: a re-revocation is a no-op
            return;
        }
        if (msg.sender != c.certifier) {
            access.requireRole(access.PLATFORM_OPERATOR_ROLE(), msg.sender);
        }
        c.status = CertificateStatus.Revoked;
        emit CertificateRevoked(certId, reasonCid, uint64(block.timestamp));

        // Clear the product's current cert pointer
        products.setCurrentCertificate(c.productId, bytes32(0));
    }

    function expireCertificate(bytes32 certId) external nonReentrant {
        Certificate storage c = _certs[certId];
        if (!c.exists) revert CertNotFound(certId);
        if (c.status != CertificateStatus.Active) return;
        if (uint64(block.timestamp) < c.expiresAt) revert ExpiredCert(c.expiresAt, uint64(block.timestamp));
        c.status = CertificateStatus.Expired;
        emit CertificateExpired(certId, uint64(block.timestamp));
        products.setCurrentCertificate(c.productId, bytes32(0));
    }

    function getCertificate(bytes32 certId) external view returns (Certificate memory) {
        Certificate memory c = _certs[certId];
        if (!c.exists) revert CertNotFound(certId);
        return c;
    }

    function getCurrentCertForProduct(bytes32 productId) external view returns (Certificate memory) {
        if (_productCertHistory[productId].length == 0) revert CertNotFound(bytes32(0));
        uint256 idx = _productCurrentIndex[productId];
        return _certs[_productCertHistory[productId][idx]];
    }

    function getCertHistoryForProduct(bytes32 productId) external view returns (bytes32[] memory) {
        return _productCertHistory[productId];
    }

    function isValid(bytes32 certId) external view returns (bool) {
        Certificate memory c = _certs[certId];
        if (!c.exists) return false;
        if (c.status != IHalalCertificationRegistry.CertificateStatus.Active) return false;
        if (uint64(block.timestamp) > c.expiresAt) return false;
        return true;
    }

    function supportsInterface(bytes4 iid) external pure returns (bool) {
        return iid == type(IHalalCertificationRegistry).interfaceId || iid == 0x01ffc9a7;
    }
}
