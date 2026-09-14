// SPDX-License-Identifier: MIT
// ═══════════════════════════════════════════════════════════════════
// PROPRIETARY — HalalChain Certificate Registry
// Copyright © HalalChain. All rights reserved.
// ═══════════════════════════════════════════════════════════════════

pragma solidity ^0.8.19;

/**
 * @title CertificateRegistry
 * @dev Registry for halal certificates on blockchain
 */
contract CertificateRegistry {
    // ============================================================
    // Types
    // ============================================================

    struct Certificate {
        bytes32 id;
        bytes32 productId;
        string issuingBody;
        string certificateNumber;
        uint256 issueDate;
        uint256 expiryDate;
        bool isValid;
        bool revoked;
        address issuer;
        bytes32 verificationHash;
        string jurisdiction;
    }

    struct CertificateRequest {
        bytes32 requestId;
        bytes32 productId;
        string issuingBody;
        uint256 requestedAt;
        address requester;
        bool processed;
    }

    // ============================================================
    // State Variables
    // ============================================================

    mapping(bytes32 => Certificate) public certificates;
    mapping(bytes32 => CertificateRequest) public certificateRequests;
    mapping(bytes32 => bool) public verifiedCertificates;
    mapping(address => bool) public authorizedIssuers;

    bytes32[] public certificateIds;

    address public owner;
    address public verifierContract;

    // ============================================================
    // Events
    // ============================================================

    event CertificateIssued(
        bytes32 indexed certificateId,
        bytes32 indexed productId,
        string issuingBody,
        uint256 expiryDate
    );

    event CertificateVerified(
        bytes32 indexed certificateId,
        bytes32 indexed productId,
        bool isValid
    );

    event CertificateRevoked(
        bytes32 indexed certificateId,
        bytes32 indexed productId,
        address revokedBy
    );

    event CertificateExpired(
        bytes32 indexed certificateId,
        bytes32 indexed productId
    );

    // ============================================================
    // Modifiers
    // ============================================================

    modifier onlyOwner() {
        require(msg.sender == owner, "Only owner");
        _;
    }

    modifier onlyAuthorizedIssuer() {
        require(authorizedIssuers[msg.sender], "Not authorized");
        _;
    }

    // ============================================================
    // Constructor
    // ============================================================

    constructor(address _verifierContract) {
        owner = msg.sender;
        verifierContract = _verifierContract;
    }

    // ============================================================
    // Core Functions
    // ============================================================

    /**
     * @dev Issue a certificate
     */
    function issueCertificate(
        bytes32 productId,
        string memory issuingBody,
        string memory certificateNumber,
        uint256 expiryDate,
        string memory jurisdiction
    ) external onlyAuthorizedIssuer returns (bytes32 certificateId) {
        certificateId = keccak256(
            abi.encodePacked(productId, certificateNumber, block.timestamp)
        );

        Certificate storage cert = certificates[certificateId];
        cert.id = certificateId;
        cert.productId = productId;
        cert.issuingBody = issuingBody;
        cert.certificateNumber = certificateNumber;
        cert.issueDate = block.timestamp;
        cert.expiryDate = expiryDate;
        cert.isValid = true;
        cert.revoked = false;
        cert.issuer = msg.sender;
        cert.jurisdiction = jurisdiction;
        cert.verificationHash = keccak256(
            abi.encodePacked(productId, issuingBody, certificateNumber, expiryDate)
        );

        certificateIds.push(certificateId);

        emit CertificateIssued(
            certificateId,
            productId,
            issuingBody,
            expiryDate
        );

        return certificateId;
    }

    /**
     * @dev Verify a certificate
     */
    function verifyCertificate(bytes32 certificateId) external view returns (bool) {
        Certificate storage cert = certificates[certificateId];
        require(cert.id != bytes32(0), "Certificate not found");

        bool isValid = cert.isValid &&
                       !cert.revoked &&
                       block.timestamp <= cert.expiryDate;

        return isValid;
    }

    /**
     * @dev Revoke a certificate
     */
    function revokeCertificate(bytes32 certificateId) external onlyAuthorizedIssuer {
        Certificate storage cert = certificates[certificateId];
        require(cert.id != bytes32(0), "Certificate not found");
        require(!cert.revoked, "Already revoked");

        cert.revoked = true;
        cert.isValid = false;

        emit CertificateRevoked(
            certificateId,
            cert.productId,
            msg.sender
        );
    }

    /**
     * @dev Request certificate verification
     */
    function requestVerification(
        bytes32 productId,
        string memory issuingBody
    ) external returns (bytes32 requestId) {
        requestId = keccak256(
            abi.encodePacked(productId, msg.sender, block.timestamp)
        );

        CertificateRequest storage request = certificateRequests[requestId];
        request.requestId = requestId;
        request.productId = productId;
        request.issuingBody = issuingBody;
        request.requestedAt = block.timestamp;
        request.requester = msg.sender;
        request.processed = false;

        return requestId;
    }

    /**
     * @dev Check certificate expiry
     */
    function checkExpiry(bytes32 certificateId) external view returns (bool) {
        Certificate storage cert = certificates[certificateId];
        require(cert.id != bytes32(0), "Certificate not found");

        return block.timestamp > cert.expiryDate;
    }

    // ============================================================
    // Admin Functions
    // ============================================================

    function addAuthorizedIssuer(address issuer) external onlyOwner {
        authorizedIssuers[issuer] = true;
    }

    function removeAuthorizedIssuer(address issuer) external onlyOwner {
        authorizedIssuers[issuer] = false;
    }

    function updateVerifierContract(address newVerifier) external onlyOwner {
        verifierContract = newVerifier;
    }

    // ============================================================
    // Utility Functions
    // ============================================================

    function getCertificateCount() external view returns (uint256) {
        return certificateIds.length;
    }

    function getCertificate(bytes32 certificateId) external view returns (Certificate memory) {
        return certificates[certificateId];
    }
}
