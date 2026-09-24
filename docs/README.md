# `docs/`

Project documentation for HalalChain.

## Layout

```
docs/
├── ARCHITECTURE.md          # Canonical system architecture
├── due-diligence.md         # Comprehensive diligence review of the repo and platform
├── FOUNDRY_LEVERAGE.md      # How we use Microsoft Foundry
├── local-development.md     # Local dev workflow
├── mvp-architecture.md      # MVP-specific architecture notes
├── architecture/            # Deeper architecture topics
│   ├── 01-architecture.md
│   ├── 02-data-placement.md
│   ├── 03-threat-model.md
│   └── 04-api.md
└── runbooks/                # Operational runbooks
    ├── chain-reorg-recovery.md
    └── compromised-key.md
```

The top-level files (`ARCHITECTURE.md`, `due-diligence.md`,
`mvp-architecture.md`, `local-development.md`, `FOUNDRY_LEVERAGE.md`) are the
high-level entry points. `architecture/` and `runbooks/` go deeper.

The main runtime stack described in these docs is:
- API: ASP.NET Core / .NET 10
- Customer website: Blazor Server
- Vendor marketplace: Razor Pages + Blazor Server + SignalR
- Python AI evidence services: `.halalchain/ai-inference` and `.halalchain/tawheed`
