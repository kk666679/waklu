# HalalChain Smart Contracts

Production-ready Solidity contracts for the HalalChain trust layer. Implements the redesigned architecture: 4 registry contracts + 1 access-control contract, deployed to Polygon (Amoy testnet → Mainnet) with Base as the L2 mirror.

## Contracts

| Contract | Purpose | Key functions |
|---|---|---|
| `HalalAccessControl` | Roles + 2-step grant + global pause | `proposeRoleGrant` / `executeRoleGrant` / `pause` / `unpause` |
| `SupplierRegistry` | Supplier identity, wallet, status | `registerSupplier` / `setStatus` / `proposeWalletRotation` / `acceptWalletRotation` |
| `HalalProductRegistry` | Product provenance + current-cert pointer | `registerProduct` / `recall` / `setCurrentCertificate` (called by cert registry) |
| `HalalCertificationRegistry` | Certificate issuance/revocation; cross-calls Product | `issueCertificate` / `revokeCertificate` / `expireCertificate` |
| `TraceabilityEventLog` | Append-only supply-chain events | `recordEvent` |

## Roles

| Role | Can | Cannot |
|---|---|---|
| `DEFAULT_ADMIN_ROLE` (timelock multisig) | Grant/revoke roles, upgrade | Issue/revoke certs |
| `PLATFORM_OPERATOR_ROLE` (backend hot wallet) | Register suppliers/products, record traceability | Issue/revoke certs |
| `CERTIFIER_ROLE` (JAKIM, MUI, ESMA, …) | Issue/revoke their own certs | Create suppliers/products |
| `INSPECTOR_ROLE` (field auditors) | Record traceability events | Issue certs |
| `PAUSER_ROLE` (separate emergency multisig) | Pause/unpause the platform | Anything else |

## Build & Test

Requires [Foundry](https://book.getfoundry.sh/getting-started/installation).

```bash
cd contracts
forge install foundry-rs/forge-std --no-commit --no-git
forge install OpenZeppelin/openzeppelin-contracts@v5.0.2 --no-commit --no-git
forge build
forge test -vv
```

> **Tip — full local stack in one command.** Instead of running the steps above manually, use the bootstrap script at the repo root, which installs Foundry, runs the tests, brings up the docker-compose stack (Anvil + kubo + Postgres), deploys the contracts, and writes the addresses into `appsettings.Development.json` for the API:
> ```bash
> /workspaces/octo-engine/infrastructure/scripts/setup-dev.sh
> ```
> Re-running it is idempotent.

## Deploy

> Always `cd` to the repo root first so relative paths resolve correctly. The bootstrap script above handles this automatically.

```bash
# Local (Anvil)
forge script script/Deploy.s.sol:Deploy --rpc-url http://127.0.0.1:8545 --broadcast

# Polygon Amoy testnet
forge script script/Deploy.s.sol:Deploy \
    --rpc-url $AMOY_RPC \
    --broadcast \
    --sig "deployTestnet(uint64)" \
    -- 86400  # 24h timelock for testnet

# Polygon mainnet (minimum 24h timelock enforced)
forge script script/Deploy.s.sol:Deploy \
    --rpc-url $POLYGON_RPC \
    --broadcast \
    --sig "deployMainnet(uint64)" \
    -- 86400

# Verify on Polygonscan
forge verify-contract --chain-id 137 --watch \
    --compiler-version 0.8.24 \
    <CONTRACT_ADDRESS> \
    src/HalalAccessControl.sol:HalalAccessControl
```

## Roles (post-deploy, via the timelock)

```solidity
// Propose (waits timelock):
bytes32 key = halalAccess.proposeRoleGrant(operatorWallet, halalAccess.PLATFORM_OPERATOR_ROLE());
// After timelock, execute:
halalAccess.executeRoleGrant(key);
```

## Test coverage

- 30+ unit tests covering: role access, supplier lifecycle (incl. wallet rotation), product registration (incl. duplicate metadata hash), certificate issuance/revocation/expiry, cross-contract state transitions, traceability event recording, EIP-165.
- 1 fuzz test for metadata hash uniqueness.
- All tests run with `forge test -vv` in <2 s on a modern laptop.
