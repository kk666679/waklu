// SPDX-License-Identifier: MIT
pragma solidity 0.8.24;

import {HalalAccessControl} from "./HalalAccessControl.sol";
import {ReentrancyGuard} from "@openzeppelin/contracts/utils/ReentrancyGuard.sol";

/**
 * @title ISupplierRegistry
 * @notice EIP-165 marker for the supplier registry.
 */
interface ISupplierRegistry {
    enum SupplierStatus { Active, Suspended, Revoked }

    struct Supplier {
        bytes32 supplierId;     // stable off-chain-issued ID, e.g. SUP-MY-000123
        address wallet;          // primary operational wallet
        address proposedWallet;   // two-step rotation: pending new wallet
        string  ipfsMetadataCid;  // IPFS CID of the supplier profile JSON
        string  jurisdiction;     // ISO-3166-1 alpha-2, e.g. "MY"
        uint64  registeredAt;
        uint64  updatedAt;
        SupplierStatus status;
        bool    exists;
    }

    event SupplierRegistered(
        bytes32 indexed supplierId, address indexed wallet, string ipfsMetadataCid, string jurisdiction, uint64 registeredAt
    );
    event SupplierStatusChanged(bytes32 indexed supplierId, SupplierStatus status, uint64 updatedAt);
    event SupplierWalletRotationProposed(bytes32 indexed supplierId, address indexed currentWallet, address indexed proposedWallet);
    event SupplierWalletRotated(bytes32 indexed supplierId, address indexed newWallet);
    event SupplierMetadataUpdated(bytes32 indexed supplierId, string ipfsMetadataCid, uint64 updatedAt);
}

/**
 * @title SupplierRegistry
 * @notice Per-supplier identity, wallet, certification references, and
 *         status. A supplier's stable ID (e.g., SUP-MY-000123) is
 *         decoupled from their wallet so wallet rotations don't lose
 *         the supplier's history. Only PLATFORM_OPERATOR_ROLE may
 *         register, suspend, or rotate wallets.
 */
contract SupplierRegistry is ISupplierRegistry, ReentrancyGuard {
    HalalAccessControl public immutable access;

    mapping(bytes32 => Supplier) private _suppliers;
    mapping(address => bytes32) private _walletToSupplier;

    error SupplierNotFound(bytes32 supplierId);
    error SupplierAlreadyRegistered(bytes32 supplierId);
    error WalletAlreadyLinkedToSupplier(address wallet, bytes32 existingSupplierId);
    error InvalidStatusTransition(SupplierStatus from, SupplierStatus to);
    error NotWalletOwner(address caller, address expected);
    error ZeroAddress();
    error ZeroBytes32();

    modifier onlyOperator() {
        access.requireRole(access.PLATFORM_OPERATOR_ROLE(), msg.sender);
        _;
    }

    constructor(HalalAccessControl access_) {
        if (address(access_) == address(0)) revert ZeroAddress();
        access = access_;
    }

    /// @notice Register a new supplier. The wallet must not already be linked.
    function registerSupplier(
        bytes32 supplierId,
        address wallet,
        string calldata ipfsMetadataCid,
        string calldata jurisdiction
    ) external onlyOperator returns (uint64 registeredAt) {
        if (_suppliers[supplierId].exists) revert SupplierAlreadyRegistered(supplierId);
        if (_walletToSupplier[wallet] != bytes32(0)) {
            revert WalletAlreadyLinkedToSupplier(wallet, _walletToSupplier[wallet]);
        }
        if (supplierId == bytes32(0) || wallet == address(0)) {
            revert ZeroAddress();
        }
        registeredAt = uint64(block.timestamp);
        _suppliers[supplierId] = Supplier({
            supplierId: supplierId,
            wallet: wallet,
            proposedWallet: address(0),
            ipfsMetadataCid: ipfsMetadataCid,
            jurisdiction: jurisdiction,
            registeredAt: registeredAt,
            updatedAt: registeredAt,
            status: SupplierStatus.Active,
            exists: true
        });
        _walletToSupplier[wallet] = supplierId;
        emit SupplierRegistered(supplierId, wallet, ipfsMetadataCid, jurisdiction, registeredAt);
    }

    function setStatus(bytes32 supplierId, SupplierStatus newStatus) external onlyOperator {
        Supplier storage s = _suppliers[supplierId];
        if (!s.exists) revert SupplierNotFound(supplierId);
        SupplierStatus old = s.status;
        if (old == newStatus) return;
        // Allowed transitions:
        //   Active -> Suspended | Revoked
        //   Suspended -> Active | Revoked
        //   Revoked   -> (terminal)
        if (old == SupplierStatus.Revoked) {
            revert InvalidStatusTransition(old, newStatus);
        }
        s.status = newStatus;
        s.updatedAt = uint64(block.timestamp);
        if (newStatus == SupplierStatus.Revoked) {
            // Free the wallet so the address can be re-linked (e.g. to a new supplier entity)
            delete _walletToSupplier[s.wallet];
        }
        emit SupplierStatusChanged(supplierId, newStatus, s.updatedAt);
    }

    function updateMetadata(bytes32 supplierId, string calldata ipfsMetadataCid) external onlyOperator {
        Supplier storage s = _suppliers[supplierId];
        if (!s.exists) revert SupplierNotFound(supplierId);
        s.ipfsMetadataCid = ipfsMetadataCid;
        s.updatedAt = uint64(block.timestamp);
        emit SupplierMetadataUpdated(supplierId, ipfsMetadataCid, s.updatedAt);
    }

    /// @notice Two-step wallet rotation: the current wallet proposes a new one.
    function proposeWalletRotation(bytes32 supplierId, address newWallet) external {
        Supplier storage s = _suppliers[supplierId];
        if (!s.exists) revert SupplierNotFound(supplierId);
        if (msg.sender != s.wallet) revert NotWalletOwner(msg.sender, s.wallet);
        if (newWallet == address(0) || _walletToSupplier[newWallet] != bytes32(0)) {
            revert WalletAlreadyLinkedToSupplier(newWallet, _walletToSupplier[newWallet]);
        }
        s.proposedWallet = newWallet;
        emit SupplierWalletRotationProposed(supplierId, s.wallet, newWallet);
    }

    /// @notice The proposed wallet accepts the rotation. Anyone can call.
    function acceptWalletRotation(bytes32 supplierId) external {
        Supplier storage s = _suppliers[supplierId];
        if (!s.exists) revert SupplierNotFound(supplierId);
        if (s.proposedWallet != msg.sender) revert NotWalletOwner(msg.sender, s.proposedWallet);
        delete _walletToSupplier[s.wallet];
        s.wallet = msg.sender;
        s.proposedWallet = address(0);
        s.updatedAt = uint64(block.timestamp);
        _walletToSupplier[msg.sender] = supplierId;
        emit SupplierWalletRotated(supplierId, msg.sender);
    }

    function getSupplier(bytes32 supplierId) external view returns (Supplier memory) {
        Supplier memory s = _suppliers[supplierId];
        if (!s.exists) revert SupplierNotFound(supplierId);
        return s;
    }

    function getSupplierIdByWallet(address wallet) external view returns (bytes32) {
        return _walletToSupplier[wallet];
    }

    function supplierExists(bytes32 supplierId) external view returns (bool) {
        return _suppliers[supplierId].exists;
    }

    // Required by IHalalPlatform interface (EIP-165)
    function supportsInterface(bytes4 iid) external pure returns (bool) {
        return iid == type(ISupplierRegistry).interfaceId || iid == 0x01ffc9a7; // ERC165
    }
}
