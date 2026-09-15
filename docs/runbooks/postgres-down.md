# Runbook: PostgreSQL Down

**Alert:** `Postgres_Down`
**Severity:** Critical

## Symptoms
- PostgreSQL is unreachable for 1 minute
- All database operations failing

## Triage
1. Check PostgreSQL container status:
   ```bash
   docker ps -a | grep postgres
   ```
2. Check PostgreSQL logs:
   ```bash
   docker logs postgres --tail=100
   ```
3. Check disk space
4. Check memory pressure

## Mitigation
1. **Container crashed?** -> `docker restart postgres`
2. **Disk full?** -> Follow `docs/runbooks/disk-space.md`
3. **OOM?** -> Increase memory limit
4. **Data corruption?** -> Restore from backup

## Postmortem triggers
- Any PostgreSQL down > 5 minutes
- Any data loss event