# `docs/`

Project documentation for HalalChain.

## Layout

```
docs/
├── ARCHITECTURE.md                   # Canonical system architecture
├── CATALOG_MODERNIZATION_2026.md     # 2026 catalog modernization notes
├── DECISION-CONTRACT.md              # Decision contract specification
├── due-diligence.md                  # Comprehensive diligence review of the repo and platform
├── ENHANCED_ARCHITECTURE_SUMMARY.md  # Enhanced architecture summary
├── local-development.md              # Local dev workflow
├── RUNTIME-MATRIX.md                 # Runtime compatibility matrix
├── SECURITY-BASELINE.md              # Security baseline requirements
├── SERVICE_MANIFEST.md               # Service manifest and ports
├── slo.yaml                          # SLO definitions
├── architecture/                     # Deeper architecture topics
│   ├── 01-architecture.md
│   ├── 02-data-placement.md
│   ├── 03-threat-model.md
│   ├── 04-api.md
│   ├── adr/                          # Architecture Decision Records
│   └── tech-debt.md                  # Technical debt register
└── runbooks/                         # Operational runbooks
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
    └── tawheed-unavailable.md
```

The top-level files are the high-level entry points. `architecture/` and `runbooks/` go deeper.

The main runtime stack described in these docs is:
- API: ASP.NET Core / .NET 10
- Customer website: Blazor Server
- Vendor marketplace: Razor Pages + Blazor Server + SignalR
- Python AI evidence services: `.halalchain/ai-inference`, `.halalchain/tawheed`, `.halalchain/agents`, and `.halalchain/local-models`
