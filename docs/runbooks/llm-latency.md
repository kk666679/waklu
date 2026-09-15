# Runbook: LLM Latency High

**Alert:** `AiInference_LlmLatency_High`
**Severity:** Warning
**SLO:** ai-inference LLM P95 latency (30s)

## Symptoms
- ai-inference /llm* P95 latency > 30s for 10 minutes
- LLM requests taking too long

## Triage
1. Check which LLM provider is being used
2. Check LLM provider status
3. Check GPU memory pressure
4. Check for large prompts

## Mitigation
1. **GPU memory pressure?** -> Follow `docs/runbooks/gpu-memory.md`
2. **LLM provider issue?** -> Switch to fallback provider
3. **Large prompts?** -> Optimize prompt size
4. **Scale ai-inference?** -> Increase replica count

## Postmortem triggers
- Any LLM latency alert lasting > 30 minutes