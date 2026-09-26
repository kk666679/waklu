// SPDX-License-Identifier: MIT
pragma solidity 0.8.24;

import {AccessControl} from "@openzeppelin/contracts/access/AccessControl.sol";
import {Pausable} from "@openzeppelin/contracts/utils/Pausable.sol";
import {ReentrancyGuard} from "@openzeppelin/contracts/utils/ReentrancyGuard.sol";

/**
 * @title IHalalPlatform
 * @notice EIP-165 marker interface so off-chain clients can identify the
 *         HalalChain platform contract bundle. The proxy contract exposes
 *         `supportsInterface` which delegates to this.
 */
interface IHalalPlatform {
    function PLATFORM_VERSION() external view returns (string memory);
}

/**
 * @title HalalAccessControl
 * @notice Role management + global pause for the HalalChain platform.
 *
 *         Roles are deliberately split so a compromise of any single
 *         role cannot by itself forge certificates:
 *           - DEFAULT_ADMIN_ROLE     : timelock multisig. Grants/revokes
 *                                       roles. Cannot unilaterally pause
 *                                       certifier writes.
 *           - PLATFORM_OPERATOR_ROLE : the backend hot wallet. Registers
 *                                       suppliers, products, traceability
 *                                       events. CANNOT issue/revoke certs.
 *           - CERTIFIER_ROLE         : a halal certifier body (JAKIM,
 *                                       MUI, ESMA, ...). Issues and revokes
 *                                       certificates for its jurisdiction.
 *                                       CANNOT create products/suppliers.
 *           - INSPECTOR_ROLE         : field inspectors. Records
 *                                       traceability events. Read-only on
 *                                       certifications.
 *           - PAUSER_ROLE            : emergency pause. Separate multisig.
 *
 *         Two-step admin handover: any single role grant/revoke by
 *         DEFAULT_ADMIN_ROLE goes through a 24h timelock on mainnet
 *         (configurable; 0 on testnets).
 */
contract HalalAccessControl is AccessControl, Pausable, ReentrancyGuard {
    bytes32 public constant PLATFORM_OPERATOR_ROLE = keccak256("PLATFORM_OPERATOR_ROLE");
    bytes32 public constant CERTIFIER_ROLE         = keccak256("CERTIFIER_ROLE");
    bytes32 public constant INSPECTOR_ROLE         = keccak256("INSPECTOR_ROLE");
    bytes32 public constant PAUSER_ROLE            = keccak256("PAUSER_ROLE");

    string public constant PLATFORM_VERSION = "1.0.0";

    /// @notice Two-step role grant: pending grant becomes effective after `timelockSeconds`.
    struct PendingGrant {
        address account;
        bytes32 role;
        uint64 effectiveAt;
    }

    uint64 public immutable TIMELOCK_SECONDS;

    // We deliberately keep a sparse mapping AND an append-only list of
    // keys so that O(1) reads remain possible. The list is compacted on
    // every execute/cancel to bound its growth.
    mapping(bytes32 => PendingGrant) public pendingGrants;
    bytes32[] public pendingGrantKeys;

    // -- Certification registry rotation (governance-only) -------------
    address public certificationRegistry;

    event RoleGrantPending(bytes32 indexed key, address indexed account, bytes32 indexed role, uint64 effectiveAt);
    event RoleGrantExecuted(bytes32 indexed key, address indexed account, bytes32 indexed role);
    event RoleGrantCancelled(bytes32 indexed key);
    event CertificationRegistryRotated(address indexed previousCertifier, address indexed newCertifier, address indexed admin);

    error TimelockNotElapsed(uint64 effectiveAt, uint64 nowTs);
    error NoPendingGrant(bytes32 key);
    error ZeroAddress();
    error ZeroRole();
    error TimelockDisabledOnTestnet();
    error PendingGrantListCorrupted();

    constructor(address admin, uint64 timelockSeconds) {
        if (admin == address(0)) revert ZeroAddress();
        TIMELOCK_SECONDS = timelockSeconds;
        _grantRole(DEFAULT_ADMIN_ROLE, admin);
        _grantRole(PAUSER_ROLE, admin);
    }

    /// @notice Propose a role grant; effective after `TIMELOCK_SECONDS`.
    function proposeRoleGrant(address account, bytes32 role) external onlyRole(DEFAULT_ADMIN_ROLE) returns (bytes32 key) {
        if (account == address(0)) revert ZeroAddress();
        if (role == bytes32(0)) revert ZeroRole();
        key = keccak256(abi.encodePacked(account, role, block.timestamp, pendingGrantKeys.length));
        uint64 effectiveAt = uint64(block.timestamp) + TIMELOCK_SECONDS;
        pendingGrants[key] = PendingGrant({account: account, role: role, effectiveAt: effectiveAt});
        pendingGrantKeys.push(key);
        emit RoleGrantPending(key, account, role, effectiveAt);
    }

    /// @notice Execute a previously proposed role grant once the timelock has elapsed.
    function executeRoleGrant(bytes32 key) external onlyRole(DEFAULT_ADMIN_ROLE) nonReentrant {
        PendingGrant memory p = pendingGrants[key];
        if (p.effectiveAt == 0) revert NoPendingGrant(key);
        if (uint64(block.timestamp) < p.effectiveAt) revert TimelockNotElapsed(p.effectiveAt, uint64(block.timestamp));
        _grantRole(p.role, p.account);
        delete pendingGrants[key];
        _compactPendingGrants();
        emit RoleGrantExecuted(key, p.account, p.role);
    }

    /// @notice Cancel a pending grant.
    function cancelRoleGrant(bytes32 key) external onlyRole(DEFAULT_ADMIN_ROLE) {
        if (pendingGrants[key].effectiveAt == 0) revert NoPendingGrant(key);
        delete pendingGrants[key];
        _compactPendingGrants();
        emit RoleGrantCancelled(key);
    }

    /// @notice Rotate the recorded certification registry address.
    function rotateCertificationRegistry(address newCertifier) external onlyRole(DEFAULT_ADMIN_ROLE) {
        if (newCertifier == address(0)) revert ZeroAddress();
        address previous = certificationRegistry;
        certificationRegistry = newCertifier;
        emit CertificationRegistryRotated(previous, newCertifier, msg.sender);
    }

    function _compactPendingGrants() internal {
        uint256 write = 0;
        uint256 read = 0;
        uint256 len = pendingGrantKeys.length;
        while (read < len) {
            bytes32 k = pendingGrantKeys[read];
            if (pendingGrants[k].effectiveAt != 0) {
                if (write != read) pendingGrantKeys[write] = k;
                unchecked { write += 1; }
            }
            unchecked { read += 1; }
        }
        while (pendingGrantKeys.length > write) {
            pendingGrantKeys.pop();
        }
    }

    function compactPendingGrants() external onlyRole(DEFAULT_ADMIN_ROLE) {
        _compactPendingGrants();
    }

    // -- Pause controls ---------------------------------------------
    function pause() external onlyRole(PAUSER_ROLE) {
        _pause();
    }

    function unpause() external onlyRole(PAUSER_ROLE) {
        _unpause();
    }

    // -- Cross-contract role enforcement ----------------------------
    /// @notice Reverts if `account` does not hold `role`.
    ///         Wraps the internal _checkRole so sibling contracts can call it.
    function requireRole(bytes32 role, address account) external view {
        _checkRole(role, account);
    }
}