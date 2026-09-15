# Runbook: Outbox Dead Letter Growth

**Alert:** `OutboxDeadLetter_Growth`
**Severity:** Warning

## Symptoms
- Outbox dead-letter growth > 5/hr
- Events failing permanently

## Triage
1. Check dead letter queue:
   ```sql
   SELECT * FROM OutboxMessages WHERE Error IS NOT NULL ORDER BY CreatedAt DESC LIMIT 10;
   ```
2. Check error patterns
3. Check if it's a specific event type

## Mitigation
1. **Specific event type?** -> Investigate that event handler
2. **Transient errors?** -> Retry failed events
3. **Permanent failures?** -> Move to dead letter queue, notify stakeholders
4. **Fix root cause?** -> Deploy fix, replay dead letter events

## Postmortem triggers
- Any dead letter growth alert lasting > 1 hour
- Any data loss due to dead letter