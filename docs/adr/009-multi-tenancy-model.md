# ADR-009 — Multi-tenancy model

**Status:** Accepted — provisional, with documented upgrade path
**Date:** 2026-09-26
**Deciders:** Platform Architecture, Steering Committee
**Consulted:** Security, Compliance, DPO, Finance
**Informed:** Product, SRE

## Context

The platform serves multiple vendors and buyers. "Multi-tenancy" could mean:

1. **Single-tenant** — one instance per customer, physically separate
2. **Multi-tenant, shared schema** — all tenants in one database, scoped
   by row
3. **Multi-tenant, schema-per-tenant** — one schema per tenant, shared
   database
4. **Multi-tenant, database-per-tenant** — one database per tenant

The choice affects isolation, cost, operational complexity, and the ability
to satisfy jurisdictional requirements.

Early discussion assumed "obviously shared schema with row-level security"
because that is the low-cost default. But the platform has real tenants
who will care about isolation: enterprise vendors in regulated markets,
halal certification bodies, and multi-national merchants. A single model
may not serve all of them.

## Decision

**Provisional: tier-based isolation, with shared schema + RLS as the
default.**

| Tenant tier | Isolation | Postgres | Blob | Credentials |
|---|---|---|---|---|
| Standard | Shared schema + RLS | Shared DB, shared schema | Shared bucket, tenant prefix | Shared DB user with RLS enforcement |
| Premium | Schema-per-tenant | Shared DB, own schema | Shared bucket, tenant prefix | Schema-scoped DB user |
| Enterprise | Database-per-tenant | Own DB | Own bucket | Separate DB credentials |

The **default tier for new tenants is Standard**. Upgrade to Premium or
Enterprise is a paid offering with a documented migration procedure.

**Non-negotiable:** tenant isolation tests pass at every tier, per module.
Sampling is not acceptable. A failure in any tier blocks the ENT-005 gate.

### Upgrade path: Standard → Premium

1. Provision new schema in the same database
2. Run migration that copies tenant's rows from shared schema to new schema
3. Freeze writes (brief maintenance window, target < 5 minutes)
4. Switch tenant's connection routing to new schema
5. Verify isolation tests
6. Resume writes
7. Optionally clean up old rows in shared schema (retain for rollback window)

Estimated downtime: 2–5 minutes for a typical tenant.

### Upgrade path: Premium → Enterprise

1. Provision new database
2. Replicate tenant's schema and data
3. Freeze writes (longer window, target < 30 minutes)
4. Repoint tenant's connection
5. Verify isolation tests
6. Resume writes
7. Optionally retain source database for rollback

Estimated downtime: 15–30 minutes.

### Upgrade path: Standard → Enterprise

Combines both steps. Estimated downtime: 30–45 minutes.

## Consequences

**Easier:**
- Cost-effective for the common case (Standard tier, shared everything)
- Enterprise tenants get true isolation without a separate deployment
- The upgrade path is a documented procedure, not a migration project
- Isolation tests are per-tier, which means the guarantees are explicit

**Harder:**
- Three tiers means three sets of isolation tests
- Application code must be tier-aware in some places (connection routing)
  — this is abstracted by a `TenantConnectionResolver`
- The shared schema default puts pressure on RLS to be correct; a bug in
  RLS is a cross-tenant leak
- Migration between tiers requires downtime, which must be scheduled
- Cost attribution is more complex in shared tiers

## Alternatives considered

### Alternative A — Shared schema + RLS only, no tiers

Rejected. Enterprise tenants in regulated markets often have contractual
or regulatory requirements for physical isolation. "Trust our RLS" is not
sufficient for some procurement processes. Offering tiers is a
differentiator.

### Alternative B — Database-per-tenant for everyone

Rejected on cost. Provisioning and operating N databases for N tenants is
expensive at scale. Most tenants do not need it. Making it the default
would price out small vendors.

### Alternative C — Single-tenant deployments (one stack per customer)

Rejected on operational cost. Each tenant would require its own full stack
(API, database, cache, storage, chain access). This is a multi-year
operational commitment that does not match the platform's target market.

### Alternative D — Shared schema, no RLS, application-enforced filtering

Rejected. RLS is a defense-in-depth mechanism. Application-level filtering
is the first line; RLS is the backstop. Removing RLS makes a single bug
catastrophic.

### Alternative E — Defer the decision, ship shared schema, decide later

Considered. Rejected because the isolation model affects schema design,
connection management, and testing infrastructure. Deciding later means
retrofitting, which is more expensive than designing for it now.

**This ADR is effectively a compromise**: the default is shared schema
(the deferred position), but the architecture supports upgrade (the
designed-for position). The upgrade paths are documented. The tier
boundaries are enforced by config.

## Dissent

**Platform Engineering** argued against implementing three tiers. Their
position: "Three tiers is three times the isolation tests, three times the
operational surface, and three times the ways to get it wrong. Ship one
tier (shared schema + RLS) and add tiers only when a paying customer
demands it."

**Partially overruled.** The tiers are not implemented in v1. Only Standard
tier is operational at launch. The Premium and Enterprise tiers are
**designed for** but not built. The upgrade paths are documented so that
when the first Premium tenant signs, we know what to do. The ADR commits
to the model, not to immediate implementation.

**Security** argued for database-per-tenant as the only safe option for a
compliance platform. Their position: "RLS is a Postgres feature. Postgres
has bugs. A single CVE means a cross-tenant leak."

**Accepted as a risk, quantified.** RLS is mature (15+ years in Postgres).
The realistic risk is not RLS bypass but application misconfiguration
(missing `SET LOCAL app.tenant_id`). Mitigation: every connection
initialization sets the tenant context; a connection without tenant context
cannot query tenant-scoped tables (RLS fails closed). Additional mitigation:
integration tests assert that queries without tenant context return zero
rows.

**DPO** raised a concern about data residency within a shared schema. Their
position: "If a Malaysian tenant's data and an EU tenant's data are in the
same database, GDPR Article 44 applies to backups and replication."

**Accepted and addressed.** The Standard tier is region-pinned: a tenant's
data resides in the tenant's declared region. Cross-region tenants share
a schema only within their region. This is a constraint on schema
placement, not on schema existence. Premium and Enterprise tenants can
override region pinning only with explicit legal review.

**One dissent held:** tenant isolation tests must run on every commit, not
just before release. The dissent was that they slow the CI pipeline.
**Overruled because:** a regression in tenant isolation is a compliance
violation, not a bug. The CI cost is the point.

## Reversibility

**Model change is expensive.** Moving from tiered to single-tier would
require migrating Premium and Enterprise tenants to shared schema, which
is a data migration with downtime. Not recommended.

**Tier addition is cheap.** Adding a fourth tier (e.g. "Regional
Enterprise" with region-pinned dedicated database) is a config change and
documentation, not a rewrite.

**Upgrade paths are the reversibility mechanism.** A tenant can move
between tiers without re-architecting the platform. This is the property
that makes the provisional decision safe.

## References

- ADR-001 — Modular monolith (module boundaries affect RLS design)
- `docs/ARCHITECTURE.md` §4 — storage layer (blob prefix isolation)
- `HalalChain.Platform.Tests/TenantIsolation/` — isolation test suite
- `bau/LIFECYCLE/data-residency.md` — region pinning rules
