# `.halalchain/local-models`

Python 3.11 FastAPI local model inference service. Served on port `8080` in the local stack. Provides direct access to locally-hosted machine learning models for embeddings, classification, and other AI tasks without relying on external APIs.

## Layout

```
.halalchain/local-models/
├── pyproject.toml       # Python project metadata
├── requirements.txt     # Pinned dependencies
├── Dockerfile           # python:3.11-slim image, uvicorn entrypoint
├── src/
│   ├── main.py          # FastAPI app + uvicorn entrypoint
│   ├── auth.py          # API-key / token validation
│   ├── cache.py         # Redis-backed response cache
│   ├── config.py
│   ├── health.py        # /health/live, /health/ready
│   ├── model_loader.py  # Dynamic model loading/unloading
│   ├── embedding_model.py
│   ├── classification_model.py
│   └── inference_api.py
├── models/              # Local model storage (mounted volume)
│   ├── embeddings/
│   ├── classifiers/
│   └── multimodal/
└── tests/
    ├── test_model_loading.py
    ├── test_embedding_inference.py
    └── test_classification_inference.py
```

## Local development

The production entrypoint is the Python `uvicorn` command in the `Dockerfile`:

```bash
pip install -r requirements.txt
uvicorn src.main:app --host 0.0.0.0 --port 8080 --reload
```

## Health

- `GET /health/live` — liveness
- `GET /health/ready` — readiness

## Model Management

This service hosts and serves machine learning models locally, enabling offline operation and reducing dependency on external API providers. Models are loaded from a persistent volume and can be updated without service downtime.

Key features:
- Dynamic model loading/unloading
- GPU acceleration support (CUDA)
- Model versioning and A/B testing
- Input/output validation and preprocessing
- Performance monitoring and metrics