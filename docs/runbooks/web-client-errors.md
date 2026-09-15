# Runbook: Web Client Error Rate High

**Alert:** `Web_HighClientErrorRate`
**Severity:** Warning

## Symptoms
- Web client error rate > 5% for 10 minutes
- API calls from Web UI failing

## Triage
1. Check which routes are failing
2. Check if it's a specific browser/version
3. Check circuit breaker state
4. Check downstream services

## Mitigation
1. **Circuit breaker open?** -> Follow `docs/runbooks/circuit-breaker.md`
2. **Downstream service down?** -> Follow that service's runbook
3. **Specific route?** -> Feature flag off
4. **Scale Web?** -> Increase replica count

## Postmortem triggers
- Any client error alert lasting > 30 minutes