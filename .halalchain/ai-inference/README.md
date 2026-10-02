# `.halalchain/ai-inference`

Python 3.11 FastAPI AI inference gateway. Served on port `7071` in the local
stack. It is the single entry point through which the .NET API reaches
embeddings, classifiers, rerankers, summarizers, LLMs, and vector stores.

## Purpose

This service is the AI tier. It holds no business logic of its own beyond
deterministic text processing (keyword classification, extractive
summarisation, token-overlap reranking, ingredient and certificate extraction)
and it delegates actual model inference to a configured provider.

## Responsibilities

- Serve embeddings, classification, reranking, and summarisation over HTTP.
- Provide RAG document ingestion and search backed by Qdrant.
- Parse documents (pdf, docx, xlsx, txt, md, html) with traversal, symlink, and
  size guards.
- Extract ingredient risk and certificate fields by rule.
- Proxy LLM generation to the configured backend.
- Fail closed on authentication and on unconfigured model providers.
- Export Prometheus metrics and structured JSON logs.

## Structure

```
.halalchain/ai-inference/
├── pyproject.toml       # Project metadata
├── requirements.txt     # Thin include: -r ../requirements/ai-inference.txt
├── package.json         # Declares npm start / npm run dev → node src/server.js
├── Dockerfile           # python:3.11.11-slim-bookworm, tini, non-root 1001
├── src/
│   ├── main.py                 # FastAPI app, auth wiring, CORS, routes
│   ├── auth.py                 # X-API-Key verification, fail-closed checks
│   ├── cache.py                # Re-export shim over halalchain_shared.cache
│   ├── certificate_extractor.py
│   ├── classifier.py           # Deterministic keyword scoring
│   ├── config.py               # Settings (pydantic-settings)
│   ├── document_processor.py   # DocumentProcessor, DocumentSecurityError
│   ├── embeddings.py           # Provider resolution: openai/foundry/openclaw/local-models
│   ├── health.py               # started_at, request_count, get_health
│   ├── ingredient_parser.py
│   ├── llm.py                  # LocalLLMProvider, OpenAI/Anthropic/OpenAI-compatible
│   ├── metrics.py              # halalchain_* Prometheus metrics
│   ├── models.py               # Request/response DTOs (camelCase)
│   ├── observability.py        # ai_inference_* metrics, tracing, JSON logging
│   ├── reranker.py
│   ├── summarizer.py
│   └── vector_store.py         # VectorStore ABC, QdrantVectorStore
└── tests/
    ├── test_auth.py
    ├── test_cross_service.py
    ├── test_document_processor.py
    └── test_ingredient_parser.py
```

> Corrections to the previous revision of this README: `src/server.js` **does not
> exist**, so `npm start` / `npm run dev` inside this directory will fail. The
> `tests/` directory **is** present (four modules). `cache.py` is a re-export
> shim, not a Redis-backed cache. `src/main.py` has no `otlp_endpoint` setting,
> so `init_tracing` never actually runs here.

## Architecture / Flow

```text
HalalChain.Platform.Api (AiGateway__BaseUrl)
   ↓  X-API-Key
POST /embeddings ─▶ embeddings.py ─▶ resolve_ai_provider_config()
                        ├── openai / anthropic / foundry / openclaw
                        └── local-models ─▶ create_local_models_client()
                                              POST http://local-models:8080/embeddings
POST /classify      ─▶ classifier.py     (deterministic keyword scoring)
POST /rerank        ─▶ reranker.py       (token overlap + phrase bonus)
POST /summarize     ─▶ summarizer.py     (extractive, word-frequency scored)
POST /ingredient-parse  ─▶ ingredient_parser.py
POST /certificate-extract ▶ certificate_extractor.py
POST /rag/add-documents, /rag/search ─▶ vector_store.py ─▶ Qdrant
POST /process/document ▶ document_processor.py
POST /llm/generate  ─▶ llm.py
```

## Dependencies

`halalchain-shared[llm]` plus `anthropic==1.11.0`, `qdrant-client`,
`python-multipart==0.0.32`, `pypdf==6.19.0`, `python-docx==1.2.0`,
`openpyxl==3.1.5`, `beautifulsoup4==4.15.0`, `lxml==6.1.3`, `markdown==3.11`,
`pyjwt==2.15.1`, `cryptography==50.0.2`, `prometheus-client==0.26.0`,
`opentelemetry-{api,sdk,exporter-otlp}==1.45.0`, `python-json-logger==4.2.0`,
`prometheus-fastapi-instrumentator==7.1.0`.

## Interfaces

### HTTP endpoints

| Method | Path | Auth | Notes |
| --- | --- | --- | --- |
| `GET` | `/` | no | Service banner |
| `GET` | `/health/live` | no | Liveness |
| `GET` | `/health/ready` | no | 200/503, reports `vector_store.get_stats()` |
| `GET` | `/metrics` | no | Prometheus text |
| `POST` | `/health` | no | Cache stats + uptime |
| `POST` | `/embeddings` | **yes** | |
| `POST` | `/summarize` | **yes** | `maxTokens` 1–500 |
| `POST` | `/classify` | **yes** | labels ⊆ `{halal, haram, mashbooh, unknown}` |
| `POST` | `/rerank` | **yes** | |
| `POST` | `/ingredient-parse` | **yes** | |
| `POST` | `/certificate-extract` | **yes** | `completeness` as `"n/6 fields extracted"` |
| `POST` | `/rag/add-documents` | **yes** | Body is a bare JSON list |
| `POST` | `/rag/search` | **yes** | **Query parameters** `query`, `top_k` |
| `POST` | `/rag/clear` | **yes** | |
| `POST` | `/llm/generate` | **yes** | **Query parameters** `prompt`, `max_tokens`, `temperature` |
| `POST` | `/cache/clear` | **yes** | |
| `GET` | `/cache/stats` | no | |
| `POST` | `/process/document` | **yes** | **Query parameter** `file_path`; processor errors map to 404/403/400/413 |

Plus FastAPI's `/docs`, `/redoc`, `/openapi.json`.

### Authentication (`src/auth.py`)

`X-API-Key` via `APIKeyHeader`, compared with `hmac.compare_digest`.
`ensure_auth_configured` raises `RuntimeError` at import when the key is empty,
is one of the known placeholders (`""`, `changeme`, `change-me`,
`change-me-minimum-32-chars-secret-key`, `your-api-key`, `test`, `secret`), or
is shorter than 32 characters.

`AI_GATEWAY_DEV_AUTH_BYPASS` disables the check, but only when
`ENVIRONMENT ∈ {development, dev, test, testing}` or `DEMO_MODE` is set. Being in
a dev environment alone does not bypass it.

### Fail-closed model providers

`embeddings.py` resolves a provider from `EMBEDDING_PROVIDER ∈ {openai, foundry,
openclaw, local-models}`. If the provider is unknown or unconfigured it raises
`RuntimeError("No embedding provider is configured")` — there is no silent local
stub. `llm.py` behaves differently: it falls back to `LocalLLMProvider`, a
truncating extractive stub, when credentials are unset.

### Deterministic processing

`classifier.py` uses `HARAM_SIGNALS` (pork, lard, gelatin, alcohol, ethanol,
carmine, rennet, pepsin, blood, cysteine), `MASHBOOH_SIGNALS`, and
`HALAL_SIGNALS`. A haram signal suppresses a halal match by ×0.1 and a mashbooh
match by ×0.5. `reranker.py` scores `token_score*0.7 + phrase_score*0.3`.
`certificate_extractor.py` maps issuer → jurisdiction
(`JAKIM→MY`, `MUI→ID`, `ESMA→AE`, `MUIS→SG`, `BPJPH→ID`).

### Document safety

`DocumentProcessor` requires a configured `document_root` and rejects symlinks,
non-files, traversal outside the root, unsupported extensions, and oversize
uploads. `process_upload` writes a server-generated
`upload-<32hex><ext>` filename rather than trusting the caller's.

### Metrics

`metrics.py` and `observability.py` maintain **two** Prometheus namespaces:
`halalchain_*` (requests, duration, active requests, cache hits/misses) and
`ai_inference_*` (same shape plus `ai_inference_llm_tokens_total{model,direction}`).

## Configuration

`Settings` is pydantic-settings with `env_file=".env"`, `case_sensitive=False`,
`extra="ignore"`; field names are the environment variable names. Notable groups:

- Server: `APP_NAME`, `APP_VERSION`, `PORT`, `LOG_LEVEL`, `ENVIRONMENT`, `DEMO_MODE`
- Auth: `AI_GATEWAY_API_KEY` (aliased to `api_key`), `ENABLE_AUTH`, `JWT_SECRET`, `JWT_ALGORITHM`, `JWT_EXPIRATION`, `AI_GATEWAY_ALLOWED_ORIGINS`, `AI_GATEWAY_DEV_AUTH_BYPASS`
- Cache: `ENABLE_CACHE`, `CACHE_STRATEGY`, `CACHE_MAX_SIZE`, `CACHE_TTL`, `REDIS_URL`
- Embeddings: `EMBEDDING_DIM`, `EMBEDDING_PROVIDER`, `EMBEDDING_MODEL`
- Vector store: `VECTOR_DB_PROVIDER`, `QDRANT_HOST`, `QDRANT_PORT`, `QDRANT_COLLECTION`, `CHROMA_PERSIST_DIR`
- RAG: `RAG_CHUNK_SIZE`, `RAG_CHUNK_OVERLAP`, `RAG_TOP_K`, `RAG_SIMILARITY_THRESHOLD`
- Documents: `DOCUMENT_ROOT`, `UPLOAD_DIR`, `MAX_DOCUMENT_SIZE`, `SUPPORTED_FILE_TYPES`
- Providers: `AI_BACKEND` plus the `OPENAI_*`, `ANTHROPIC_*`, `FOUNDRY_*`, `OPENCLAW_*` families

`LOCAL_MODELS_URL` and `TAWHEED_URL` appear in `Settings` but no code path
reads either.

## Integration

| Direction | Peer | How |
| --- | --- | --- |
| Called by | `HalalChain.Platform.Api` | `AiGateway__BaseUrl` + `AiGateway__ApiKey`, via the `AiInference` / `AiInferenceFallback` HttpClients and `ResilientAiInferenceProvider` |
| Called by | `HalalChain.Platform.Api` | `LocalModels__BaseUrl` |
| Calls | `local-models` | `POST /embeddings` via `halalchain_shared.service_client` when `EMBEDDING_PROVIDER=local-models`. **This is the only live Python-to-Python call in the repository** |
| Called by | `HalalChain-Cli` | `AI_INFERENCE_URL`, health probes and direct calls |

Compose sets `REDIS_URL`; no code path reads it — caching is in-process LRU.

## Development

```bash
pip install -r requirements.txt && pip install -e .
uvicorn src.main:app --host 0.0.0.0 --port 7071 --reload
```

…or `npm run ai:dev` from the repository root, which also installs `_shared`
first and sets `AI_GATEWAY_DEV_AUTH_BYPASS=true`.

## Testing

```bash
python -m pytest
```

In the CI matrix. Coverage:

- `test_auth.py` — fail-closed behaviour for missing, placeholder, and
  short keys; explicit dev bypass; dev environment alone does **not** bypass
- `test_cross_service.py` — a 384-dim vector obtained via `local-models`,
  `/health/live`, `/health`, `/embeddings`, `/classify`, and the
  `RuntimeError` when the provider is `local`
- `test_document_processor.py` — traversal, absolute-path escape, symlink,
  missing file, directory, unsupported extension, oversize, filename sanitisation
- `test_ingredient_parser.py` — risk keyword rules, E-code extraction, summary counts

## Deployment

Image built from `.halalchain/ai-inference/Dockerfile` on
`python:3.11.11-slim-bookworm`, published on host port `7071`, with volume
`ai_inference_documents`. Depends on `redis`, `qdrant`, and `local-models`
being healthy. Build context is `.halalchain/` so `_shared` is available. Tag
pinned in [`service-manifest.yaml`](../../service-manifest.yaml). Helm release
name: `aiInference`.

Runbook: [`docs/runbooks/ai-inference-errors.md`](../../docs/runbooks/ai-inference-errors.md),
[`llm-latency.md`](../../docs/runbooks/llm-latency.md).

## Related Components

- [.halalchain/_shared](../_shared/README.md) — backend resolution, cache, service client
- [.halalchain/local-models](../local-models/README.md) — the embedding provider it calls
- [HalalChain.Platform.Api](../../HalalChain.Platform.Api/README.md) — the primary caller
- [infrastructure/grafana/dashboards/ai-inference-slo.json](../../infrastructure/README.md) — the SLO dashboard
- [HalalChain.Platform.Contracts/AI](../../HalalChain.Platform.Contracts/README.md) — the .NET request/response DTOs

## Notes / Limitations

- `package.json` in this directory references a non-existent `src/server.js`.
- `_shared`'s `AIInferenceClient` sends JSON bodies to `/llm/generate` and
  `/rag/search`, which take query parameters — 422. It is unused today, but the
  mismatch is real.
- Tracing is initialised against a settings attribute that does not exist, so
  `init_tracing` never runs in this service.
- `summarizer.py`'s sentence-splitting regex is `r"[.!?]+s+"`, which looks like a
  typo for `\s+` and will not split sentences as intended.
- `REDIS_URL` is configured but unused; the cache strategy name `"redis"`
  silently resolves to the in-process LRU.