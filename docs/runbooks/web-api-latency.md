# Runbook: Web API Latency High

**Alert:** `Web_HighApiLatency`
**Severity:** Warning

## Symptoms
- Web client API P95 latency > 1000ms for 10 minutes
- Web UI slow to respond

## Triage
1. Check which routes are slow
2. Check downstream services
3. Check circuit breaker state
4. Check for recent deploys

## Mitigation
1. **Downstream slow?** -> Follow that service's runbook
2. **Circuit breaker open?** -> Follow `docs/runbooks/circuit-breaker.md`
3. **Recent deploy?** -> Roll back
4. **Scale Web?** -> Increase replica count

## Postmortem triggers
- Any API latency alert lasting > 30 minutes