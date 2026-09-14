# HalalChain Platform

> AI-native halal commerce platform — modular monolith evolving toward a full marketplace with on-chain attestation.

## Architectural principle

> **AI discovers and interprets evidence. Deterministic systems decide business and compliance outcomes.**

AI agents collect evidence. The Policy Engine decides compliance status. An LLM never assigns or overrides a halal verdict.

This rule is enforced in two places:

- the `Modelfile` system prompt for the local halal assistant, and
- the API design in `HalalChain.Platform.Api/Modules/Halal/` — it forwards to
  `tawheed` for **evidence**, not verdicts.

## Repository layout

```
.
├── HalalChain.Platform.sln        # .NET 10 solution
├── Directory.Build.props          # Shared MSBuild defaults (net10.0, nullable, implicit usings)
├── global.json                    # Pins the .NET SDK to 10.0.200 (rollForward: latestFeature)
├── docker-compose.yml             # Local full-stack orchestration
├── service-manifest.yaml          # Authoritative inventory of services and ports
├── Modelfile                      # Ollama system prompt for the halal assistant
├── package.json                   # Root Node workspace manifest
│
├── HalalChain.Platform.Contracts/ # Shared DTOs, enums, and Solidity contracts
│   ├── Halal/ Catalog/ Commerce/ Vendors/ Auth/ Api/ AI/
│   └── contracts/                 # Foundry workspace (Solidity)
│
├── HalalChain.Platform.Api/       # Core REST API (modular monolith, ASP.NET Core)
│   └── Modules/ Halal/ Vendors/ Catalog/ Commerce/ AI/ Blockchain/ Events/ Indexer/ Ipfs/ Verification/
│
├── HalalChain.Platform.Http/      # Typed HTTP client library used by the frontends
│
├── HalalChain.Marketplace/        # ASP.NET Core MVC vendor marketplace UI
│
├── HalalChain.Web/                # Blazor Server customer-facing UI (Radzen)
│
├── HalalChain.Mcp/                # Model-Context-Protocol server (console host)
├── HalalChain.Mcp.Tests/          # xUnit tests for the MCP server
│
├── HalalChain.Platform.Tests/     # xUnit tests for the API + persistence + vendor isolation
│
├── HalalChain-Cli/                # Node 22 operator CLI (halalchain)
│
├── .halalchain/
│   ├── ai-inference/              # Python FastAPI AI gateway (embeddings, classify, rerank, LLM)
│   ├── tawheed/                   # Python FastAPI multi-agent evidence + Policy Engine
│   └── config.json                # Local shared config (gitignored in real use)
│
├── docs/                          # Architecture, runbooks, MVP notes
├── infrastructure/                # Dev docker-compose, helper scripts
└── .github/                       # CI workflows
```

## Services and ports

| Service        | Runtime                | Port | Public | Notes                                   |
|----------------|------------------------|------|--------|-----------------------------------------|
| `platform-api` | .NET 10 (ASP.NET Core) | 5001 | yes    | Core REST API, modular monolith         |
| `halalchain`   | .NET 10 (Blazor)       | 5200 | yes    | Customer-facing UI                      |
| `marketplace`  | .NET 10 (ASP.NET MVC)  | 5201 | yes    | Vendor marketplace UI                   |
| `ai-inference` | Python 3.11 / Node 20  | 7071 | no     | Embeddings, classify, rerank, LLM       |
| `tawheed`      | Python 3.11 (FastAPI)  | 8000 | no     | Evidence collection + deterministic Policy Engine |
| `postgres`     | postgres:17-alpine     | 5432 | no     | Primary relational store                |
| `redis`        | redis:7-alpine         | 6379 | no     | Cache, sessions, distributed locks      |
| `neo4j`        | neo4j:5-community      | 7687 | no     | Reserved for supply-chain graph         |
| `qdrant`       | qdrant/qdrant:v1.19.0  | 6333 | no     | Vector store for embeddings             |

See `service-manifest.yaml` for the authoritative inventory.

## Build, test, run

```bash
# .NET
dotnet restore HalalChain.Platform.sln
dotnet build   HalalChain.Platform.sln -c Release
dotnet test    HalalChain.Platform.sln -c Release --no-build

# Full local stack
docker compose up --build
```

The first build after a rename may need a clean: `dotnet clean HalalChain.Platform.sln` then rebuild.

## CLI

The operator CLI lives in `HalalChain-Cli/`:

```bash
npm run halalchain:cli          # or: node HalalChain-Cli/bin/halalchain.js
```

## Configuration

- Real secrets must come from environment variables, not from `appsettings.json`.
  The non-development `Jwt:Key` value is rejected at startup if it matches the
  example value.
- `.env` files must not be committed. `.env.example` documents the MCP env vars;
  `docker-compose.yml` documents the platform services' env vars.

## Documentation

- `AGENTS.md` — contributor and build instructions.
- `docs/ARCHITECTURE.md` — overall system architecture.
- `docs/mvp-architecture.md` — MVP-specific architecture notes.
- `docs/local-development.md` — local dev workflow.
- `docs/runbooks/` — operational runbooks (chain reorg, compromised keys).
- `docs/FOUNDRY_LEVERAGE.md` — how we use Microsoft Foundry.
- `service-manifest.yaml` — declarative service inventory.
