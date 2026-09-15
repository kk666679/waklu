# Runbook: AI Inference Error Rate High

**Alert:** `AiInference_ErrorRate_High`
**Severity:** Warning

## Symptoms
- ai-inference error rate > 5% for 10 minutes
- AI requests failing

## Triage
1. Check which endpoints are failing
2. Check LLM provider status
3. Check for authentication issues
4. Check GPU health

## Mitigation
1. **LLM provider issue?** -> Switch to fallback provider
2. **Auth issue?** -> Rotate API keys
3. **GPU issue?** -> Follow `docs/runbooks/gpu-memory.md`
4. **Scale ai-inference?** -> Increase replica count

## Postmortem triggers
- Any error rate alert lasting > 30 minutes