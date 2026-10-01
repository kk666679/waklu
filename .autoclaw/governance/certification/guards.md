# Certification Guards — Reference

Every guard named in `transitions.yaml`, with its source of truth and its
condition. A guard added to `transitions.yaml` without an entry here is a guard
nobody can audit; `AutoclawStructureTests.Certification_Guards_Are_Documented`
fails the build on that.

---

## participant_identity_verified

**Source:** `participants/registry`
**Condition:** `participant.verification.status == "verified"`
**and** `participant.verification.expires_at > now()`

Used by: T01

---

## tenant_scope_matches

**Source:** `tenants/model.yaml`
**Condition:** `request.tenant_id == participant.tenant_id`

Used by: T01

---

## evidence_completeness

**Source:** `evidence/chain/`
**Formula:**
`count(evidence.state == "verified") / count(evidence.required_by_scheme)`

The denominator is scheme-specific. Under MS-1500 it is
`governance/certification/schemes/ms1500.yaml → required_evidence`.

Used by: T03 (threshold `>= 0.9`)

---

## laboratory_accreditation_passed

**Source:** `compliance/rules/laboratory-accreditation.yaml`
**Condition:** the rule returned `outcome == "compliant"` for every laboratory
report attached to this application.

A report with no accreditation record at all is *not* compliant. Absence of
evidence is not evidence of compliance, and this rule is the first place that
could silently become otherwise.

Used by: T03

---

## delegation_valid_if_consultant_involved

**Source:** `compliance/rules/delegated-authority-check.yaml`
**Condition:** the rule returned `outcome != "denied"` for the acting
consultant.

Only evaluated when a consultant is a party to the application. When no
consultant is involved the guard holds vacuously.

Used by: T03

---

## all_corrective_actions_verified

**Source:** `governance/certification/audit.jsonl`
**Condition:** every corrective action raised at T05 has a matching resolution
event whose attached evidence reached state `verified`.

Used by: T06

---

## no_outstanding_blocking_rule_violations

**Source:** `compliance/rules/*.yaml` (all)
**Condition:** no rule with `severity == "blocking"` returned
`outcome == "non-compliant"`.

Warning and informational severities do not block. They are still recorded in
the evaluation result and surfaced to the reviewer.

Used by: T07

---

## shariah_board_approval_present

**Source:** evidence of type `shariah-opinion`
**Condition:** there exists evidence where `evidence.type == "shariah-opinion"`
**and** `state == "verified"`
**and** `applies_to == application.id`

The count of such opinions is scheme-dependent: `ms1500.yaml` requires two.

Used by: T08

---

## application.completeness

**Source:** `evidence/` — the application record's declared field set
**Formula:** `count(provided required fields) / count(required fields)`
**Threshold at T02:** `>= 0.5`

Used by: T02

---

## tawheed.evaluate.result

**Source:** `tawheed/api/evaluate.md` — the only authoritative evaluation call.
**Values consumed by this machine:** `compliant`, `requires-governance`

Agents cannot produce either value. A proposal that arrives carrying a
`result` field is discarded before it reaches the machine.

Used by: T07, T08, T13

---

## Guards referenced but not defined here

`audit.report_present`, `audit.auditor_accreditation_valid`, `audit.tenant_matches`,
`audit.findings`, `corrective_action.evidence_complete`, `rejection_reason_present`,
`suspension_reason_present`, `suspension_reason_resolved`, `re_inspection_completed`,
`renewal_evidence_complete`, `certificate.scope`, `certificate.valid_until`.

These read directly off the audit and certificate records rather than off a
derived dataset, so they are field comparisons with no external source. They are
listed here so the set of guard names is complete, and the documentation test
only requires the name to appear in this file.
