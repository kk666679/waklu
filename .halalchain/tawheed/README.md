# `.halalchain/tawheed`

Python 3.11 FastAPI evidence service and deterministic Policy Engine. Served on port `8000` in the local stack. Receives evidence from agents, applies policy rules, and outputs compliance decisions without LLM involvement in verdict assignment.

## Layout

```
.halalchain/tawheed/
├── pyproject.toml       # Python project metadata
├── requirements.txt     # Pinned dependencies
├── Dockerfile           # python:3.11-slim image, uvicorn entrypoint
├── src/
│   ├── main.py          # FastAPI app + uvicorn entrypoint
│   ├── auth.py          # API-key / token validation
│   ├── cache.py         # Redis-backed response cache
│   ├── config.py
│   ├── health.py        # /health/live, /health/ready
│   ├── policy_engine.py # Deterministic policy evaluation (REGO/OPA)
│   ├── evidence_collector.py
│   ├── evidence_store.py
│   └── decision_logger.py
└── tests/
    ├── test_policy_engine.py
    └── test_evidence_collection.py
```

## Local development

The production entrypoint is the Python `uvicorn` command in the `Dockerfile`:

```bash
pip install -r requirements.txt
uvicorn src.main:app --host 0.0.0.0 --port 8000 --reload
```

## Health

- `GET /health/live` — liveness
- `GET /health/ready` — readiness

## Policy Engine

The policy engine uses deterministic rules to evaluate evidence and produce halal/compliance verdicts. It never uses LLMs for verdict assignment, ensuring transparency and auditability.

Key features:
- Rule-based evaluation (JSON/logic rules)
- Evidence weighting and aggregation
- Tamper-evident decision logging
- Hierarchical policy inheritance