# ENT-004 — Governed Production Rollout, Operations & Continuous Improvement

**Role:** Release
**Owner:** SRE + Release Management
**Approver:** Change Advisory Board
**Duration:** 6 weeks (stabilization) + transition to BAU
**Exit:** Gate `gate-004-005`

## Objective

Bring the platform to production under governance. Transition from project
mode to business-as-usual. **This phase owns the BAU handover.**

## Mandatory constraints

- **P1–P10** apply in production. Every production incident that touches a
  principle escalates to the Architecture Review Board.
- **Rollback is one command and completes within 15 minutes.** If it cannot,
  the rollout does not proceed.
- **The project team is not the BAU team.** Handover requires demonstrated
  competency, not a document.

## Rollout strategy

| Stage | Traffic | Duration | Gate to advance | Rollback trigger |
|---|---|---|---|---|
| Dark launch | 0% (shadow) | 2 weeks | Zero SEV1, zero SEV2, error budget ≥ 95% | Any SEV1 |
| Canary | 1% | 1 week | Error budget ≥ 95%, zero compliance-gate failures | Any SEV2 or compliance failure |
| Progressive 1 | 10% | 1 week | Error budget ≥ 90% | Burn rate > 14.4× 1h |
| Progressive 2 | 50% | 1 week | Error budget ≥ 90% | Burn rate > 6× 6h |
| Progressive 3 | 100% | 1 week | Error budget ≥ 90% | Any SEV1 |
| Full | 100% | Ongoing | Error budget policy enforced | Per policy |

Rollback at any step is a single command:

```bash
helm rollback halalchain -n halalchain
```

Completion within 15 minutes is verified by drill before each stage.

## Deliverables

| # | Deliverable | Location | Owner |
|---|---|---|---|
| D1 | BAU operating model | `bau/` (this scaffold) | SRE Lead |
| D2 | On-call rotation live and staffed | `bau/INCIDENT-MANAGEMENT/on-call-rotation.md` | SRE Lead |
| D3 | SLOs instrumented and alerting | `bau/SLOS/` | SRE |
| D4 | Error budget policy in effect | `bau/SLOS/error-budget-policy.md` | SRE |
| D5 | Change Advisory Board in session | `bau/CHANGE-MANAGEMENT/CAB.md` | Release Mgmt |
| D6 | First incident postmortem filed | `bau/INCIDENT-MANAGEMENT/postmortems/` | IC |
| D7 | Cost baseline established | `bau/COST/cost-model.md` | Finance |
| D8 | All runbooks walked through | `bau/RUNBOOKS/` | SRE |
| D9 | Rollback drill logged | `bau/RUNBOOKS/infra/rollback-drill.md` | SRE |
| D10 | BAU handover sign-off | `bau/GOVERNANCE/handover-signoff.md` | SRE Lead + Product |
| D11 | Decision log maintained | `bau/GOVERNANCE/decision-log.md` | Governance |

## BAU handover requirements

The project team is not the BAU team. Handover requires **demonstrated
competency**, not documentation:

| Requirement | Evidence | Verifier |
|---|---|---|
| Runbooks walked through by BAU on-call | Walkthrough log per runbook | SRE Lead |
| Two tabletop exercises per runbook | Exercise logs | SRE Lead |
| One live incident handled by BAU, project team observing | Postmortem with observers noted | SRE Lead |
| On-call rotation covers 100% of hours | Rotation calendar | SRE Lead |
| Escalation paths tested end-to-end | Test log | SRE Lead |
| Access provisioned and verified | Access audit | Security |

**Handover is not complete until a BAU engineer has handled a real incident
without project team intervention.**

## Exit gate criteria

| # | Criterion | Evidence | Verifier |
|---|---|---|---|
| G4.1 | Traffic at 100% for 14 consecutive days | Traffic dashboard | SRE |
| G4.2 | Error budget ≥ 90% over the 14 days | SLO dashboard | SRE |
| G4.3 | Zero SEV1 in the 14 days | Incident log | SRE |
| G4.4 | On-call rotation fully staffed | Rotation calendar | SRE Lead |
| G4.5 | At least one postmortem filed | Postmortem directory | Governance |
| G4.6 | CAB has met at least 4 times | Meeting minutes | Release Mgmt |
| G4.7 | Rollback drill completed within 15 min | Drill log | SRE |
| G4.8 | Cost baseline established and reviewed | Cost model | Finance |
| G4.9 | BAU handover signed by SRE Lead + Product | Signoff doc | Governance |
| G4.10 | No open P1–P10 violations in production | Arch test dashboard | Arch Review Board |

## Risks

| Risk | Likelihood | Impact | Mitigation | Owner |
|---|---|---|---|---|
| Rollback exceeds 15 min in practice | Medium | Critical | Drill weekly until proven; block progression on failure | SRE |
| Project team retained indefinitely | High | High | Handover has hard date; retention requires Steering approval | Program Mgmt |
| Compliance gate fails at scale | Low | Critical | Dark launch exercises the gate at volume before real traffic | Platform Eng |
| On-call burnout during stabilization | Medium | High | Cap on-call rotations at 1 week; enforce recovery days | SRE Lead |
| First real incident during off-hours | High | Medium | Ensure secondary on-call is senior; escalation path tested | SRE Lead |
| Cost overrun during ramp | Medium | Medium | Cost alerts at 80% and 100% of budget | Finance |

## RACI

R=Responsible, A=Accountable, C=Consulted, I=Informed

| Activity | SRE | Release Mgmt | Platform Eng | Compliance | Finance | CAB |
|---|---|---|---|---|---|---|
| Rollout execution | R | A | C | I | I | C |
| Rollback drills | R | C | C | I | I | I |
| On-call staffing | R | C | C | I | I | I |
| CAB operation | C | R | C | C | I | A |
| Incident response | R | C | C | C | I | I |
| BAU handover | R | A | C | C | I | I |
| Cost baseline | C | I | C | I | R | I |

## Out of scope

- Multi-region (ENT-005)
- Certification (ENT-003 — must be complete first)
- Feature development (post-BAU is change management)

## Handoff to ENT-005

At phase close, Platform SRE and Finance receive:

- Stable production at 100% traffic
- Working SLOs and error budget policy
- Staffed on-call rotation
- Cost baseline
- Established CAB cadence
- Governance bodies in session

Verification:

```bash
.cline_inbox/hooks/gate-check.sh gate-004-005
```

A failed gate means the platform is not yet BAU-stable. Extend the phase;
do not proceed to scale-out.
