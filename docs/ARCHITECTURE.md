# HalalChain Platform — Architecture

> AI discovers and interprets evidence. Deterministic systems decide business and compliance outcomes.

## 1. System Overview

```
                         INTERNET
                            │
              ┌─────────────┼─────────────┐
              ▼             ▼             ▼
       halalchain      marketplace    platform-api
        (Blazor)          (MVC)        (REST API)
          :5200           :5201           :5001
                                            │
                     ┌──────────────────────┼─────────────────────┐
                     │                      │                     │
                     ▼                      ▼                     ▼
                 ai-inference          tawheed               PostgreSQL
                  (Python/Node)       (Python)                :5432
                    :7071               :8000
                     │                    │
                     ├────────────────────┤
                     ▼                    ▼
                  Qdrant               Redis
                  :6333                :6379
```

## 2. Service Catalog

See `service-manifest.yaml` for machine-readable service definitions.

### Application Services

| Service | Runtime | Port | Public | Purpose |
|---------|---------|------|--------|---------|
| platform-api | .NET 10 | 5001 | Yes | Core REST API — modular monolith |
| halalchain | .NET 10 | 5200 | Yes | Blazor Server frontend |
| marketplace | .NET 10 | 5201 | Yes | MVC vendor marketplace |
| ai-inference | Python/Node | 7071 | No | Embeddings, classify, rerank |
| tawheed | Python | 8000 | No | Multi-agent halal verification |

### Infrastructure

| Service | Image | Port | Purpose |
|---------|-------|------|---------|
| postgres | postgres:17-alpine | 5432 | Primary database |
| redis | redis:7-alpine | 6379 | Cache, sessions, locks |
| neo4j | neo4j:5-community | 7687 | Graph DB (planned) |
| qdrant | qdrant/qdrant:v1.19.0 | 6333 | Vector embeddings |

## 3. Health Endpoints

Every HTTP service exposes three endpoints:

| Endpoint | Purpose | Docker uses | K8s uses |
|----------|---------|-------------|----------|
| `/health` | Diagnostic aggregate — may include dependency status | | |
| `/health/live` | Process is alive — MUST NOT require external dependencies | Healthcheck | Liveness probe |
| `/health/ready` | Service can receive traffic — MAY require critical dependencies | | Readiness probe |

**Dependency readiness classification:**

| Service | postgres | redis | ai-inference | tawheed |
|---------|----------|-------|--------------|---------|
| platform-api | Required | Optional | Optional | Optional |
| halalchain | — | — | — | — |
| marketplace | — | — | — | — |
| ai-inference | — | Optional | — | — |
| tawheed | Optional | Optional | — | — |

Services remain available for operations that don't require unavailable dependencies.

## 4. Configuration

### Environment Variable Conventions

**ASP.NET services** use `__` for config section binding (matches `IConfiguration`):
```
Jwt__Issuer
Jwt__Key
ConnectionStrings__Postgres
ConnectionStrings__Redis
AiGateway__BaseUrl
Tawheed__BaseUrl
PlatformApi__BaseUrl
```

**Python services** use `_` for pydantic-settings:
```
POSTGRES_URL
REDIS_URL
NEO4J_URI
DEMO_MODE
DEFAULT_JURISDICTION
```

These are runtime-appropriate conventions, not inconsistencies.

### Secrets

- Development: `appsettings.json` contains dev-only defaults, user-secrets for real values
- Docker: `.env` file (gitignored), referenced by `docker-compose.yml`
- Production: environment variables or secret manager (TBD)

Never commit real secrets. Use `.env.example` as a template.

## 5. API Communication

### Synchronous (HTTP)

```
Browser → halalchain/marketplace → platform-api (JWT auth)
platform-api → ai-inference (POST, internal)
platform-api → tawheed (POST, internal)
```

### Asynchronous (Current)

```
platform-api → InProcessEventBus → OutboxBackgroundService → PostgreSQL outbox table
```

The outbox pattern ensures reliable event publication within database transactions.

### Asynchronous (Future)

```
platform-api → Redis Streams → Consumer Groups
                                  ├── AI worker
                                  ├── Verification worker
                                  └── Document worker
```

## 6. Halal Verification Flow

```
AI Agents (tawheed)
    │
    ├── Certificate evidence
    ├── Ingredient evidence
    └── Supplier evidence
            │
        Evidence Store
            │
    Deterministic Policy Engine
            │
    ┌───────┴───────┐
 VERIFIED      MANUAL_REVIEW / REJECT
```

The LLM is used **only** for document interpretation when structure is ambiguous.
It **never** assigns or overrides a compliance status.

## 7. Data Architecture

| Database | Owner | Purpose |
|----------|-------|---------|
| PostgreSQL | platform-api | Catalog, vendors, orders, halal records, outbox |
| Redis | shared | Cache, sessions, rate limits |
| Qdrant | ai-inference | Document/product embeddings, semantic search |
| Neo4j | (planned) | Supply-chain relationships, provenance |

**Rule:** A service owns its data. Other services consume through APIs, not direct database access.

## 8. Repository Structure

```
HalalChain.Platform.sln       .NET solution (8 projects + 1 xUnit MCP test project)
global.json                   .NET SDK version pin (10.0.200)
Directory.Build.props         Shared .csproj properties
.editorconfig                 Code style rules
.gitignore                    Ignore patterns
.env.example                  Environment template
docker-compose.yml            Full stack orchestration
docker/Dockerfile.template    Parameterized .NET service Dockerfile source of truth
service-manifest.yaml         Service catalog (source of truth)
Modelfile                     Ollama system prompt for the local halal assistant

# ── .NET projects (HalalChain.*) ───────────────────────────────────────
HalalChain.Platform.Contracts/    Shared DTOs, enums, Solidity contracts
HalalChain.Platform.Http/         Typed HttpClient library
HalalChain.Platform.Api/          Core REST API (modular monolith, ASP.NET Core)
HalalChain.Marketplace/           ASP.NET Core MVC vendor marketplace
HalalChain.Web/                   Blazor Server customer-facing UI
HalalChain.Mcp/                   Model-Context-Protocol server (console host)
HalalChain.Mcp.Tests/             xUnit tests for the MCP server
HalalChain.Platform.Tests/        xUnit tests for the API + persistence

# ── Operator CLI (HalalChain-*, hyphen) ───────────────────────────────
HalalChain-Cli/                   Node 22 operator CLI (binary: halalchain)

# ── Python services (.halalchain/, leading dot) ────────────────────────
.halalchain/ai-inference/         FastAPI AI gateway (embeddings/classify/rerank/LLM)
.halalchain/tawheed/              FastAPI evidence + deterministic Policy Engine
.halalchain/_shared/              Shared Python package (LLM provider wiring, cache)
.halalchain/config.json           Local shared config (gitignored in real use)

# ── Docs, CI, infra ───────────────────────────────────────────────────
docs/                             Architecture, runbooks, MVP notes
infrastructure/                   Dev docker-compose overlay, helper scripts
.github/                          CI workflows
```

### Directory naming convention

- **`HalalChain.*`** (dot) — .NET projects inside the canonical solution.
- **`HalalChain-*`** (hyphen) — operator-facing primary services that produce
  a deployable artifact (e.g. the Node CLI).
- **`.halalchain/`** (leading dot) — Python services and supporting runtime
  assets that are NOT part of the .NET solution. Treated as gitignored-style
  private workspace; CI is not required to build them in lockstep with .NET.

### HalalChain.Mcp

`HalalChain.Mcp` is a runnable Model-Context-Protocol server. It is
intentionally **not** a deployable service in `docker-compose.yml` — it
ships as a console host and is invoked by MCP clients (IDEs, agent runners)
on the operator's machine, not as a long-running container.

### Modelfile placement

`Modelfile` is the Ollama system prompt for the local halal assistant. It
sits at the repository root (not under `.halalchain/`) because Ollama
expects to be invoked from the directory that owns the `Modelfile`, and
`docs/FOUNDRY_LEVERAGE.md` references it from the root.

### Architectural principle

> **AI discovers and interprets evidence. Deterministic systems decide
> business and compliance outcomes.**

The full invariant and its enforcement points are documented once in
`docs/ARCHITECTURE.md` § 6. Other locations reference this canonical
statement rather than re-stating it.

## 9. Future Architecture

The following are planned but NOT currently implemented:

- **Workers:** ai-worker, verification-worker, document-worker (background processing)
- **Event bus:** Redis Streams with consumer groups (replacing in-process bus)
- **Reverse proxy:** Ingress layer for unified public endpoints
- **API versioning:** `/api/v1/`, `/api/v2/` URL strategy
- **Observability:** Correlation IDs, structured logging, distributed tracing
- **Multi-region:** Cross-region backup replication
