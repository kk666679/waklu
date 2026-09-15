# Runbook: OTel Collector Refusing Data

**Alert:** `OtelCollector_RefusingData`
**Severity:** Warning

## Symptoms
- OTel collector refusing spans or metric points
- Data loss in observability pipeline

## Triage
1. Check collector logs:
   ```bash
   docker logs otel-collector --tail=100
   ```
2. Check collector metrics:
   ```bash
   curl http://localhost:8888/metrics
   ```
3. Check for configuration errors
4. Check for memory pressure

## Mitigation
1. **Config error?** -> Fix config, restart collector
2. **Memory pressure?** -> Increase memory limit
3. **Pipeline error?** -> Check processor configuration
4. **Restart collector?** -> `docker restart otel-collector`

## Postmortem triggers
- Any data refusal alert lasting > 5 minutes
- Any observability data loss