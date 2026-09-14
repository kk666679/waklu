// SPDX-License-Identifier: MIT
// ═══════════════════════════════════════════════════════════════════
// PROPRIETARY — HalalChain Verification Contract
// Copyright © HalalChain. All rights reserved.
// ═══════════════════════════════════════════════════════════════════

pragma solidity ^0.8.19;

/**
 * @title HalalVerification
 * @dev Contract for halal product verification on blockchain
 */
contract HalalVerification {
    // ============================================================
    // Types
    // ============================================================

    enum Verdict { HALAL, HARAM, MASHBOOH }
    enum VerificationStatus { PENDING, VERIFIED, REJECTED }

    struct Verification {
        bytes32 productId;
        Verdict verdict;
        bytes32 evidenceHash;
        address verifier;
        VerificationStatus status;
        uint256 timestamp;
        string jurisdiction;
        bytes32[] certificateIds;
        bytes32[] supplyChainIds;
    }

    struct Certificate {
        bytes32 id;
        bytes32 productId;
        string issuer;
        string certificateNumber;
        uint256 issueDate;
        uint256 expiryDate;
        bool valid;
    }

    // ============================================================
    // State Variables
    // ============================================================

    mapping(bytes32 => Verification) public verifications;
    mapping(bytes32 => Certificate) public certificates;
    mapping(bytes32 => bool) public verifiedProducts;

    bytes32[] public verificationIds;
    bytes32[] public certificateIds;

    address public owner;
    address public oracleAddress;

    uint256 public constant VERIFICATION_COST = 0.001 ether;

    // ============================================================
    // Events
    // ============================================================

    event VerificationRecorded(
        bytes32 indexed verificationId,
        bytes32 indexed productId,
        Verdict verdict,
        address verifier,
        uint256 timestamp
    );

    event VerificationVerified(
        bytes32 indexed verificationId,
        bytes32 indexed productId,
        address verifier
    );

    event CertificateRegistered(
        bytes32 indexed certificateId,
        bytes32 indexed productId,
        string issuer,
        uint256 expiryDate
    );

    event VerificationStatusChanged(
        bytes32 indexed verificationId,
        VerificationStatus status,
        address updatedBy
    );

    // ============================================================
    // Modifiers
    // ============================================================

    modifier onlyOwner() {
        require(msg.sender == owner, "Only owner can call this");
        _;
    }

    modifier onlyOracle() {
        require(msg.sender == oracleAddress, "Only oracle can call this");
        _;
    }

    modifier validProduct(bytes32 productId) {
        require(productId != bytes32(0), "Invalid product ID");
        _;
    }

    // ============================================================
    // Constructor
    // ============================================================

    constructor(address _oracleAddress) {
        owner = msg.sender;
        oracleAddress = _oracleAddress;
    }

    // ============================================================
    // Core Functions
    // ============================================================

    /**
     * @dev Record a verification result
     */
    function recordVerification(
        bytes32 productId,
        Verdict verdict,
        bytes32 evidenceHash,
        string memory jurisdiction,
        bytes32[] memory certificateIds,
        bytes32[] memory supplyChainIds
    ) external payable returns (bytes32 verificationId) {
        require(msg.value >= VERIFICATION_COST, "Insufficient payment");
        require(productId != bytes32(0), "Invalid product ID");

        verificationId = keccak256(
            abi.encodePacked(productId, block.timestamp, msg.sender)
        );

        Verification storage verification = verifications[verificationId];
        verification.productId = productId;
        verification.verdict = verdict;
        verification.evidenceHash = evidenceHash;
        verification.verifier = msg.sender;
        verification.status = VerificationStatus.PENDING;
        verification.timestamp = block.timestamp;
        verification.jurisdiction = jurisdiction;
        verification.certificateIds = certificateIds;
        verification.supplyChainIds = supplyChainIds;

        verificationIds.push(verificationId);

        emit VerificationRecorded(
            verificationId,
            productId,
            verdict,
            msg.sender,
            block.timestamp
        );

        return verificationId;
    }

    /**
     * @dev Verify a verification record
     */
    function verifyVerification(bytes32 verificationId) external onlyOracle returns (bool) {
        Verification storage verification = verifications[verificationId];
        require(verification.timestamp > 0, "Verification not found");
        require(verification.status == VerificationStatus.PENDING, "Already processed");

        verification.status = VerificationStatus.VERIFIED;
        verifiedProducts[verification.productId] = true;

        emit VerificationVerified(
            verificationId,
            verification.productId,
            msg.sender
        );

        return true;
    }

    /**
     * @dev Register a certificate
     */
    function registerCertificate(
        bytes32 productId,
        string memory issuer,
        string memory certificateNumber,
        uint256 expiryDate
    ) external onlyOracle returns (bytes32 certificateId) {
        certificateId = keccak256(
            abi.encodePacked(productId, certificateNumber, block.timestamp)
        );

        Certificate storage cert = certificates[certificateId];
        cert.id = certificateId;
        cert.productId = productId;
        cert.issuer = issuer;
        cert.certificateNumber = certificateNumber;
        cert.issueDate = block.timestamp;
        cert.expiryDate = expiryDate;
        cert.valid = true;

        certificateIds.push(certificateId);

        emit CertificateRegistered(
            certificateId,
            productId,
            issuer,
            expiryDate
        );

        return certificateId;
    }

    /**
     * @dev Get verification details
     */
    function getVerification(bytes32 verificationId) external view returns (
        bytes32 productId,
        Verdict verdict,
        bytes32 evidenceHash,
        address verifier,
        VerificationStatus status,
        uint256 timestamp
    ) {
        Verification storage verification = verifications[verificationId];
        require(verification.timestamp > 0, "Verification not found");

        return (
            verification.productId,
            verification.verdict,
            verification.evidenceHash,
            verification.verifier,
            verification.status,
            verification.timestamp
        );
    }

    /**
     * @dev Check if product is verified
     */
    function isProductVerified(bytes32 productId) external view returns (bool) {
        return verifiedProducts[productId];
    }

    /**
     * @dev Get verification history for a product
     */
    function getVerificationHistory(bytes32 productId) external view returns (bytes32[] memory) {
        uint256 count = 0;
        for (uint256 i = 0; i < verificationIds.length; i++) {
            if (verifications[verificationIds[i]].productId == productId) {
                count++;
            }
        }

        bytes32[] memory history = new bytes32[](count);
        uint256 index = 0;
        for (uint256 i = 0; i < verificationIds.length; i++) {
            if (verifications[verificationIds[i]].productId == productId) {
                history[index] = verificationIds[i];
                index++;
            }
        }

        return history;
    }

    // ============================================================
    // Admin Functions
    // ============================================================

    function updateOracle(address newOracle) external onlyOwner {
        oracleAddress = newOracle;
    }

    function withdrawFees() external onlyOwner {
        payable(owner).transfer(address(this).balance);
    }

    function setVerificationStatus(bytes32 verificationId, VerificationStatus status) external onlyOwner {
        Verification storage verification = verifications[verificationId];
        require(verification.timestamp > 0, "Verification not found");
        verification.status = status;

        emit VerificationStatusChanged(verificationId, status, msg.sender);
    }

    // ============================================================
    // Fallback
    // ============================================================

    receive() external payable {}
}
