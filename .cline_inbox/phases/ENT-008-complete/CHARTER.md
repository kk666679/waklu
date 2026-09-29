# ENT-008 — Editor Completion, Auto-Layout & Platform Handover

**Role:** Complete
**Owner:** UI Platform + SRE
**Approver:** Product + Architecture Review Board
**Duration:** 6 weeks
**Exit:** Gate `gate-008-009`

## Objective

Complete the workflow editor with auto-layout, finalize handover from the
authoring team to the operations team, and certify the editor as production.

## Mandatory constraints

- **P1–P10** apply. The editor cannot introduce a path to a verdict.
- **Auto-layout is deterministic.** Same graph input produces same layout
  coordinates. Auditors must be able to reproduce a workflow's visual state.
- **Handover is demonstrated, not documented.** Same bar as ENT-004.

## Auto-layout

| Algorithm | Use case | Library | Determinism |
|---|---|---|---|
| Layered (Sugiyama) | Default for workflows | `dagre` or `elkjs` | Deterministic |
| Force-directed | Exploration mode | `d3-force` | Seeded, deterministic |
| Manual | User-preferred positions | persisted in node metadata | N/A |

**Determinism requirement:** the same graph serialized twice must produce
byte-identical layout coordinates. Verified by test on every commit.

## Editor completion checklist

| Item | Owner | Pass criteria | Evidence |
|---|---|---|---|
| Keyboard-only operation | UI | Full workflow authorable without mouse | Accessibility test |
| Screen reader support | UI | WCAG 2.2 AA | Audit report |
| Undo/redo across 100 operations | UI | No state corruption | Test log |
| Save/load round-trip | UI | Byte-identical on serialize → deserialize | Test |
| Version diff | UI | Visual diff of two workflow versions | Manual demo |
| Simulation mode | UI | Workflow runs against fixture data | Test |
| Export to Python | UI | Generates valid `.halalchain/agents/` module | Integration test |
| Import from Python | UI | Parses existing workflow | Integration test |
| Multi-user editing | UI | SignalR-based collaborative editing | Concurrency test |
| Mobile-responsive layout | UI | Usable on tablet | Manual test |

## Handover to BAU

| Artifact | From | To | Evidence |
|---|---|---|---|
| Editor runbook | UI Platform | SRE | Walkthrough log |
| Node library maintenance guide | UI Platform | UI Platform (retained) | Document |
| On-call escalation path | UI Platform | SRE | Test log |
| Incident playbook | UI Platform | SRE | Tabletop exercise log |
| Access provisioning | UI Platform | SRE | Access audit |

**Handover is complete when a BAU engineer has resolved a real editor issue
without UI Platform intervention.**

## Deliverables

| # | Deliverable | Location | Owner |
|---|---|---|---|
| D1 | Auto-layout implementation | `HalalChain.Web/Services/Workflow/Layout/` | UI Platform |
| D2 | Layout determinism test | `HalalChain.Platform.Tests/Unit/Workflow/Layout/` | UI Platform |
| D3 | Accessibility audit | `bau/COMPLIANCE/evidence/wcag-aa-audit.pdf` | UI Platform |
| D4 | Editor runbook | `bau/RUNBOOKS/editor/` | UI Platform |
| D5 | Handover sign-off | `bau/GOVERNANCE/handover-signoff.md` | SRE Lead + Product |
| D6 | Simulation mode | `HalalChain.Web/Services/Workflow/Simulation/` | UI Platform |
| D7 | Python import/export | `HalalChain.Web/Services/Workflow/Codec/` | UI Platform |

## Exit gate criteria

| # | Criterion | Evidence | Verifier |
|---|---|---|---|
| G8.1 | WCAG 2.2 AA audit passed | Signed audit report | Architecture Review Board |
| G8.2 | Keyboard-only operation verified | Accessibility test | UI Platform |
| G8.3 | Layout determinism test passes | Test report | UI Platform |
| G8.4 | Round-trip to Python byte-identical | Integration test | Platform Eng |
| G8.5 | Simulation mode demo complete | Demo recording | Product |
| G8.6 | Handover signed by SRE Lead + Product | Signoff doc | Governance |
| G8.7 | Editor runbook walked through by SRE | Walkthrough log | SRE Lead |
| G8.8 | Incident playbook exercised | Tabletop log | SRE Lead |
| G8.9 | No P1–P10 violations introduced | Arch test dashboard | Arch Review Board |

## Risks

| Risk | Likelihood | Impact | Mitigation | Owner |
|---|---|---|---|---|
| Auto-layout non-deterministic | Medium | High | Seeded algorithms; test on every commit | UI Platform |
| WCAG AA fails on complex workflows | Medium | Medium | Audit at mid-phase, not end | UI Platform |
| Handover rushed | High | High | Hard date; retention requires Steering approval | Program Mgmt |
| Simulation mode diverges from real execution | Medium | High | Simulation calls same `.halalchain/agents/` code | Platform Eng |
| Python round-trip loses semantics | Medium | Critical | Round-trip test on 50+ workflows | Platform Eng |
| Editor becomes decision surface | Low | Critical | CI grep for verdict fields continues | Arch Review Board |

## RACI

| Activity | UI Platform | SRE | Platform Eng | Product | Arch Review Board |
|---|---|---|---|---|---|
| Auto-layout | R | I | C | I | A |
| Accessibility | R | I | I | C | A |
| Handover | R | A | C | C | I |
| Simulation | R | I | C | C | I |
| Python codec | R | I | C | I | A |
| Editor runbook | R | A | C | I | I |

## Out of scope

- Continuous assurance (ENT-009)
- Feature development (post-BAU is change management)
- Workflow execution (runs in `.halalchain/agents/`)

## Handoff to ENT-009

At phase close, ENT-009 receives:

- Completed workflow editor
- Auto-layout with determinism guarantee
- WCAG 2.2 AA certification
- Signed BAU handover
- Python codec with round-trip guarantee

Verification:

```bash
.cline_inbox/hooks/gate-check.sh gate-008-009
```

A failed gate means the editor is not production-ready. Extend the phase;
do not proceed to continuous assurance.
