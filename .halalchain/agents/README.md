# `.halalchain/agents`

Python 3.11 FastAPI agent orchestration service. Served on port `8081` in the local stack. Responsible for evidence collection workflows, agent lifecycle management, and coordinating with the evaluation DAG for deterministic scoring.

## Layout

```
.halalchain/agents/
├── pyproject.toml       # Python project metadata
├── requirements.txt     # Pinned dependencies
├── Dockerfile           # python:3.11-slim image, uvicorn entrypoint
├── src/
│   ├── main.py          # FastAPI app + uvicorn entrypoint
│   ├── auth.py          # API-key / token validation
│   ├── cache.py         # Redis-backed response cache
│   ├── config.py
│   ├── health.py        # /health/live, /health/ready
│   ├── orchestrator.py  # Agent workflow orchestration
│   ├── evidence_collector.py
│   ├── task_queue.py
│   └── evaluation_client.py
├── app/
│   ├── agents/          # Agent definitions and configurations
│   ├── runtime/         # Agent execution environment
│   ├── workflows/       # Predefined evidence collection workflows
│   └── eval/            # Evaluation DAG integration (shared with HalalChain.Agents .NET library)
│       ├── dag/         # Directed acyclic graph for scoring
│       ├── metrics/     # Evaluation metrics and scoring functions
│       ├── runners/     # Evaluation execution strategies
│       └── goldens/     # Golden test datasets
└── tests/
    ├── test_orchestrator.py
    ├── test_evidence_collection.py
    └── test_evaluation_integration.py
```

## Local development

The production entrypoint is the Python `uvicorn` command in the `Dockerfile`:

```bash
pip install -r requirements.txt
uvicorn src.main:app --host 0.0.0.0 --port 8081 --reload
```

## Health

- `GET /health/live` — liveness
- `GET /health/ready` — readiness

## Agent Orchestration

This service manages the lifecycle of AI agents that collect evidence for halal compliance verification. Agents are specialized for different evidence types (certificate verification, ingredient analysis, manufacturing process review, etc.) and work together through defined workflows.

Key features:
- Agent lifecycle management (spawn, monitor, retire)
- Workflow orchestration and sequencing
- Evidence validation and sanitization
- Integration with evaluation DAG for scoring
- Fault tolerance and retry mechanisms