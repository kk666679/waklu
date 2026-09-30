# ENT-005 — Scale-Out, Multi-Region/Multi-Tenant & Cost Optimization

**Role:** Scale
**Owner:** Platform SRE + Finance
**Approver:** Steering Committee
**Duration:** 12 weeks
**Exit:** Gate `gate-005-006`

## Objective

Scale for multi-region operation, tenant isolation, and unit-cost reduction.
**Without violating any of the seven enforcement layers.**

## Mandatory constraints

- **P1–P10** apply in every region, every tenant tier, and every cost
  optimization. There is no region where an LLM can assign a verdict, and
  no tenant tier where evidence is mutable.
- **Evidence retention (C-008) is not subject to cost pressure.** If cost
  optimization would shorten halal evidence retention, retention wins and
  the conflict escalates to Steering.
- **Tenant isolation is tested per module, per tier.** Not sampled.

## Multi-region architecture

| Concern | Approach | Rationale |
|---|---|---|
| Data residency | Per-tenant region pinning | GDPR, PDPA, jurisdictional halal standards |
| Postgres | Regional primary + cross-region read replica | Read scaling; no cross-region writes |
| Blob storage | Region-local buckets | Latency; residency |
| Blob cross-region replication | Audit-only, not hot | Retention meets audit; not for serving |
| Chain anchoring | Single anchor region | One source of truth for Merkle roots |
| Chain reads | All regions read from single source | Verification is idempotent |
| Agent runtime | Regional deployment | Reasoning is regional |
| Agent trace storage | Region-local | Traces may contain PII |
| `tawheed` | Regional deployment, single policy source | Policy is global; execution is regional |
| Compliance gate | Regional, reads global registry | Registry is global; validation is regional |

**Non-negotiable:** the `CertificateRegistry` contract is deployed once. All
regions read the same chain. A certificate valid in one region is valid in
all regions, subject to jurisdictional policy overlays.

## Jurisdictional policy overlays

Different halal standards apply in different markets. This is handled by
**policy overlays**, not by separate `tawheed` instances:

| Market | Standard | Overlay |
|---|---|---|
| Malaysia | JAKIM | `policy/jakim.yaml` |
| Indonesia | MUI | `policy/mui.yaml` |
| Gulf | GCC Halal | `policy/gcc.yaml` |
| Global | HFA | `policy/hfa.yaml` |

A vendor's certificate is validated against **every market it ships to**.
Multi-market vendors require multi-standard validation.

**Regulatory watch** (see `.cline_inbox/bau/COMPLIANCE/regulatory-watch.md`) monitors
for standard changes. A change to any standard triggers a policy review
within 30 days.

## Multi-tenancy

| Isolation level | Tenant tier | Postgres | Blob | Enforcement |
|---|---|---|---|---|
| Shared schema + RLS | Standard | Shared | Shared prefix | Row-level security |
| Schema-per-tenant | Premium | Own schema | Shared prefix | Schema grants |
| Database-per-tenant | Enterprise | Own DB | Own bucket | Separate credentials |

**Non-negotiable:** tenant isolation tests must pass at **every tier**.
Sampling is not acceptable. A failure in any tier blocks the phase.

Isolation tests per module:

| Module | Test |
|---|---|
| `Vendors` | Query without tenant filter returns zero rows from other tenants |
| `Catalog` | Products scoped by tenant at query layer |
| `Compliance` | Verdict bindings tenant-scoped |
| `Orders` | Cross-tenant order access denied |
| `Payments` | Payment records tenant-scoped |
| `Reviews` | Reviews visible only to same-tenant users |
| Storage | Blob access denied cross-tenant via signed URL scoping |
| Agents | Agent runs scoped to tenant; traces not leaked |

## Cost optimization targets

| Metric | Baseline | Target | Method | Verification |
|---|---|---|---|---|
| Cost per 1k API calls | ENT-004 baseline | −40% | Caching, batching, right-sizing | Monthly invoice |
| Blob storage cost/GB | ENT-004 baseline | −25% | Lifecycle tiering, dedupe | Monthly invoice |
| Anchor gas per evidence | ENT-004 baseline | −60% | Larger batches, adaptive cadence | On-chain metrics |
| Agent cost per workflow | ENT-004 baseline | −50% | Prompt caching, model routing | Agent telemetry |
| Postgres cost per tenant | ENT-004 baseline | −30% | Connection pooling, read replicas | DB metrics |

**Every cost target has a monthly invoice or metric as its verification.**
Self-reported savings are not accepted.

## Capacity planning

| Metric | Current | 12-month projection | Provisioning trigger |
|---|---|---|---|
| Active vendors | — | 10× current | Postgres read replica |
| Concurrent workflows | — | 5× current | Agent pool scale |
| Evidence storage | — | 20× current | Blob tiering |
| Anchor batches/day | — | 8× current | Multi-sig anchor |
| API requests/sec peak | — | 15× current | Regional deployment |

Capacity plan reviewed quarterly. Projections update with actuals.

## Deliverables

| # | Deliverable | Location | Owner |
|---|---|---|---|
| D1 | Region topology design | `docs/REGIONS.md` | Platform Arch |
| D2 | Regional failover runbook | `.cline_inbox/bau/RUNBOOKS/infra/region-failover.md` | SRE |
| D3 | Data residency policy | `.cline_inbox/bau/LIFECYCLE/data-residency.md` | DPO + Legal |
| D4 | Tenant isolation test suite | `HalalChain.Platform.Tests/TenantIsolation/` | Platform Eng |
| D5 | Jurisdictional policy overlays | `.halalchain/tawheed/policy/overlays/` | Compliance |
| D6 | Cost attribution model | `.cline_inbox/bau/COST/cost-attribution.md` | Finance |
| D7 | Cost optimization backlog | `.cline_inbox/bau/COST/optimization-backlog.md` | Finance + Platform |
| D8 | Capacity plan (12-month) | `.cline_inbox/bau/CAPACITY/plan.md` | SRE |
| D9 | Multi-region certification | `.cline_inbox/bau/COMPLIANCE/evidence/multi-region-audit.pdf` | Security |
| D10 | Region failover drill log | `.cline_inbox/bau/RUNBOOKS/infra/region-failover-drill.md` | SRE |

## Exit gate criteria

| # | Criterion | Evidence | Verifier |
|---|---|---|---|
| G5.1 | Two regions operational, both passing all quality gates | Region status dashboards | SRE |
| G5.2 | Region failover drill completes within RTO | Drill log | SRE Lead |
| G5.3 | Tenant isolation tests pass in all three tiers | Test report | Platform Eng |
| G5.4 | Data residency policy in effect; DPO sign-off | Signed policy | DPO |
| G5.5 | All cost targets met for 30 consecutive days | Monthly invoices | Finance |
| G5.6 | Jurisdictional overlays deployed and tested | Overlay tests | Compliance |
| G5.7 | Capacity plan reviewed with Steering | Signed plan | Steering |
| G5.8 | No P1–P10 violations introduced by scale-out | Arch test dashboard | Arch Review Board |
| G5.9 | Multi-region audit complete | Audit report | External auditor |
| G5.10 | Halal evidence retention maintained in all regions | Retention report per region | Compliance |

## Risks

| Risk | Likelihood | Impact | Mitigation | Owner |
|---|---|---|---|---|
| Regional latency between chain reads | Medium | Medium | Cache chain reads with short TTL; registry change rate is low | Platform Eng |
| Tenant isolation gap in one module | Medium | Critical | Test every module; do not sample | Platform Eng |
| Cost optimization violates P1–P10 | Medium | Critical | Every optimization passes architecture review | Arch Review Board |
| Jurisdictional policy conflict | Medium | High | Escalate to Shariah Advisor; document resolution | Compliance |
| Region failover slower than RTO | Medium | High | Monthly drills until consistently under RTO | SRE |
| Data residency regulation changes | Medium | High | Regulatory watch; 90-day compliance window | Legal + DPO |
| Cost savings self-reported, not verified | High | Medium | Every target has invoice or metric as verification | Finance |

## RACI

R=Responsible, A=Accountable, C=Consulted, I=Informed

| Activity | Platform Arch | SRE | Platform Eng | Finance | DPO | Compliance | Steering |
|---|---|---|---|---|---|---|---|
| Region topology | R | C | C | I | C | I | A |
| Failover runbook | C | R | C | I | I | I | I |
| Data residency | C | C | C | I | R | C | A |
| Tenant isolation | C | I | R | I | C | C | A |
| Jurisdictional overlays | C | I | C | I | I | R | A |
| Cost model | C | C | C | R | I | I | A |
| Capacity plan | C | R | C | C | I | I | A |
| Multi-region audit | C | C | C | I | C | R | A |

## Out of scope

- Ongoing operations (transitions to BAU under ENT-006)
- Feature development (change management)
- Decommissioning (ENT-006)

## Handoff to ENT-006

At phase close, Enterprise Governance receives:

- Multi-region platform operational
- Tenant isolation verified at all tiers
- Cost targets met and verified
- Capacity plan for 10× growth
- Jurisdictional policy overlays deployed
- Data residency policy in effect

Verification:

```bash
.cline_inbox/hooks/gate-check.sh gate-005-006
```

A failed gate means scale-out is not complete. Extend the phase; do not
proceed to governance.
