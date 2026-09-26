# ADR-004 — HybridCache as the default caching abstraction

**Status:** Accepted
**Date:** 2026-09-26
**Deciders:** Platform Architecture
**Consulted:** Platform Engineering, SRE
**Informed:** Steering Committee

## Context

The platform has two cache needs. First, small, hot, in-process data:
tenant metadata, policy version lookups, chain registry reads. Second,
larger, shared, cross-instance data: session state, rate limit counters,
expensive AI response caches.

`IMemoryCache` handles the first. `IDistributedCache` handles the second.
Neither handles both. Every service that needs both writes custom
orchestration: check L1, on miss check L2, on miss compute, then populate
both. This pattern is repeated, buggy, and prone to cache stampedes (many
requests recompute the same value simultaneously when a popular entry
expires).

.NET 10 shipped `HybridCache` as a GA abstraction that handles L1 + L2 with
stampede protection built in. It is a drop-in replacement for the common
two-tier pattern.

## Decision

We adopt **HybridCache** as the platform's default caching abstraction.

- Application code depends on `HybridCache` (from
  `Microsoft.Extensions.Caching.Hbrid`), not on `IMemoryCache` or
  `IDistributedCache` directly.
- Redis is the L2 backend in production. In-memory-only is the L2 backend
  in tests and single-instance dev.
- Cache entry options are declared per call site; no global default.
- The `CachingBehavior` pipeline in `HalalChain.Application` wraps query
  handlers with HybridCache by default.
- Exceptions: cases where only L1 makes sense (per-request data) or only
  L2 makes sense (cross-instance coordination) may bypass HybridCache.
  These are documented and reviewed.

## Consequences

**Easier:**
- One caching API for the whole platform
- Stampede protection is automatic; no more manual locking around cache fills
- Cache metrics are unified (HybridCache emits consistent telemetry)
- L2 failure degrades gracefully to L1 by default; configurable to fail hard
- The `CachingBehavior` becomes a one-liner registration

**Harder:**
- HybridCache is new. Libraries that assume `IDistributedCache` need
  adaptation. We are early on this; the ecosystem is still catching up.
- Serialization behavior is opinionated. Some payloads that worked with
  `IDistributedCache` directly need custom serializers under HybridCache.
- L1 size is bounded by process memory; a large L1 with many instances is
  memory pressure. We size L1 conservatively.

## Alternatives considered

### Alternative A — Continue with IMemoryCache + IDistributedCache manual orchestration

Rejected. The manual pattern is repeated, buggy, and stampede-prone. Every
service writes its own version. The cost is invisible until a popular entry
expires under load.

### Alternative B — Redis-only (no L1)

Rejected. L1 is the whole point of caching hot data. Reading from Redis for
a value that changes once per hour is unnecessary network cost. We lose
most of the L1 latency benefit.

### Alternative C — IMemoryCache-only (no L2)

Rejected. Cross-instance consistency is required. Two API replicas with
different policy version caches would produce different verdicts, which
violates the deterministic guarantee.

### Alternative D — FusionCache or a third-party alternative

Considered. FusionCache is mature and feature-rich. Rejected because
HybridCache is first-party and integrates cleanly with the rest of the .NET
10 stack. Third-party has advantages (backplane support, more tuning
options) that we do not need yet.

### Alternative E — No cache

Rejected on unit-economics grounds. The cost model (ENT-005 target) assumes
caching saves ~40% on API cost. Removing cache would fail the cost targets.

## Dissent

**SRE** argued for Redis-only, citing cache consistency. Their position:
"L1 introduces a consistency window where two replicas disagree. For a
compliance platform, that is unacceptable."

**Overruled with conditions.** The compliance-critical reads — policy
versions, certificate registry state — do use L2-only caching with short
TTL. L1 is reserved for non-compliance-critical data (tenant metadata, static
config). Additionally, the compliance re-verification at checkout (P9) is
explicitly not cacheable at any tier. SRE's concern is valid and is
addressed by scoping, not by rejecting HybridCache.

**Platform Engineering** raised concern about HybridCache's age. Their
position: "It shipped in .NET 9, GA in .NET 10. That is 18 months. It is
young."

**Accepted as a risk.** We pin the exact version and monitor for issues. If
HybridCache proves unstable, we fall back to FusionCache, which has a
similar API. Estimated migration cost: 1 engineer-week.

**One dissent held:** all cache keys must include a tenant identifier where
tenant scoping applies. This is captured as an architecture test rule.
The dissent was that tenant-scoped keys add ceremony to every cache call.

**Overruled because:** a cache key that omits tenant will leak data across
tenants under load. The ceremony is the point.

## Reversibility

**Cheap.** HybridCache's API is close to `IDistributedCache`. Migration
back to manual orchestration or to a third-party library is a
mechanical refactor of cache call sites. Estimated cost: 3–5 engineer-days
depending on how many call sites exist.

## References

- ADR-001 — Modular monolith (single deployment simplifies cache topology)
- `.cline_inbox/PRINCIPLES.md` — P9 (checkout re-verification is not cacheable)
- `Microsoft.Extensions.Caching.Hybrid` package documentation
- `HalalChain.Application/Behaviors/CachingBehavior.cs`
