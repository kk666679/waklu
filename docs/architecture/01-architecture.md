# HalalChain — Architecture Overview

> Companion to `docs/ARCHITECTURE.md` (the original marketplace design). This document covers the **blockchain, smart-contract, and IPFS trust layer** added in 2026.

## 1. Goals

- **Provenance** — a tamper-evident, append-only record of supply-chain events per product.
- **Certification** — third-party halal certifiers (JAKIM, MUI, ESMA, …) issue certificates whose authenticity is verifiable by anyone with a QR code.
- **Trust** — the trust signal (certificate status, revocation) is on a public chain; our backend is an indexer/cache, not the source of truth.
- **Privacy** — only pseudonymous identifiers, CIDs, and hashes go on-chain or to public IPFS. PII stays in our private DB.
- **Composability** — the trust layer is provider-pluggable (chain, IPFS, KMS, secrets) and can be swapped without touching the marketplace app.

## 2. Three architectures considered

| | A — Backend-mediated | B — Direct Web3 | C — Hybrid A + read-only B (chosen) |
|---|---|---|---|
| Writes | Backend holds hot wallet, signs, dispatches | Browser holds wallet, user signs | **Same as A** |
| Reads | Backend's indexer cache | Browser queries chain directly via public RPC | **Backend cache + a public `/verify/{id}` page that ALSO calls the chain for the trust signal** |
| Pros | Best DevEx (C#), lowest gas (batched), key custody in HSM, rate-limiting | "Pure" Web3 narrative | **Best of both: trust for consumers, ops for vendors** |
| Cons | Backend is a write SPOF (mitigated by chain being the trust anchor) | Terrible UX for B2B certifiers, gas paid by user, RPC outage = full outage | Slightly more code |
| Security | ★★★★★ | ★★ (phishing) | ★★★★★ |
| Cost | $ | $$$ (users pay gas) | $ (writes) + $0 (reads on our cache) |
| Fit | ✅ for B2B/B2C regulated platform | ❌ for halal domain | ✅ **chosen** |

## 3. Component diagram

```
                 ┌──────────────────────────────────────────────────────────┐
                 │                    PUBLIC (consumers)                   │
                 │  ┌─────────────────┐   ┌──────────────────────────┐    │
                 │  │ Blazor /verify/  │←──│ Public RPC (Polygon)     │    │
                 │  │ {id} page         │   │ for trust-signal read     │    │
                 │  └────────┬─────────┘   └──────────────────────────┘    │
                 └───────────┼─────────────────────────────────────────────┘
                             │ also calls our /api/v1/verify/{id} (cache + indexer)
                             ▼
        ┌──────────────────────────────────────────────────────────┐
        │                  HALALCHAIN BACKEND (C# / .NET 10)          │
        │  ┌──────────────┐   ┌──────────────┐   ┌──────────────┐     │
        │  │ Verification │   │ Certification│   │  IPFS        │     │
        │  │  /api/v1/verify│   │  (vendor     │   │  Pinata /     │     │
        │  └──────────────┘   │   portal)    │   │  Kubo         │     │
        │  ┌──────────────┐   └──────────────┘   └──────────────┘     │
        │  │   Indexer     │                       ▲                  │
        │  │  (event log → │───────────────────────┘ (off-chain docs) │
        │  │   mirror DB)  │                                              │
        │  └──────────────┘                                               │
        │  ┌──────────────────────────────────────────────────────┐    │
        │  │              Blockchain module                         │    │
        │  │  Nethereum → Polygon (Amoy testnet / mainnet)         │    │
        │  │  TransactionQueue (durable outbox) + OutboxDispatcher  │    │
        │  └──────────────────────────────────────────────────────┘    │
        │  ┌──────────────────────────────────────────────────────┐    │
        │  │  Existing: Catalog, Commerce, Halal, Vendors, Events │    │
        │  │  (catalog, vendors, orders, etc. — unchanged)          │    │
        │  └──────────────────────────────────────────────────────┘    │
        └──────────────────────────────────────────────────────────┘
                             │                            │
                             ▼                            ▼
        ┌──────────────────────────┐    ┌──────────────────────────┐
        │   Polygon Amoy → Mainnet │    │  IPFS (Pinata / kubo)    │
        │   (5 contracts)            │    │  (provider-pluggable)   │
        └──────────────────────────┘    └──────────────────────────┘
```

## 4. Smart-contract layout (5 contracts + 1 factory + 1 proxy)

```
IHalalPlatform (interface, EIP-165)
 ├── HalalAccessControl           (roles, 2-step grant, global pause)
 ├── SupplierRegistry             (per-supplier identity, wallet, status)
 ├── HalalProductRegistry         (per-product provenance, current cert pointer)
 ├── HalalCertificationRegistry   (per-certificate, cross-calls product on issue/revoke)
 └── TraceabilityEventLog         (append-only supply-chain events)
```

See `contracts/src/` and `contracts/README.md` for the deployed interfaces.

## 5. Data placement (the most important decision)

| Data | Where | Why |
|---|---|---|
| Supplier ID, wallet, status | on-chain | Publicly verifiable identity |
| Product ID, current cert pointer, status, supplier | on-chain | Trust anchor for the consumer QR page |
| Certificate ID, issuer, expiry, document CID, status | on-chain | The trust signal |
| Traceability event hash | on-chain | Tamper-evident audit log |
| `metadataHash = SHA-256(cid ‖ fields)` | on-chain | Detects IPFS CID swap |
| Product name, description, search fields, prices, orders, analytics | off-chain DB | Operational, no trust value |
| Certificate body (PDF/image) | IPFS (encrypted for sensitive) | Large, immutable, public |
| Certifier audit reports, lab results | IPFS | Large, append-only |
| **PII**: real names, passport numbers, phone, email | **NEVER on-chain, NEVER on public IPFS** | GDPR, privacy |

## 6. Tech stack (matches the existing .NET project)

| Layer | Choice | Why |
|---|---|---|
| Contracts | Solidity 0.8.24 + OpenZeppelin v5 | Industry standard, audited |
| Tooling | Foundry (forge/cast/anvil) | Fast, Solidity-native, no Node |
| C# ↔ chain | Nethereum 4.21 | Mature .NET Web3, no Node interop |
| IPFS | Provider-pluggable (`IStorageService`), default Pinata prod / kubo dev | Best SLA, swappable |
| Secrets | Azure Key Vault (or AWS SM / Vault) in production; .env in dev | Required for hot-wallet key, IPFS JWT |
| Off-chain DB | PostgreSQL (existing) — `*_chain_*` tables added for the indexer mirror | Reuse existing |

## 7. Transaction lifecycle

```
API request
  ↓ validate
SmartContractService.SubmitAsync
  ↓ enqueue (durable, outbox)
ChainTxOutbox row (Status=Requested)
  ↓ background dispatcher tick (every 5s)
OutboxDispatcherService → Nethereum signs + sends
  ↓ Status=Pending
Nethereum polls receipt (until N confirmations on Polygon)
  ↓ Status=Confirmed | Failed | Reverted
EventIndexerHostedService observes the log
  ↓ mirror
ProductsOnChain / CertificatesOnChain / etc. rows
  ↓ consumer reads
GET /api/v1/verify/{productId}
```

## 8. Where to look

| Concern | File |
|---|---|
| Smart contracts | `contracts/src/*.sol`, `contracts/test/*.t.sol` |
| Deploy | `contracts/script/Deploy.s.sol` |
| C# blockchain service | `HalalChain.Platform.Api/Modules/Blockchain/` |
| C# IPFS service | `HalalChain.Platform.Api/Modules/Ipfs/` |
| C# event indexer | `HalalChain.Platform.Api/Modules/Indexer/` |
| C# public verification | `HalalChain.Platform.Api/Modules/Verification/VerificationController.cs` |
| Off-chain mirror tables | `HalalChain.Platform.Api/Persistence/Entities/ChainMirror.cs`, `ChainTxOutbox.cs` |
| Dev infra (Anvil fork, kubo IPFS, postgres) | `infrastructure/docker-compose.dev.yml` |
| Data-placement rule | `docs/architecture/02-data-placement.md` |
| Threat model | `docs/architecture/03-threat-model.md` |
| API reference | `docs/architecture/04-api.md` |

## 9. Next steps (post-MVP)

- Phase 3: KMS-backed wallet, multi-RPC failover, security monitoring.
- Phase 4: `HalalEscrow` contract + on-chain order lifecycle.
- Phase 5: Public verification widget (browser extension).
- Phase 6: Third-party audit + bug bounty.
- Phase 7: Polygon mainnet + Base mirror.
