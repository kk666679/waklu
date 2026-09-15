# Runbook: Redis Eviction Rate High

**Alert:** `Redis_EvictionRate_High`
**Severity:** Warning

## Symptoms
- Redis evicting keys > 100/s for 5 minutes
- Memory pressure causing evictions

## Triage
1. Check eviction rate:
   ```bash
   redis-cli INFO stats | grep evicted_keys
   ```
2. Check memory usage
3. Check which keys are being evicted

## Mitigation
1. **Increase maxmemory?** -> `CONFIG SET maxmemory 2gb`
2. **Reduce key count?** -> Delete unused keys
3. **Scale Redis?** -> Add replica
4. **Enable cluster mode?** -> Distribute data

## Postmortem triggers
- Any eviction alert lasting > 10 minutes
- Any data loss due to eviction