# `.halalchain/tawheed`

Python 3.11 FastAPI service implementing evidence-driven multi-agent halal
verification plus a deterministic Policy Engine. Served on port `8000` in the
local stack.

Tawheed never relies on an LLM to assign or override a halal verdict. Its
agents collect **evidence**; the Policy Engine in `src/policy/engine.py`
applies the rule set and produces the final status.

## Layout

```
.halalchain/tawheed/
├── pyproject.toml
├── requirements.txt
├── Dockerfile           # python:3.11-slim, uvicorn entrypoint
├── src/
│   ├── main.py          # FastAPI app + uvicorn entrypoint
│   ├── core/            # Config, LLM (provider-agnostic), shared models
│   ├── agents/          # Evidence-collecting agents
│   │   ├── base.py
│   │   ├── certificate_agent.py
│   │   ├── document_agent.py
│   │   ├── ingredient_agent.py
│   │   ├── supplier_agent.py
│   │   └── orchestrator.py
│   ├── policy/
│   │   └── engine.py    # Deterministic verdict engine
│   ├── api/             # routes.py, schemas.py
│   └── adapters/        # Reserved for external-system adapters (Neo4j, etc.)
└── tests/
    ├── __init__.py
    └── test_policy_engine.py
```

## Architectural invariant

`policy/engine.py` is the only place in the system that issues halal
verdicts. LLM outputs (via `core/llm.py`) are treated strictly as evidence —
they are passed into the engine alongside structured inputs but never
short-circuit the rule evaluation.

## Local development

```bash
pip install -r requirements.txt
DEMO_MODE=true uvicorn src.main:app --host 0.0.0.0 --port 8000 --reload
```

## Run tests

```bash
pytest
```
