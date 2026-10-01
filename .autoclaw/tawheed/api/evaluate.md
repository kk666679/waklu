# POST /tawheed/evaluate

The single authoritative decision call. There is no other path to a compliance
result, and no other component may produce one.

## Contract

```
Input:  TawheedRequest   (tawheed/request.schema.json)
Output: TawheedResponse  (tawheed/response.schema.json)
```

## Preconditions

All four must hold, checked in this order:

- caller identity is authenticated
- caller holds capability `tawheed.evaluate`
- `request.tenant_id` matches the caller's tenant
- all referenced evidence is in state ≥ `verified`

A request that fails any precondition is refused without being evaluated. In
particular, evidence in state `evaluated` or below is not accepted as input:
feeding a result back in as an input is how a conclusion becomes an assumption.

## Guarantees

- **deterministic** — same input → same output
- **auditable** — every call logged with request hash + policy version
- **no side effects** beyond the audit write

## Callers

`workflows/*` only. Never agents, never skills, never MCP.

## Evaluation order

Rules run in the order given by `compliance/rules/index.yaml`. The first rule is
`delegated-authority-check`, which short-circuits: a caller without authority
learns nothing about the subject it asked about, not even whether the subject
exists.

Findings are combined by severity. Any `blocking` finding is non-compliant
regardless of what the remaining rules say.

## Response semantics

`decision` is one of:

| Value | Meaning |
|-------|---------|
| `compliant` | every applicable blocking rule passed |
| `non-compliant` | at least one blocking rule failed |
| `requires-governance` | evidence is complete but a human authority must decide |
| `requires-review` | evidence is incomplete; route to certifier-assistant |
| `error` | the evaluation could not be performed; never treated as compliant |

`requires-governance` is not a soft non-compliant. It is the terminal state of
deterministic evaluation when the deterministic answer is "no rule was
violated, and no rule can be trusted to answer this" — for example a
jurisdiction with no configured scheme.

## What this endpoint does not do

It does not issue certificates, does not move the certification state machine,
and does not write to a chain. Authority to certify belongs to the
certification body through `governance/certification/transitions.yaml`. This
endpoint tells that body what the rules say; it does not tell it what to decide.
