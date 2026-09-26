#!/usr/bin/env bash
# HalalChain — write ENT-006 through ENT-009 phase charters.
# Usage: ./write-charters-2.sh [/path/to/repo]
set -euo pipefail

ROOT="${1:-$(pwd)}"
PHASES="$ROOT/.cline_inbox/phases"

mkdir -p "$PHASES"/{ENT-006-govern,ENT-007-author,ENT-008-complete,ENT-009-close}

# ═══════════════════════════════════════════════════════════════════════
# ENT-006
# ═══════════════════════════════════════════════════════════════════════
cat > "$PHASES/ENT-006-govern/CHARTER.md" <<'EOF_ENT006'
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
| Regulatory response | A | R | C | C | I | I |

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
EOF_ENT006

═══════════════════════════════════════════════════════════════════════

ENT-007

═══════════════════════════════════════════════════════════════════════

cat > "$PHASES/ENT-007-author/CHARTER.md" <<'EOF_ENT007'

ENT-007 — Enterprise Node Palette (@xyflow/react / reactflow)

Role: Author
Owner: UI Platform
Approver: Architecture Review Board
Duration: 8 weeks
Exit: Gate gate-007-008

Objective

Author the enterprise node palette for the platform's workflow editor, built
on @xyflow/react. The palette lets ops and compliance staff compose,
inspect, and audit agent workflows visually.

Why this exists

The agent workflows in .halalchain/agents/workflows/ are deterministic
Python, but compliance staff need to see them, trace them, and cite them
in audits. The palette is the visual surface for that.

The rule this palette must not break

No node in the palette emits a verdict. The palette is a composition
surface, not a decision surface. The terminal node EmitVerdictRequest
hands evidence to tawheed and receives a verdict — it never computes one.

Enforced three ways:

1. Palette grep test in CI (P1)
2. Node contract schema (closed emits enum)
3. Serializer round-trip test proves the emitted Python has no verdict path

Node categories

Category Nodes Purpose Emits
Evidence FetchDocument, ClassifyDocument, ExtractFields Evidence gathering EvidenceProposal
Verification VerifyCertificate, CheckRegistry, CrossReference Verification EvidenceProposal
Policy EvaluatePolicy, CheckRequirement, ResolveGap Deterministic calls Policy result
Agents AgentInvoke, AgentEscalate, BudgetCheck Agent orchestration EvidenceProposal
Control Branch, Parallel, Merge, Loop, Halt Flow control Flow directive
Human ApprovalGate, ReviewTask, Override Human-in-the-loop Escalation or approval
Chain AnchorBatch, VerifyInclusion Blockchain AnchorReceipt
Terminal EmitVerdictRequest, End Handoff to tawheed Verdict request only

Node contract

Every node declares:

```python
class NodeContract(BaseModel):
    node_type: str
    version: str
    inputs: list[PortSpec]
    outputs: list[PortSpec]
    tools: list[ToolCapability]          # declared capability
    budget_contribution: BudgetDelta     # what it costs
    emits: Literal["EvidenceProposal", "PolicyResult", "FlowDirective",
                   "Escalation", "AnchorReceipt", "VerdictRequest"]
    # Deliberately absent, enforced by CI grep:
    #   verdict, is_halal, approved, status
```

The emits field is closed. A node that wants to emit anything else is a
node that should not exist.

Non-negotiable constraints

1. Every node's output schema validates against EvidenceProposal or its
   declared emits type
2. The palette refuses to instantiate nodes that would bypass tawheed
3. Every composed workflow is serializable to .halalchain/agents/ Python
   format and round-trippable byte-for-byte
4. Every node displays its tool capability declaration on hover
5. No node may be authored that lacks a NodeContract
6. Palette CI runs the same grep as .halalchain/ for forbidden fields

Deliverables

# Deliverable Location Owner
D1 Node library (all 8 categories) HalalChain.Web/Components/Workflow/Nodes/ UI Platform
D2 Palette panel HalalChain.Web/Components/Workflow/Palette/ UI Platform
D3 Serializer (React Flow JSON ↔ Python workflow) HalalChain.Web/Services/Workflow/ UI Platform
D4 Schema validation layer HalalChain.Web/Services/Workflow/Validation/ UI Platform
D5 Node contract tests HalalChain.Platform.Tests/Unit/Workflow/ UI Platform
D6 Round-trip test suite HalalChain.Platform.Tests/Unit/Workflow/Roundtrip/ UI Platform
D7 Palette documentation docs/WORKFLOW-EDITOR.md UI Platform
D8 Forbidden-node CI grep infrastructure/ci/check-palette.sh Platform Eng

Exit gate criteria

# Criterion Evidence Verifier
G7.1 All 8 node categories implemented Directory listing Architecture Review Board
G7.2 Zero verdict-emitting nodes CI grep clean Platform Eng
G7.3 Round-trip test passes for 20+ composed workflows Test report UI Platform
G7.4 Serializer emits valid Python accepted by .halalchain/agents/ Integration test Platform Eng
G7.5 Every node has a NodeContract Schema validation Architecture Review Board
G7.6 Node hover shows tool capability Manual test UI Platform
G7.7 Palette refuses to compose verdict-bypassing workflows Negative tests UI Platform
G7.8 Palette documentation complete Manual review UI Platform

Risks

Risk Likelihood Impact Mitigation Owner
A node is added that emits a verdict Medium Critical CI grep; contract schema; round-trip test UI Platform
Serializer drift from .halalchain/ format High High Round-trip test on every commit UI Platform
Node contract schema too loose Medium High Closed emits enum; schema reviewed by Arch Board UI Platform
xyflow API breaking change Medium Medium Pin version; abstract behind wrapper UI Platform
Palette becomes a shadow workflow engine Low Critical All nodes call deterministic services; no local execution Architecture Review Board
Missing tool capability declaration Medium Medium Schema requires tools field UI Platform

RACI

Activity UI Platform Platform Arch Platform Eng Compliance Arch Review Board
Node library R A C C C
Palette panel R A I I I
Serializer R A C I C
Schema validation R A C C C
Round-trip tests R C C I I
Forbidden-node CI C A R C I
Documentation R A I C I

Out of scope

· Auto-layout (ENT-008)
· Editor completion (ENT-008)
· Workflow execution (runs in .halalchain/agents/)

Handoff to ENT-008

At phase close, ENT-008 receives:

· Complete node library with contracts
· Palette panel with capability display
· Serializer with round-trip guarantee
· Schema validation layer
· All tests passing
· Documentation

Verification:

```bash
.cline_inbox/hooks/gate-check.sh gate-007-008
```

A failed gate means the palette is incomplete. Extend the phase; do not
proceed to editor completion.
EOF_ENT007

═══════════════════════════════════════════════════════════════════════

ENT-008

═══════════════════════════════════════════════════════════════════════

cat > "$PHASES/ENT-008-complete/CHARTER.md" <<'EOF_ENT008'

ENT-008 — Editor Completion, Auto-Layout & Platform Handover

Role: Complete
Owner: UI Platform + SRE
Approver: Product + Architecture Review Board
Duration: 6 weeks
Exit: Gate gate-008-009

Objective

Complete the workflow editor with auto-layout, finalize handover from the
authoring team to the operations team, and certify the editor as production.

Mandatory constraints

· P1–P10 apply. The editor cannot introduce a path to a verdict.
· Auto-layout is deterministic. Same graph input produces same layout
coordinates. Auditors must be able to reproduce a workflow's visual state.
· Handover is demonstrated, not documented. Same bar as ENT-004.

Auto-layout

Algorithm Use case Library Determinism
Layered (Sugiyama) Default for workflows dagre or elkjs Deterministic
Force-directed Exploration mode d3-force Seeded, deterministic
Manual User-preferred positions persisted in node metadata N/A

Determinism requirement: the same graph serialized twice must produce
byte-identical layout coordinates. Verified by test on every commit.

Editor completion checklist

Item Owner Pass criteria Evidence
Keyboard-only operation UI Full workflow authorable without mouse Accessibility test
Screen reader support UI WCAG 2.2 AA Audit report
Undo/redo across 100 operations UI No state corruption Test log
Save/load round-trip UI Byte-identical on serialize → deserialize Test
Version diff UI Visual diff of two workflow versions Manual demo
Simulation mode UI Workflow runs against fixture data Test
Export to Python UI Generates valid .halalchain/agents/ module Integration test
Import from Python UI Parses existing workflow Integration test
Multi-user editing UI SignalR-based collaborative editing Concurrency test
Mobile-responsive layout UI Usable on tablet Manual test

Handover to BAU

Artifact From To Evidence
Editor runbook UI Platform SRE Walkthrough log
Node library maintenance guide UI Platform UI Platform (retained) Document
On-call escalation path UI Platform SRE Test log
Incident playbook UI Platform SRE Tabletop exercise log
Access provisioning UI Platform SRE Access audit

Handover is complete when a BAU engineer has resolved a real editor issue
without UI Platform intervention.

Deliverables

# Deliverable Location Owner
D1 Auto-layout implementation HalalChain.Web/Services/Workflow/Layout/ UI Platform
D2 Layout determinism test HalalChain.Platform.Tests/Unit/Workflow/Layout/ UI Platform
D3 Accessibility audit bau/COMPLIANCE/evidence/wcag-aa-audit.pdf UI Platform
D4 Editor runbook bau/RUNBOOKS/editor/ UI Platform
D5 Handover sign-off bau/GOVERNANCE/handover-signoff.md SRE Lead + Product
D6 Simulation mode HalalChain.Web/Services/Workflow/Simulation/ UI Platform
D7 Python import/export HalalChain.Web/Services/Workflow/Codec/ UI Platform

Exit gate criteria

# Criterion Evidence Verifier
G8.1 WCAG 2.2 AA audit passed Signed audit report Architecture Review Board
G8.2 Keyboard-only operation verified Accessibility test UI Platform
G8.3 Layout determinism test passes Test report UI Platform
G8.4 Round-trip to Python byte-identical Integration test Platform Eng
G8.5 Simulation mode demo complete Demo recording Product
G8.6 Handover signed by SRE Lead + Product Signoff doc Governance
G8.7 Editor runbook walked through by SRE Walkthrough log SRE Lead
G8.8 Incident playbook exercised Tabletop log SRE Lead
G8.9 No P1–P10 violations introduced Arch test dashboard Arch Review Board

Risks

Risk Likelihood Impact Mitigation Owner
Auto-layout non-deterministic Medium High Seeded algorithms; test on every commit UI Platform
WCAG AA fails on complex workflows Medium Medium Audit at mid-phase, not end UI Platform
Handover rushed High High Hard date; retention requires Steering approval Program Mgmt
Simulation mode diverges from real execution Medium High Simulation calls same .halalchain/agents/ code Platform Eng
Python round-trip loses semantics Medium Critical Round-trip test on 50+ workflows Platform Eng
Editor becomes decision surface Low Critical CI grep for verdict fields continues Arch Review Board

RACI

Activity UI Platform SRE Platform Eng Product Arch Review Board
Auto-layout R I C I A
Accessibility R I I C A
Handover R A C C I
Simulation R I C C I
Python codec R I C C I A
Editor runbook R A C I I

Out of scope

· Continuous assurance (ENT-009)
· Feature development (post-BAU is change management)
· Workflow execution (runs in .halalchain/agents/)

Handoff to ENT-009

At phase close, ENT-009 receives:

· Completed workflow editor
· Auto-layout with determinism guarantee
· WCAG 2.2 AA certification
· Signed BAU handover
· Python codec with round-trip guarantee

Verification:

```bash
.cline_inbox/hooks/gate-check.sh gate-008-009
```

A failed gate means the editor is not production-ready. Extend the phase;
do not proceed to continuous assurance.
EOF_ENT008

═══════════════════════════════════════════════════════════════════════

ENT-009

═══════════════════════════════════════════════════════════════════════

cat > "$PHASES/ENT-009-close/CHARTER.md" <<'EOF_ENT009'

ENT-009 — Continuous Assurance, Audit Automation, Regulatory Change & Benefits Realization

Role: CLOSE — FINAL ✅
Owner: Enterprise Governance + Internal Audit
Approver: Board Audit Committee
Duration: Continuous — terminal phase
Exit: None. This phase never closes; it is the steady state.

Objective

Institutionalize the platform's assurance, audit, and improvement functions
so the enterprise can operate indefinitely without project-mode overhead.

The terminal state

When this phase is fully operating, the enterprise is:

· Governed — bodies in place, decisions logged
· Assured — controls tested continuously
· Auditable — evidence produced automatically
· Adaptive — regulatory changes tracked and responded to
· Measured — benefits realized against business case

This is the CLOSE ✅ FINAL state. The platform is enterprise-grade.

Mandatory constraints

· P1–P10 are permanent. Any failure in continuous assurance escalates
to the Board Audit Committee within one reporting cycle.
· Halal evidence retention (C-008) is not subject to cost pressure.
  Restated from ENT-005 because it is the first thing a cost-focused
auditor will challenge.
· Audit automation target is 80%. Below 80% automated evidence, the
assurance program is underperforming.
· Benefits are measured against baseline, not projections.

Continuous assurance

Function Cadence Owner Output
Control testing (automated) Daily Platform Pass/fail in bau/COMPLIANCE/evidence/
Control testing (manual) Quarterly Internal Audit Signed attestation
SLO review Monthly SRE Error budget report
Cost review Monthly Finance Variance report
Access review Quarterly Security Attestation
Vendor review Annual Procurement Scorecard
Halal certification renewal Annual Compliance Updated certificate
Board reporting Quarterly Governance Assurance report

Audit automation

Every control in bau/COMPLIANCE/controls-matrix.md has one of:

· Automated evidence — CI job produces artifacts on schedule
· Semi-automated — system produces, human attests
· Manual — documented process with checklist

Target: 80% of controls produce automated evidence by end of year 1.

Current status tracked in .cline_inbox/manifests/controls.yaml via the
evidence_type field per control.

Regulatory change monitoring

Source Monitored by Cadence Escalation
Halal certification bodies (JAKIM, MUI, GCC) Compliance Monthly 30-day policy review
Data protection (GDPR, PDPA, CCPA) Legal + DPO Continuous 14-day review
Financial (AAOIFI, central bank) Finance + Shariah Board Monthly 7-day review
Chain (Polygon protocol changes) Platform Quarterly Next release cycle
Security advisories Security Immediate 24-hour response

Benefits realization

Track against business case in
bau/CONTINUOUS-IMPROVEMENT/benefits-realization.md.

Benefit Baseline Target Measurement
Vendor onboarding time 14 days 3 days Median, rolling 30d
Certificate verification time 48 hours 1 hour P95
Compliance violations caught pre-sale unknown ≥ 99% Ratio
Cost per halal verdict unknown benchmark −30% Monthly
Auditor hours per audit 400 80 Per audit cycle
Vendor retention unknown +20pp Annual

Benefits are not a reason to compromise P1–P10. A cost saving that
breaks the halal gate is a loss, not a benefit.

Deliverables (continuous)

# Deliverable Cadence Owner
D1 Continuous assurance dashboard Live Internal Audit
D2 Monthly SLO + cost report Monthly SRE + Finance
D3 Quarterly attestation package Quarterly Internal Audit
D4 Regulatory watch log Continuous Legal + Compliance
D5 Benefits realization report Monthly Product
D6 Board assurance report Quarterly Governance
D7 Annual benefits review Annual Steering + Board
D8 Automated control evidence (80%+) Continuous Platform

Terminal exit gate criteria

These are steady-state criteria, checked once per year and after any major
change. They are not phase-exit criteria — this phase does not exit.

# Criterion Evidence Verifier
G9.1 80% of controls produce automated evidence controls.yaml analysis Internal Audit
G9.2 Zero unmanaged control exceptions Exception register Internal Audit
G9.3 Benefits tracked monthly for 12 months Reports Steering
G9.4 Regulatory watch caught and responded to ≥ 1 change Log entries Legal
G9.5 Quarterly board reports delivered Meeting minutes Board Audit Committee
G9.6 Zero P1–P10 violations in the reporting period Arch test dashboard Arch Review Board
G9.7 Halal certification renewed on schedule Certificate Compliance
G9.8 No open SEV1 or SEV2 without postmortem Incident log Governance

Risks

Risk Likelihood Impact Mitigation Owner
Assurance degrades over time High High Automated evidence; quarterly attestation Internal Audit
Cost pressure erodes retention Medium Critical Retention is P-adjacent, non-negotiable, escalated to Board Compliance
Regulatory change missed Low Critical Multiple monitoring sources; cross-checked Legal + DPO
Board reporting becomes ceremonial High Medium Reports tied to actionable metrics; decisions required Governance
Benefits not realized Medium High Monthly tracking; owner presents plan when off-track Product
Manual controls slip Medium Medium Target 80% automated; manual controls audited quarterly Internal Audit
Halal certification body change Medium High Multiple bodies maintained; contingency body identified Compliance

RACI

Activity Internal Audit Governance SRE Finance Compliance Legal Board
Control testing R/A C C I C I I
SLO review I C R C I I I
Cost review I C C R I C I I
Regulatory watch C C C C R R I
Benefits tracking C C I C I I A
Board reporting R R/A C C C C A
Halal renewal C C I I R I I
Attestation R/A C C I C I I

Out of scope

Nothing. This phase has the broadest mandate of any phase.

Terminal state declaration

When all G9 criteria hold for 12 consecutive months, the enterprise declares:

HalalChain is enterprise-grade. Governance, assurance, and continuous
improvement operate as steady-state functions. No further project-mode
phases are required.

This declaration is signed by:

· Board Audit Committee Chair
· Enterprise Governance Lead
· CISO
· Compliance Lead
· Shariah Advisor

And filed at bau/GOVERNANCE/enterprise-grade-declaration.md.

This is CLOSE ✅ FINAL.

Verification

```bash
.cline_inbox/hooks/gate-check.sh gate-009-final
```

Unlike phase gates, this check runs continuously, not once. A failure is an
immediate escalation to the Board Audit Committee.
EOF_ENT009

═══════════════════════════════════════════════════════════════════════

Verify

═══════════════════════════════════════════════════════════════════════

echo "✅ Wrote 4 phase charters to $PHASES"
echo
echo "Character by character total line counts per phase:"
for d in ENT-006-govern ENT-007-author ENT-008-complete ENT-009-close; do
  f="$PHASES/$d/CHARTER.md"
  if [[ -f "$f" ]]; then
    lines=$(wc -l < "$f")
    first_line=$(head -1 "$f" | sed 's/[^a-zA-Z0-9 ]//g')
    printf "  %-22s %5s lines  %s\n" "$d" "$lines" "$first_line"
  else
    printf "  %-22s MISSING\n" "$d"
    exit 1
  fi
done

echo "✅ All phase charters written successfully."
EOF
