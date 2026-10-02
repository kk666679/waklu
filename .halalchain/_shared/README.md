# `.halalchain/_shared/`

The `halalchain-shared` Python package — primitives genuinely shared by more
than one service.

## Purpose

Four FastAPI services need the same three things: a way to pick an LLM backend
from the environment, a cache, and a typed HTTP client for calling sibling
services. Duplicating those four times would mean four places to fix a provider
config change. This package is the single definition.

It is a **library**. There is no `main.py`, no FastAPI app, and no routes.

## Responsibilities

- Resolve `AI_BACKEND` (and the provider config that follows from it) from the
  environment, normalising legacy variable names.
- Provide a small in-process LRU cache with per-key TTL.
- Provide typed async HTTP clients for `ai-inference`, `tawheed`, and
  `local-models`, with API-key forwarding and timeouts.

## Structure

```
.halalchain/_shared/
├── pyproject.toml                       # halalchain-shared 0.1.0, setuptools, src layout
├── src/halalchain_shared/
│   ├── __init__.py                      # Re-exports the public surface; __version__ = "0.1.0"
│   ├── ai_backend.py                    # AI_BACKEND resolution + provider config
│   ├── cache.py                         # LRUCache, build_cache
│   └── service_client.py                # ServiceClient + typed clients + factories
└── tests/
    ├── test_ai_backend.py
    └── test_service_client.py
```

## Architecture / Flow

```text
ai-inference ─┐
tawheed ──────┼─▶ halalchain_shared ─▶ resolve_ai_backend()  ─▶ openai / anthropic /
local-models ─┤                       build_cache()         ─▶ LRU cache
agents ───────┘                       create_*_client()     ─▶ httpx.AsyncClient
```

## Dependencies

Core: `fastapi==0.120.0`, `uvicorn[standard]==0.40.0`, `pydantic>=2.12.5,<3`,
`pydantic-settings>=2.10.1,<3`, `httpx==0.28.0`, `structlog==25.1.0`,
`python-dotenv>=1.1.1,<2`.

Extras:

| Extra | Adds |
| --- | --- |
| `llm` | `openai==3.22.1` |
| `base` | same as core |
| `skills` | `agent-skills==1.1.1`, `haiku-skills[signing]==0.18.1` |
| `mcp` | `mcp==2.2.0`, `mcp-types==2.2.0` |
| `mcp-cli` | `mcp[cli]==2.2.0` |
| `dev` | `pytest==8.4.0`, `pytest-asyncio==0.24.0`, `pytest-httpx==0.35.0`, `ruff==0.9.0`, `mypy==1.15.0`, `coverage[toml]==7.10.0` |

## Interfaces

### `ai_backend`

`AI_BACKEND` accepts `local`, `demo`, `openai`, `anthropic`, `foundry`,
`openclaw`. Two read-only legacy aliases are honoured: `LLM_PROVIDER` and
`DEFAULT_LLM_PROVIDER`.

| Function | Returns |
| --- | --- |
| `resolve_ai_backend(default)` | The resolved backend token |
| `resolve_ai_provider_config(...)` | frozen `AIProviderConfig(backend, base_url, api_key, model, enabled)` |
| `get_async_openai_client(cfg)` | `AsyncOpenAI`, importing `openai` lazily |

### `cache`

| Function | Behaviour |
| --- | --- |
| `LRUCache(max_size=10000, ttl=3600)` | Per-key TTL, tracks `hits`/`misses`, exposes `stats()` with a `hitRate` string |
| `build_cache(strategy, ...)` | `"lru"` **and** `"redis"` both return a fresh `LRUCache`; `None`, `"none"`, and anything unrecognised return `None` |

There is no Redis implementation. The `"redis"` strategy name is accepted and
degrades to in-process caching; a multi-replica deployment therefore does not
share a cache even when configured to.

### `service_client`

Factories: `create_ai_inference_client()`, `create_tawheed_client()`,
`create_local_models_client()`.

Each returns a typed client over a shared `httpx.AsyncClient` with `timeout=30.0`
and an `X-API-Key` header when an API key is configured.

## Configuration

| Variable | Read by | Default |
| --- | --- | --- |
| `AI_BACKEND` | `ai_backend` | — |
| `LLM_PROVIDER`, `DEFAULT_LLM_PROVIDER` | `ai_backend` (legacy) | — |
| `FOUNDRY_BASE_URL`, `FOUNDRY_API_KEY`, `FOUNDRY_MODEL` | `ai_backend` | — |
| `OPENCLAW_BASE_URL`, `OPENCLAW_API_KEY`, `OPENCLAW_MODEL` | `ai_backend` | `OPENCLAW_BASE_URL=ollama` |
| `OPENAI_API_KEY`, `OPENAI_BASE_URL`, `OPENAI_MODEL` | `ai_backend` | — |
| `ANTHROPIC_API_KEY`, `ANTHROPIC_BASE_URL`, `ANTHROPIC_MODEL` | `ai_backend` | — |
| `AI_INFERENCE_URL` | `service_client` | `http://localhost:7071` |
| `AI_GATEWAY_API_KEY` | `service_client` | — |
| `TAWHEED_URL` | `service_client` | `http://localhost:8000` |
| `TAWHEED_API_KEY` | `service_client` | — |
| `LOCAL_MODELS_URL` | `service_client` | `http://localhost:8080` |
| `LOCAL_MODELS_API_KEY` | `service_client` | — |

## Integration

Consumed by all four services (`ai-inference`, `tawheed`, `agents`,
`local-models`) as `halalchain-shared` via `{ workspace = true }`. It is
installed separately in Docker because pip cannot hash-check a local
requirement.

### Declared routes vs. live routes

`service_client` is the clearest statement of the intended cross-service
contract. Two of its clients have drifted from the services they call:

| Client call | Live endpoint | Status |
| --- | --- | --- |
| `POST /embeddings` on ai-inference | `POST /embeddings` | Matches |
| `POST /classify` on ai-inference | `POST /classify` | Matches |
| `POST /summarize` on ai-inference | `POST /summarize` | Matches |
| `POST /rerank` on ai-inference | `POST /rerank` | Matches |
| `GET /health/ready` on ai-inference | `GET /health/ready` | Matches |
| `POST /llm/generate` on ai-inference | `POST /llm/generate` takes **query parameters**, the client sends a JSON body | Mismatch — 422 |
| `POST /rag/search` on ai-inference | `POST /rag/search` takes **query parameters**, the client sends a JSON body | Mismatch — 422 |
| `POST /api/v1/evaluate` on tawheed | tawheed exposes `POST /v1/products/{product_id}/verify` | Mismatch — 404 |
| `POST /api/v1/evidence/query` on tawheed | does not exist | Mismatch — 404 |
| `GET /health` on tawheed | `GET /health` | Matches |
| `POST /embeddings`, `/classify`, `/generate`, `GET /health/ready` on local-models | all four exist | Match |

In addition, `HalalChain.Platform.Api`'s `TawheedHttpClient` posts to
`POST /api/v1/verify`, which tawheed does not expose. See
[`docs/architecture/05-integration-workflows.md`](../../docs/architecture/05-integration-workflows.md).

## Development

```bash
cd .halalchain/_shared
pip install -e ".[dev]"
```

## Testing

```bash
python -m pytest
```

`pyproject.toml` sets `testpaths = ["tests"]` and `asyncio_mode = "auto"`.

> `.halalchain/_shared` is **not** in the CI `python-tests` matrix
> (`.github/workflows/ci.yml`), which covers `ai-inference`, `tawheed`, and
> `local-models`. Its lock is `requirements/dev.txt`, which is what
> `.halalchain/requirements-test.txt` points at. Run these tests manually.

## Deployment

Not deployable — no image is built from this directory. It is copied into each
service image (`local-models` does this by copying
`halalchain_shared` directly into `dist-packages`).

## Related Components

- [.halalchain/](../README.md) — the Python tier
- [ai-inference](../ai-inference/README.md)
- [tawheed](../tawheed/README.md)
- [agents](../agents/README.md)
- [local-models](../local-models/README.md)
- [requirements/](../requirements/README.md) — where `base.txt`, `dev.txt`, `llm.txt`, and `mcp.txt` are generated

## Notes / Limitations

- `TawheedClient` and `AIInferenceClient` are exported and typed but have **no
  in-tree caller**; only `LocalModelsClient` is actually used, by
  `ai-inference/embeddings.py`.
- The `/api/v1/evaluate` and `/api/v1/evidence/query` routes look like an older
  API design that `tawheed` moved away from. Whoever repairs the contract should
  decide which side is authoritative first.