# `.autoclaw/byok/` — Bring Your Own Key

Tenants bring their own provider credentials. This directory holds the
*lifecycle* of those credentials: which providers are supported, what a
credential reference looks like, what a submission must contain, what it costs,
where it may land, and when it is rotated.

**It does not hold the credentials.** A credential value never enters this
repository. What enters is a sealed reference the vault adapter can resolve at
call time. `AutoclawStructureTests.Byok_Contains_Refs_Not_Secrets` fails the
build if a value-shaped string appears under `byok/`.

## Layout

```
providers/    one file per supported provider: auth shape, region list, capabilities
policies/     residency, budget, rotation — the three gates
schemas/      credential submission, credential metadata, execution request
vault/        the adapter contract, not the secrets
```

## The three gates

Every execution passes through all three, in this order, and each fails closed:

1. **Residency** — `policies/residency.yaml`. Is the provider region one this
   tenant's region permits? A non-permitted region is a denial. Silent
   rerouting is forbidden: it would satisfy the policy while moving the data
   somewhere the tenant did not agree to.
2. **Budget** — `policies/budget.yaml`. Is there headroom for this tenant,
   agent, and task? A denial surfaces as `requires-review` and a human decides.
3. **Rotation age** — `policies/rotation.yaml`. Is the credential within its
   maximum age? Past the age is a refusal, not a warning.

Then, and only then, does the request reach the provider.

## Execution

`workflows/byok-execution.yaml` is the only path from a workflow to a provider
call. Agents do not dispatch provider calls themselves; they request one and
the workflow runs the gates.

## What a provider file contains

Auth shape, region list, declared capabilities, and the BYOK reference prefix.
Not a key, not a key id, not an account name that would let someone correlate
the tenant from a public log.

## Adding a provider

1. Add `byok/providers/<name>.yaml`.
2. Add it to `policies/budget.yaml` under `by_provider`.
3. Add its regions to `policies/residency.yaml` for every tenant region it can
   serve — and only those.
4. Re-run the architecture tests.

A provider with no residency entry is denied for every tenant. That is the
correct default for a provider nobody has thought about yet.
