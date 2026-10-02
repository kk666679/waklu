# HalalChain.Automation

Scheduled jobs for the HalalChain platform. Runs as a background worker,
orchestrated by Aspire as a separate replica.

## The invariant

**Automation is a scheduler. It never decides.**

Every job delegates to an Application-layer handler or a port. No job
contains policy logic, inspects evidence, or writes a verdict. The test:
if you deleted the schedule and called the same handler on a button click,
the outcome would be identical.

## Jobs

| Job | Cron (UTC) | Purpose |
|---|---|---|
| `certificate-expiry-sweep` | `0 2 * * *` | Transition products on certificate expiry |
| `expiring-soon-notification` | `0 9 * * *` | Remind vendors before expiry |
| `suspended-listing-cleanup` | `0 3 * * 0` | Archive long-suspended listings |
| `evidence-retention-sweep` | `0 4 * * 0` | Apply evidence retention policy |
| `access-log-archive` | `0 5 1 * *` | Segment and archive the audit log |
| `anchor-cadence` | `0 * * * *` | Anchor pending Merkle batch |
| `chain-mirror-sync` | `*/5 * * * *` | Reconcile ChainMirror with chain |
| `outbox-dispatch` | `* * * * *` | Submit pending chain transactions |
| `periodic-revalidation` | `0 6 * * 1` | Trigger revalidation agent workflow |
| `escalation-reminder` | `0 */4 * * *` | Nudge unresolved escalations |
| `dead-letter-reprocess` | `*/15 * * * *` | Retry dead-lettered outbox entries |

## Distributed lock

Jobs run under a Postgres advisory lock. Two replicas of this worker will
not run the same job simultaneously. Jobs that are already idempotent
still acquire the lock — the lock prevents duplicate work, not duplicate
state.

## Adding a job

1. Implement `IScheduledJob` under `Jobs/<Category>/`.
2. Delegate to an Application-layer handler. Do not add logic here.
3. Register with `RegisterJob<T>()` in `Program.cs`.

There is **no test project for this host** — `HalalChain.Automation.Tests` does
not exist and there is no `tests/` directory in this project, so step 4 of the
original checklist ("add a test under `tests/`") cannot currently be completed.
Adding a job therefore means adding coverage elsewhere (or creating the test
project) — do not assume a local test harness exists.

The eleven registered jobs are listed above; their names and cron expressions
are asserted nowhere, so a rename is not caught by the suite.

## What does NOT belong here

- Verdict computation — `tawheed`
- Agent workflow implementations — `.halalchain/agents/`
- HTTP endpoints — `HalalChain.Platform.Api`
- Deployment scripts — `infrastructure/scripts/`
