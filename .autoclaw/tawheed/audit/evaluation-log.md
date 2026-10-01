# Tawheed Evaluation Log

Append-only. One record per `/evaluate` call, including calls that returned
`decision: error`. A failed evaluation that was not recorded is
indistinguishable from one that never happened, and that ambiguity is exactly
what an audit exists to remove.

## Record shape

```
ts                 UTC, ISO-8601, from the engine's clock (not the request's evaluated_at)
audit_id           unique; appears on the response
request_hash       sha256 over the canonicalised TawheedRequest
policy_version     the version actually in force, after all merges
jurisdiction       resolved, after overrides
scheme             resolved, after overrides
tenant_id
subject            { type, id }
actor              { participant_id, role, type }
decision           one of the response.schema.json enum values
findings[]         rule_id, rule_version, severity, outcome, code
duration_ms
error_code         populated only when decision = error
```

## What is never recorded

- field values of any evidence, beyond the field *paths* a rule read
- raw MCP arguments
- credential values, in any form
- any `secret`, `password`, `api_key`, or `private_key` value

The redaction list is `security/secrets/never-log.yaml`, applied before the
record is written, not after it is read.

## Immutability

Records backing an `authoritative` transition in
`governance/certification/transitions.yaml` are immutable. So are the records
for any evaluation that produced `compliant` — a compliant result that can be
edited after the fact is not a result.

## Retention

See `retention.yaml`. In short: authoritative records are retained for the life
of the certificate plus the jurisdiction's statutory period; everything else for
the shorter of the tenant's configured period and 24 months.

## Replay

`evaluated_at` is supplied by the caller. A stored `request_hash` plus its
`policy_version` is sufficient to re-derive the response byte for byte, which is
what makes a disputed result adjudicable rather than merely arguable.
