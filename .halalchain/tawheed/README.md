# `.halalchain/tawheed`

Python 3.11 FastAPI evidence service and **deterministic Policy Engine**. Served
on port `8000` in the local stack. It collects evidence with rule-based agents,
applies policy rules, and outputs compliance decisions without any LLM
involvement in verdict assignment.

## Purpose

This is the platform's compliance authority. It is the only component in the
repository that produces a `ComplianceStatus`. The .NET API forwards evidence
here precisely because it must not decide compliance itself.

## Responsibilities

- Run four rule-based evidence agents (ingredient, certificate, supplier,
  document) and aggregate their signals.
- Compute a risk score from agent confidence and failure ratio.
- Evaluate evidence against versioned, jurisdiction-scoped policy thresholds
  and emit a verdict with reason codes.
- Accept signed IoT sensor observations with replay and clock-skew rejection.
- Emit determinism fingerprints and Prometheus metrics for every verdict.

## Structure

```
.halalchain/tawheed/
├── pyproject.toml       # Project metadata + observability/llm/dev extras
├── requirements.txt     # Thin include: -r ../requirements/tawheed.txt
├── Dockerfile           # python:3.11.11-slim-bookworm, uvicorn, non-root 1001
├── src/
│   ├── main.py          # FastAPI app: title, CORS, tracing, /health
│   ├── observability.py # Prometheus metrics, tracing, JSON logging,
│   │                    #   _fingerprint determinism guard
│   ├── api/
│   │   ├── routes.py    # APIRouter(prefix="/v1"), module-level singletons
│   │   └── schemas.py   # VerifyProductResponse and friends
│   ├── core/
│   │   ├── config.py    # Settings + secret validation, @lru_cache
│   │   ├── llm.py       # get_llm_config / get_llm_client (no call sites today)
│   │   └── models.py    # ComplianceStatus, EvidenceSignals, RiskSignals,
│   │                    #   VerificationResult
│   ├── policy/
│   │   └── engine.py    # evaluate() — the deterministic Policy Engine
│   ├── agents/
│   │   ├── base.py           # BaseAgent ABC, AgentEvidence
│   │   ├── orchestrator.py   # AgentOrchestrator, AllAgentsFailed
│   │   ├── ingredient_agent.py
│   │   ├── certificate_agent.py
│   │   ├── supplier_agent.py
│   │   └── document_agent.py
│   └── adapters/
│       └── iot.py       # SensorObservation, ObservationVerifier, ObservationStore
└── tests/
    ├── test_config.py
    ├── test_policy_engine.py
    ├── test_verdict_invariant.py
    ├── test_orchestrator.py
    └── test_iot_observations.py
```

> The previous revision of this README described `auth.py`, `cache.py`,
> `health.py`, `policy_engine.py`, `evidence_collector.py`,
> `evidence_store.py`, `decision_logger.py`, and a "REGO/OPA" engine. **None of
> those files exist.** The layout above is what is committed.

## Architecture / Flow

```text
POST /v1/products/{product_id}/verify
   ↓
AgentOrchestrator  ── asyncio.gather(return_exceptions=True) ──▶
   ├─ IngredientAgent   → ingredient_score   (HARAM/MASHBOOH keywords, E-codes)
   ├─ CertificateAgent  → certificate_score  (JAKIM, MUI, BPJPH, MUIS, ESMA, GSO,
   │                                              IFANCA, HFA)
   ├─ SupplierAgent     → supplier_score     (known halal countries)
   └─ DocumentAgent     → document_score + traceability
   ↓  aggregate: per-signal max(), risk = min(1.0, (1-avg_confidence)*0.7 + failure_ratio*0.3)
policy/engine.evaluate(product_id, evidence, risk, missing_evidence, policy_version, jurisdiction)
   ↓
VerificationResult  →  _fingerprint()  →  tawheed_verdicts_total{verdict}
```

## Dependencies

Core: `halalchain-shared` (workspace).

| Extra | Adds | Why |
| --- | --- | --- |
| `observability` | `prometheus-client==0.26.0`, `opentelemetry-api/sdk/exporter-otlp==1.45.0`, `python-json-logger==4.2.0` | The project file marks these **boot-required, not optional** — `main.py` imports them at module scope |
| `llm` | `halalchain-shared[llm]` | No current call site |
| `dev` | `pytest==8.4.0`, `pytest-asyncio==0.24.0`, `pytest-httpx==0.35.0`, `ruff`, `mypy`, `coverage` | Tests |

The image lock is `requirements/tawheed.txt` — 46 packages.

## Interfaces

### HTTP endpoints

| Method | Path | Notes |
| --- | --- | --- |
| `GET` | `/health` | `{status, service, demo_mode, iot_enabled, policy_version, jurisdiction}` |
| `POST` | `/v1/products/{product_id}/verify` | Returns `{result: VerificationResult}`. The same code path runs whether `DEMO_MODE` is true or false |
| `GET` | `/v1/agents/health` | Currently returns `{name: True}` for every agent — it does not probe them |
| `GET` | `/v1/policies` | `{"policies": ["MY-v3", "ID-v1"]}` |
| `POST` | `/v1/evidence/sensor-observations` | 202 on accept; 503 when `IOT_ENABLED` is false; 422 on `ObservationRejected` |

Plus FastAPI's `/docs`, `/redoc`, and `/openapi.json`.

> **There is no `/health/live` or `/health/ready`**, despite the previous
> revision of this README claiming both. Docker Compose health-checks
> `/health`, which is correct.

> **No route enforces authentication.** `JWT_SECRET` is validated at startup,
> but no request path checks a token or an API key.

### Policy Engine

```python
POLICY_VERSIONS = {
  "MY-v3": {min_cert: .8, min_ingredient: .7, min_supplier: .6, max_risk: .65},
  "ID-v1": {same},
}
```

Evaluation order in `policy/engine.evaluate`:

1. Any `missing_evidence` → `INCOMPLETE` / `MISSING_MANDATORY_EVIDENCE`
2. `risk.overall_risk >= RISK_THRESHOLD_HOLD` → `HOLD` / `HIGH_RISK_SCORE`
3. Threshold breaches append `CERTIFICATE_INSUFFICIENT`,
   `INGREDIENT_SOURCE_UNVERIFIED`, `SUPPLIER_UNVERIFIED` → `NON_COMPLIANT` when
   `certificate_verification == 0`, else `MANUAL_REVIEW`
4. Otherwise → `VERIFIED`

`ComplianceStatus` defines six members — `VERIFIED`, `MANUAL_REVIEW`,
`INCOMPLETE`, `HOLD`, `NON_COMPLIANT`, `UNVERIFIED` — but `evaluate` never
produces `UNVERIFIED`.

### Metrics

`tawheed_verdicts_total{verdict}`, `tawheed_verdict_latency_ms{verdict}`,
`tawheed_evidence_errors{agent}`, `tawheed_determinism_violation{subject}`,
`tawheed_iot_observations_accepted_total{metric}`,
`tawheed_iot_observations_rejected_total{reason}`.

The determinism guard caches a fingerprint per key and raises
`RuntimeError("DETERMINISM VIOLATION: …")` if the value for that key changes.
As wired it is called as `_fingerprint(result, result)`, so key and value both
derive from the same object and it cannot detect drift between two calls.
The `test_verdict_invariant.py` structural test, not this guard, is what
currently protects determinism.

## Configuration

Read via `Settings` (pydantic-settings, `env_file=".env"`, `extra="ignore"`).
Names include: `DEMO_MODE`, `ENVIRONMENT`, `POSTGRES_URL`, `REDIS_URL`,
`QDRANT_URL`, `QDRANT_COLLECTION`, `NEO4J_URI`, `NEO4J_USER`, `NEO4J_PASSWORD`,
`DEFAULT_POLICY_VERSION`, `DEFAULT_JURISDICTION`, `RISK_THRESHOLD_MANUAL_REVIEW`,
`RISK_THRESHOLD_HOLD`, `IOT_ENABLED`, `IOT_DEVICE_KEYS_JSON`,
`IOT_MAX_CLOCK_SKEW_SECONDS`, `IOT_OBSERVATION_STORE_PATH`, `JWT_SECRET`,
`LLM_PROVIDER`, `OTLP_ENDPOINT`. `main.py` additionally reads
`TAWHEED_ALLOWED_ORIGINS` (default `http://localhost:5001,http://localhost:5200,http://localhost:5201`);
a wildcard origin raises `RuntimeError`.

Secret validation, mirroring the .NET API's startup checks:

- `JWT_SECRET` must be ≥32 characters and not a placeholder outside development.
- `NEO4J_PASSWORD` is rejected if it is `tawheed123`, `neo4j`, `password`, or
  `admin` outside development.
- When `IOT_ENABLED` is true outside development, `IOT_DEVICE_KEYS_JSON` must
  define at least one device with a ≥32-character key.

> `RISK_THRESHOLD_MANUAL_REVIEW` is configured but not read by `evaluate`.

## Integration

| Direction | Peer | How |
| --- | --- | --- |
| Called by | `HalalChain.Platform.Api` | `Tawheed__BaseUrl` → `TawheedHttpClient`. **Path mismatch**: the client posts to `POST /api/v1/verify`; tawheed serves `POST /v1/products/{product_id}/verify`, and the client's response DTO expects a flat object where tawheed returns `{"result": …}` |
| Called by | `_shared`'s `TawheedClient` | Declared but has no in-tree caller; two of its three routes do not exist |
| Calls out | nobody | No outbound HTTP. Reads Postgres/Redis/Qdrant/Neo4j URLs but uses none of them in the present code |
| Called by | `HalalChain-Cli` | `TAWHEED_URL`, used for health probes |

Compose supplies `LOCAL_MODELS_URL` and `AI_INFERENCE_URL` to this service;
no source file reads either.

## Development

```bash
pip install -r requirements.txt && pip install -e .
uvicorn src.main:app --host 0.0.0.0 --port 8000 --reload
```

…or `npm run tawheed:dev` from the repository root, or
`docker compose up --build`.

## Testing

```bash
python -m pytest
```

In the CI matrix (`ai-inference`, `tawheed`, `local-models`).

- `test_config.py` — production rejects empty/placeholder/short `JWT_SECRET`,
  dev permits placeholders, default `NEO4J_PASSWORD` rejected, IoT key validation
- `test_policy_engine.py` — one case per verdict: `VERIFIED`, `MANUAL_REVIEW`,
  `INCOMPLETE`, `HOLD`, `NON_COMPLIANT`
- `test_verdict_invariant.py` — the structural guarantee that LLM-shaped strings
  cannot cast to `ComplianceStatus`, and that `AgentOrchestrator`'s source never
  mentions `ComplianceStatus`
- `test_orchestrator.py` — total failure raises `AllAgentsFailed`; partial
  failures are preserved and affect risk
- `test_iot_observations.py` — signed observation accepted once; tampered,
  stale, and replayed rejected; JSONL persistence round-trips

## Deployment

Container image built from `.halalchain/tawheed/Dockerfile`, published on host
port `8000`, with volume `tawheed_evidence:/var/lib/tawheed/evidence`. The Helm
chart deploys it as the `tawheed` release. Image tag pinned in
[`service-manifest.yaml`](../../service-manifest.yaml).

Runbook: [`docs/runbooks/tawheed-latency.md`](../../docs/runbooks/tawheed-latency.md),
[`tawheed-unavailable.md`](../../docs/runbooks/tawheed-unavailable.md),
[`determinism-violation.md`](../../docs/runbooks/determinism-violation.md).

## Related Components

- [HalalChain.Platform.Api/Modules/Hal](../../HalalChain.Platform.Api/README.md) — forwards evidence here, never decides
- [.halalchain/_shared](../_shared/README.md) — LLM backend resolution (unused by the current code path)
- [.halalchain/agents](../agents/README.md) — the sibling agent runtime
- [DECISION-CONTRACT.md](../../docs/DECISION-CONTRACT.md) — the invariant this service implements
- [ADR-005: verdict binding is not a boolean](../../docs/adr/005-verdict-binding-not-boolean.md)

## Notes / Limitations

- No route is authenticated. Anyone who can reach port 8000 can request a
  verdict.
- `AgentOrchestrator` returns a 5-tuple but `api/routes.py` unpacks only 4
  values.
- `core/llm.py` (`get_llm_config`, `get_llm_client`) has no call site; the
  agents are entirely rule-based today.
- `GET /v1/agents/health` always reports healthy.
- The determinism guard cannot detect the drift it exists to detect, as wired.