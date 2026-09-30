# ENT-003 — Production Readiness, Security & Compliance Certification

**Role:** Certify
**Owner:** Security & Compliance
**Approver:** CISO + External Auditor
**Duration:** 8 weeks
**Exit:** Gate `gate-003-004`

## Objective

Independent verification that the platform meets its security, privacy, and
halal-compliance claims. **No self-certification.** Every claim is falsified
by an independent reviewer.

## Mandatory constraints

- **P1–P10** are the certification substrate. Any violation found during
  certification is a finding, not a discussion.
- **No certification claim without a falsification attempt.** Every "compliant"
  statement is paired with an attempt to disprove it. Failed falsification is
  the evidence.
- **Registrar key custody is audited at the KMS level**, not just at the
  application level.

## Certifications targeted

| Framework | Scope | Auditor | Evidence |
|---|---|---|---|
| SOC 2 Type II | Platform, storage, agents | External | Continuous control testing |
| ISO 27001:2022 | ISMS | External | Documented ISMS |
| GDPR | EU data handling | External + DPO | DPA, DPIA, RoPA |
| PCI DSS SAQ-A | Payment flow (no card data stored) | External | SAQ-A questionnaire |
| Halal assurance | Compliance gate integrity | Halal certification body | Audit trail + verdict traceability |

## Security testing

| Test | Owner | Pass criteria | Evidence |
|---|---|---|---|
| External penetration test | Third party | Zero critical, zero high | Signed report |
| Internal red team (agent exploit) | Security | No path from `agents` to blob write | Report with attack graphs |
| Verdict-leak fuzzing | Security | No code path produces verdict outside `tawheed` | Fuzzer output |
| Registrar key compromise drill | Security | Documented recovery within 4 hours | Drill log |
| Supply chain audit | Platform | Zero known high/critical | `dotnet list package --vulnerable` |
| Contract audit | Third party | Zero high, all mediums triaged | Auditor report |
| Dependency confusion check | Security | No internal names in public registries | Report |
| Secrets scan | Security | No secrets in git history | TruffleHog output |

## Falsification requirement

For every control in `.cline_inbox/bau/COMPLIANCE/controls-matrix.md`, an independent
reviewer documents:

1. The claim ("control X prevents Y")
2. One concrete attempt to falsify the claim
3. The result: **falsified** or **held**
4. If falsified: remediation plan and re-test

**The certification cannot complete if any control is falsified and
unremediated.**

The full falsification record is filed at
`.cline_inbox/phases/ENT-003-certify/falsification-report.md`.

## Compliance evidence collection

Every control requires:

- A **test** or process that produces evidence
- An **automated or manual capture**
- A **retention schedule** meeting audit requirements
- A **location** in `.cline_inbox/bau/COMPLIANCE/evidence/`

Target: **80% of controls produce automated evidence** by end of phase.
Below 80%, the phase does not close.

## Deliverables

| # | Deliverable | Location | Owner |
|---|---|---|---|
| D1 | SOC 2 Type II report | `.cline_inbox/bau/COMPLIANCE/evidence/soc2-type2-signed.pdf` | Security |
| D2 | ISO 27001 certificate | `.cline_inbox/bau/COMPLIANCE/evidence/iso27001-cert.pdf` | Security |
| D3 | GDPR compliance pack (DPIA, RoPA, DPA) | `.cline_inbox/bau/COMPLIANCE/gdpr/` | DPO |
| D4 | PCI DSS SAQ-A | `.cline_inbox/bau/COMPLIANCE/evidence/pci-saq-a.pdf` | Security |
| D5 | Halal assurance certificate | `.cline_inbox/bau/COMPLIANCE/evidence/halal-cert.pdf` | Compliance |
| D6 | External pentest report | `.cline_inbox/bau/COMPLIANCE/evidence/pentest-findings.yaml` | Security |
| D7 | Red team report (agent exploit) | `.cline_inbox/bau/COMPLIANCE/evidence/red-team-agent.pdf` | Security |
| D8 | Verdict-leak fuzz report | `.cline_inbox/bau/COMPLIANCE/evidence/verdict-fuzz-report.yaml` | Security |
| D9 | Registrar key compromise drill log | `.cline_inbox/bau/COMPLIANCE/evidence/registrar-drill-log.md` | Security |
| D10 | Supply chain audit | `.cline_inbox/bau/COMPLIANCE/evidence/supply-chain-audit.pdf` | Platform |
| D11 | Falsification report | `.cline_inbox/phases/ENT-003-certify/falsification-report.md` | Independent reviewer |
| D12 | Control evidence index | `.cline_inbox/bau/COMPLIANCE/evidence/index.yaml` | Compliance |

## Exit gate criteria

| # | Criterion | Evidence | Verifier |
|---|---|---|---|
| G3.1 | SOC 2 Type II report signed | PDF | External auditor |
| G3.2 | ISO 27001 certificate issued | PDF | External auditor |
| G3.3 | GDPR compliance pack reviewed by DPO | Signed pack | DPO |
| G3.4 | PCI DSS SAQ-A signed | PDF | External auditor |
| G3.5 | Halal assurance certificate issued | PDF | Halal certification body |
| G3.6 | Zero critical, zero high pentest findings | Report | CISO |
| G3.7 | Zero agent-to-blob-write paths | Red team report | CISO |
| G3.8 | Verdict-leak fuzzing: no leaks found | Fuzz report | CISO |
| G3.9 | Registrar key drill: recovery within 4 hours | Drill log | Security |
| G3.10 | Supply chain: zero known high/critical | Audit report | Platform Eng |
| G3.11 | Falsification: all controls held or remediated | Falsification report | Independent reviewer |
| G3.12 | 80% automated control evidence | Evidence index | Compliance Lead |

## Risks

| Risk | Likelihood | Impact | Mitigation | Owner |
|---|---|---|---|---|
| Pentest finds critical late | Medium | Critical | Dry-run pentest at mid-phase; remediate before final | Security |
| Red team finds agent exploit | Medium | Critical | Same — dry run at mid-phase | Security |
| Registrar key drill fails | Low | Critical | Pre-drill walkthrough; KMS vendor on standby | Security |
| Auditor scope creep | Medium | High | Fixed-scope SOW; change requests through CAB | Program Mgmt |
| Halal certification body delays | Medium | High | Engage early; multiple certification bodies in parallel | Compliance |
| Falsification reveals systemic gap | Low | Critical | Escalate to Steering immediately; do not certify | Independent reviewer |
| Evidence collection not automated enough | High | Medium | Automate during ENT-002, not after | Platform Eng |

## RACI

| Activity | Security | Compliance | DPO | Platform Eng | External Auditor |
|---|---|---|---|---|---|
| Pentest | R | I | I | C | A |
| Red team | R | I | I | C | I |
| Verdict-leak fuzz | R | C | I | C | I |
| Registrar drill | R | I | I | C | I |
| Supply chain | C | I | I | R | I |
| SOC 2 | R | C | C | C | A |
| ISO 27001 | R | C | C | C | A |
| GDPR | C | C | R | C | A |
| PCI SAQ-A | R | I | I | C | A |
| Halal assurance | C | R | I | I | A |
| Falsification | I | I | I | I | R/A |

## Out of scope

- Production deployment (ENT-004)
- Remediation of findings (loops back to ENT-002 if needed)
- Multi-region certification (extended in ENT-005)

## Handoff to ENT-004

At phase close, Release Management receives:

- All certification artifacts
- All security test reports
- Falsification record (all controls held)
- Control evidence index with 80%+ automated
- Registrar key custody attestation

Verification:

```bash
.cline_inbox/hooks/gate-check.sh gate-003-004
```

A failed gate means the platform does not release. The Certification Owner
and Release Manager jointly escalate to Steering.
