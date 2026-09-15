# Runbook: Outbox Lag High

**Alert:** `OutboxLag_High`
**Severity:** Warning

## Symptoms
- Outbox lag > 60s for 5 minutes
- Events not being dispatched in a timely manner

## Triage
1. Check outbox pending count
2. Check outbox dispatcher logs
3. Check database performance
4. Check event handler health

## Mitigation
1. **Database slow?** -> Follow `docs/runbooks/postgres-slow-queries.md`
2. **Handler issue?** -> Check event handler logs
3. **Scale outbox?** -> Increase batch size or replica count
4. **Dead letter?** -> Move stuck messages to dead letter queue

## Postmortem triggers
- Any outbox lag alert lasting > 10 minutes
- Any event processing delay > 5 minutes