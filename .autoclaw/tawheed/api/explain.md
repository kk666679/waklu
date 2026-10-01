# POST /tawheed/explain

Renders a human-readable explanation of a **prior** evaluation. Read-only.

## Contract

```
Input:  TawheedRequest + evaluation_id, or request_hash
Output: TawheedResponse + per-rule findings with field paths
```

## Preconditions

Same four as `/evaluate`, plus:

- the referenced evaluation exists in `audit/evaluation-log.md`
- the caller holds the `tawheed.explain` capability

An evaluation id the caller is not entitled to is reported as *not found*, not
as *forbidden*. The distinction itself is a tenancy leak.

## Guarantees

Deterministic given the stored evaluation. This endpoint does not re-evaluate —
it reads the recorded findings and renders them. A re-evaluation here would
produce a second answer to the same question under possibly a different policy
version, and the two would be indistinguishable to the reader.

## Output

For each applicable rule:

- rule id and version
- severity
- the input field paths that were read
- which predicate failed
- the violation code and the actions that were triggered

Redaction applies: fields listed in a rule's `audit.redact` are shown as
present-but-redacted, never omitted. Omission would let a reader infer the
value from its absence.
