# `docs/`

Project documentation for HalalChain.

## Purpose

The human-facing documentation tier: architecture, ADRs, security baseline,
runtime inventory, SLOs, and operational runbooks. System-wide architecture
belongs here rather than in any single component's README.

## Layout

```
docs/
├── ARCHITECTURE.md                   # Canonical system architecture
├── CATALOG_MODERNIZATION_2026.md     # 2026 catalog modernization notes
├── DECISION-CONTRACT.md              # Decision contract specification
├── due-diligence.md                  # Comprehensive diligence review of the repo and platform
├── ENHANCED_ARCHITECTURE_SUMMARY.md  # Enhanced architecture summary
├── local-development.md              # Local dev workflow
├── RUNTIME-MATRIX.md                 # Runtime compatibility matrix (service ⇄ port inventory)
├── SECURITY-BASELINE.md              # Security baseline requirements
├── SERVICE_MANIFEST.md               # Service manifest and ports
├── slo.yaml                          # SLO definitions
├── architecture/                     # Deeper architecture topics
│   ├── 01-architecture.md
│   ├── 02-data-placement.md
│   ├── 03-threat-model.md
│   ├── 04-api.md
│   ├── 05-integration-workflows.md   # Cross-directory integration map and workflow catalogue
│   ├── workspace-health.md           # Per-component wiring status
│   ├── tech-debt.md                  # Technical debt register
│   └── adr/0001-phase1-domain-entity-migration.md
├── adr/                              # Architecture Decision Records (001–011)
│   ├── 001-vertical-slice-modular-monolith.md
│   ├── 002-storage-port-adapter-and-hashers.md
│   ├── 003-mcp-v2-stateless-transport.md
│   ├── 004-hybridcache-default.md
│   ├── 005-verdict-binding-not-boolean.md
│   ├── 006-chain-network-selection.md
│   ├── 007-payment-provider-selection.md
│   ├── 008-registrar-key-custody.md
│   ├── 009-multi-tenancy-model.md
│   ├── 010-browser-automation-engine.md
│   └── 011-browser-automation-security.md
└── runbooks/                         # 25 operational runbooks
    ├── ai-inference-errors.md
    ├── chain-reorg-recovery.md
    ├── circuit-breaker.md
    ├── compromised-key.md
    ├── determinism-violation.md
    ├── disk-space.md
    ├── gpu-memory.md
    ├── latency-p95-breach.md
    ├── latency-p99-breach.md
    ├── llm-latency.md
    ├── otel-refusing.md
    ├── outbox-dead-letter.md
    ├── outbox-lag.md
    ├── postgres-connections.md
    ├── postgres-down.md
    ├── postgres-slow-queries.md
    ├── redis-evictions.md
    ├── redis-memory.md
    ├── service-down.md
    ├── signalr-drops.md
    ├── slo-burn-fast.md
    ├── slo-burn-slow.md
    ├── tawheed-evidence-errors.md
    ├── tawheed-latency.md
    ├── tawheed-unavailable.md
    ├── web-api-latency.md
    └── web-client-errors.md
```

The top-level files are the high-level entry points. `architecture/` and
`runbooks/` go deeper.

The main runtime stack described in these docs is:
- API: ASP.NET Core / .NET 10
- Customer website: Blazor Server
- Vendor marketplace: Razor Pages + Blazor Server + SignalR
- Python AI evidence services: `.halalchain/ai-inference`, `.halalchain/tawheed`, `.halalchain/agents`, and `.halalchain/local-models`

## Where to start

| If you want to understand… | Read |
| --- | --- |
| The whole system | [ARCHITECTURE.md](ARCHITECTURE.md) and the [root README](../README.md) |
| How components connect end to end | [architecture/05-integration-workflows.md](architecture/05-integration-workflows.md) |
| What actually runs vs. what is aspirational | [architecture/workspace-health.md](architecture/workspace-health.md) |
| Why a decision was made | [adr/](adr/) |
| What is unfinished | [architecture/tech-debt.md](architecture/tech-debt.md) |
| What to do when an alert fires | [runbooks/](runbooks/) |
| Which ports and services exist | [RUNTIME-MATRIX.md](RUNTIME-MATRIX.md) |

## Documentation ownership

System-wide facts live here. Directory-level behaviour lives in each
directory's own `README.md`:

| Document | Scope |
| --- | --- |
| `docs/ARCHITECTURE.md` | Cross-cutting architecture |
| `docs/architecture/05-integration-workflows.md` | Integration map, workflow catalogue, integration matrix |
| `README.md` (root) | Build order, layering rules, repository status |
| `AGENTS.md` | Stack, solution layout, build/test/run commands, conventions |
| `<directory>/README.md` | What that directory contains and how it connects |

Run `python3 infrastructure/scripts/check-docs.py` after changing services,
ports, or image tags — it fails the build when `service-manifest.yaml`,
`docker-compose.yml`, and `RUNTIME-MATRIX.md` disagree.

## Related Components

- [Root README](../README.md)
- [AGENTS.md](../AGENTS.md)
- [infrastructure/](../infrastructure/README.md) — where the Prometheus rules and Grafana dashboards behind `runbooks/` live
- [deploy/](../deploy/README.md)
- [.github/workflows/](../.github/workflows/README.md) — which of these docs are validated in CI