# Runbook: Circuit Breaker Open

**Alert:** `Web_CircuitBreakerOpen`
**Severity:** Warning

## Symptoms
- Circuit breaker is open for 1 minute
- API calls to a specific target are failing fast

## Triage
1. Check which circuit breaker is open
2. Check the target service health
3. Check error rate on the target
4. Check recent deploys

## Mitigation
1. **Target service down?** -> Follow `docs/runbooks/service-down.md`
2. **Target service slow?** -> Follow that service's runbook
3. **Wait for half-open?** -> Circuit breaker will auto-recover
4. **Manual reset?** -> If auto-recovery fails

## Postmortem triggers
- Any circuit breaker open > 5 minutes
- Any customer-visible failure due to circuit breaker