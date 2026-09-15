# Runbook: Tawheed High Verdict Latency

**Alert:** `Tawheed_VerdictLatencyP95_High`
**Severity:** Warning
**SLO:** tawheed-verdict-latency (1500ms)

## Symptoms
- Tawheed P95 verdict latency > 1.5s for 5 minutes
- Verifications taking longer than expected

## Triage
1. Check if it's a specific agent or all agents
2. Check agent health:
   ```bash
   curl http://tawheed:8000/v1/agents/health
   ```
3. Check LLM latency (if using external LLM)
4. Check database query performance

## Mitigation
1. **LLM latency?** -> Check LLM provider, follow `docs/runbooks/llm-latency.md`
2. **Agent issue?** -> Check agent logs
3. **Database slow?** -> Follow `docs/runbooks/postgres-slow-queries.md`
4. **Scale tawheed?** -> Increase replica count

## Postmortem triggers
- Any latency alert lasting > 30 minutes