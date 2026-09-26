# ADR-004 — HybridCache as the default caching abstraction

**Status:** Accepted
**Date:** 2026-09-26
**Deciders:** Platform Architecture
**Consulted:** Platform Engineering, SRE

## Context

The platform has two caching needs: small hot in-process data (policy
versions, chain registry reads) and larger shared cross-instance data
(session state, rate limit counters). `IMemoryCache` handles the first,
`IDistributedCache` the second. Every service that needs both writes custom
orchestration — and stampsedes on popular entry expiry.

.NET 10 shipped `HybridCache` GA with L1 + L2 and built-in stampede
protection.

## Decision

**HybridCache is the default caching abstraction.**

- Application code depends on `HybridCache`, not `IMemoryCache` or
  `IDistributedCache` directly.
- Redis is L2 in production. In-memory-only in tests and single-instance dev.
- Cache entry options are per-call-site; no global default.
- `CachingBehavior` in `HalalChain.Application` wraps query handlers.
- Exceptions (L1-only or L2-only cases) are documented and reviewed.

**Non-negotiable:** the checkout compliance re-verification (P9) is not
cacheable at any tier. Cache keys including tenant scope must include the
tenant identifier.

## Consequences

**Easier:** One API for the whole platform. Automatic stampede protection.
Unified telemetry. Graceful L2→L1 degradation. `CachingBehavior` is a
one-line registration.

**Harder:** HybridCache is new. Some libraries assuming `IDistributedCache`
need adaptation. Serialization is opinionated. L1 size is bounded by
process memory.

## Alternatives considered

**Manual IMemoryCache + IDistributedCache.** Rejected. Repeated, buggy,
stampede-prone.

**Redis-only.** Rejected. Loses L1's latency benefit for hot data.

**IMemoryCache-only.** Rejected. Cross-instance consistency is required.
Two replicas with different policy version caches would produce different
verdicts.

**FusionCache.** Considered. Mature, feature-rich. Rejected in favor of
first-party integration. Can migrate later if HybridCache proves unstable
— APIs are similar. ~1 engineer-week migration.

**No cache.** Rejected on unit economics. Cost model assumes ~40% API
savings from caching.

## Dissent

**SRE** wanted Redis-only for consistency. *Overruled with conditions:*
compliance-critical reads (policy versions, certificate registry) use
L2-only with short TTL. L1 is for non-compliance-critical data. P9 explicitly
not cacheable. SRE's concern is valid; addressed by scoping, not by
rejecting the abstraction.

**Platform Engineering** flagged HybridCache's age (18 months). *Accepted
as a risk.* Pin exact version. Migration to FusionCache is a fallback.

**One rule held:** tenant-scoped cache keys must include the tenant
identifier. *Overruled the dissent* that this adds ceremony: a key without
tenant leaks data across tenants under load.

## Reversibility

Cheap. HybridCache's API is close to `IDistributedCache`. Migration to
manual orchestration or FusionCache is a mechanical refactor. ~3–5
engineer-days.

## References

- `.cline_inbox/PRINCIPLES.md` — P9
- `HalalChain.Application/Behaviors/CachingBehavior.cs`
- `Microsoft.Extensions.Caching.Hybrid` documentation
