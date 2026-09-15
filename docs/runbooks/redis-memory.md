# Runbook: Redis Memory Pressure

**Alert:** `Redis_MemoryPressure`
**Severity:** Warning

## Symptoms
- Redis memory usage > 85% for 10 minutes
- Redis may start evicting keys

## Triage
1. Check Redis memory usage:
   ```bash
   redis-cli INFO memory
   ```
2. Check which keys are using memory
3. Check eviction policy

## Mitigation
1. **Eviction policy?** -> Set to `allkeys-lru` if not already
2. **Large keys?** -> Identify and compress/delete
3. **Scale Redis?** -> Add replica
4. **Increase maxmemory?** -> `CONFIG SET maxmemory 1gb`

## Postmortem triggers
- Any Redis memory alert lasting > 30 minutes
- Any data loss due to eviction