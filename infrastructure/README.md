# `infrastructure/`

Observability stack configuration, dev-only compose overlays, and the repository's
helper scripts.

## Purpose

Two jobs:

1. **Observability** — Prometheus, Grafana, Loki, Tempo, OpenTelemetry
   Collector, and AlertManager configuration, plus the SLO definitions they
   implement.
2. **Local infrastructure** — compose overlays for IPFS and a local Polygon fork,
   and the scripts CI and developers run (SCSS build, docs validation, AlertManager
   secret writing).

## Responsibilities

- Declare scrape targets, recording rules, and SLO alerts for every service.
- Provision Grafana datasources (Prometheus, Loki, Tempo) and eleven dashboards.
- Configure log and trace pipelines, including a tail-sampling policy and a
  PII redaction processor.
- Route alerts to Slack, email, or PagerDuty through AlertManager templates.
- Provide the dev compose overlay that adds IPFS and Anvil without redefining the
  base services.
- Provide the scripts CI invokes: `build-scss.mjs`, `check-docs.py`.

## Structure

```
infrastructure/
├── README.md
├── docker-compose.dev.yml            # Dev overlay: IPFS + optional Anvil (--profile chain)
├── docker-compose.observability.yml   # The observability stack
├── docker/
│   └── Dockerfile.template
├── alertmanager/
│   ├── alertmanager.yml               # Routing tree
│   ├── secrets/.gitignore            # Secrets are gitignored; written at deploy time
│   └── templates/{slack,email}.tmpl
├── grafana/
│   ├── grafana.ini
│   ├── dashboards/                    # platform-overview, api-slo, web-ux,
│   │                                 #   ai-inference-slo, tawheed-slo, circuit-breakers,
│   │                                 #   database, outbox, feature-flags, business-kpis
│   │                                 #   + dashboards.yml (provisioning index)
│   └── datasources/{prometheus,loki,tempo}.yml
├── loki/{loki.yml,rules.yml}
├── otel/
│   ├── agent-config.yml
│   ├── gateway-config.yml
│   └── processors/{redact-pii,resource-enrich,tail-sampling}.yml
├── prometheus/
│   ├── prometheus.yml
│   ├── recording-rules.yml
│   └── rules/{infra,slo-platform-api,slo-ai-inference,slo-tawheed,slo-web}.yml
├── tempo/tempo.yml
└── scripts/
    ├── build-scss.mjs                 # npm run scss:build
    ├── watch-scss.mjs                 # npm run scss:watch
    ├── check-docs.py                  # CI gate: manifest ↔ compose ↔ RUNTIME-MATRIX
    └── write-alertmanager-secrets.py  # Renders the AlertManager secret files
```

> The previous revision of this README described only
> `docker-compose.dev.yml` and `scripts/setup-dev.sh`. It described the
> directory as "Postgres, IPFS, an Anvil Polygon fork", which was true of the
> dev overlay only and missed the entire observability stack. `setup-dev.sh`
> does not live here — it is at
> `HalalChain.Platform.Api/Infrastructure/scripts/setup-dev.sh`.

## Architecture / Flow

```text
platform-api, halalchain, marketplace  ──OTLP──▶ otel/gateway-config.yml
ai-inference, tawheed, agents, local-models ─────▶ │
                                                     ├─▶ Tempo  (traces)
                                                     ├─▶ Loki   (logs)
                                                     └─▶ Prometheus (metrics scrape)
                                                                  │
                                                          recording-rules.yml
                                                          rules/slo-*.yml, rules/infra.yml
                                                                  │
                                                       AlertManager ─▶ Slack / email / PagerDuty
                                                                  │
                                                                Grafana (11 dashboards)
```

## Usage

### Observability stack

```bash
docker compose -f infrastructure/docker-compose.observability.yml up
```

### Dev overlay (IPFS + local chain)

The dev compose file is intentionally separate from the root
`docker-compose.yml`. It exposes IPFS on host port `5011` and starts the local
Polygon fork only under the `chain` profile:

```bash
docker compose -f infrastructure/docker-compose.dev.yml up
docker compose -f infrastructure/docker-compose.dev.yml --profile chain up
```

Per AGENTS.md the recommended invocation layers both files:

```bash
docker compose \
  -f docker-compose.yml \
  -f infrastructure/docker-compose.dev.yml \
  --profile chain up --build
```

The `chain` profile requires `ANVIL_MNEMONIC` and `AMOY_RPC_URL`.

The full local stack (API + UIs + Python services + all infra) is defined at the
root [`docker-compose.yml`](../docker-compose.yml).

## Configuration

### Scripts

| Script | Invoked by | Purpose |
| --- | --- | --- |
| `build-scss.mjs` | `npm run scss:build`; CI `build-and-test`; `Directory.Build.targets` when `SassEnabled=true` | Compiles every `wwwroot/scss/app.scss` / `site.scss` into `wwwroot/css` |
| `watch-scss.mjs` | `npm run scss:watch` | Same, in watch mode |
| `check-docs.py` | CI `build-and-test` | Cross-checks `service-manifest.yaml`, `docker-compose.yml`, and `docs/RUNTIME-MATRIX.md`. Fails on `:latest`, on a bare major tag, on a manifest entry with no compose service, and on a documented port the service does not publish |
| `write-alertmanager-secrets.py` | deploy-time | Renders secret files under `infrastructure/alertmanager/secrets/` from the environment |

`check-docs.py` locates the repository root by searching upward for
`HalalChain.Platform.sln` rather than by a fixed `..` hop — the file moved under
`infrastructure/` and the old relative path silently pointed at a non-existent
file.

### AlertManager secrets

AlertManager does not expand `${VAR}` in its config, so secrets are read from
files. `infrastructure/alertmanager/secrets/` is gitignored; the writer script
creates each file named by a `*_file:` entry in `alertmanager.yml`.

CI (`observability.yml`) substitutes a placeholder Slack webhook, runs
`amtool check-config` against `prom/alertmanager:v0.28.0`, and then asserts that
every `*_file:` path in the config has a matching entry in the writer script.
A missing secrets directory is expected and is not an error.

## Integration

### Producers

- **Metrics:** `HalalChain.Automation` registers `JobTelemetry.MeterName` and
  exports OTLP. The .NET services and Python services expose Prometheus-format
  `/metrics` or FastAPI instrumentators.
- **Traces:** the Python services call `init_tracing` (in `ai-inference` this
  never runs — see its README).
- **Alerts:** `docs/slo.yaml` is the human-facing SLO source;
  `infrastructure/prometheus/rules/slo-*.yml` encodes them as PromQL;
  `docs/runbooks/*.md` is the human response for each.

### Consumers

- `HalalChain.Platform.Tests/Observability/AlertCoverageTests.cs` asserts every
  Prometheus alert rule has a test behind it.

## Development

```bash
npm run scss:build
python3 infrastructure/scripts/check-docs.py
docker compose -f infrastructure/docker-compose.observability.yml config   # validate
```

## Testing

There is no test project for configuration files. Coverage comes from CI:

| Workflow | Job | Checks |
| --- | --- | --- |
| `ci.yml` | `build-and-test` | `npm run scss:build`, then `check-docs.py` |
| `observability.yml` | `validate` | `amtool check-config`, AlertManager secret wiring |

Both trigger on changes under `infrastructure/**`.

## Deployment

AlertManager, Grafana, Loki, Tempo, and the collector are configured here but
deployed outside this repository's Helm chart — `deploy/helm/halalchain`
contains no observability templates. The services' own OTLP endpoints are set
through `OTEL_EXPORTER_OTLP_ENDPOINT`.

## Related Components

- [docs/runbooks](../docs/README.md) — the operational response for each alert
- [docs/slo.yaml](../docs/slo.yaml) — SLO definitions
- [docs/RUNTIME-MATRIX.md](../docs/RUNTIME-MATRIX.md) — service/port inventory that `check-docs.py` validates
- [service-manifest.yaml](../service-manifest.yaml) — frozen image tags that `check-docs.py` validates
- [deploy/](../deploy/README.md) — Helm chart and release runbooks
- [Directory.Build.targets](../Directory.Build.targets) — the MSBuild hook into `build-scss.mjs`
- [HalalChain.Automation](../HalalChain.Automation/README.md) — emits job telemetry into this stack

## Notes / Limitations

- The root `docker-compose.yml` does not reference
  `docker-compose.observability.yml`; you must pass it with `-f`.
- The dev overlay deliberately does **not** redefine `postgres`, `redis`,
  `qdrant`, or `neo4j`. Dropping a `docker-compose.override.yml` next to the root
  file is the intended way to mutate those.
- `HalalChain.Platform.Api/Infrastructure/docker-compose.dev.yml` is a
  near-duplicate of this directory's `docker-compose.dev.yml`. Two copies of the
  same overlay exist; only one of them is referenced by AGENTS.md.
- `ai-inference` reports no traces because its tracing initialisation reads a
  settings attribute that does not exist.