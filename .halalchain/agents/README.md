# `.halalchain/agents`

Python 3.11 FastAPI agent orchestration service, plus the evaluation DAG that
scores agent traces. Mapped to host port `8081` in the local stack.

> **Status: cannot start as committed.** See "Known breakage" below.

## Purpose

Two things live here:

1. An HTTP service that runs named evidence-collection workflows
   (`supplier_onboarding`, `certificate_review`, `product_verification`) and
   returns evidence proposals.
2. An evaluation harness — node scorers, greedy-parent root-cause attribution,
   a failure taxonomy, and golden datasets — that scores those workflows.

The .NET half of the same runtime is [`HalalChain.Agents`](../../HalalChain.Agents/README.md),
which provides `IAgentWorkflow` over HTTP plus the .NET evaluation DAG. The
failure taxonomy and handoff field list are duplicated across the two runtimes
deliberately, and must be changed together.

## Structure

```
.halalchain/agents/
├── pyproject.toml       # halalchain-agents; extras: runtime, vector, eval
├── Dockerfile           # python:3.11.11-slim-bookworm, python -m halalchain_agents.main
└── app/
    ├── halalchain_agents/
    │   ├── __init__.py
    │   └── main.py      # FastAPI app factory + module-level app
    └── eval/
        ├── dag/
        │   ├── trace_loader.py    # AgentTrace, BlobStore protocol, TraceLoader
        │   ├── node_scorers.py    # 7 scorers + SCORERS registry
        │   ├── propagation.py     # EvalResult, greedy_parent_attribution
        │   └── taxonomy.py        # FailureCategory (21 members)
        ├── metrics/
        │   ├── handoff_contract.py
        │   └── explanation_faithfulness.py
        ├── runners/
        │   ├── ci_runner.py       # Threshold-gated CI evaluation
        │   └── shadow_runner.py   # Periodic background evaluation
        └── goldens/
            ├── certificate_review.jsonl
            └── supplier_onboarding.jsonl
```

`app/agents/`, `app/runtime/`, `app/workflows/`, and `app/tests/` exist on disk
but contain **only `__pycache__` directories and no `.py` files**.

> **There is no `src/`, no `requirements.txt`, and no `app/tests/` test
> module.** The previous revision of this README documented `src/main.py`,
> `auth.py`, `cache.py`, `config.py`, `health.py`, `orchestrator.py`,
> `task_queue.py`, `evaluation_client.py`, and three test files. None exist.

## Architecture / Flow

```text
HalalChain.Agents/Runtime/AgentWorkflowClient.cs
   │  POST {Agents:BaseUrl}/api/v1/workflows/{workflow}/run
   ▼
GET /api/v1/workflows            → ["supplier_onboarding","certificate_review",
POST /api/v1/workflows/{w}/run      "product_verification"]
   ↓  (would call) agents.base, agents.collection, runtime.dag, workflows  ← missing
   ↓  returns EvidenceProposal objects — no verdict field, by design

Separately: eval/runners/*.py  →  TraceLoader  →  BlobStore  →  NodeScorers
                                            →  greedy_parent_attribution  →  EvalReport
```

## Dependencies

Core: `halalchain-shared` (workspace).

| Extra | Adds |
| --- | --- |
| `runtime` | `mcp>=2.2.0,<3`, `mcp-types`, `haiku-skills[signing]>=0.18.1`, `agent-skills>=1.1.1` |
| `vector` | `qdrant-client[fastembed]>=1.15.0` |
| `eval` | `deepeval>=4.2.0,<5.0.0` |

`pyproject.toml` carries a comment explaining that `ragas` is intentionally
excluded (unmaintained; SSRF advisory PYSEC-2026-3046) and that its four RAGAS
metrics are reimplemented as DeepEval metrics. `HalalChain.Architecture.Tests`
enforces the .NET half of that decision with
`AgentsAssembly_ShouldNotReference_Ragas` and `EvalExtra_NotInProductionDockerfile`.

## Interfaces

### HTTP endpoints

| Method | Path | Notes |
| --- | --- | --- |
| `GET` | `/health/live` | `{status, service, version}` |
| `GET` | `/health/ready` | Same body as liveness |
| `GET` | `/` | Service banner |
| `GET` | `/api/v1/workflows` | Hard-coded allow-list of three workflow names |
| `POST` | `/api/v1/workflows/{workflow}/run` | Body `{workflow, input}`. 404 for an unknown workflow |

No route enforces authentication.

The response envelope deliberately has **no verdict-shaped field**. `main.py`'s
docstring states this explicitly, and it is the service-level expression of the
platform invariant.

### Evaluation

| Module | Public surface |
| --- | --- |
| `dag/trace_loader.py` | `AgentTrace`, `BlobStore` protocol (`read` required, `list` optional), `TraceLoader(blob_store, on_error=…)`, `InMemoryBlobStore`. Key layout: `traces/{workflow_name}/{trace_id}.json` |
| `dag/node_scorers.py` | `NodeScore`, `NodeScorer` ABC, and `CollectorScorer` (recall ≥0.8), `ClassifierScorer` (accuracy ≥0.9), `VerifierScorer` (exact), `HandoffScorer` (required fields `product_id`, `certificate`, `ingredients`, `manufacturer`), `GapScorer` (F1 ≥0.7), `RecollectionScorer` (reads `loop_closed`), `VerdictScorer` (exact match). Plus `SCORERS` and `get_scorer(node_type)` |
| `dag/propagation.py` | `EvalResult`, `greedy_parent_attribution(trace, node_scores)` (backward walk to the first failed ancestor), `aggregate_taxonomy(results)` |
| `dag/taxonomy.py` | `FailureCategory` — 21 members across collector/classifier/verifier/handoff/gap/recollection/verdict/infra groups |
| `runners/ci_runner.py` | `CIRunner(blob_store, goldens_dir)`, `load_goldens`, `evaluate_trace`, `assert_no_regression` |
| `runners/shadow_runner.py` | `ShadowRunner(blob_store, goldens_dir, interval_minutes=60, lookback_hours=24)`, `run_once`, `start`, `stop`. Writes `eval/traces/{trace_id}/{node_id}.json` |

`deepeval` is imported lazily inside `HandoffScorer`, but **eagerly** at module
level in `eval/metrics/handoff_contract.py`.

## Configuration

| Variable | Default | Read by |
| --- | --- | --- |
| `PORT` | `8080` | `main.py` `__main__` block |
| `LOG_LEVEL` | `INFO` | `main.py` |
| `AGENTS_MAX_STEPS` | `20` | `main.py` |
| `AGENTS_MAX_WALL_SECONDS` | `300` | `main.py` |

Compose additionally sets `ENVIRONMENT`, `REDIS_URL`, `AI_INFERENCE_URL`,
`TAWHEED_URL`, and `LOCAL_MODELS_URL` — **no present source file reads any of
them.**

## Integration

| Direction | Peer | How |
| --- | --- | --- |
| Called by | `HalalChain.Agents/Runtime/AgentWorkflowClient.cs` | `POST {Agents:BaseUrl}/api/v1/workflows/{workflow}/run`. This path matches exactly. `AddAgentRuntime` throws if `Agents:BaseUrl` is unset, so the API cannot start without it |
| Called by | `HalalChain-Cli` | `AGENTS_URL` env var (config key `agents.url`, default `http://localhost:8081`), health probes |
| Calls out | nothing | Declared URLs are unread |

The evaluation DAG mirrors [`HalalChain.Agents`](../../HalalChain.Agents/README.md)
node-for-node: the same 7 scorer concepts, the same greedy-parent attribution,
and a failure taxonomy whose count is pinned at 21 on both sides so reports can
be diffed by wire name.

## Development

The README in earlier revisions said:

```bash
pip install -r requirements.txt
uvicorn src.main:app --host 0.0.0.0 --port 8081 --reload
```

**Neither works** — there is no `requirements.txt` and no `src/main.py`. The
Dockerfile runs `python -m halalchain_agents.main`, which is also inconsistent
with `COPY agents/app ./app` (the package lands at `/app/app/halalchain_agents`
and is not importable from `/app`).

## Testing

**No runnable tests.** `pyproject.toml` declares `testpaths = ["tests"]` but
the tests directory is `app/tests`, which does not exist. `.halalchain/agents`
is not in the CI `python-tests` matrix, and there is no `conftest.py` anywhere
under `.halalchain`.

`ci_runner.py` embeds two `@pytest.mark.asyncio` tests (`supplier_onboarding`
and `certificate_review`) that depend on `blob_store` and `goldens_dir` fixtures
which are not defined in that file and have no `conftest.py` to supply them.

Both runners also import `from .trace_loader import …` and
`from .node_scorers import …`, but those modules live in `eval/dag/`, not
`eval/runners/` — so the relative imports do not resolve.

## Deployment

Image built from `.halalchain/agents/Dockerfile`, mapped to host port `8081`.
It depends on `redis`, `ai-inference`, `tawheed`, and `local-models` being
healthy, but reads none of their URLs. Tag pinned in
[`service-manifest.yaml`](../../service-manifest.yaml).

## Related Components

- [HalalChain.Agents](../../HalalChain.Agents/README.md) — the .NET half: runtime client, budget ledger, evaluation DAG
- [.halalchain/_shared](../_shared/README.md)
- [.halalchain/tawheed](../tawheed/README.md) — where workflow evidence ends up
- [HalalChain.Platform.Api](../../HalalChain.Platform.Api/README.md) — registers `AddAgentRuntime`
- [HalalChain.Agents.Tests](../../HalalChain.Agents.Tests/README.md) — the .NET evaluation tests
- [docs/architecture/workspace-health.md](../../docs/architecture/workspace-health.md)

## Notes / Limitations — known breakage

1. **Cannot import.** `app/halalchain_agents/main.py` imports `agents.base`,
   `agents.collection`, `runtime.dag`, and `workflows`. None of those packages
   exist as source.
2. **Docker CMD mismatch.** `python -m halalchain_agents.main` does not match
   the `COPY agents/app ./app` layout.
3. **Broken relative imports** in both runners (`eval/dag/*` vs `eval/runners/*`).
4. **No tests, no CI job.**
5. **Goldens mismatch.** `goldens/*.jsonl` use the key `expected_loop_closed`
   while `RecollectionScorer` reads the node output key `loop_closed`.
6. **Missing golden.** `ShadowRunner` sweeps `product_verification`, but there
   is no `goldens/product_verification.jsonl`.

Until items 1–3 are resolved, this service is present in `docker-compose.yml`
but cannot answer a request.