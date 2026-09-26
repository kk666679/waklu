# ADR-001 — Modular monolith, vertical slices

**Status:** Accepted
**Date:** 2026-09-26
**Deciders:** Platform Architecture, Enterprise Architecture
**Consulted:** Finance, Compliance, Security

## Context

The platform spans commerce, compliance, evidence storage, agentic reasoning,
and blockchain anchoring. The domains interact densely: `Catalog` reads
`Compliance`, `Cart` reads `Compliance`, `Orders` reads `Cart` and `Payments`,
`Compliance` calls `tawheed`. Cross-domain transactions are the norm.

The team is small. The domains are not yet stable enough to fix service
boundaries. A microservices split would spend six months on infrastructure
before shipping one business feature.

## Decision

**Modular monolith with vertical slice organization.**

One deployable unit — `HalalChain.Platform.Api` — internally organized as
independent modules under `Modules/<Name>/`. Each module owns its endpoints,
features, contracts, and persistence. Modules communicate through in-process
interfaces and an event bus, never through direct table access to another
module's data.

Vertical slices mean a feature lives in one folder:

```text
Modules/Compliance/Features/BindVerdict/
  ├─ BindVerdictCommand.cs
  ├─ BindVerdictHandler.cs
  ├─ BindVerdictValidator.cs
  └─ BindVerdictEndpoint.cs
```

Not horizontal layers (`Controllers/`, `Services/`, `Repositories/`).

## Consequences

**Easier:** Single deployment. Single CI. Cross-module transactions are
cheap. Refactoring boundaries is a code change, not a deployment. Vertical
slices localize feature work to one folder.

**Harder:** Module discipline is enforced by architecture tests, not by
physical separation. The single deployment is a blast radius. Scaling is
coarse. Extract-to-service is a real refactor.

## Alternatives considered

**Microservices from day one.** Rejected. Dense interaction graph, small
team, unstable boundaries. We would pay the cost of distributed systems to
learn boundaries we will redraw.

**Layer-first monolith.** Rejected. Every feature touches every layer.
Vertical slices localize the change.

**Per-module databases.** Rejected. The discipline value is real; the
operational cost is not justified at our scale.

## Dissent

**Enterprise Architecture** wanted microservices, citing differing regulatory
scopes across compliance domains. *Overruled:* auditability comes from the
audit trail and access controls, not database topology. Weak boundaries in
a distributed system are strictly worse for compliance than strong boundaries
in a monolith.

**Finance** raised coarse scaling cost. *Accepted as a risk, deferred:* at
100× scale we will have data to pick which module to extract. Premature
extraction costs more than late extraction.

**One rule held despite pressure:** `Blockchain` must never reference
`Compliance`. Enforced by architecture test. The dissent — "if it's one
deployment, why have the rule?" — is answered by: module boundaries are
what make the monolith maintainable, and `Blockchain` is a read-only sink
relative to decisions.

## Reversibility

Moderate. Extracting a module requires replacing in-process interfaces with
HTTP, splitting tables, introducing sagas for previously transaction-safe
operations. ~4–8 engineer-weeks per module. Not cheap, not a rewrite.

## References

- ADR-002 — Storage ports (leaf-module pattern)
- ADR-005 — VerdictBinding (module-boundary enforcement example)
- `.cline_inbox/PRINCIPLES.md` — P4
