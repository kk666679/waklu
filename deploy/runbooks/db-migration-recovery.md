# Database migration recovery

## Goal

Recover safely from a failed or partial database migration without losing service availability.

## Preconditions

- Keep a recent database backup and a known-good migration baseline.
- Ensure the application is configured with a fast rollback path.
- Keep the migration history in the source control and deployment logs.

## Recovery procedure

1. Assess migration status and schema drift.
2. Stop writes if the migration may leave the database in a partially applied state.
3. Restore the database from the last confirmed backup if the migration cannot be reversed safely.
4. Re-run the last known-good deployment revision.
5. Validate the application schema and ensure all health checks pass.
6. Re-run the migration only after analysis and signoff.

## Operational safeguards

- Use reversible migrations where possible.
- Avoid destructive schema changes without a backup and a rollback rehearsal.
- Log migration IDs and deployment metadata in the release record.

## Example commands

```bash
psql "$POSTGRES_URL" -c "\dt"
pg_dump "$POSTGRES_URL" > backup.sql
```
