# Runbook: Tawheed Determinism Violation (P0)

**Alert:** `Tawheed_DeterminismViolation`
**Severity:** P0 - treated like a data integrity incident
**Page:** Yes (PagerDuty P0 + #incident-p0)

## What this means
The Policy Engine produced **different verdicts for the same evidence + policy version**. This violates the core architectural principle:

> AI agents collect evidence. The deterministic Policy Engine decides compliance.

A violation means either:
1. A code path bypasses `PolicyEngine.evaluate` and calls something else
2. Evidence collection is non-deterministic (e.g., time-dependent, network-dependent)
3. Policy rules are non-deterministic (should be pure functions)
4. The fingerprint cache has a bug

## Immediate actions (first 5 minutes)
1. **Stop the bleeding**: disable the verification endpoint
   ```bash
   kubectl patch deployment platform-api -n prod --patch \
     '{"spec":{"template":{"spec":{"containers":[{"name":"platform-api","env":[{"name":"Halal__VerificationEnabled","value":"false"}]}]}}}}'
   ```
2. **Capture state** for forensics:
   ```bash
   # Find the subject_id from the alert
   kubectl logs -n prod -l app=tawheed --since=1h | grep "DETERMINISM VIOLATION"
   ```
3. **Notify**: #incident-p0, tag the platform lead, CTO, and compliance officer.

## Investigation (target: 30 minutes to root cause)
1. Identify the subject_id from the alert payload
2. Fetch the evidence bundle from Postgres:
   ```sql
   SELECT * FROM evidence_bundles WHERE subject_id = '<id>' ORDER BY collected_at DESC;
   ```
3. Re-run the evaluation twice with the same bundle:
   ```bash
   curl -X POST https://tawheed.internal/v1/products/<id>/verify \
     -H "X-Service-Key: $KEY" -d '{"product_id":"<id>"}' | jq .verdict
   # Repeat, compare
   ```
4. If verdicts differ -> **policy rules are impure**. Audit `app/policy/rules/*.py` for:
   - `datetime.now()` calls inside `evaluate()` (should be passed in)
   - Random number generation
   - External I/O
   - Dict iteration order dependence (use `sorted()`)
5. If verdicts are the same -> the fingerprint cache is buggy. Review `_fingerprint` in `engine.py`.

## Remediation
- **Impure rule** -> fix the rule, add a regression test
- **Cache bug** -> disable cache (`TAWHEED_FINGERPRINT_CACHE=false`), redeploy, fix, re-enable
- **Bypass path** -> audit `AgentOrchestrator` and `HalalController` for direct calls to anything other than `PolicyEngine.evaluate`

## Mandatory postmortem
This is a **P0 postmortem** - full RCA within 48h. Include:
- Timeline
- Root cause
- Blast radius (how many verdicts were affected?)
- Whether customer-facing decisions were impacted
- Whether regulatory notification is required

## Prevention
- Add property-based tests (Hypothesis) for every rule
- Add CI check: `grep -rn "datetime.now\|random\.\|uuid4" app/policy/rules/` must return zero
- Add nightly job that replays last 1000 evidence bundles and asserts identical verdicts