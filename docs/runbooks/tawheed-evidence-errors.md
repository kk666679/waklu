# Runbook: Tawheed Evidence Fetch Errors

**Alert:** `Tawheed_EvidenceFetchErrors`
**Severity:** Warning

## Symptoms
- Tawheed evidence collection failing > 1/s
- Agents returning errors

## Triage
1. Check which agents are failing:
   ```bash
   curl http://tawheed:8000/v1/agents/health
   ```
2. Check agent logs for specific errors
3. Check external dependencies (certificate APIs, supplier databases)
4. Check if it's a transient network issue

## Mitigation
1. **Specific agent failing?** -> Disable that agent, investigate
2. **External dependency down?** -> Follow the dependency's runbook
3. **Network issue?** -> Check network policies
4. **Scale tawheed?** -> Increase replica count

## Postmortem triggers
- Any evidence fetch error lasting > 10 minutes