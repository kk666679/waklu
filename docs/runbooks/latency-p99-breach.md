# Runbook: Latency P99 Breach

**Alert:** `PlatformApi_LatencyP99_Breach`
**Severity:** Critical
**SLO:** platform-api-latency (1.5s)

## Symptoms
- P99 latency > 1.5s for 5 minutes
- Some users experiencing very slow responses

## Triage
1. Open [API SLO dashboard](https://grafana.halalchain.dev/d/api-slo)
2. Check "Latency Heatmap" panel
3. Query Loki for slow requests:
   ```
   {env="prod", service_name="platform-api"} | json | duration_ms > 1500 | json
   ```
4. Check for correlated trace IDs in Tempo

## Mitigation
1. **Recent deploy?** -> Roll back immediately
2. **Single slow route?** -> Feature flag off
3. **Database issue?** -> Follow `docs/runbooks/postgres-slow-queries.md`
4. **Dependency issue?** -> Follow the dependency's runbook

## Postmortem triggers
- Any critical latency alert lasting > 10 minutes
- Any customer-visible latency degradation