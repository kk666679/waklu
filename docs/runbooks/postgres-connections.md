# Runbook: PostgreSQL High Connections

**Alert:** `Postgres_HighConnections`
**Severity:** Warning

## Symptoms
- PostgreSQL connection usage > 80% for 10 minutes
- New connections may be rejected

## Triage
1. Check current connections:
   ```sql
   SELECT count(*), datname FROM pg_stat_activity GROUP BY datname;
   ```
2. Check max_connections setting
3. Check for connection leaks

## Mitigation
1. **Connection leak?** -> Identify and fix the leaking service
2. **Increase max_connections?** -> `ALTER SYSTEM SET max_connections = 500;`
3. **Use connection pooling?** -> Enable PgBouncer
4. **Scale PostgreSQL?** -> Add read replica

## Postmortem triggers
- Any connection alert lasting > 30 minutes
- Any connection rejection