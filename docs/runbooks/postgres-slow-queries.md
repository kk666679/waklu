# Runbook: PostgreSQL Slow Queries

**Alert:** `Postgres_SlowQueries`
**Severity:** Warning

## Symptoms
- Average query time > 1s for 10 minutes
- Database queries taking too long

## Triage
1. Check slow queries:
   ```sql
   SELECT query, mean_time, calls FROM pg_stat_statements ORDER BY mean_time DESC LIMIT 10;
   ```
2. Check for missing indexes
3. Check for table bloat
4. Check for lock contention

## Mitigation
1. **Missing index?** -> Create index
2. **Table bloat?** -> VACUUM or REINDEX
3. **Lock contention?** -> Identify blocking queries
4. **Query issue?** -> Optimize query or add caching

## Postmortem triggers
- Any slow query alert lasting > 30 minutes