# ADR-001 — Vertical slice modular monolith

**Status:** Accepted
**Date:** 2026-09-26
**Deciders:** Platform Architecture, Enterprise Architecture
**Consulted:** Finance, Compliance, Security
**Informed:** Steering Committee

## Context

The platform spans commerce, compliance, evidence storage, agentic reasoning,
and blockchain anchoring. Early discussions considered a microservices split
along domain boundaries — a `Vendors` service, a `Catalog` service, an
`Orders` service, and so on — because that is the conventional answer to
"how do you build a large system."

We assessed the actual workload. The team is small (single-digit engineers
at launch). The domains interact heavily: `Catalog` needs `Compliance` for
product status, `Cart` needs `Compliance` for re-verification, `Orders`
needs `Cart` and `Payments`, `Compliance` needs `tawheed` for verdicts.
Cross-domain transactions are the norm, not the exception.

We also assessed the operational cost. A microservices split means N
deployments, N databases (or a shared DB with all the discipline problems
that creates), N CI pipelines, N on-call surfaces, and distributed tracing
across N boundaries — all before any of that is needed.

## Decision

We adopt a **modular monolith** with **vertical slice** organization.

The entire platform is one deployable unit — `HalalChain.Platform.Api` — but
internally organized as independent modules under `Modules/<Name>/`. Each
module owns its endpoints, features, contracts, and persistence concerns.
Modules communicate through in-process interfaces and an event bus, never
through direct database access to another module's tables.

Vertical slices mean that a feature — say "Register a vendor" — lives in
one folder, with its command, handler, validator, and endpoint together.
We do not layer horizontally (Controllers / Services / Repositories as
top-level folders). Layer-by-layer organization couples every feature to
every other feature through the shared layers.

## Consequences

**Easier:**
- Single deployment; single CI pipeline; single debugging surface
- Cross-module transactions are cheap; the database enforces consistency
- Refactoring module boundaries is a code change, not a deployment
- New engineers see one solution, not twenty
- Vertical slices keep feature work localized

**Harder:**
- Modules must be disciplined about not reaching into each other's data.
  We enforce this with `HalalChain.Architecture.Tests`.
- The single deployment is a blast radius. A bug in `Promotions` can take
  down `Compliance`. Mitigated by SLO isolation and circuit breakers.
- Scaling is coarse. We scale the whole API, not one module. Acceptable at
  our scale; revisit if a module dominates load.
- Extract-to-service is a real refactor, not a config change. We accept
  this; a well-bounded module extracts cleanly.

## Alternatives considered

### Alternative A — Microservices from day one

Rejected. The interaction graph is dense. We would spend the first six
months on infrastructure (service discovery, distributed tracing, saga
orchestration, per-service CI) before shipping a single business feature.
The team is too small to operate twenty services. The domains are not yet
stable enough to fix service boundaries — we would be paying the cost of
distributed systems to learn boundaries we will redraw anyway.

### Alternative B — Layer-first monolith (Controllers / Services / Repositories)

Rejected. Layer-first organization makes every feature touch every layer.
Adding "certificate expiry sweep" would touch `Controllers/`, `Services/`,
`Repositories/`, `Models/`, and the DI container. Vertical slices localize
this to one folder, which is what we want.

### Alternative C — Modular monolith with mandatory per-module databases

Rejected. The discipline value is real but the operational cost is not
justified at our scale. We get most of the boundary discipline from
architecture tests without the operational overhead.

## Dissent

**Enterprise Architecture** argued for microservices from day one, citing
the compliance domains' differing regulatory scopes and the eventual need
for independent scaling. Their position: "A `Compliance` service with its
own database is auditable in a way a shared schema is not."

**Overruled because:** auditability is a function of the audit trail and
access controls, not the database topology. We can produce a SOC 2-compliant
audit trail in a shared schema if the architecture tests enforce module
boundaries. The alternative — a distributed system with weak boundaries
because we lack the operational maturity to enforce them — is strictly
worse for compliance.

**Finance** raised concern about the coarse scaling: we pay for API capacity
even when only one module is hot. Their position: "The cost curve will
punish us at 100× scale."

**Accepted as a risk, deferred.** At 100× scale we will have the data to
know which module to extract and the team size to do it. Premature extraction
is more expensive than late extraction.

**One dissent held:** the `Blockchain` module must never reference
`Compliance`. This is captured as an architecture test rule. The dissent
was that the module split was too permissive — "if everything is one
deployment, why have the rule?" Answer: because module boundaries are the
thing that makes the monolith maintainable, and `Blockchain` is a read-only
sink relative to decisions. The rule stands.

## Reversibility

**Reversible with moderate cost.** Extracting a module to a service requires:

1. Replace in-process interface with HTTP or gRPC client
2. Extract the module's tables to a separate schema/database
3. Introduce a saga for cross-module transactions that were previously
   transaction-safe
4. Add a deployment pipeline for the extracted service

Estimated cost per module extraction: 4–8 engineer-weeks, dominated by
transaction rework. We would extract only if a module's load profile
genuinely diverges from the rest.

Not cheap, but not a rewrite. The architecture is designed to survive this.

## References

- ADR-002 — Storage port/adapter split (also discusses the "leaf module" pattern)
- ADR-005 — VerdictBinding (an example of module-boundary enforcement)
- `.cline_inbox/PRINCIPLES.md` — P4 (only Compliance mutates ProductStatus)
- `HalalChain.Architecture.Tests/Rules/ModuleBoundaryRules.cs`
