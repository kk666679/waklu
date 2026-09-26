# Agent / contributor guide

This file documents how to build, test, and operate the HalalChain platform.

## Stack

- .NET 10 (SDK pinned in `global.json` to `10.0.200` with `rollForward: latestFeature`).
- Python 3.11+ (FastAPI services in `.halalchain/`).
- Node ≥22 (CLI under `HalalChain-Cli/`; root `package.json` defines an npm workspace).
- Solidity via Foundry (in `HalalChain.Platform.Contracts/contracts/`).
- Docker / Docker Compose for the local stack.

## Solution layout

The main solution currently contains **10 .NET projects**, including the MCP test project and supporting domain/application layers used across the platform:

| Project                              | Type                | Role                                  |
|--------------------------------------|---------------------|---------------------------------------|
| `HalalChain.Domain`                  | .NET class library  | Domain model and business concepts     |
| `HalalChain.Platform.Contracts`      | .NET class library  | Shared DTOs + Solidity sources        |
| `HalalChain.Platform.Http`           | .NET class library  | Typed `HttpClient` for the API        |
| `HalalChain.Platform.Api`            | ASP.NET Core        | Core REST API (modular monolith)      |
| `HalalChain.Platform.Tests`          | xUnit               | API, persistence, vendor isolation    |
| `HalalChain.Marketplace`             | ASP.NET Core web app | Vendor marketplace UI (Razor Pages + Blazor Server + SignalR) |
| `HalalChain.Web`                     | Blazor Server       | Customer-facing UI (Radzen)           |
| `HalalChain.Mcp`                     | .NET console host   | Model-Context-Protocol server (runnable; not a deployable compose service) |
| `HalalChain.Mcp.Tests`               | xUnit               | MCP server tests                      |
| `HalalChain.Architecture.Tests`      | xUnit               | Architecture guard tests              |

`Radzen.Blazor.Api.Generator.csproj` sits alongside the main API in
`HalalChain.Platform.Api/` and is invoked by the main API project only when
`-p:GenerateApiPages=true` is passed.

## Repository layout conventions

- `HalalChain-*` (hyphen) prefixes mark **primary services that produce
  deployable artifacts** — for example the operator CLI in `HalalChain-Cli/`.
- `.halalchain/` is a **leading-dot, Python-first directory** containing
  supporting runtime services that are not part of the canonical .NET
  solution. It currently holds:
  - `ai-inference` — FastAPI AI gateway (embeddings / classify / rerank / LLM)
  - `tawheed` — FastAPI evidence + deterministic Policy Engine
  - `config.json` — local-only development config (gitignored in real use)
  - `_shared/` — Python package containing code genuinely shared by
    `ai-inference` and `tawheed` (LLM provider selection wiring,
    LRU/Redis cache abstraction, environment normalization).

### `Modelfile`

The repository root `Modelfile` is the system prompt for the local
OpenClaw / Ollama-hosted `halalchain-assistant` model. It is intentionally
kept at the repository root (not under `.halalchain/`) because Ollama
expects it at the location from which `ollama create` is invoked and the
build instructions in `docs/FOUNDRY_LEVERAGE.md` reference it from there.

## Build

```bash
dotnet restore HalalChain.Platform.sln
dotnet build   HalalChain.Platform.sln -c Release
```

The first build after a rename may need a clean: `dotnet clean HalalChain.Platform.sln` then rebuild.

## Test

```bash
dotnet test HalalChain.Platform.sln -c Release --no-build
```

## Run locally

`docker compose up --build` brings up the full stack (see `docker-compose.yml`).
The `platform-api` listens on `http://localhost:5001`, `halalchain` (Blazor Server)
on `5200`, `marketplace` (Razor Pages + Blazor Server + SignalR) on `5201`,
`ai-inference` on `7071`, `tawheed` on `8000`, and infra (Postgres, Redis,
Neo4j, Qdrant) on the ports documented in `service-manifest.yaml`.

To add IPFS / a local Polygon-fork chain (Anvil) for contract testing, layer
the development overlay:

```bash
docker compose \
  -f docker-compose.yml \
  -f infrastructure/docker-compose.dev.yml \
  --profile chain up --build
```

> `infrastructure/docker-compose.dev.yml` is an *overlay* — it intentionally
> does not redefine `postgres`/`redis`/etc. Drop a `docker-compose.override.yml`
> next to the root file if you need to mutate one of those.

## CLI

```bash
npm install                          # install root workspace deps
npm run halalchain:cli               # run the Node CLI
npm run ai:dev                       # run the Node-based AI smoke harness
```

The CLI is defined by `HalalChain-Cli/package.json` (binary: `halalchain`).

## Configuration

- Real secrets must come from environment variables, not from
  `appsettings.json`. The non-development `Jwt:Key` value is rejected at
  startup if it matches the example value.
- `.env` files must not be committed. `.env.example` documents the MCP env
  vars; `docker-compose.yml` documents the platform services' env vars.
- The shared local config at `.halalchain/config.json` is a placeholder for
  development secrets; replace it with real environment variables in any
  non-development environment.

## Architectural principle

AI agents collect evidence. The deterministic Policy Engine decides
compliance status. The LLM never assigns or overrides a halal verdict.
This is enforced in:

- the `Modelfile` system prompt for the local halal assistant, and
- the API design in `HalalChain.Platform.Api/Modules/Halal/` — it forwards
  to `tawheed` for evidence, never for verdicts.

## Updating versions

- All non-major NuGet bumps should preserve `Directory.Build.props` defaults
  (`net10.0`, nullable, implicit usings).
- Python `requirements.txt` files should be re-pinned with hashes after
  every functional change.
- Docker base images (`mcr.microsoft.com/dotnet/{aspnet,sdk}`, `postgres`,
  `redis`, `neo4j`, `qdrant/qdrant`) must be pinned to a specific tag, not
  `latest` or a bare major version.

## Where to find what

- API modules and endpoints: `HalalChain.Platform.Api/Modules/<Name>/`.
- Shared contracts/DTOs: `HalalChain.Platform.Contracts/`.
- Solidity sources and Foundry config: `HalalChain.Platform.Contracts/contracts/`.
- AI gateway Python service: `.halalchain/ai-inference/`.
- Tawheed evidence + Policy Engine: `.halalchain/tawheed/`.
- Customer UI: `HalalChain.Web/`.
- Vendor UI: `HalalChain.Marketplace/` (Razor Pages + Blazor Server + SignalR).
- MCP server: `HalalChain.Mcp/`.
- Documentation: `docs/`.
- Tech debt register (current findings, severities, verified evidence): `docs/architecture/tech-debt.md`.
- Operational runbooks: `docs/runbooks/`.
