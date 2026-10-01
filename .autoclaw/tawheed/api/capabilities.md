# `tawheed` Capabilities

What this sealed boundary is permitted to do, and what it is not. The list is
closed: anything not named here is refused, and the refusal is logged.

## Grants

| Capability | Effect |
|------------|--------|
| `tawheed.evaluate` | produce a `TawheedResponse` |
| `tawheed.explain` | render a recorded evaluation |
| `tawheed.policy.read` | read `policies/**` and the rule index |
| `tawheed.audit.read` | read the evaluation log within the caller's tenant |

## Refusals

| Requested | Refused because |
|------------|----------------|
| assign a verdict to an agent-produced proposal | P3 — agents propose, the engine concludes |
| issue, approve, or revoke a certificate | P9 — authority belongs to the certification body |
| move the certification state machine | P9 — that is `governance/certification/` |
| write to a chain or read a compliance status from one | P10 — anchors are integrity, not authority |
| accept evidence below state `verified` as input | P1 — a conclusion fed back in becomes an assumption |
| evaluate a request for a tenant the caller does not hold | P6 |
| skip `delegated-authority-check` | P14 — absence of a delegation is a denial |

## Tenant scoping

Every capability is evaluated per tenant. A caller holding `tawheed.evaluate` for
tenant A cannot evaluate for tenant B, including for the same product, because
the policy set, the evidence, and the jurisdiction all differ by tenant.

## Why the grant list is this short

Every capability granted here is a capability to *say* something conclusive.
The surface area of the deterministic boundary is deliberately smaller than the
surface area of the agent fleet, because a conclusion that is wrong is more
expensive than a proposal that never becomes one.
