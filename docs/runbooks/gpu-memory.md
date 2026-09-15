# Runbook: GPU Memory Pressure

**Alert:** `AiInference_GpuMemoryPressure`
**Severity:** Warning

## Symptoms
- GPU memory usage > 90% for 5 minutes
- AI inference requests failing due to OOM

## Triage
1. Check GPU memory usage:
   ```bash
   kubectl top nodes -n prod
   ```
2. Check which models are loaded
3. Check for memory leaks

## Mitigation
1. **Reduce batch size** -> Lower concurrent inference requests
2. **Unload unused models** -> Remove models not in use
3. **Scale GPU nodes** -> Add more GPU nodes
4. **Restart ai-inference** -> Clear GPU memory

## Postmortem triggers
- Any GPU memory alert lasting > 10 minutes
- Any OOM kill of ai-inference pod