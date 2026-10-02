# `deploy/`

Kubernetes Helm chart and operational release runbooks.

## Purpose

This directory defines how the platform is released: a provider-agnostic Helm
chart implementing blue/green and canary rollouts, the property tests that prove
the chart renders correctly, and the runbooks an operator follows when a
deployment goes wrong.

It is the deployment counterpart to [`infrastructure/`](../infrastructure/README.md),
which configures observability and local compose overlays.

## Responsibilities

- Render Deployments, Services, an Ingress, and HPAs for the five deployable
  services.
- Support blue/green slot switching for `halalchain` and `marketplace`.
- Support a canary deployment for `platform-api`.
- Provide `helm lint`-able property tests for the chart's invariants.
- Document rollback, incident response, and database migration recovery.

## Structure

```
deploy/
├── DEPLOYMENT-STRATEGY.md            # Why Helm; release model; promotion flow
├── helm/halalchain/
│   ├── Chart.yaml                    # apiVersion v2, version 0.1.0, appVersion 1.0.0
│   ├── values.yaml                   # See the key table below
│   ├── templates/
│   │   ├── _helpers.tpl
│   │   ├── deployment.yaml           # All five services
│   │   ├── deployment-bluegreen.yaml # halalchain + marketplace slots
│   │   ├── deployment-canary.yaml    # platform-api canary
│   │   ├── service.yaml
│   │   ├── ingress.yaml
│   │   └── hpa.yaml
│   └── tests/
│       ├── prop01-five-deployments.sh      # Five Deployments render for any valid values
│       ├── prop06-service-selector-slot.sh # Service selector tracks the active slot
│       └── prop10-latest-tag-fails.sh      # A `latest` tag is rejected
└── runbooks/
    ├── rollback.md
    ├── incident-response.md
    └── db-migration-recovery.md
```

## Architecture / Flow

```text
make deploy NAMESPACE=<ns> TAG=<sha>
   ↓
helm upgrade --install halalchain ./deploy/helm/halalchain \
   --set platformApi.image.tag=TAG --set web.image.tag=TAG \
   --set marketplace.image.tag=TAG --set aiInference.image.tag=TAG \
   --set tawheed.image.tag=TAG --wait --timeout 5m
   ↓
Kubernetes: Deployment + Service + Ingress + HPA per service
   ↓
make smoke-test BASE_URL=<url>   →  5001/health/ready, 5200/health/live, 5201/health/live
   ↓
make promote ACTIVE_SLOT=green   |   make canary-promote STABLE_TAG=<sha>
```

## Dependencies

`helm` 3.x. No cloud provider is assumed — the chart is deliberately portable.

The five deployable services map to images pinned in
[`service-manifest.yaml`](../service-manifest.yaml): `platform-api`, `halalchain`
(web), `marketplace`, `ai-inference`, `tawheed`.

**Not deployed:** `agents`, `local-models`, `HalalChain.Automation`, and
`HalalChain.Mcp`. `values.yaml` has no key for any of them.

## Interfaces

### Chart values

| Key | Purpose |
| --- | --- |
| `replicaCount` | Global default (2) |
| `image` | Global image settings |
| `platformApi`, `web`, `marketplace`, `aiInference`, `tawheed` | Per-service image, ports, env, probes |
| `ingress` | `enabled` (false), `className` (nginx), `annotations` |
| `blueGreen` | `enabled` (false), `activeSlot` (blue), `previewTag` |
| `canary` | `enabled` (false), `replicaCount` (1), `imageTag` |
| `resources` | Global limits 500m/512Mi, requests 250m/256Mi |

Blue/green covers `halalchain` and `marketplace` only; the canary covers
`platform-api` only. Both are off by default.

### Make targets (`Makefile` at the repository root)

| Target | Required variables | Effect |
| --- | --- | --- |
| `make deploy` | `NAMESPACE`, `TAG` | `helm upgrade --install` for all five services, `--wait --timeout 5m` |
| `make rollback` | `NAMESPACE`, `REVISION` | `helm rollback --wait` |
| `make smoke-test` | `BASE_URL` | Probes `5001/health/ready`, `5200/health/live`, `5201/health/live`; prints PASS/FAIL |
| `make promote` | `NAMESPACE`, `ACTIVE_SLOT`, `TAG` | Switches the Service selector via `--reuse-values` |
| `make canary-promote` | `NAMESPACE`, `STABLE_TAG` | Disables the canary and promotes its tag |

Each target uses a `$(call require,VAR)` guard macro that fails with
`Variable 'X' is required but not set`.

## Usage

```bash
helm lint ./deploy/helm/halalchain
helm template halalchain ./deploy/helm/halalchain --set platformApi.image.tag=abc123

# Chart property tests
./deploy/helm/halalchain/tests/prop01-five-deployments.sh
./deploy/helm/halalchain/tests/prop06-service-selector-slot.sh
./deploy/helm/halalchain/tests/prop10-latest-tag-fails.sh
```

## Configuration

Secrets do not belong in `values.yaml`. Supply them through the usual Kubernetes
mechanisms (`Secret` objects, External Secrets, sealed secrets) and reference
them from the per-service `env`/`existingSecret` keys.

Image tags must be explicit. `prop10-latest-tag-fails.sh` encodes this, and
`infrastructure/scripts/check-docs.py` enforces the same rule against
`service-manifest.yaml` and `docker-compose.yml`.

## Integration

- **Consumes** the container images built by the root [`Dockerfile`](../Dockerfile),
  `HalalChain.Web/Dockerfile`, `HalalChain.Marketplace/Dockerfile`, and the
  `.halalchain/*/Dockerfile` files, plus the CI workflow `release.yml`.
- **Probed by** `make smoke-test` against the same `/health/*` endpoints the
  Compose health checks use.
- **Alerted on** by `infrastructure/prometheus/rules/slo-*.yml`; the operator
  response is in [`docs/runbooks/`](../docs/README.md).

## Development

```bash
helm lint ./deploy/helm/halalchain
for t in ./deploy/helm/halalchain/tests/*.sh; do "$t"; done
```

When adding a template, add a property test alongside it. The three existing
scripts encode invariants that would otherwise only be checked by eye.

## Testing

The shell scripts under `deploy/helm/halalchain/tests/` are the chart's tests.
Each names the property it enforces and the requirement it validates, tracks
pass/fail counts independently so a full report prints even on failure, and
exits non-zero on any failure.

They are **not** wired into `.github/workflows/`. CI builds images and runs
`.NET`/Python tests; chart rendering is validated manually or by whatever
release pipeline invokes the chart.

## Deployment

This directory *is* the deployment configuration. See
[`DEPLOYMENT-STRATEGY.md`](DEPLOYMENT-STRATEGY.md) for the promotion flow and
`deploy/runbooks/` for the response procedures.

## Related Components

- [DEPLOYMENT-STRATEGY.md](DEPLOYMENT-STRATEGY.md)
- [infrastructure/](../infrastructure/README.md) — observability this deployment reports into
- [service-manifest.yaml](../service-manifest.yaml) — the frozen image tags this chart must use
- [Makefile](../Makefile) — the deploy/rollback/promote targets
- [.github/workflows/release.yml](../.github/workflows/README.md) — the image build and publish pipeline
- [docs/runbooks/](../docs/README.md) — platform-level incident runbooks

## Notes / Limitations

- The chart deploys five of the eleven Compose services. `agents`,
  `local-models`, `HalalChain.Automation`, and `HalalChain.Mcp` have no chart
  entry, so a production deployment has no agent runtime and no local model
  host. `HalalChain.Platform.Api` calls `AddAgentRuntime` unconditionally, which
  throws when `Agents:BaseUrl` is unset — see
  [HalalChain.Agents](../HalalChain.Agents/README.md#wiring-status).
- The Helm chart has no observability templates, so AlertManager, Grafana,
  Loki, and Tempo must be deployed separately.
- Blue/green and canary cover only three of the five services between them.