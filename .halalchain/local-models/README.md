# `.halalchain/local-models`

Python 3.11 FastAPI local model inference service. Served on port `8080` in the
local stack. It hosts embedding models in-process so the platform can produce
embeddings without calling an external provider.

## Purpose

One service, one file: `src/main.py` defines the settings, model registry,
FastAPI app, and every route inline. It exists so `ai-inference` has an
embedding provider that does not leave the network.

## Responsibilities

- Serve embeddings from a locally loaded sentence-transformers model.
- Report which models are loaded and which backend is active.
- Cache embeddings in-process.
- Report health for the Compose health check.

It does **not** currently do real classification or generation — see
"Placeholders" below.

## Structure

```
.halalchain/local-models/
├── pyproject.toml       # Project metadata; extras: inference, quantization,
│                        #   gguf, optimized, dev
├── requirements.txt     # Thin include: -r ../requirements/local-models.txt
├── Dockerfile           # Multi-stage: python:3.11-slim builder → runtime
├── src/
│   └── main.py          # The entire service
└── tests/
    └── test_embedding_inference.py
```

There is no `src/__init__.py` and no `src/halalchain_local_models/` package —
the module is `src.main`.

> Corrections to the previous revision of this README: `auth.py`, `cache.py`,
> `config.py`, `health.py`, `model_loader.py`, `embedding_model.py`,
> `classification_model.py`, `inference_api.py`, the `models/` tree, and two of
> the three listed test files **do not exist**.

## Architecture / Flow

```text
ai-inference/embeddings.py
   │  create_local_models_client()  →  LOCAL_MODELS_URL
   ▼
POST /embeddings
   ↓
_load_embedding_encoder()          # torch + transformers, CUDA when available
   │  AutoTokenizer / AutoModel     # EMBEDDING_MODEL
   │  (default sentence-transformers/all-MiniLM-L6-v2)
   ↓
_encode_embedding()                # attention-masked mean pooling + L2 normalise,
   │                                # truncation=True, max_length=512
   ↓
LRUCache  →  {embedding: [...], dimension: 384, model: "..."}
```

## Dependencies

Core: `halalchain-shared`.

| Extra | Adds |
| --- | --- |
| `inference` | `transformers>=5.13,<6`, `torch>=2.8,<3`, `accelerate>=1.0,<2`, `safetensors>=0.5,<1`, `tokenizers>=0.21,<1`, `huggingface-hub>=1.5,<2` |
| `quantization` | `bitsandbytes>=0.50,<1`, `auto-gptq>=0.7,<1` (0.8.x was never published; resolves to 0.7.1) |
| `gguf` | `llama-cpp-python>=0.3,<0.4` |
| `optimized` | `vllm>=0.30,<1` — the project file notes this is unresolvable against the pinned `fastapi==0.120.0` and is excluded from the image lock and build |
| `dev` | `pytest==8.4.0`, `pytest-asyncio`, `pytest-httpx`, `ruff`, `mypy`, `coverage` |

This is the only service with a multi-stage Dockerfile that installs from a
builder stage and copies site-packages forward, because the CUDA/torch
dependency tree is large.

## Interfaces

### HTTP endpoints

| Method | Path | Notes |
| --- | --- | --- |
| `GET` | `/` | Service banner |
| `GET` | `/health/live` | Liveness (Compose health check) |
| `GET` | `/health/ready` | `{status, service, backend, models_loaded}` |
| `GET` | `/health` | Typed `HealthResponse`, includes `version` |
| `POST` | `/embeddings` | **Sync `def`** — runs in the threadpool. 400 on empty text or unconfigured model; 503 when the model is unavailable |
| `POST` | `/classify` | **Placeholder** — 400 when `labels` is empty |
| `POST` | `/generate` | **Placeholder** |
| `POST` | `/cache/clear` | Reaches into `cache._cache` / `cache._expiry` directly |
| `GET` | `/cache/stats` | |
| `GET` | `/models` | `{"models": [...], "backend": ...}` |

**No route enforces authentication.** `Depends` is imported but unused, and the
`LOCAL_MODELS_API_KEY` variable that `_shared`'s `LocalModelsClient` reads has no
server-side counterpart here.

### Placeholders

`POST /classify` returns a score of `0.5` for every label with
`bestLabel = labels[0]`. `POST /generate` returns
`f"Generated response for: {prompt[:50]}..."`. Neither does real inference.
`ai-inference` does not call either endpoint today — it only calls
`POST /embeddings`.

## Configuration

| Variable | Default | Notes |
| --- | --- | --- |
| `PORT` | `8080` | |
| `ENVIRONMENT` | `development` | |
| `LOG_LEVEL` | `INFO` | |
| `AI_BACKEND` | `local` | |
| `LOCAL_MODELS_ALLOWED_ORIGINS` | `http://localhost:5001,http://localhost:5200,http://localhost:5201,http://localhost:7071,http://localhost:8000` | A wildcard origin raises `RuntimeError`; an empty list in production raises `RuntimeError` |
| `ENABLE_CACHE` | `true` | |
| `CACHE_STRATEGY` | `lru` | |
| `CACHE_MAX_SIZE` | `10000` | |
| `CACHE_TTL` | `3600` | |
| `REDIS_URL` | `redis://redis:6379/0` | Read but **unused** |
| `EMBEDDING_MODEL` | `sentence-transformers/all-MiniLM-L6-v2` | |

Compose additionally sets `TRANSFORMERS_CACHE` and `HF_HOME`; no code reads
either.

Note that `Settings` here is a plain Python class reading `os.environ` directly —
not a pydantic `BaseSettings`, unlike every other service in this tier.

## Integration

| Direction | Peer | How |
| --- | --- | --- |
| Called by | `ai-inference` | `POST /embeddings`, when `EMBEDDING_PROVIDER=local-models`. Verified live by `ai-inference/tests/test_cross_service.py` |
| Called by | `HalalChain.Platform.Api` | `LocalModels__BaseUrl` |
| Called by | `_shared`'s `LocalModelsClient` | `/embeddings`, `/classify`, `/generate`, `/health/ready` — all four paths match the live service |
| Called by | `HalalChain-Cli` | `LOCAL_MODELS_URL`, health probes |

Compose depends on `redis`, which this service does not use.

## Development

```bash
pip install -r requirements.txt && pip install -e .
uvicorn src.main:app --host 0.0.0.0 --port 8080 --reload
```

The README in earlier revisions gave the same command; it is correct. There is
no `npm` script for this service.

## Testing

```bash
python -m pytest
```

In the CI matrix. CI installs `../requirements/base.txt` (not
`requirements.txt`) for this service.

`tests/test_embedding_inference.py` has three tests: a configured model returns
the expected dimension; an unconfigured model name returns 400; and a
`_encode_embedding` failure returns 503. Because `_embed` is invoked
synchronously, the 503 path does not require torch to be installed.

There is no `conftest.py`; the test imports `from src import main` directly.

## Deployment

Image built from `.halalchain/local-models/Dockerfile`, published on host port
`8080`, with volume `local_models_cache:/models` and a 60-second health-check
start period (model download). Build context is `.halalchain/`. Tag pinned in
[`service-manifest.yaml`](../../service-manifest.yaml).

The Helm chart does **not** deploy this service — it is absent from
`values.yaml`, which covers `platformApi`, `web`, `marketplace`, `aiInference`,
and `tawheed` only.

Runbook: [`docs/runbooks/gpu-memory.md`](../../docs/runbooks/gpu-memory.md),
[`disk-space.md`](../../docs/runbooks/disk-space.md).

## Related Components

- [.halalchain/ai-inference](../ai-inference/README.md) — the only live caller
- [.halalchain/_shared](../_shared/README.md) — `LocalModelsClient`, backend resolution, cache
- [HalalChain.Platform.Api](../../HalalChain.Platform.Api/README.md) — `LocalModels__BaseUrl`
- [docker-compose.yml](../../docker-compose.yml)

## Notes / Limitations

- The Dockerfile's `CMD ["python","-m","halalchain_local_models.main"]` names a
  module that does not exist; the source is `src/main.py`. The image's entry
  command therefore needs correcting before the built image is usable as-is.
- `/classify` and `/generate` are stubs. Anything that starts depending on them
  will get structurally valid but meaningless output.
- No authentication on any route.
- `REDIS_URL` is read and ignored; caching is per-process, so with multiple
  replicas the same embedding is computed once per replica.