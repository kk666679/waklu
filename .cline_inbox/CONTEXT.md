# Platform Context — read before acting

## What HalalChain is

AI-native halal commerce and compliance platform. Multivendor marketplace
gated on deterministic halal verdicts.

## The one rule

> **AI gathers evidence. Deterministic systems decide business and
> compliance outcomes.**

If a task would let an LLM, an agent, or a chain assign a halal verdict, the
task is wrong. Stop and escalate.

## Stack (locked)

- .NET 10 LTS, C# 14 — SDK pinned in `global.json`
- Python 3.11+ (FastAPI services under `.halalchain/`)
- Node 22+ (CLI, tooling)
- Foundry stable, Solidity `^0.8.24`
- MCP C# SDK v2, MAF 1.0-rc1, AG-UI .NET SDK

## Solution reality (verified)

`dotnet sln HalalChain.Platform.sln list` returns **18** projects, matching the
table in `AGENTS.md`. If these ever diverge, the `.sln` is authoritative and
`AGENTS.md` is what needs fixing.

## Seven enforcement layers

1. **Prompt** — `Modelfile` states the rule to the local assistant
2. **API** — `Modules/Halal/` forwards to `tawheed`, never for verdicts
3. **Storage** — read-only creds for reasoning services; no `Delete` on
   `IBlobStore`
4. **Agent runtime** — `EvidenceProposal` has no verdict field; CI enforces
5. **Marketplace** — `VerdictBinding`, not boolean; only `Compliance` mutates
   `ProductStatus`
6. **Blockchain** — contracts record existence/revocation/expiry, never
   verdicts
7. **Contract tests** — invariants assert no verdict type exists on-chain

Verified 2026-09-26: layer 6 is clean (no `verdict`/`halalStatus`/`isHalal`/
`approved` in `contracts/src/**/*.sol`, 5 files). Layer 5's guard rule exists
in `HalalChain.Architecture.Tests/`.

## Two hashers, two concerns

- **SHA-256** — `BlobRef` content addressing in storage
- **Keccak256** — Merkle tree internal nodes, on-chain

Conflating them breaks every inclusion proof. `Sha256MerkleTree` must not
exist.

## Where things live

| Concern | Path | State |
|---|---|---|
| API modules | `HalalChain.Platform.Api/Modules/<Name>/` | exists |
| Storage ports | `HalalChain.Application/Storage/` | `IBlobStore` present |
| Storage adapters | `HalalChain.Storage/Adapters/` | `FileSystem` only — S3 pending |
| Agent proposal type | `HalalChain.Application/Agentic/Models/EvidenceProposal.cs` | present |
| Agent runtime (Python) | `.halalchain/agents/app/halalchain_agents/` | present |
| Agent eval DAG (Python) | `.halalchain/agents/app/eval/` | present |
| ADRs | `docs/adr/001..009-*.md` | 9 present |
| Architecture doc | `docs/ARCHITECTURE.md` | present |
| Contracts | `HalalChain.Platform.Contracts/contracts/src/` | 5 contracts |
| Contract tests | `HalalChain.Platform.Contracts/contracts/test/` | present |
| Architecture guards | `HalalChain.Architecture.Tests/` | present |

**Not yet built** (do not reference as if they exist): `KeccakMerkleTree`,
`HalalChain.Web/Components/Workflow/`, blockchain Merkle namespace, the
Payments module, and the S3/MinIO adapter.

## Read next

- `PRINCIPLES.md` — non-negotiable rules
- `phases/ENT-001-specify/CHARTER.md` — the current phase
- `../docs/ARCHITECTURE.md` — full architecture
- `../AGENTS.md` — build/test conventions
