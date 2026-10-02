# `.halalchain/`

The Python runtime tier of HalalChain: the four services that gather and
evaluate halal evidence, plus the pinned dependency locks that make them
reproducible.

## Purpose

This directory holds every Python service in the repository. They are kept out
of the .NET solution because they have a different runtime, a different
dependency model (`uv` + hashed locks rather than NuGet), and a different
deployment unit.

The four services:

| Service | Port (host) | Role |
| --- | --- | --- |
| [`ai-inference/`](ai-inference/README.md) | 7071 | AI gateway: embeddings, classify, rerank, summarise, RAG, LLM, document parsing |
| [`tawheed/`](tawheed/README.md) | 8000 | **Compliance authority.** Evidence collection agents + the deterministic Policy Engine |
| [`agents/`](agents/README.md) | 8081 | Agent workflow orchestration and the evaluation DAG |
| [`local-models/`](local-models/README.md) | 8080 | Self-hosted embedding/classification model host |

Plus [`_shared/`](_shared/README.md), a library rather than a service, and
[`requirements/`](requirements/README.md), the locked dependency files.

## Responsibilities

- Host the four FastAPI services that the .NET tier calls over HTTP.
- Enforce the platform's central invariant in this tier: agents collect
  evidence, the deterministic Policy Engine in `tawheed` decides.
- Pin every Python dependency with hashes and verify the pins in CI.
- Provide shared primitives (LLM backend selection, caching, cross-service HTTP
  clients) without duplicating them per service.

## Architecture / Flow

```text
HalalChain.Platform.Api ──HTTP──▶ ai-inference   (embeddings, classify, …)
              │                 └▶ local-models (self-hosted embeddings)
              ├──HTTP──▶ tawheed       (evidence + deterministic verdict)
              └──HTTP──▶ agents        (workflow runs via AgentWorkflowClient)

Postgres, Redis, Neo4j, Qdrant  ◀── all four services
```

## Structure

```
.halalchain/
├── pyproject.toml              # uv workspace root (5 members)
├── config.json                 # Local dev placeholder config (Jwt.Key only)
├── requirements.txt            # Comments only — per-service includes
├── requirements-test.txt       # -r requirements/dev.txt
├── requirements/               # 10 hashed lock files
├── _shared/                    # halalchain-shared Python package
├── ai-inference/               # AI gateway service
├── tawheed/                    # Evidence + Policy Engine
├── agents/                     # Agent orchestration + evaluation
└── local-models/               # Local model hosting
```

## Dependencies

Each service is a `uv` workspace member with its own `pyproject.toml`; the
workspace root pins `qdrant-client==1.19.1` as a constraint. Shared core pins
include `fastapi==0.120.0`, `uvicorn[standard]==0.40.0`, `httpx==0.28.0`,
`pydantic>=2.12.5`, `structlog==25.1.0`.

All services share `halalchain-shared` via `{ workspace = true }`. That is why
the Docker build context for the services is `.halalchain/` rather than the
repository root — the `_shared` package must be reachable at build time.

## Interfaces

### Between services

| From | To | How |
| --- | --- | --- |
| `ai-inference` | `local-models` | `halalchain_shared.service_client.create_local_models_client()` → `POST /embeddings` |
| `HalalChain.Platform.Api` | `ai-inference` | HTTP via `AiInference__BaseUrl` |
| `HalalChain.Platform.Api` | `tawheed` | HTTP via `Tawheed__BaseUrl` |
| `HalalChain.Platform.Api` | `agents` | HTTP via `Agents:BaseUrl` (`HalalChain.Agents/Runtime/AgentWorkflowClient.cs`) |
| `HalalChain-Cli` | all of the above | HTTP health probes and direct calls |

**What does not happen:** `tawheed` makes no outbound HTTP calls at all. It
reads `POSTGRES_URL`, `REDIS_URL`, `QDRANT_URL`, and `NEO4J_URI`, but its
evidence agents are rule-based. `agents` sets `AI_INFERENCE_URL`, `TAWHEED_URL`,
and `LOCAL_MODELS_URL` in compose but no present source file reads them.

## Usage

### Local development (per service)

```bash
npm run ai:dev       # ai-inference on 7071
npm run tawheed:dev  # tawheed on 8000
```

Both root scripts install `_shared`, then the service's `requirements.txt`, then
the service itself in editable mode, then start uvicorn. There is no equivalent
root script for `agents` or `local-models`.

### Docker Compose

```bash
docker compose up --build
```

Compose publishes `ai-inference` 7071, `tawheed` 8000, `agents` 8081,
`local-models` 8080.

## Configuration

Environment variable names are documented per service. The root
[`.env.example`](../.env.example) carries the same names in the `ai-inference`,
`tawheed`, and AI-backend blocks. Values are never committed.

Secret handling is deliberately fail-closed in two services:

- `ai-inference` refuses to start without a real `AI_GATEWAY_API_KEY` — it
  raises at import when the key is missing, a placeholder, or under 32
  characters. `AI_GATEWAY_DEV_AUTH_BYPASS` is honoured only in
  development/test/demo environments.
- `tawheed` validates `JWT_SECRET` the same way, and additionally rejects
  default Neo4j credentials outside development.

## Integration

- The .NET tier reaches this tier only over HTTP. There is no shared process.
- `HalalChain.Platform.Api` holds `Tawheed__BaseUrl`, `AiGateway__BaseUrl`, and
  `LocalModels__BaseUrl`; `HalalChain.Agents` holds `Agents:BaseUrl` and
  throws when it is unset.
- Prometheus metrics from this tier land in `infrastructure/prometheus`, and
  traces in `infrastructure/tempo`/`loki`.

## Development

```bash
# One service
cd .halalchain/tawheed
pip install -r requirements.txt && pip install -e .
uvicorn src.main:app --host 0.0.0.0 --port 8000 --reload

# Tests (matches the CI matrix)
python -m pytest
```

CI runs pytest for `ai-inference`, `tawheed`, and `local-models` only — see
`.github/workflows/ci.yml`. `.halalchain/_shared` and `.halalchain/agents` are
**not** in that matrix.

## Testing

| Directory | In CI matrix | Command |
| --- | --- | --- |
| `_shared` | No | `pytest` from `.halalchain/_shared` |
| `ai-inference` | Yes | `python -m pytest` |
| `tawheed` | Yes | `python -m pytest` |
| `local-models` | Yes | `python -m pytest` |
| `agents` | No | No runnable tests present — see its README |

## Deployment

Four Dockerfiles, all on `python:3.11.11-slim-bookworm`, all running as a
non-root user (uid/gid 1001 or 1000). All four are pinned in
[`service-manifest.yaml`](../service-manifest.yaml).

## Related Components

- [Root README](../README.md) — full architecture and workflows
- [HalalChain.Platform.Api](../HalalChain.Platform.Api/README.md) — the primary caller
- [HalalChain.Agents](../HalalChain.Agents/README.md) — the .NET half of the agent runtime
- [docker-compose.yml](../docker-compose.yml) — service definitions
- [docs/architecture/05-integration-workflows.md](../docs/architecture/05-integration-workflows.md) — cross-directory workflows

## Notes / Limitations

- The `agents` service cannot start as committed: `app/halalchain_agents/main.py`
  imports `agents.base`, `agents.collection`, `runtime.dag`, and `workflows`,
  but `app/agents/`, `app/runtime/`, and `app/workflows/` contain only stale
  `__pycache__` directories and no `.py` files. See its README.
- `_shared`'s `ServiceClient` declares routes that do not match the live
  services (`/api/v1/evaluate`, `/api/v1/evidence/query`, and two
  query-parameter/JSON-body mismatches). See the integration doc for the
  specific mismatches.
- `build_cache("redis", ...)` returns an in-process `LRUCache`, not a Redis
  client. The strategy name is honoured only for `"lru"`/`"redis"` → LRU;
  everything else returns `None`.
- `local-models/Dockerfile`'s `CMD` names a module path
  (`halalchain_local_models.main`) that does not exist; the source is `src/main.py`.
- `.halalchain/config.json` is a local-only placeholder containing a `Jwt.Key`
  dev value. No Python source file reads it.