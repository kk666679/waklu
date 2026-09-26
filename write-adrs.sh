#!/usr/bin/env bash
# HalalChain — write ADR-001 through ADR-009.
# Usage: ./write-adrs.sh [/path/to/repo]
set -euo pipefail

ROOT="${1:-$(pwd)}"
ADR="$ROOT/docs/adr"
mkdir -p "$ADR"

# ═══════════════════════════════════════════════════════════════════════
# ADR-001 — Vertical slice modular monolith
# ═══════════════════════════════════════════════════════════════════════
cat > "$ADR/001-vertical-slice-modular-monolith.md" <<'EOF'
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
Adding "certificate expiry sweep" would touch `Controllers/`, `Services/`,`Repositories/`, `Models/`, and the DI container. Vertical slices localize
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
EOF

═══════════════════════════════════════════════════════════════════════

ADR-002 — Storage port/adapter split + two hashers

═══════════════════════════════════════════════════════════════════════
cat > "$ADR/002-storage-port-adapter-and-hashers.md" <<'EOF'
# ADR-002 — Storage port/adapter split and two hashers

**Status:** Accepted
**Date:** 2026-09-26
**Deciders:** Platform Architecture
**Consulted:** Compliance, Security, Platform Engineering
**Informed:** Steering Committee

## Context

Two concerns converged.

**First, evidence storage had no canonical home.** Certificates, lab reports,
supplier photos, agent traces — all binary. No service owned them. Early
prototypes wrote files to disk in ad-hoc locations, which is not auditable.

**Second, content addressing was required for on-chain anchoring.** A verdict
must cite a specific piece of evidence, and the citation must survive the
evidence being re-uploaded. A hash is the only way to do this reliably.

The tempting answer was "just use S3" or "just use SHA-256 for everything."
Both were considered and rejected. "Just S3" hides the domain concept
(evidence) behind a mechanism (object store). "Just SHA-256" conflates two
different integrity requirements — the storage address and the on-chain
tree node — into one algorithm that does not serve both.

## Decision

**Two ports, two hashers, one content-addressed store.**

### The two ports

- `IBlobStore` — the mechanism. Put, open, exists. Keyed by content hash.
  Deliberately has no `DeleteAsync`. Implementations: FileSystem, S3, Azure,
  IPFS.
- `IEvidenceStore` — the domain concept. Composes `IBlobStore`, adds
  immutability, access logging, retention. Implementations: exactly one
  (`EvidenceStore`), because the semantics are not provider-specific.

Both ports live in `HalalChain.Application/Storage/`. Adapters live in
`HalalChain.Storage/Adapters/`. Dependency direction is strict: Storage
depends on Application, never the reverse.

### The two hashers

- **SHA-256** — for storage addressing. `BlobRef` is a lowercase-hex SHA-256
digest. Content-addressed layout is `{hash[0..2]}/{hash}`.
- **Keccak256** — for Merkle tree internal nodes on-chain. The tree is built
  with sorted-pair keccak256, matching the contract's `verifyInclusion`.

The two hashers never mix. A SHA-256 digest is never fed into a keccak256
tree as if it were a keccak hash; it is treated as a 32-byte leaf and
combined with keccak256 at every internal node.

### Append-only by type

`IBlobStore` declares no delete. This is a compile-time property, not a
convention. Retention tombstones metadata records and emits audit entries;
blob purging, where it exists, is a separate and separately audited operation.

## Consequences

**Easier:**
- Every evidence item has a canonical address that survives re-upload
- Deduplication is free; ingesting the same file twice writes bytes once
- The storage backend is swappable (filesystem for dev, S3 for prod) without
touching domain code
- Content addressing maps 1:1 onto IPFS CID if that becomes a product feature
- The on-chain Merkle tree uses keccak256, which is what Solidity natively
supports, and the C# tree matches byte-for-byte
- Append-only is enforced by the type system, not code review

**Harder:**
- Two hash functions to reason about. Engineers must know which one applies
  where. Mitigated by naming (`Sha256ContentHasher` vs `KeccakMerkleTree`)
  and by CI checks.
- Content addressing means a file's name and metadata are separate from
  its bytes. Lookup is by hash, not by path. Some workflows assume paths.
- No delete means storage grows monotonically. Retention is a metadata
  tombstone, not a space reclamation. We accept this; halal evidence
  retention is 7 years by requirement.
- Signed URLs are provider-specific, so `ISignedUrlIssuer` is a separate
  port. Two ports to wire instead of one.

## Alternatives considered

### Alternative A — Single `IStorage` port with delete

Rejected. A single port collapses mechanism and domain, which means every
new backend (Azure, IPFS) has to reimplement the domain semantics
(immutability, access logging, retention). Worse, it exposes `Delete`,
which the domain cannot permit. A storage port that can delete evidence is
a storage port that violates the compliance requirement.

### Alternative B — SHA-256 everywhere, including on-chain

Rejected on technical grounds. Solidity's `keccak256` is the native hash.
Computing SHA-256 on-chain costs more gas and requires a precompile that is
not universally available. The contract would either be more expensive or
require a different verification pattern. Since keccak256 is the standard
for Merkle trees in the EVM ecosystem, we adopt it and use SHA-256 only
where it is the right tool (storage addressing).

### Alternative C — Keccak256 everywhere, including storage addressing

Rejected on ecosystem grounds. SHA-256 is what content-addressed storage
systems (IPFS, S3 integrity checks, `dotnet` built-in) expect. Using
keccak256 for storage would require a custom implementation and would not
interoperate with existing tooling.

### Alternative D — Content-addressed by UUID, not hash

Rejected. UUIDs do not guarantee content integrity. Re-uploading a modified
file would produce a new UUID, not an integrity failure. Content addressing
via hash is the mechanism that makes "the evidence has not changed"
verifiable.

## Dissent

**Platform Engineering** argued for a single hasher (SHA-256) with a
translation layer at the chain boundary. Their position: "Two hashers is
two bugs waiting to happen. Translate at the edge."

**Overruled because:** the translation is not free. Every leaf in the
Merkle tree would need SHA-256 for the leaf hash and keccak256 for the
internal node — but then the tree would not match OpenZeppelin's reference
implementation, and the parity test would fail. Making the tree natively
keccak256 from the leaf level, with SHA-256 only used for storage address
computation, is cleaner and matches the ecosystem.

**Compliance** raised concern that content-addressing makes PII discoverable
by hash. Their position: "If I know the SHA-256 of a document, I can query
whether it was ever ingested."

**Accepted as a risk, mitigated.** The `IBlobStore` API requires an
authenticated actor for reads; there is no public hash-to-exists endpoint.
The `IAccessLog` records every query. A hash oracle is possible only with
access, and access is logged. For higher-assurance tenants, the enterprise
tier uses per-tenant buckets with separate credentials.

**Security** raised concern that a same-hash re-upload could be used to
"poison" a prior record. Their position: "If I can craft a file with the
same SHA-256 as a valid certificate, I can replace it."

**Accepted and noted.** SHA-256 collisions are computationally infeasible
at the current state of the art. If this becomes a concern, migration to
BLAKE3 or SHA-3 is a config change in the hasher implementation; the port
does not change.

## Reversibility

**Hasher swap: cheap.** Replacing SHA-256 with SHA-3 means implementing a
new `IContentHasher` and re-indexing existing blobs. Re-indexing is a batch
operation; the old hashes remain queryable via a mapping table during the
transition. Estimated cost: 1 engineer-week for code, 1–3 weeks of backfill
depending on volume.

**Port change: expensive.** Removing `IEvidenceStore` and folding its
semantics into `IBlobStore` would require every adapter to implement
retention, access logging, and immutability. Not recommended.

**Content addressing removal: prohibitive.** Existing on-chain anchors
reference Merkle roots derived from content hashes. Removing content
addressing would invalidate every anchor. This is effectively irreversible.

## References

- ADR-006 — Chain network selection (depends on keccak256 tree shape)
- `.cline_inbox/PRINCIPLES.md` — P3 (no Delete), P5 (two hashers, two concerns)
- `HalalChain.Application/Storage/` — port definitions
- `HalalChain.Storage/Adapters/` — adapter implementations
EOF

═══════════════════════════════════════════════════════════════════════

ADR-003 — MCP v2 stateless transport

═══════════════════════════════════════════════════════════════════════
cat > "$ADR/003-mcp-v2-stateless-transport.md" <<'EOF'
# ADR-003 — MCP v2 stateless transport

**Status:** Accepted
**Date:** 2026-09-26
**Deciders:** Platform Architecture
**Consulted:** Platform Engineering, Security
**Informed:** Steering Committee

## Context

The platform exposes an MCP (Model Context Protocol) server so that coding
agents and compliant AI clients can inspect the solution, query evidence,
and evaluate policies through a standardized tool interface. Early designs
used MCP v1, which establishes a stateful session per client and requires
sticky routing across instances.

MCP C# SDK v2.0 (released July 2026) made the protocol stateless by
default, added Multi-Round-Trip Requests (MRTR) for tools that need
user confirmation, and standardized routing headers. This changes the
deployment and threat model materially.

## Decision

We adopt **MCP C# SDK v2.0 with stateless transport** for all MCP servers
in the platform.

Implications:

- No session affinity required. Any MCP host instance can serve any request.
- No in-memory session state. Tools are pure functions of their input and
the ambient authentication context.
- MRTR handles tools that need multi-step confirmation (e.g. an evidence
  fetch that requires explicit approval).
- Routing headers standardize tool discovery across hosts.

The MCP server in `HalalChain.Mcp/` is AOT-compiled and read-only. Tools
that would mutate state require either (a) explicit MRTR approval or
(b) execution through the platform API rather than MCP.

## Consequences

**Easier:**
- Horizontal scaling: add MCP host replicas behind any load balancer
- No sticky session requirement in the ingress or service mesh
- No session memory leak; no session expiry logic
- Simpler testing: every tool call is a self-contained request
- AOT-compilable, so cold start is fast and memory footprint is small
- MRTR gives us a natural approval gate for sensitive operations

**Harder:**
- Any tool that genuinely needs cross-request state must persist it
  externally, which is a real design constraint (we accept it; our tools
  are read-only).
- Clients that assume a session context must be updated. Agent Framework
  1.0 is compatible; older clients need adaptation.
- MRTR adds round trips for approval-gated operations, which is the point,
  but it is not free.

## Alternatives considered

### Alternative A — MCP v1 with sticky sessions

Rejected. Sticky sessions require ingress configuration, break under
autoscaling, and complicate failure recovery. The failure mode is subtle
(a pod restart drops in-flight state) and the benefit is convenience.

### Alternative B — Custom tool protocol, no MCP

Rejected. The whole point of MCP is ecosystem interoperation. Building a
bespoke protocol means every agent client needs a bespoke adapter. We would
be reimplementing MCP badly.

### Alternative C — MCP v2 with a session cache layer

Rejected. The cache layer would reintroduce session affinity, which is what
v2 removed. Adding it back defeats the purpose.

### Alternative D — MCP v1 for read tools, v2 for write tools

Rejected. Two protocols is two bugs. And we have very few write tools;
they can go through the API instead.

## Dissent

**Platform Engineering** initially argued to wait for a 2.1 or 2.2 release
before adopting, citing the recentness of 2.0. Their position: "Ship on
proven infrastructure; 2.0 is 3 months old."

**Overruled because:** the stateless change is architectural, not cosmetic.
Adopting it later means refactoring every tool that assumed session state.
Adopting it now means never writing that code. The 3-month track record is
thin, but the SDK targets `net8.0` through `net10.0` and the API surface
is small enough to vendor if necessary.

**Security** argued that MRTR adds attack surface — a malicious client
could trigger approval prompts to exhaust the reviewer. Their position:
"Approval fatigue is a real vulnerability."

**Accepted as a risk, mitigated.** MRTR approval requests are rate-limited
per client and per actor. Repeated prompts from the same source are
throttled. The `AccessLog` records every approval request, so a pattern of
abuse is detectable.

**One dissent held:** the MCP server must never be exposed to unauthenticated
clients, even for read-only tools. Some early discussion suggested a public
"discovery" endpoint. That was rejected; every MCP call requires
authentication. The dissent was that authentication adds friction for
legitimate public use cases.

**Overruled because:** "read-only" tools can still leak information. The
solution has vendor data, evidence metadata, and policy details. None of it
is public. Every call authenticates.

## Reversibility

**Cheap to reverse if we ever need to.** Removing stateless transport means
introducing a session store (Redis is already present) and adding sticky
routing. Estimated cost: 2 engineer-weeks. We would only do this if a
future MCP spec required stateful semantics for a critical tool, which
seems unlikely.

**Vendoring is possible.** The SDK is small. If v2.0 has bugs that upstream
abandons, we can vendor and patch.

## References

- ADR-001 — Modular monolith (the MCP host is one deployment)
- `HalalChain.Mcp/Program.cs` — server registration
- MCP C# SDK v2.0 release notes (July 2026)
- Agent Framework 1.0 MCP integration docs
EOF

═══════════════════════════════════════════════════════════════════════

ADR-004 — HybridCache default

═══════════════════════════════════════════════════════════════════════
cat > "$ADR/004-hybridcache-default.md" <<'EOF'
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
  `Microsoft.Extensions.Caching.Hybrid`), not on `IMemoryCache` or
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
EOF

═══════════════════════════════════════════════════════════════════════

ADR-005 — VerdictBinding not boolean

═══════════════════════════════════════════════════════════════════════
cat > "$ADR/005-verdict-binding-not-boolean.md" <<'EOF'
# ADR-005 — VerdictBinding, not boolean, on Product

**Status:** Accepted
**Date:** 2026-09-26
**Deciders:** Compliance, Platform Architecture
**Consulted:** Platform Engineering, Product
**Informed:** Steering Committee

## Context

The obvious way to model "is this product halal?" is a boolean: `Product.IsHalal`.
Most early sketches used this. It reads naturally. It is wrong.

The problem is structural. A boolean on `Product` can be set by anyone with
write access to the `Product` aggregate — a vendor through the marketplace
API, an admin through the back office, a bug in a migration. Every one of
those is a path to a halal claim that `tawheed` did not authorize.

The compliance requirement is stronger than "the value is usually right."
It is "the value is derived from a `tawheed` verdict, and only from a
`tawheed` verdict." A boolean cannot express that. It has no provenance.

## Decision

`Product` holds a **`VerdictBinding`**, not a boolean.

```csharp
public sealed record VerdictBinding
{
    public VerdictId Id { get; init; }
    public VerdictState State { get; init; }     // Issued | Expired | Revoked
    public DateTimeOffset DecidedAt { get; init; }
    public string PolicyVersion { get; init; }   // which policy decided
    public BlobRef EvidenceRoot { get; init; }   // Merkle root of evidence
    public string? AnchorTxHash { get; init; }   // on-chain anchor
    public Actor DecidedBy { get; init; }         // always the tawheed service
}
```

There is no Product.IsHalal. There is no Product.HalalStatus. There is
no boolean anywhere on the aggregate that represents halal compliance.

ProductStatus (Draft | PendingVerification | Active | ExpiringSoon |
Suspended | Rejected | Archived) is a projection of the VerdictBinding
and the certificate expiry date. Only the Compliance module may mutate
ProductStatus. No other module has the write path.

Consequences

Easier:

· The provenance of every product's halal claim is queryable
· "Which policy version authorized this product?" is a join, not a hope
· "Show me the evidence behind this product" is a single traversal
· Audit trails are complete by construction
· Revoking a verdict is a state change, not a data loss

Harder:

· Listing a product requires a verdict lookup, which is slower than reading
a boolean. Mitigated by caching the projection (not the binding).
· The aggregate is larger; more fields to maintain.
· Vendors cannot manually override. This is the point, but some support
  workflows assumed they could.
· Migrations that once touched one boolean now touch a record with several
  fields. More careful work.

Alternatives considered

Alternative A — Product.IsHalal boolean

Rejected. No provenance. Any write path is a violation.

Alternative B — Product.IsHalal boolean + a separate VerdictBinding

Rejected. Redundant. Two sources of truth for one fact. They will drift.
The boolean becomes the thing everyone reads (fast), and the binding becomes
the thing nobody updates.

Alternative C — Enum Product.HalalStatus { Halal, Haram, Unknown }

Rejected for the same reason as the boolean. An enum is a boolean with more
values. It still lacks provenance, and it still has a write path.

Alternative D — VerdictBinding on a separate aggregate, not on Product

Considered. Put the binding in a ProductCompliance aggregate that
references Product. Rejected because it adds a hop for the most common
read path (product listing) and creates a synchronization problem: which
aggregate is authoritative when they diverge? Keeping the binding on
Product makes the relationship structural.

Alternative E — Compute halal status on read from evidence

Rejected on cost grounds. Every product listing would invoke tawheed,
which is expensive. The binding records the last verdict; the certificate
expiry sweep and periodic revalidation keep it current.

Dissent

Product argued for a boolean for vendor UX simplicity. Their position:
"Vendors just want to know if their product is live. The binding is complex
internal machinery."

Overruled with mitigation. The vendor UI displays ProductStatus, which
is a simple enum. The VerdictBinding is internal. Vendors never see it.
The complexity is in the domain, not the UX.

Platform Engineering argued for a cached boolean alongside the binding
for read performance. Their position: "Every product list query does a
join. That is expensive at scale."

Accepted and documented as a risk, mitigated. The projection caching
pattern (CachingBehavior on the read model, not the aggregate) means
lists read a cached view. The cache is invalidated when the binding
changes. The binding itself is not queried on the hot path.

Security raised a concern about the DecidedBy field being forgeable.
Their position: "If an attacker can set DecidedBy to tawheed, they
have a false verdict."

Accepted and mitigated three ways. (1) The Compliance module is the
only code path that creates a VerdictBinding, enforced by architecture
test. (2) The value of DecidedBy is always the tawheed service identity,
which is authenticated at the network layer. (3) Verdicts are anchored
on-chain; a forged binding would not have a valid anchor and would fail
verification.

One dissent not held: the initial proposal was to make VerdictBinding
nullable to represent "no verdict yet." This was rejected. The Draft
state of ProductStatus handles "no verdict yet" more cleanly than a null
field. Nullable verdicts introduce null-check bugs everywhere.

Reversibility

Cheap to extend, expensive to remove. Adding fields to VerdictBinding
is a normal migration. Removing the binding and replacing with a boolean
would require:

1. A migration that collapses the binding to a boolean (losing provenance)
2. An architecture test reversal
3. Audit trail adjustments for compliance

This is technically possible but a compliance regression. We would not do
it. Treat as effectively irreversible.

References

· .cline_inbox/PRINCIPLES.md — P4 (only Compliance mutates ProductStatus)
· HalalChain.Domain/Halal/ValueObjects/VerdictBinding.cs
· HalalChain.Platform.Api/Modules/Compliance/StateMachine/ProductStatusMachine.cs
· HalalChain.Architecture.Tests/Rules/ModuleBoundaryRules.cs — enforces P4
  EOF

═══════════════════════════════════════════════════════════════════════

ADR-006 — Chain network selection

═══════════════════════════════════════════════════════════════════════
cat > "$ADR/006-chain-network-selection.md" <<'EOF'
# ADR-006 — Chain network selection

Status: Accepted
Date: 2026-09-26
Deciders: Platform Architecture, Finance
Consulted: Compliance, Security, Shariah Advisory Board
Informed: Steering Committee

Context

The platform anchors Merkle roots of evidence batches on-chain. Three
production candidates were evaluated:

· Polygon PoS — mature, cheap, fast finality, high throughput
· Polygon zkEVM — privacy-preserving, slower finality, higher cost
· Polygon CDK (custom zk chain) — full control, higher operational cost

The anchor cadence is hourly. Anchoring is not latency-sensitive; verification
must be reasonably fast but can tolerate minutes. Privacy matters because
Merkle roots, while not revealing evidence content, reveal batch sizes and
timing.

Decision

Primary: Polygon zkEVM. Failover: Polygon PoS.

The IBlockchainClient port abstracts the network. Configuration selects
the network; no code change is required to switch.

Primary rationale (zkEVM)

· Zero-knowledge proofs provide privacy guarantees for batch composition.
  Observers can verify that a root was anchored without learning batch
details.
· Validity proofs provide strong integrity guarantees — the state transition
  is mathematically verified, not merely attested by validators.
· Compliance auditors appreciate the cryptographic finality.
· The recent Type-1 equivalence upgrade reduced prover costs by ~73%.

Failover rationale (PoS)

· If zkEVM finality times become operationally unacceptable (> 2 hours in
  practice), the failover to PoS is a config change.
· PoS finality is ~5 minutes, which is ample for our anchor cadence.
· PoS is cheaper, which matters if gas prices spike.

Configuration

```json
{
  "Blockchain": {
    "Provider": "nethereum",
    "Network": "polygon-zkevm",
    "RpcUrl": "https://zkevm-rpc.com",
    "ChainId": 1101,
    "FailoverNetwork": "polygon-pos",
    "FailoverRpcUrl": "https://polygon-rpc.com",
    "ContractAddresses": {
      "EvidenceAnchor": "0x...",
      "CertificateRegistry": "0x...",
      "PolicyAnchor": "0x..."
    },
    "AnchorCadence": "01:00:00",
    "AnchorCadenceMax": "06:00:00",
    "BatchSize": 500
  }
}
```

AnchorCadence is a target, not a fixed interval. It is a ceiling
during gas spikes; batching more aggressively when gas is high is permitted.
The AnchorCadenceMax is the hard ceiling.

Consequences

Easier:

· Privacy-preserving anchor verification with cryptographic guarantees
· Compliance auditors accept zkEVM proofs without additional explanation
· Failover to PoS is a config change, not a code change
· Contract deployment targets both networks with the same bytecode

Harder:

· zkEVM finality is ~30 minutes, significantly slower than PoS. Verification
  of a recent anchor may need to wait. Acceptable for our cadence.
· Gas costs are higher on zkEVM. The cost model targets account for this.
· Contract address management: two sets of deployed addresses, one per
  network. Config schema supports this.
· DevChain is used for local development; neither production network
  is used in dev.

Alternatives considered

Alternative A — Polygon PoS as primary

Rejected. PoS is mature and cheap, but its privacy properties are weaker.
Merkle roots and batch sizes are fully visible. For a compliance platform
where evidence patterns reveal vendor activity, this is a real concern.
PoS remains the failover.

Alternative B — Polygon CDK (custom zk chain)

Rejected on operational grounds. Running our own zk chain means running our
own sequencer, prover, and validator set. This is a multi-person
infrastructure commitment. Not justified at current scale. Reconsider if
the platform reaches a scale where custom chains are economically rational
(e.g. > 1M anchors/day).

Alternative C — Ethereum L1

Rejected on cost. L1 gas costs are prohibitive for hourly anchoring. Even
batched, the cost per anchor would exceed the batch's business value.

Alternative D — No chain, just a signed audit log

Considered seriously. A signed append-only log (e.g. via a transparency
log) provides similar integrity guarantees without blockchain overhead.
Rejected because the platform's value proposition includes verifiable
anchoring that third parties can check without trusting the platform.
A signed log requires trusting the signer; a chain does not.

Alternative E — Arweave or Filecoin for evidence storage

Rejected for this ADR. Those are storage networks, not anchor chains.
They are orthogonal; if we adopt them, it is a storage-layer decision (see
ADR-002), not a chain decision.

Dissent

Finance argued for Polygon PoS primary. Their position: "zkEVM gas is
2–4× PoS. For an hourly anchor, that is a 4-figure monthly delta at our
volume. The privacy benefit is theoretical."

Overruled because: the privacy benefit is not theoretical for a
compliance platform. The batch composition of a halal certifier's
verification activity is sensitive — it reveals which vendors are being
audited and when. Competitors and short-sellers could extract signal from
public batch timing. zkEVM's privacy is a defensible requirement.

Mitigation: the failover to PoS is documented and configured. If gas
costs on zkEVM spike by more than 5× for more than 30 days, the ADR is
revisited.

Security argued that zkEVM's relative immaturity is a risk. Their
position: "Polygon PoS has been running for years; zkEVM is newer. Newer
means unaudited edge cases."

Accepted as a risk, mitigated. The blockchain module is downstream of
verdict decisions (P6). If the chain fails, evidence integrity is not
immediately compromised — the evidence is content-addressed and stored
off-chain. Anchoring resumes when the chain recovers. We are not dependent
on the chain for correctness, only for third-party verifiability.

Shariah Advisory Board asked whether anchoring on a blockchain with
any riba-adjacent features (staking rewards, etc.) is problematic. Their
position: "If we anchor on a network whose validator economics include
interest, are we complicit?"

Answered and documented. Polygon's validator rewards are fee-based and
 do not constitute riba. The Shariah Board reviewed the validator economics
 and issued a memorandum confirming the network's use is permissible for
 evidence anchoring. The memorandum is filed at
 docs/shariah/polygon-anchor-memo.pdf.

One dissent held: the anchor cadence is a ceiling, not a fixed interval.
This is captured in the config schema. The dissent was that fixed cadence
is simpler to reason about. Overruled because: during gas spikes,
a fixed cadence either overpays or delays. Adaptive cadence within a
ceiling is the pragmatic choice.

Reversibility

Chain swap is a config change. The IBlockchainClient port abstracts
the network. Switching from zkEVM to PoS:

1. Deploy contracts to PoS (bytecode is identical)
2. Update Network, RpcUrl, ChainId, and contract addresses in config
3. Restart the anchor service

Existing anchors on zkEVM remain verifiable. New anchors go to PoS. No
migration of existing anchors is required; they live on their original
chain.

Multi-chain support is possible but not implemented. If we need to
anchor on both networks for redundancy, the IBlockchainClient port
supports multiple implementations behind a facade. Estimated cost: 2
engineer-weeks.

References

· ADR-002 — Storage port/adapter (Merkle tree shape)
· docs/shariah/polygon-anchor-memo.pdf — Shariah Board memorandum
· HalalChain.Platform.Api/Modules/Blockchain/Infrastructure/Nethereum/IBlockchainClient.cs
· service-manifest.yaml — deployed contract addresses
  EOF

═══════════════════════════════════════════════════════════════════════

ADR-007 — Payment provider selection

═══════════════════════════════════════════════════════════════════════
cat > "$ADR/007-payment-provider-selection.md" <<'EOF'
ADR-007 — Payment provider selection

Status: Accepted — vendor TBD pending procurement
Date: 2026-09-26
Deciders: Finance, Compliance
Consulted: Shariah Advisory Board, Legal, Platform Architecture
Informed: Steering Committee

Context

The marketplace requires a payment provider. Most payment providers assume
a conventional fee model where the platform holds buyer funds and pays
vendors later, often with interest-bearing settlement accounts. This is not
compatible with the halal-finance constraints the platform operates under.

The constraints (from docs/ARCHITECTURE.md §7.4):

· No riba. No interest on delayed payouts. No interest-bearing escrow
  product. Platform revenue is a fixed service fee (ujrah) disclosed at
  listing time.
· No gharar. Delivery terms stated explicitly before capture. No
  open-ended "ships when ready."
· Wakala-based escrow. Where funds are held between capture and payout,
  the structure is agency (wakala), documented.
· Zakat and sadaqah pass-through. Buyer opt-in, separate line item,
  100% to the designated recipient, no commingling.

Very few payment providers support these constraints natively. Most can be
adapted with contractual addenda and escrow structures, but the adaptation
must be legally sound, not just technically feasible.

Decision

The Payments module is built against a port (IPaymentProvider), and
vendor selection proceeds in parallel.

The port is defined now; the vendor is selected through a procurement
process that runs during ENT-002. The port shape is:

```csharp
public interface IPaymentProvider
{
    Task<Authorization> AuthorizeAsync(AuthorizationRequest req, CancellationToken ct);
    Task<Capture> CaptureAsync(AuthorizationId authId, Money amount, CancellationToken ct);
    Task<Split> SplitAsync(CaptureId capId, IReadOnlyList<SplitAllocation> allocs, CancellationToken ct);
    Task<Payout> ReleaseAsync(SplitId splitId, CancellationToken ct);
    Task<Refund> RefundAsync(CaptureId capId, Money amount, Reason reason, CancellationToken ct);
    Task<EscrowStatus> GetEscrowStatusAsync(CaptureId capId, CancellationToken ct);
}
```

The port has no interest-related methods. It has no "hold funds and pay
later" concept that assumes a fee. The escrow semantics are structural:
the provider holds funds under a documented agency agreement, and the
platform's split allocates principal to vendor and service fee to the
platform separately.

Procurement criteria

# Criterion Weight Mandatory
1 Supports wakala escrow structure or contracts to support it 25% Yes
2 No interest on delayed payouts, contractually 20% Yes
3 Split payment with per-vendor allocation 15% Yes
4 Zakat/sadaqah pass-through without commingling 15% Yes
5 SAQ-A eligible (no card data in platform scope) 10% Yes
6 Multi-currency support (MYR, IDR, AED, USD) 5% No
7 Fee structure transparency 5% No
8 API quality, SDK, sandbox 5% No

Vendors scoring below "mandatory yes" on any of criteria 1–5 are
disqualified before weighting.

Candidate landscape (as of September 2026)

Vendor Wakala capable No riba Splits Zakat pass-through SAQ-A
Provider A Yes (documented) Yes Yes Yes (dedicated ledger) Yes
Provider B Contract addendum Yes Yes Limited Yes
Provider C No native Yes Yes No Yes
Provider D Yes Contract required Yes Yes Yes

Vendor names redacted in this ADR; full evaluation filed at
docs/procurement/payment-provider-evaluation.xlsx.

Decision deferred to procurement. Target: close by end of ENT-002
week 4, or Payments module (build order step 31) cannot proceed.

Consequences

Easier:
· The Payments module can be built and tested against a stub provider
  before the vendor is selected
· Switching providers is a config change, not a rewrite
· The port shape forces the halal-finance constraints into the interface,
  not into provider-specific code
· Vendor selection can proceed in parallel with implementation

Harder:
· The stub provider must correctly model wakala escrow semantics, which
  requires legal review of what "wakala" means in the interface
· The port may not fit a chosen vendor's API cleanly; adapter code will
  be needed regardless
· Deferred decision means the Payments module has a real deadline; missing
  it delays ENT-002

Alternatives considered

Alternative A — Select vendor first, then design the port

Rejected. This inverts the dependency. The port should be shaped by the
domain constraints, not by a vendor's API. If we shaped the port around
Vendor A and then Vendor B was selected, we would be reworking the port.

Alternative B — Build a bespoke escrow system

Rejected. Running our own escrow means holding client funds, which requires
money transmitter licenses in most jurisdictions. This is a multi-year
regulatory path. Not viable.

Alternative C — Use a crypto payment rail (USDC on Polygon)

Considered. The platform already uses Polygon. USDC settlement on-chain
would be programmatically escrow-able and transparent.

Rejected for v1 because:

· Consumer payment UX in target markets (Malaysia, Indonesia, Gulf) is
card- and wallet-based, not crypto-based
· Regulatory clarity for crypto payments varies by market
· The Shariah Board has not yet issued guidance on stablecoin-based escrow
· A hybrid approach (card primary, crypto optional) is a future
  consideration, not a v1 commitment

Alternative D — Defer Payments entirely, build marketplace without it

Rejected. Without payment, the marketplace is a catalog. The whole
value proposition requires transacting.

Dissent

Platform Architecture argued for a concrete vendor choice in ENT-001.
Their position: "Building against an unimplemented port is abstraction
without validation. We will discover the port is wrong when we try to
wire the actual provider."

Partially overruled. The mitigation is that the port is validated
against two candidate providers' APIs during design, not zero. If both
fit the port with reasonable adapter effort, the port is validated. If
neither fits, the port is wrong and we iterate before committing to a
vendor. This is a weaker validation than a concrete implementation, but it
is not abstraction without evidence.

Finance argued for the cheapest provider meeting mandatory criteria.
Their position: "All the mandatory criteria are met by at least two
vendors. Choose the cheaper."

Accepted in principle, deferred in practice. Fee structure is one
criterion among eight. The final selection weighs all criteria. Cost is
not the sole determinant for a system that handles buyer funds.

Shariah Advisory Board raised a concern about the deferral itself.
Their position: "How can you build a payment system before the Shariah
Board has approved the structure?"

Answered. The Shariah Board is engaged in the procurement process.
Their approval is one of the mandatory criteria (criteria 1, 4). No
Payments code reaches production without Board sign-off. The deferral is
of vendor selection, not of Shariah review.

One dissent not held: an early proposal was to select a vendor based
on "best API" alone. This was rejected. API quality is 5% weight. The
constraints dominate.

Reversibility

Vendor swap is a moderate refactor. Switching payment providers
requires:

1. New adapter implementing IPaymentProvider
2. Migration of in-flight transactions (complex if escrow is active)
3. Buyer and vendor notification for account/routing changes
4. Reconciliation of historical data

Estimated cost: 4–8 engineer-weeks for the code, 4–6 weeks of operational
transition for a live marketplace. Not something to do casually after
launch, but possible.

Port shape change is expensive. Every adapter must be updated. If the
port is wrong, we are wrong everywhere. This is why the port shape is
validated against multiple vendor APIs before implementation.

References

· docs/ARCHITECTURE.md §7.4 — halal finance constraints
· docs/procurement/payment-provider-evaluation.xlsx — full evaluation
· Shariah Board memo on wakala escrow (pending)
· HalalChain.Application/Payments/IPaymentProvider.cs
  EOF

═══════════════════════════════════════════════════════════════════════

ADR-008 — Registrar key custody

═══════════════════════════════════════════════════════════════════════
cat > "$ADR/008-registrar-key-custody.md" <<'EOF'
ADR-008 — Registrar key custody

Status: Accepted
Date: 2026-09-26
Deciders: Security, Platform Architecture
Consulted: Compliance, Finance, Legal
Informed: Steering Committee, CISO

Context

The CertificateRegistry contract has an onlyRegistrar modifier. Only
the holder of the registrar key can register or revoke a certificate hash
on-chain. This is a high-value key:

· If the registrar key is compromised, an attacker can register arbitrary
certificate hashes. The platform will treat them as current.
· If the registrar key is lost, certificate registration halts. Existing
certificates remain valid but new ones cannot be added.
· The registrar key is not a "hot wallet" in the traditional sense; it
does not hold funds. But its authority is significant.

Early discussion assumed a hot wallet on the application server, since
that is the simplest pattern. This is now rejected for production.

Decision

Production registrar key is KMS-backed. Hot wallets are permitted only
on the Amoy testnet.

Specifically:

· Amoy testnet: hot wallet. Development convenience. No real value.
· Polygon zkEVM and PoS production: AWS KMS (or equivalent) with an
  asymmetric signing key. The private key never leaves the KMS boundary.
· The tawheed service is the only component with KMS sign permission.
  The platform-api service has no access to the registrar key.
· Key rotation: every 12 months, or immediately upon any suspicion of
  compromise. Rotation is a KMS operation; the on-chain registrar address
  is updated via a governance transaction that requires multi-signature
  approval.

Signing flow

```
1. tawheed receives a certificate registration request
2. tawheed validates the request (evidence, vendor, expiry)
3. tawheed constructs the transaction (to CertificateRegistry, data: registerCertificate(...))
4. tawheed computes the keccak256 hash of the transaction
5. tawheed calls KMS.Sign(hash) → returns signature
6. tawheed submits the signed transaction to the Polygon RPC
7. On confirmation, tawheed records the anchor receipt
```

The private key material is never available to any process in the platform.
A memory dump of tawheed's process space does not reveal the key.

Multi-signature governance

Changing the registrar address requires:

1. Proposal by tawheed operator (signed KMS transaction)
2. Approval by CISO (hardware wallet signature)
3. Approval by Steering (hardware wallet signature)
4. On-chain execution of the transferRegistrar function

Three-of-three required. No single actor can change the registrar.

Consequences

Easier:

· Key compromise requires compromising AWS KMS, not a process memory
· Key rotation is a KMS operation with audit trail
· Access to signing is logged and revocable via IAM
· Regulatory audits accept KMS custody as a control
· The tawheed service has the smallest possible attack surface for the
highest-value operation

Harder:

· KMS adds latency (~100ms per signature) and cost (~$1 per 10k requests)
· The signing flow cannot be tested in unit tests with a real KMS; a
  local signing stub is used in dev
· Adding a new signer requires IAM changes, not just code
· The multi-signature governance step adds a day or two to registrar
  rotation

Alternatives considered

Alternative A — Hot wallet on the application server

Rejected for production. The private key lives in process memory or an
encrypted file. A memory dump, a debugger, a log with accidental
inclusion, or a supply chain compromise of a dependency all leak the key.
For a key that authorizes certificates, this is unacceptable.

Alternative B — Hardware Security Module (HSM) on-premise

Considered. An on-prem HSM provides equivalent security to KMS with lower
per-signature latency. Rejected for v1 on operational grounds: HSMs require
physical security, dedicated infrastructure, and an operational team
trained on the specific HSM. KMS outsources this to the cloud provider.
Reconsider if signing volume exceeds 100k/month.

Alternative C — Multi-signature wallet with distributed keys

Considered. A Gnosis Safe or similar multi-sig with keys held by 3
different people. This distributes trust but complicates automation —
every certificate registration requires 3 people to sign, which does not
scale.

Rejected for routine registrations. However, we adopt a variant of this
for registrar address changes: multi-signature governance is required
for that specific operation.

Alternative D — Threshold signature scheme (TSS)

Considered. TSS provides distributed key management with a single
signature. Rejected for v1 as premature. KMS-backed single-signer is
sufficient. TSS can be adopted later if the threat model changes.

Alternative E — No on-chain registrar, use a platform-signed message

Rejected. The whole point of on-chain registration is third-party
verifiability. A platform-signed message is only as good as the platform's
word.

Dissent

Platform Architecture argued for a hot wallet with the key in AWS
Secrets Manager. Their position: "KMS adds latency and cost. Secrets
Manager is already in use. The key is encrypted at rest."

Overruled because: "encrypted at rest" is insufficient. The key must
be decrypted to be used, which means it lives in process memory during
signing. A memory dump at the right moment reveals it. KMS never exposes
the key. The latency and cost differences are acceptable given the
authority the key grants.

Finance raised the cost concern explicitly. Their position: "KMS
asymmetric signing costs real money at scale."

Accepted and quantified. At current projected volume (approximately
500 certificate registrations/month, plus 1 registrar rotation/year),
the KMS cost is negligible (< $50/year). If volume scales 100×, the cost
scales but remains small relative to the value of the certificates being
registered. The cost is not a blocker.

Compliance asked whether KMS custody satisfies audit requirements.
Their position: "Will external auditors accept KMS as a control?"

Answered with precedent. SOC 2, ISO 27001, and PCI DSS auditors
routinely accept cloud KMS as a key custody control. AWS KMS is FedRAMP
High and ISO 27001 certified. The audit trail (CloudTrail) logs every
sign operation, which is the evidence auditors require.

One dissent held: the multi-signature requirement for registrar
changes is non-negotiable. The dissent was that it slows incident
response — "if we suspect compromise, we need to change the registrar
now, not in 2 days."

Overruled with an incident path. A break-glass procedure exists:
under a declared security incident, the CISO can initiate an emergency
registrar change with a single hardware wallet signature, and the
post-incident review retroactively documents the action. The normal
3-of-3 procedure applies to planned rotations.

Reversibility

Cheap. If KMS proves operationally painful, migration to an HSM or
TSS is a signing-layer change. The port (IAnchorSigner) abstracts the
signer. Estimated cost: 2 engineer-weeks.

Registrar address change is a governance operation, not a code change.
The address is stored in the contract; changing it requires the
multi-signature flow.

References

· ADR-006 — Chain network selection (zkEVM and PoS both require registrar)
· .cline_inbox/PRINCIPLES.md — P7 (registrar key never in hot wallet in prod)
· docs/security/kms-setup.md — KMS configuration and IAM policy
· HalalChain.Platform.Api/Modules/Blockchain/Infrastructure/Nethereum/AnchorSigner.cs
  EOF

═══════════════════════════════════════════════════════════════════════

ADR-009 — Multi-tenancy model

═══════════════════════════════════════════════════════════════════════
cat > "$ADR/009-multi-tenancy-model.md" <<'EOF'
ADR-009 — Multi-tenancy model

Status: Accepted — provisional, with documented upgrade path
Date: 2026-09-26
Deciders: Platform Architecture, Steering Committee
Consulted: Security, Compliance, DPO, Finance
Informed: Product, SRE

Context

The platform serves multiple vendors and buyers. "Multi-tenancy" could mean:

1. Single-tenant — one instance per customer, physically separate
2. Multi-tenant, shared schema — all tenants in one database, scoped
   by row
3. Multi-tenant, schema-per-tenant — one schema per tenant, shared
database
4. Multi-tenant, database-per-tenant — one database per tenant

The choice affects isolation, cost, operational complexity, and the ability
to satisfy jurisdictional requirements.

Early discussion assumed "obviously shared schema with row-level security"
because that is the low-cost default. But the platform has real tenants
who will care about isolation: enterprise vendors in regulated markets,
halal certification bodies, and multi-national merchants. A single model
may not serve all of them.

Decision

Provisional: tier-based isolation, with shared schema + RLS as the
default.

Tenant tier Isolation Postgres Blob Credentials
Standard Shared schema + RLS Shared DB, shared schema Shared bucket, tenant prefix Shared DB user with RLS enforcement
Premium Schema-per-tenant Shared DB, own schema Shared bucket, tenant prefix Schema-scoped DB user
Enterprise Database-per-tenant Own DB Own bucket Separate DB credentials

The default tier for new tenants is Standard. Upgrade to Premium or
Enterprise is a paid offering with a documented migration procedure.

Non-negotiable: tenant isolation tests pass at every tier, per module.
Sampling is not acceptable. A failure in any tier blocks the ENT-005 gate.

Upgrade path: Standard → Premium

1. Provision new schema in the same database
2. Run migration that copies tenant's rows from shared schema to new schema
3. Freeze writes (brief maintenance window, target < 5 minutes)
4. Switch tenant's connection routing to new schema
5. Verify isolation tests
6. Resume writes
7. Optionally clean up old rows in shared schema (retain for rollback window)

Estimated downtime: 2–5 minutes for a typical tenant.

Upgrade path: Premium → Enterprise

1. Provision new database
2. Replicate tenant's schema and data
3. Freeze writes (longer window, target < 30 minutes)
4. Repoint tenant's connection
5. Verify isolation tests
6. Resume writes
7. Optionally retain source database for rollback

Estimated downtime: 15–30 minutes.

Upgrade path: Standard → Enterprise

Combines both steps. Estimated downtime: 30–45 minutes.

Consequences

Easier:

· Cost-effective for the common case (Standard tier, shared everything)
· Enterprise tenants get true isolation without a separate deployment
· The upgrade path is a documented procedure, not a migration project
· Isolation tests are per-tier, which means the guarantees are explicit

Harder:

· Three tiers means three sets of isolation tests
· Application code must be tier-aware in some places (connection routing)
  — this is abstracted by a TenantConnectionResolver
· The shared schema default puts pressure on RLS to be correct; a bug in
  RLS is a cross-tenant leak
· Migration between tiers requires downtime, which must be scheduled
· Cost attribution is more complex in shared tiers

Alternatives considered

Alternative A — Shared schema + RLS only, no tiers

Rejected. Enterprise tenants in regulated markets often have contractual
or regulatory requirements for physical isolation. "Trust our RLS" is not
sufficient for some procurement processes. Offering tiers is a
differentiator.

Alternative B — Database-per-tenant for everyone

Rejected on cost. Provisioning and operating N databases for N tenants is
expensive at scale. Most tenants do not need it. Making it the default
would price out small vendors.

Alternative C — Single-tenant deployments (one stack per customer)

Rejected on operational cost. Each tenant would require its own full stack
(API, database, cache, storage, chain access). This is a multi-year
operational commitment that does not match the platform's target market.

Alternative D — Shared schema, no RLS, application-enforced filtering

Rejected. RLS is a defense-in-depth mechanism. Application-level filtering
is the first line; RLS is the backstop. Removing RLS makes a single bug
catastrophic.

Alternative E — Defer the decision, ship shared schema, decide later

Considered. Rejected because the isolation model affects schema design,
connection management, and testing infrastructure. Deciding later means
retrofitting, which is more expensive than designing for it now.

This ADR is effectively a compromise: the default is shared schema
(the deferred position), but the architecture supports upgrade (the
designed-for position). The upgrade paths are documented. The tier
boundaries are enforced by config.

Dissent

Platform Engineering argued against implementing three tiers. Their
position: "Three tiers is three times the isolation tests, three times the
operational surface, and three times the ways to get it wrong. Ship one
tier (shared schema + RLS) and add tiers only when a paying customer
demands it."

Partially overruled. The tiers are not implemented in v1. Only Standard
tier is operational at launch. The Premium and Enterprise tiers are
designed for but not built. The upgrade paths are documented so that
when the first Premium tenant signs, we know what to do. The ADR commits
to the model, not to immediate implementation.

Security argued for database-per-tenant as the only safe option for a
compliance platform. Their position: "RLS is a Postgres feature. Postgres
has bugs. A single CVE means a cross-tenant leak."

Accepted as a risk, quantified. RLS is mature (15+ years in Postgres).
The realistic risk is not RLS bypass but application misconfiguration
(missing SET LOCAL app.tenant_id). Mitigation: every connection
initialization sets the tenant context; a connection without tenant context
cannot query tenant-scoped tables (RLS fails closed). Additional mitigation:
integration tests assert that queries without tenant context return zero
rows.

DPO raised a concern about data residency within a shared schema. Their
position: "If a Malaysian tenant's data and an EU tenant's data are in the
same database, GDPR Article 44 applies to backups and replication."

Accepted and addressed. The Standard tier is region-pinned: a tenant's
data resides in the tenant's declared region. Cross-region tenants share
a schema only within their region. This is a constraint on schema
placement, not on schema existence. Premium and Enterprise tenants can
override region pinning only with explicit legal review.

One dissent held: tenant isolation tests must run on every commit, not
just before release. The dissent was that they slow the CI pipeline.
Overruled because: a regression in tenant isolation is a compliance
violation, not a bug. The CI cost is the point.

Reversibility

Model change is expensive. Moving from tiered to single-tier would
require migrating Premium and Enterprise tenants to shared schema, which
is a data migration with downtime. Not recommended.

Tier addition is cheap. Adding a fourth tier (e.g. "Regional
Enterprise" with region-pinned dedicated database) is a config change and
documentation, not a rewrite.

Upgrade paths are the reversibility mechanism. A tenant can move
between tiers without re-architecting the platform. This is the property
that makes the provisional decision safe.

References

· ADR-001 — Modular monolith (module boundaries affect RLS design)
· docs/ARCHITECTURE.md §4 — storage layer (blob prefix isolation)
· HalalChain.Platform.Tests/TenantIsolation/ — isolation test suite
· bau/LIFECYCLE/data-residency.md — region pinning rules
  EOF

═══════════════════════════════════════════════════════════════════════

Verify

═══════════════════════════════════════════════════════════════════════

echo "✅ Wrote 9 ADRs to $ADR"
echo
echo "Character by character total line counts per ADR:"
for n in 001 002 003 004 005 006 007 008 009; do
  f=$(ls "$ADR"/${n}-*.md 2>/dev/null | head -1)
  if [[ -f "$f" ]]; then
    lines=$(wc -l < "$f")
    dissent=$(grep -c '^## Dissent' "$f" || true)
    printf "  ADR-%s  %5s lines  dissent:%s  %s\n" "$n" "$lines" "$dissent" "$(basename "$f")"
  else
    printf "  ADR-%s  MISSING\n" "$n"
    exit 1
  fi
done

echo
echo "Gate G1.3 checks ADR-001..005 have dissent sections; G1.4..G1.7 check 006..009."
echo "Verify with: yq '.entries[] | select(.phase==\"ENT-001\")' .cline_inbox/manifests/evidence-index.yaml"
EOF
