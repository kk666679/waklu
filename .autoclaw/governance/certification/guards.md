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

**Expression as written in transitions.yaml:** `evidence_completeness >= 0.9`

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
**Expression as written in transitions.yaml:** `application.completeness >= 0.5`
**Threshold at T02:** `>= 0.5`

Used by: T02

---

## tawheed.evaluate.result

**Source:** `tawheed/api/evaluate.md` — the only authoritative evaluation call.
**Values consumed by this machine:** `compliant`, `requires-governance`

Agents cannot produce either value. A proposal that arrives carrying a
`result` field is discarded before it reaches the machine.

**Expressions as written in transitions.yaml:**

- `tawheed.evaluate.result == "requires-governance"` — T07, gates entry into
  GovernanceReview. A result of `compliant` here is *not* sufficient to certify;
  it means tawheed found nothing requiring a board, so a human still decides.
- `tawheed.evaluate.result == "compliant"` — T08 and T13, the two authoritative
  transitions. This is the only place in the machine where tawheed's output is
  load-bearing, and it is load-bearing in the direction of *permitting* a
  human decision, never of making one.

Used by: T07, T08, T13

---

## Guards that read directly off the audit record

These are field comparisons against the audit and certificate records rather
than a derived dataset, so they have no external source beyond the record
itself. They are documented individually because a guard with no stated source
is a guard nobody can verify.

### audit.report_present

**Source:** the audit report attached at T04
**Expression:** `audit.report_present == true`
**Condition:** an audit report object is attached to the application and is
non-empty. A report that exists but is empty fails: absence of evidence is not
evidence.

Used by: T04

---

### audit.auditor_accreditation_valid

**Source:** `compliance/rules/laboratory-accreditation.yaml`, and the auditor's
participant record in `participants/`
**Expression:** `audit.auditor_accreditation_valid == true`
**Condition:** the accreditation covering the audit at the time it was performed
was valid and in scope for the scheme. An accreditation that has since lapsed
does not retroactively validate an audit it covered.

Used by: T04

---

### audit.tenant_matches

**Source:** `tenants/model.yaml`
**Expression:** `audit.tenant_matches == true`
**Condition:** the audit was performed against the same tenant as the
application. A cross-tenant audit is a denial, not a warning — this is the guard
that makes an audit usable as evidence at all.

Used by: T04

---

### audit.findings.count

**Source:** the audit report attached at T04
**Expression:** `audit.findings.count > 0`
**Condition:** the audit raised at least one finding. Paired with
`audit.findings[].severity contains "major"` below, which is the guard that
actually gates the transition — a count of findings consisting only of
informational items does not warrant a corrective-action cycle.

Used by: T05

---

### audit.findings[].severity

**Source:** the audit report attached at T04
**Expression:** `audit.findings[].severity contains "major"`
**Condition:** at least one finding carries severity `major`. Minor and
informational findings are recorded and surfaced to the reviewer but do not
open a corrective-action task.

Used by: T05

---

### corrective_action.evidence_complete

**Source:** `governance/certification/audit.jsonl`, and the corrective-action
resolution events emitted at T06
**Expression:** `corrective_action.evidence_complete == true`
**Condition:** every corrective action raised at T05 has a resolution event
whose attached evidence reached state `verified`. Note that this is the
per-finding completeness check; `all_corrective_actions_verified` is the
aggregate. Both are evaluated at T06 and both must hold.

Used by: T06

---

### certificate.valid_until

**Source:** the certificate record itself
**Expression:** `certificate.valid_until < now()`
**Condition:** the certificate's validity window has closed. Expiry is the one
authoritative transition whose source role is `system` rather than
`certification-body`, because a certificate does not stop being valid because
nobody noticed the date.

Used by: T11

---

### certificate.scope

**Source:** the issued certificate and the original application
**Expression:** `certificate.scope == application.scope`
**Condition:** the scope of the certificate being issued matches the scope that
was applied for — product, site, and period. A certificate broader than its
application is a scope escalation and this guard is where it is stopped.

Used by: T08

---

### re_inspection_completed

**Source:** `governance/certification/audit.jsonl`
**Expression:** `re_inspection_completed == true`
**Condition:** a re-inspection was performed after the suspension reason was
resolved, and reached a recorded outcome. Resolution of the *reason* is not
resolution of the *certificate*; both are required before reinstatement.

Used by: T12

---

### renewal_evidence_complete

**Source:** `evidence/` — the renewal submission
**Expression:** `renewal_evidence_complete == true`
**Condition:** the renewal application carries the evidence set its scheme
requires. Renewal issues a new certificate, so this is the completeness check
that T02 performs for a first application, restated for the renewal path.

Used by: T13

---

### rejection_reason_present

**Source:** the rejection decision recorded at T09
**Expression:** `rejection_reason_present`
**Condition:** a reason is recorded before the application is rejected. T09 is
an authoritative transition and its audit record is immutable, so an
unattributed rejection cannot later be explained, contested, or appealed.

Rejected is a terminal state, so this guard is the only place the reason is ever
required; there is no later transition that could supply it.

Used by: T09

---

### suspension_reason

**Source:** the suspension notice recorded at T10
**Expressions:**

- `suspension_reason_present` — a reason is recorded before suspension. Used by
  T10.
- `suspension_reason_resolved` — the reason recorded at T10 no longer applies.
  Used by T12.
- `( suspension_reason IN [fraud-suspected, evidence-invalidated, regulator-directive] )`
  — the reason is one of the three that carry a downstream consequence beyond
  the suspension itself: producer and buyer notification, and a block on
  downstream verifications. A reason outside this set suspends the certificate
  without the wider notification. Used by T10.

The closed enumeration is deliberate. An open-ended reason field would let a
suspension quietly take the consequential path or quietly avoid it, and the
difference is who gets told.
