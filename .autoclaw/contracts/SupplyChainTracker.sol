// SPDX-License-Identifier: MIT
// ═══════════════════════════════════════════════════════════════════
// PROPRIETARY — HalalChain Supply Chain Tracker
// Copyright © HalalChain. All rights reserved.
// ═══════════════════════════════════════════════════════════════════

pragma solidity ^0.8.19;

/**
 * @title SupplyChainTracker
 * @dev Tracks product supply chain on blockchain
 */
contract SupplyChainTracker {
    // ============================================================
    // Types
    // ============================================================

    enum EventType {
        RAW_MATERIAL,
        PROCESSING,
        PACKAGING,
        DISTRIBUTION,
        RETAIL,
        CONSUMPTION
    }

    enum HandlingStatus {
        HALAL_COMPLIANT,
        NON_COMPLIANT,
        PENDING_VERIFICATION
    }

    struct SupplyEvent {
        bytes32 eventId;
        bytes32 productId;
        EventType eventType;
        string location;
        string description;
        address recorder;
        uint256 timestamp;
        bytes32 nextEventId;
        HandlingStatus handlingStatus;
        bool verified;
    }

    struct ProductProvenance {
        bytes32 productId;
        bytes32 firstEvent;
        bytes32 lastEvent;
        uint256 eventCount;
        bool complete;
        bytes32 verificationHash;
    }

    // ============================================================
    // State Variables
    // ============================================================

    mapping(bytes32 => SupplyEvent) public events;
    mapping(bytes32 => ProductProvenance) public provenances;
    mapping(bytes32 => bytes32[]) public productEvents;
    mapping(bytes32 => bool) public verifiedEvents;
    mapping(address => bool) public authorizedRecorders;

    bytes32[] public eventIds;

    address public owner;
    address public verifierContract;

    // ============================================================
    // Events
    // ============================================================

    event EventRecorded(
        bytes32 indexed eventId,
        bytes32 indexed productId,
        EventType eventType,
        address recorder,
        uint256 timestamp
    );

    event EventVerified(
        bytes32 indexed eventId,
        bytes32 indexed productId,
        bool verified
    );

    event ProvenanceComplete(
        bytes32 indexed productId,
        bytes32 verificationHash
    );

    // ============================================================
    // Modifiers
    // ============================================================

    modifier onlyOwner() {
        require(msg.sender == owner, "Only owner");
        _;
    }

    modifier onlyAuthorized() {
        require(authorizedRecorders[msg.sender], "Not authorized");
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
     * @dev Record supply chain event
     */
    function recordEvent(
        bytes32 productId,
        EventType eventType,
        string memory location,
        string memory description,
        bytes32 prevEventId
    ) external onlyAuthorized returns (bytes32 eventId) {
        eventId = keccak256(
            abi.encodePacked(productId, eventType, block.timestamp, msg.sender)
        );

        SupplyEvent storage event = events[eventId];
        event.eventId = eventId;
        event.productId = productId;
        event.eventType = eventType;
        event.location = location;
        event.description = description;
        event.recorder = msg.sender;
        event.timestamp = block.timestamp;
        event.nextEventId = bytes32(0);
        event.handlingStatus = HandlingStatus.PENDING_VERIFICATION;
        event.verified = false;

        // Link to previous event
        if (prevEventId != bytes32(0)) {
            events[prevEventId].nextEventId = eventId;
        }

        eventIds.push(eventId);
        productEvents[productId].push(eventId);

        // Update provenance
        if (provenances[productId].productId == bytes32(0)) {
            provenances[productId].productId = productId;
            provenances[productId].firstEvent = eventId;
        }
        provenances[productId].lastEvent = eventId;
        provenances[productId].eventCount++;

        emit EventRecorded(
            eventId,
            productId,
            eventType,
            msg.sender,
            block.timestamp
        );

        return eventId;
    }

    /**
     * @dev Verify event handling
     */
    function verifyHandling(
        bytes32 eventId,
        HandlingStatus status
    ) external onlyOwner {
        SupplyEvent storage event = events[eventId];
        require(event.eventId != bytes32(0), "Event not found");

        event.handlingStatus = status;
        event.verified = true;
        verifiedEvents[eventId] = true;

        emit EventVerified(
            eventId,
            event.productId,
            true
        );
    }

    /**
     * @dev Complete provenance tracking
     */
    function completeProvenance(bytes32 productId) external onlyOwner {
        ProductProvenance storage provenance = provenances[productId];
        require(provenance.productId != bytes32(0), "Product not found");

        provenance.complete = true;
        provenance.verificationHash = keccak256(
            abi.encodePacked(productId, provenance.firstEvent, provenance.lastEvent, provenance.eventCount)
        );

        emit ProvenanceComplete(
            productId,
            provenance.verificationHash
        );
    }

    /**
     * @dev Get product provenance
     */
    function getProvenance(bytes32 productId) external view returns (
        bytes32[] memory eventIds,
        bool complete,
        bytes32 verificationHash
    ) {
        ProductProvenance storage provenance = provenances[productId];
        require(provenance.productId != bytes32(0), "Product not found");

        return (
            productEvents[productId],
            provenance.complete,
            provenance.verificationHash
        );
    }

    /**
     * @dev Get traceability
     */
    function getTraceability(bytes32 productId) external view returns (
        bytes32 firstEvent,
        bytes32 lastEvent,
        uint256 eventCount
    ) {
        ProductProvenance storage provenance = provenances[productId];
        require(provenance.productId != bytes32(0), "Product not found");

        return (
            provenance.firstEvent,
            provenance.lastEvent,
            provenance.eventCount
        );
    }

    // ============================================================
    // Admin Functions
    // ============================================================

    function addAuthorizedRecorder(address recorder) external onlyOwner {
        authorizedRecorders[recorder] = true;
    }

    function removeAuthorizedRecorder(address recorder) external onlyOwner {
        authorizedRecorders[recorder] = false;
    }

    function updateVerifierContract(address newVerifier) external onlyOwner {
        verifierContract = newVerifier;
    }
}
