# ENT-006 — Governance, Lifecycle Management & Decommissioning

**Role:** Govern
**Owner:** Enterprise Governance
**Approver:** Steering Committee + Legal
**Duration:** Continuous (no fixed end)
**Exit:** None — runs alongside ENT-007 through ENT-009

## Objective

Ensure the platform remains compliant, cost-effective, and appropriately
governed over its full lifecycle, including end-of-life. Governance does not
end. This phase starts when ENT-005 hands off and never closes.

## Mandatory constraints

- **P1–P10** are permanent. No governance decision overrides them.
- **Halal evidence retention (C-008) is 7 years minimum**, non-negotiable,
  and supersedes cost optimization, privacy requests, and business decisions.
- **Every governance decision is logged** in `bau/GOVERNANCE/decision-log.md`.
  The log is append-only and immutable.
- **No decommission of a service holding halal evidence** without Shariah
  Advisor sign-off.

## Governance bodies

| Body | Cadence | Chair | Scope | Decision authority |
|---|---|---|---|---|
| Steering Committee | Monthly | Exec sponsor | Strategy, budget, escalations | Budget, phase gates, P1–P10 exceptions (none) |
| Architecture Review Board | Bi-weekly | Chief Architect | ADRs, tech debt, breaking changes | Architecture decisions |
| Change Advisory Board | Weekly | Release Manager | Standard + normal changes | Change approval |
| Security Review Board | Monthly | CISO | Findings, exceptions, audits | Security exceptions |
| Data Governance Council | Quarterly | DPO | Retention, residency, deletion | Data decisions |
| Shariah Advisory Board | Monthly | External advisor | Halal standards, finance | Halal interpretation (binding) |

## Lifecycle stages

| Stage | Trigger | Owner | Exit criteria | Typical duration |
|---|---|---|---|---|
| Introduction | New service/feature | Platform Eng | In production | 1–3 months |
| Growth | Adoption increasing | Product | SLO-stable 90 days | 6–18 months |
| Maturity | SLO-stable, cost-optimized | SRE | Sustained 12 months | Indefinite |
| Decline | Usage falling, cost rising | Product | Deprecation notice | 3–6 months |
| Deprecation | Notice published | Product + Legal | 180-day sunset window | 6 months |
| Sunset | Window elapsed | SRE | Data archived, resources removed | 1 month |
| Decommission | Post-sunset | Governance | Certificate of closure | 1 month |

## Data retention schedule

See `bau/LIFECYCLE/data-retention.md` for the full table. Key points:

| Data class | Retention | Override |
|---|---|---|
| Halal evidence | **7 years** | None — non-negotiable |
| Verdicts | 7 years | None |
| Agent traces | 3 years | Privacy if PII-bearing |
| Orders | 7 years | Tax |
| Customer PII | Account + 90d | GDPR |
| Blockchain anchors | Permanent | Cannot delete by design |

## Decommissioning checklist

Every decommission requires, in order:

1. **Business case** for removal
2. **Impact assessment** — consumers, integrations, contracts
3. **Deprecation notice** — 180 days external, 90 internal
4. **Migration plan** for consumers
5. **Data retention plan** — what stays, what goes
6. **Evidence retention check** — Shariah Advisor sign-off if halal evidence
7. **Steering approval**
8. **Consumer migration** — traffic to zero
9. **Data archive** — evidence to `EvidenceStore`, other data to cold storage
10. **Infrastructure teardown**
11. **DNS / routing removal**
12. **Credential rotation / revocation**
13. **Cost closure** — no orphaned resources
14. **Certificate of closure** filed in `bau/GOVERNANCE/`

## Deliverables (continuous)

| # | Deliverable | Cadence | Owner |
|---|---|---|---|
| D1 | Governance body minutes | Per meeting | Program Mgmt |
| D2 | Decision log entries | Per decision | Governance |
| D3 | Lifecycle review report | Quarterly | Product |
| D4 | Retention compliance attestation | Quarterly | Compliance |
| D5 | Decommission certificates | Per decommission | Governance |
| D6 | Exception register | Continuous | Compliance |
| D7 | Regulatory response log | Per regulation change | Legal |
| D8 | Annual governance review | Annual | Steering |

## Governance metrics

| Metric | Target | Owner |
|---|---|---|
| Decisions logged vs decisions made | 100% | Governance |
| Decisions resolved within SLA | ≥ 95% | Governance |
| Exception register open items | 0 unmanaged | Compliance |
| Lifecycle reviews completed | 100% quarterly | Product |
| Decommissions with proper sign-off | 100% | Governance |

## Risks

| Risk | Likelihood | Impact | Mitigation | Owner |
|---|---|---|---|---|
| Governance body quorum failure | Medium | High | Deputy chairs; async decisions for non-binding | Governance |
| Decision log drift | High | Medium | CI check that decisions with tickets have log entries | Governance |
| Retention violation discovered late | Low | Critical | Quarterly attestation; automated retention monitoring | Compliance |
| Decommission without Shariah sign-off | Low | Critical | Hard gate in workflow; escalation on bypass attempt | Governance |
| Governance slows delivery | High | Medium | Timeboxed decisions; escalation path; deputy authority | Governance |
| Data retention regulation changes | Medium | High | Regulatory watch; 90-day compliance window | Legal + DPO |
| Decommission leaves data behind | Medium | High | Automated resource audit post-decommission | SRE |

## RACI

| Activity | Steering | Compliance | Legal | Shariah Board | Governance | SRE |
|---|---|---|---|---|---|---|
| Steering decisions | R/A | C | C | C | I | I |
| ADR ratification | I | C | C | C | R/A | C |
| Retention decisions | A | R | C | C | I | I |
| Decommission approval | A | R | C | C | R | I |
| Data classification | I | R | C | C | C | C |
| Exception approval | A | R | C | C | C | I |
| Regulatory response | A | R | R | C | I | I |

## Out of scope

- Day-to-day operations (ENT-004 / BAU)
- Feature development (change management)
- Security incident response (BAU)

## Handoff to ENT-007

ENT-006 runs in parallel with ENT-007 through ENT-009. No handoff — it
governs all of them. There is no `gate-006-007` because this phase never
closes; its enforcement is the ongoing operation of every other gate.

Verification:

```bash
.cline_inbox/hooks/gate-check.sh
```

Lists all gates. ENT-006 has no gate of its own; it validates that every
other gate in the registry is enforced.
