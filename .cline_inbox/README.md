# .cline_inbox

Agent-facing inbox for the HalalChain enterprise delivery program.

## Purpose

Machine-readable source of truth for the 9-phase enterprise rollout. Every
phase has a charter, gate criteria, RACI, and an artifact manifest. Read by:

- **Agents** via `INBOX.md`, `CONTEXT.md`, `PRINCIPLES.md`
- **CI gates** via `manifests/gates.yaml`
- **Auditors** via `manifests/evidence-index.yaml` and `manifests/controls.yaml`

## Convention

| Path | Read by | Purpose |
|---|---|---|
| `INBOX.md` | Agent | Live task queue. Pick the first unchecked item. |
| `CONTEXT.md` | Agent | Platform orientation. Read before acting. |
| `PRINCIPLES.md` | Agent + humans | Non-negotiable rules. Violations fail the build. |
| `phases/ENT-*/` | Humans + CI | Charter, gate criteria, artifacts |
| `manifests/` | CI + auditors | Machine-readable phase/gate/control/evidence index |
| `schemas/` | CI | JSON schemas for validation |
| `hooks/` | Git hooks + CI | Gate and principle enforcement |

## Rules for agents

1. Read `CONTEXT.md` and `PRINCIPLES.md` before any task.
2. Work only on the first unchecked item in `INBOX.md`.
3. A failing phase gate must not be bypassed. Escalate to a human.
4. Record every artifact produced in `manifests/evidence-index.yaml`.
5. Do not edit a `CHARTER.md` to make a gate pass. Charters are ratified
   artifacts; changing one requires a new ADR.

## Relationship to the root `gates.yaml`

The root `gates.yaml` is the **lightweight status mirror** (id, status,
description, phase, approver) used by dashboards. This directory's
`manifests/gates.yaml` is the **authoritative registry** (criteria file,
verifier, automated checks).

- Gate **identity and status** live in the root file.
- Gate **criteria and enforcement** live here.
- If they disagree on a status, the root file is stale. Fix the root file.

## Running the gates

Requires **Node 18+** (no `yq`, no `jq`, no `bash`).

```bash
node .cline_inbox/hooks/gate-check.mjs              # list gates
node .cline_inbox/hooks/gate-check.mjs gate-001-002 # run one
node .cline_inbox/hooks/gate-check.mjs --all
```

## Ownership

- Maintainer: `platform-architecture`
- Approver: `enterprise-steering-committee`
- Review cadence: per phase transition
