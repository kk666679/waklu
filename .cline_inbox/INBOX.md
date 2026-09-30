# Agent Inbox

Live task queue. Work top to bottom. Tick when done. Never skip a gate.

## Current phase: ENT-001 (Define)

Charter: `phases/ENT-001-specify/CHARTER.md` — read it first. Gate criteria
G1.1–G1.10 in `GATE-CRITERIA.md`.

- [ ] ENT-001-T01 — Ratify `docs/ARCHITECTURE.md` with steering minutes (G1.1)
- [ ] ENT-001-T02 — Reconcile project count against `dotnet sln list` (G1.2)
- [ ] ENT-001-T03 — Confirm ADR-001…005 merged, each with dissent (G1.3)
- [ ] ENT-001-T04 — Ratify ADR-006 chain network (G1.4)
- [ ] ENT-001-T05 — Ratify ADR-007 PSP w/ vendor letter (G1.5)
- [ ] ENT-001-T06 — Ratify ADR-008 registrar key custody (G1.6)
- [ ] ENT-001-T07 — Ratify ADR-009 multi-tenancy or document provisional (G1.7)
- [ ] ENT-001-T08 — Pin all Docker image tags (G1.8)
- [ ] ENT-001-T09 — Seed `manifests/evidence-index.yaml`, ≥ 9 entries (G1.9)
- [ ] ENT-001-T10 — Capture baseline metrics in `.cline_inbox/bau/CONTINUOUS-IMPROVEMENT/` (G1.10)
- [ ] ENT-001-T11 — File the falsification report for P1–P7

## Verified already (2026-09-26)

- Project count is **18** in both `.sln` and `AGENTS.md` → G1.2 satisfied.
- ADRs 001–009 all exist on disk → presence confirmed, but **dissent sections
  still need inspection** for G1.3.
- P6 (no verdict type on-chain) clean across 5 contracts.

## Gate ENT-001 → ENT-002

```bash
node .cline_inbox/hooks/gate-check.mjs gate-001-002
```

Automated criteria only. A green run is necessary, not sufficient — six
criteria are human judgements.

---

## Queued (locked until the prior gate passes)

- ENT-002 — Build
- ENT-003 — Certify
- ENT-004 — Release
- ENT-005 — Scale
- ENT-006 — Govern (continuous, parallel)
- ENT-007 — Author (xyflow palette)
- ENT-008 — Complete
- ENT-009 — CLOSE (continuous, terminal)
