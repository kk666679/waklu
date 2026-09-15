# Runbook: Latency P95 Breach

**Alert:** `PlatformApi_LatencyP95_Breach`
**Severity:** Warning
**SLO:** platform-api-latency (500ms)

## Symptoms
- P95 latency > 500ms for 10 minutes
- Users experiencing slow responses

## Triage
1. Open [API SLO dashboard](https://grafana.halalchain.dev/d/api-slo)
2. Check "Latency Heatmap" panel - which routes are slow?
3. Check if it's a specific route or global
4. Check downstream dependencies (tawhee, ai-inference, postgres)
5. Check for recent deploys

## Mitigation
1. **Recent deploy?** -> Roll back
2. **Single slow route?** -> Feature flag off, investigate
3. **Database slow?** -> Follow `docs/runbooks/postgres-slow-queries.md`
4. **Dependency slow?** -> Follow the dependency's runbook

## Postmortem triggers
- Any latency alert lasting > 30 minutes
- Any customer-visible latency degradation