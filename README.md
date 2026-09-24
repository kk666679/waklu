# HalalChain Platform

AI-native halal commerce and compliance platform for supplier verification, evidence collection, policy evaluation, and vendor-facing workflows. The repository combines a .NET modular platform, customer + marketplace front ends, Python AI/evidence services, and a local developer stack for running the full system together.

## Architectural principle

> AI gathers evidence. Deterministic systems decide business and compliance outcomes.

This project keeps the LLM and AI services in a supporting role. They observe, classify, summarize, and enrich evidence; the deterministic policy engine is responsible for the halal verdict and operating decisions. That rule is reflected in the local assistant prompt and in the API design where evidence is sent to the `tawheed` service rather than letting an LLM assign the final verdict.

## Repository overview

```text
.
├── AGENTS.md                     # Contributor/build guide for the repository
├── HalalChain.Platform.sln      # Main .NET solution
├── Directory.Build.props        # Shared .NET defaults (net10.0, nullable, implicit usings)
├── global.json                  # Pins the .NET SDK (10.0.200 with latestFeature roll-forward)
├── docker-compose.yml           # Full local stack definition for the platform and infra
├── service-manifest.yaml        # Canonical service inventory and ports
├── Modelfile                    # Local assistant system prompt for Ollama/OpenClaw
├── package.json                 # Root Node workspace scripts and tooling
├── README.md                    # This document
├── docs/                        # Architecture, development, and operations docs
├── infrastructure/              # Compose overlays and helper scripts
├── HalalChain-Cli/              # Node-based operator/CLI toolkit
├── HalalChain.Application/      # Application-layer services and orchestration
├── HalalChain.Domain/           # Domain model for catalog, vendors, halal policy, and commerce
├── HalalChain.Platform.Api/     # ASP.NET Core modular monolith API
├── HalalChain.Platform.Contracts/ # Shared DTOs, API contracts, and Solidity/Foundry artifacts
├── HalalChain.Platform.Http/    # Typed HTTP client used by front ends
├── HalalChain.Platform.Tests/   # API and platform integration tests
├── HalalChain.Web/              # Customer-facing Blazor Server UI
├── HalalChain.Marketplace/      # Vendor marketplace UI (Razor Pages + Blazor Server + SignalR)
├── HalalChain.Mcp/              # MCP server for solution inspection and tooling
├── HalalChain.Mcp.Tests/        # MCP server tests
├── HalalChain.Architecture.Tests/ # Architecture guard tests
├── .halalchain/                 # Python services and shared library
│   ├── _shared/                 # Shared Python package for AI/inference and policy engine
│   ├── ai-inference/            # FastAPI AI gateway for embeddings, classification, rerank, LLM calls
│   ├── tawheed/                 # FastAPI evidence gathering + deterministic policy engine
│   └── config.json              # Local-only shared config placeholder
├── scripts/                     # Supporting scripts
└── .env.example                 # Example env file for local setup (when present in the repo)
```

## Core components

| Component | Purpose |
|---|---|
| `HalalChain.Platform.Api` | Main ASP.NET Core modular monolith exposing the platform API |
| `HalalChain.Platform.Contracts` | Shared DTOs, request/response models, and Solidity contract artifacts |
| `HalalChain.Platform.Http` | Typed client library for interacting with the API |
| `HalalChain.Domain` | Business entities and domain concepts |
| `HalalChain.Application` | Application use cases and orchestration logic |
| `HalalChain.Web` | Blazor Server customer experience |
| `HalalChain.Marketplace` | Vendor marketplace UI using Razor Pages + Blazor Server + SignalR |
| `HalalChain.Mcp` | MCP server exposing read-only tooling and solution introspection |
| `HalalChain-Cli` | Operator CLI for local workflows and AI tooling |
| `.halalchain/ai-inference` | AI gateway for embeddings, classification, rerank, and model access |
| `.halalchain/tawheed` | Evidence ingestion and deterministic policy evaluation |
| `docker-compose.yml` | Runs the full platform, Python services, and supporting infra |

## Stack and runtime services

The repo is built around .NET 10, Python 3.11+, Node 22+, and Docker Compose. The default local stack includes:

| Service | Runtime | Port | Purpose |
|---|---|---:|---|
| `platform-api` | ASP.NET Core / .NET 10 | 5001 | Main API |
| `halalchain` | Blazor Server / .NET 10 | 5200 | Customer UI |
| `marketplace` | Razor Pages + Blazor Server / .NET 10 | 5201 | Vendor marketplace UI with SignalR |
| `ai-inference` | Python FastAPI | 7071 | AI gateway and document processing |
| `tawheed` | Python FastAPI | 8000 | Policy engine and evidence collection |
| `postgres` | Postgres 17 | 5432 | Primary relational store |
| `redis` | Redis 7 | 6379 | Cache and distributed state |
| `neo4j` | Neo4j 5 | 7687 | Graph data layer |
| `qdrant` | Qdrant | 6333 | Vector search and embeddings |

The authoritative service inventory and ports are defined in `service-manifest.yaml` and the Compose definition in `docker-compose.yml`.

## Prerequisites

- .NET SDK 10 (pinned by `global.json`)
- Node.js 22 or newer
- Python 3.11+
- Docker and Docker Compose
- Optional local model runtime such as Ollama/OpenClaw for the AI assistant

## Quick start

### 1) Restore and build the .NET solution

```bash
dotnet restore HalalChain.Platform.sln
dotnet build HalalChain.Platform.sln -c Release
```

### 2) Run the test suite

```bash
dotnet test HalalChain.Platform.sln -c Release --no-build
```

### 3) Start the full local stack

```bash
docker compose up --build
```

This brings up the API, customer app, marketplace, AI inference service, policy engine, and required backing infrastructure.

### 4) Run the Node CLI and Python dev services

```bash
npm install
npm run halalchain:cli
npm run ai:dev
npm run tawheed:dev
```

The root Node workspace also provides SCSS build helpers and test scripts for the CLI toolkit.

## Configuration and secrets

- Real credentials and production secrets should come from environment variables, not committed config files.
- Do not commit `.env` files in normal development workflows.
- `docker-compose.yml` defines the required environment values for the platform services and infra.
- The non-development JWT key is rejected if it matches the example value, and platform services expect required variables like `JWT__KEY`, `AI_GATEWAY_API_KEY`, `POSTGRES_CONNECTION_STRING`, and `REDIS_CONNECTION_STRING` to be set when running the full stack.

## Developer docs

- `AGENTS.md` — build/test workflow and repository conventions
- `docs/ARCHITECTURE.md` — overall architecture and component relationships
- `docs/mvp-architecture.md` — MVP-specific architecture notes
- `docs/local-development.md` — local setup and developer workflow
- `docs/runbooks/` — operational runbooks and recovery procedures
- `service-manifest.yaml` — declarative inventory of services and ports

## Design intent

The repository is intentionally structured around a clear split of responsibilities:

- AI services collect and interpret evidence
- the deterministic policy engine makes compliance decisions
- the .NET platform exposes business capabilities and integrations
- UI and marketplace layers consume those capabilities in a modular, service-oriented way

This separation keeps the platform aligned with a compliance-first model rather than letting the LLM become the source of truth for halal outcomes.
