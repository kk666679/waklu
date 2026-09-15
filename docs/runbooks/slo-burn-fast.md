# Runbook: SLO Error Budget Burning Fast (14x rate)

**Alert:** `PlatformApi_ErrorBudgetBurn_Fast`
**Severity:** Critical
**SLO:** platform-api-availability (99.9%)
**Page:** Yes (PagerDuty + #alerts-critical)

## Symptoms
- 5xx error ratio > 1.44% over 5 minutes
- Same ratio sustained over 1 hour
- Users likely experiencing failures right now

## Impact
- Error budget at current rate exhausts in ~4 days
- Customer-visible
- Do **not** wait for confirmation from product

## Triage (target: 5 minutes)
1. Open [API SLO dashboard](https://grafana.halalchain.dev/d/api-slo)
2. Check "Top Routes by Error Rate" table - which route?
3. Check "Error Ratio by Service" - is it isolated to one service?
4. Query Loki for recent errors:
   ```
   {env="prod", service_name="platform-api"} |= "level=error" | json | line_format "{{.message}}"
   ```
5. Check `PlatformApi_DownstreamTawheedErrors` alert - dependency?
6. Check recent deploys:
   ```bash
   gh release list --limit 5
   git log --oneline -10 origin/main
   ```

## Mitigation (in order)
1. **Recent deploy in last 30 min?** -> Roll back:
   ```bash
   kubectl rollout undo deployment/platform-api -n prod
   ```
2. **Single route failing?** -> Feature flag off:
   ```json
   "services.<route>.enabled": false
   ```
   Reload: `kubectl rollout restart deployment/halalchain -n prod` (or hot-reload if enabled)
3. **Dependency down (tawheed/ai-inference)?** -> Follow `docs/runbooks/service-down.md`
4. **Database pressure?** -> Follow `docs/runbooks/postgres-connections.md`

## Postmortem triggers
- Any critical severity alert lasting > 15 minutes
- Any customer-visible 5xx originating from platform-api
- Any incident requiring rollback

## Escalation
- **Primary:** on-call engineer
- **15 min no resolution:** platform lead + CTO