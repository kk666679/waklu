// SPDX-License-Identifier: MIT
pragma solidity 0.8.24;

import {HalalAccessControl} from "./HalalAccessControl.sol";
import {HalalProductRegistry} from "./HalalProductRegistry.sol";
import {ReentrancyGuard} from "@openzeppelin/contracts/utils/ReentrancyGuard.sol";

/**
 * @title ITraceabilityEventLog
 * @notice EIP-165 marker for the traceability event log.
 */
interface ITraceabilityEventLog {
    enum EventType { Manufactured, Processed, Packaged, Shipped, Received, Inspected, Recalled }

    struct TraceabilityEvent {
        bytes32 eventId;          // unique event ID
        bytes32 productId;        // reference to HalalProductRegistry
        bytes32 batchId;          // batch/lot identifier
        address actor;             // wallet of the entity recording the event
        EventType eventType;
        string  locationCid;       // IPFS CID of the location JSON
        string  evidenceCid;      // IPFS CID of the supporting evidence
        string  notesCid;         // IPFS CID of free-form notes
        uint64  timestamp;
        bool    exists;
    }

    event TraceabilityEventRecorded(
        bytes32 indexed eventId, bytes32 indexed productId, bytes32 indexed batchId,
        uint8 eventType, address actor, uint64 timestamp
    );
}

/**
 * @title TraceabilityEventLog
 * @notice Append-only log of supply-chain events. Events are
 *         attributable on-chain (actor wallet) and reference IPFS CIDs
 *         for the off-chain details. Only PLATFORM_OPERATOR_ROLE or
 *         INSPECTOR_ROLE may record events.
 *
 *         This contract deliberately does not store the full event
 *         payload on-chain — only the hash-pointer + the on-chain
 *         attribution. The full JSON is on IPFS.
 */
contract TraceabilityEventLog is ITraceabilityEventLog, ReentrancyGuard {
    HalalAccessControl public immutable access;
    HalalProductRegistry public immutable products;

    mapping(bytes32 => TraceabilityEvent) private _events;
    mapping(bytes32 => bytes32[]) private _productEvents; // productId => list of eventIds (chronological)
    uint256 public totalEvents;

    error EventNotFound(bytes32 eventId);
    error EventAlreadyExists(bytes32 eventId);
    error ProductNotFound(bytes32 productId);
    error ZeroBytes32();
    error ZeroAddress();
    error NotAuthorised(address caller);

    modifier onlyOperatorOrInspector() {
        if (!access.hasRole(access.PLATFORM_OPERATOR_ROLE(), msg.sender) &&
            !access.hasRole(access.INSPECTOR_ROLE(), msg.sender)) {
            revert NotAuthorised(msg.sender);
        }
        _;
    }

    constructor(HalalAccessControl access_, HalalProductRegistry products_) {
        if (address(access_) == address(0) || address(products_) == address(0)) revert ZeroAddress();
        access = access_;
        products = products_;
    }

    function recordEvent(
        bytes32 eventId,
        bytes32 productId,
        bytes32 batchId,
        EventType eventType,
        string calldata locationCid,
        string calldata evidenceCid,
        string calldata notesCid
    ) external onlyOperatorOrInspector nonReentrant returns (uint64 timestamp) {
        if (eventId == bytes32(0) || productId == bytes32(0) || batchId == bytes32(0)) {
            revert ZeroBytes32();
        }
        if (_events[eventId].exists) revert EventAlreadyExists(eventId);
        if (!products.productExists(productId)) revert ProductNotFound(productId);

        timestamp = uint64(block.timestamp);
        _events[eventId] = TraceabilityEvent({
            eventId: eventId,
            productId: productId,
            batchId: batchId,
            actor: msg.sender,
            eventType: eventType,
            locationCid: locationCid,
            evidenceCid: evidenceCid,
            notesCid: notesCid,
            timestamp: timestamp,
            exists: true
        });
        _productEvents[productId].push(eventId);
        unchecked { totalEvents += 1; }
        emit TraceabilityEventRecorded(eventId, productId, batchId, uint8(eventType), msg.sender, timestamp);
    }

    function getEvent(bytes32 eventId) external view returns (TraceabilityEvent memory) {
        TraceabilityEvent memory e = _events[eventId];
        if (!e.exists) revert EventNotFound(eventId);
        return e;
    }

    function getEventsForProduct(bytes32 productId) external view returns (bytes32[] memory) {
        return _productEvents[productId];
    }

    function supportsInterface(bytes4 iid) external pure returns (bool) {
        return iid == type(ITraceabilityEventLog).interfaceId || iid == 0x01ffc9a7;
    }
}
