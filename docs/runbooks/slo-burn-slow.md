# Runbook: SLO Error Budget Burning Slow (6x rate)

**Alert:** `PlatformApi_ErrorBudgetBurn_Slow`
**Severity:** Warning
**SLO:** platform-api-availability (99.9%)

## Symptoms
- 5xx error ratio > 0.6% over 30 minutes
- Same ratio sustained over 6 hours
- Error budget is eroding but slowly

## Impact
- Error budget will exhaust in ~5 days if sustained
- Not customer-visible yet, but trending toward SLO breach

## Triage (target: 15 minutes)
1. Open [API SLO dashboard](https://grafana.halalchain.dev/d/api-slo)
2. Check "Top Routes by Error Rate" table
3. Check if there's a gradual increase over the 6h window
4. Query Loki for recent errors:
   ```
   {env="prod", service_name="platform-api"} |= "level=error" | json | line_format "{{.message}}"
   ```

## Mitigation
1. **Identify the root cause** - is it a specific route, dependency, or systemic issue?
2. **If a single route is degrading:** feature flag it off
3. **If a dependency is degrading:** follow the dependency's runbook
4. **If no clear cause:** schedule investigation for the next on-call window

## Postmortem triggers
- Any warning severity alert lasting > 2 hours
- Any SLO breach

## Escalation
- **Primary:** on-call engineer
- **1 hour no resolution:** platform lead