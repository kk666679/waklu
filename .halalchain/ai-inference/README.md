# `.halalchain/ai-inference`

Python 3.11 FastAPI AI inference gateway. Served on port `7071` in the local
stack. Acts as the single entry point through which the .NET API talks to
embeddings, classifiers, rerankers, summarizers, LLMs, and vector stores.

## Layout

```
.halalchain/ai-inference/
├── pyproject.toml       # Python project metadata
├── requirements.txt     # Pinned dependencies
├── Dockerfile           # python:3.11-slim image, uvicorn entrypoint
├── package.json         # Optional Node smoke-harness manifest (for src/server.js)
└── src/
    ├── main.py          # FastAPI app + uvicorn entrypoint
    ├── server.js        # Optional Node smoke harness mirroring the Python API surface
    ├── auth.py          # API-key / token validation
    ├── cache.py         # Redis-backed response cache
    ├── certificate_extractor.py
    ├── classifier.py    # Halal-vs-not classifiers
    ├── config.py
    ├── document_processor.py
    ├── embeddings.py
    ├── health.py        # /health/live, /health/ready
    ├── ingredient_parser.py
    ├── llm.py           # Multi-provider LLM (OpenAI, Anthropic, Foundry, OpenClaw/Ollama)
    ├── metrics.py
    ├── models.py
    ├── reranker.py
    ├── summarizer.py
    └── vector_store.py  # Qdrant adapter
```

> The `tests/` directory referenced from `pyproject.toml` is not yet present.
> The `testpaths` setting is a forward declaration.

## Local development

The production entrypoint is the Python `uvicorn` command in the `Dockerfile`:

```bash
pip install -r requirements.txt
uvicorn src.main:app --host 0.0.0.0 --port 7071 --reload
```

The `package.json` is kept only for the optional Node-based smoke harness
(`src/server.js`) that mirrors a small subset of the Python API surface. Run
it from the repo root with:

```bash
npm run ai:dev
```

## Health

- `GET /health/live` — liveness
- `GET /health/ready` — readiness
