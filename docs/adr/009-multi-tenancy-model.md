# ADR-009 — Multi-tenancy model

**Status:** Accepted — provisional, tiered with documented upgrade path
**Date:** 2026-09-26
**Deciders:** Platform Architecture, Steering Committee
**Consulted:** Security, Compliance, DPO, Finance

## Context

Multi-tenancy options: single-tenant per customer; shared schema with RLS;
schema-per-tenant; database-per-tenant. Each affects isolation, cost,
operational complexity, and the ability to satisfy jurisdictional
requirements.

Early assumption: "obviously shared schema with RLS." But enterprise
vendors in regulated markets, halal certification bodies, and multi-national
merchants will care about isolation. A single model may not serve all.

## Decision

**Provisional: tier-based isolation, default = shared schema + RLS.**

| Tier | Isolation | Postgres | Blob | Credentials |
|---|---|---|---|---|
| Standard | Shared schema + RLS | Shared DB, shared schema | Shared bucket, tenant prefix | RLS-enforced DB user |
| Premium | Schema-per-tenant | Shared DB, own schema | Shared bucket, tenant prefix | Schema-scoped DB user |
| Enterprise | Database-per-tenant | Own DB | Own bucket | Separate DB credentials |

**Default tier: Standard.** Upgrade is a paid offering with documented
procedures.

**Non-negotiable:** tenant isolation tests pass at **every tier**, per
module. Sampling not acceptable. A failure in any tier blocks the ENT-005
gate.

### Upgrade paths

**Standard → Premium:** new schema, migrate rows, freeze writes briefly
(<5 min), switch routing, verify, resume.

**Premium → Enterprise:** new DB, replicate schema and data, freeze writes
(<30 min), repoint connection, verify, resume.

**Standard → Enterprise:** combines both. <45 min total.

### Tier implementation status

Only **Standard tier** is operational in v1. Premium and Enterprise are
**designed for** but not built. The ADR commits to the model; the upgrade
paths are documentation for when the first Premium or Enterprise tenant
signs.

## Consequences

**Easier:** Cost-effective for the common case. Enterprise tenants get
true isolation without separate deployments. Upgrade paths documented, not
migration projects. Isolation guarantees are explicit per tier.

**Harder:** Three tiers means three isolation test suites. Application code
must be tier-aware for connection routing (abstracted by
`TenantConnectionResolver`). Shared schema puts pressure on RLS
correctness. Migration requires downtime. Cost attribution is more complex
in shared tiers.

## Alternatives considered

**Shared schema + RLS only.** Rejected. Enterprise tenants have contractual
or regulatory requirements for physical isolation. "Trust our RLS" is not
sufficient for some procurement processes.

**Database-per-tenant for all.** Rejected on cost. N databases is expensive.
Making it the default prices out small vendors.

**Single-tenant deployments.** Rejected. Each tenant needs a full stack.
Multi-year operational commitment not matching the target market.

**Shared schema, app-enforced filtering, no RLS.** Rejected. RLS is
defense-in-depth. Removing it makes a single bug catastrophic.

**Defer the decision.** Considered. Rejected: the isolation model affects
schema design, connection management, testing. Deciding later means
retrofitting.

**This ADR is effectively a compromise:** default is shared schema (the
deferred position), but the architecture supports upgrade (the designed-for
position). Upgrade paths are documented. Tier boundaries are config-enforced.

## Dissent

**Platform Engineering** argued against three tiers. *Partially overruled:*
tiers are not implemented in v1. Only Standard is operational. Premium and
Enterprise are designed for but not built. The ADR commits to the model,
not immediate implementation.

**Security** wanted database-per-tenant as the only safe option. *Accepted
as a risk, quantified:* RLS is mature (15+ years in Postgres). Realistic
risk is application misconfiguration (missing `SET LOCAL app.tenant_id`).
Mitigations: every connection initialization sets tenant context; a
connection without tenant context cannot query tenant-scoped tables (RLS
fails closed); integration tests assert queries without tenant context
return zero rows.

**DPO** raised data residency within a shared schema — Malaysian tenant's
data and EU tenant's data in the same database raises GDPR Article 44
concerns for backups. *Addressed:* Standard tier is region-pinned.
Cross-region tenants share a schema only within their region. Premium and
Enterprise can override region pinning only with legal review.

**One rule held:** tenant isolation tests run on every commit, not just
before release. *Overruled the dissent* that they slow CI: a regression in
tenant isolation is a compliance violation, not a bug.

## Reversibility

Model change is expensive (migrating Premium/Enterprise to shared is a
data migration with downtime). Tier addition is cheap (config + docs).
Upgrade paths are the reversibility mechanism — a tenant moves between
tiers without re-architecting.

## References

- ADR-001 — Modular monolith
- `docs/ARCHITECTURE.md` §4 — blob prefix isolation
- `HalalChain.Platform.Tests/TenantIsolation/`
- `bau/LIFECYCLE/data-residency.md`
