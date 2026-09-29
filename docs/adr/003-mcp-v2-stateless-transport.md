# ADR-002 — Storage ports, two hashers, append-only evidence

**Status:** Accepted

**Date:** 2026-09-26

**Deciders:** Platform Architecture

**Consulted:** Compliance, Security, Platform Engineering

## Context

Two problems converged. First, evidence had no canonical home — early
prototypes wrote files ad-hoc, which is not auditable. Second, on-chain
anchoring requires content addressing so a verdict can cite a specific
piece of evidence and survive re-upload.

"Just use S3" hides the domain concept behind the mechanism. "Just use
SHA-256 everywhere" conflates the storage address with the on-chain tree
node — two hashes serving two different purposes.

## Decision

**Two ports, two hashers, one append-only store.**

### Ports (shipped)

\IBlobStore\ in \HalalChain.Application/Storage/IBlobStore.cs\ — mechanism:

\\\csharp
public interface IBlobStore
{
    Task<BlobRef> PutAsync(Stream content, BlobMetadata metadata, CancellationToken ct = default);
    Task<Stream?> OpenReadAsync(BlobRef reference, CancellationToken ct = default);
    Task<bool> ExistsAsync(BlobRef reference, CancellationToken ct = default);
    // DeleteAsync deliberately absent. See below.
}
\\\

\IEvidenceStore\ — domain concept. Composes \IBlobStore\, adds immutability,
access logging, retention. Exactly one implementation (\EvidenceStore\)
because semantics are not provider-specific.

Both ports live in \HalalChain.Application/Storage/\. Adapters live in
\HalalChain.Storage/Adapters/\. Dependency direction: Storage → Application,
never the reverse.

### Hashers

- **SHA-256** for \BlobRef\ content addressing. Lowercase hex. Layout
  \{hash[0..2]}/{hash}\.
- **Keccak256** for on-chain Merkle tree internal nodes. Sorted-pair.

The two never mix. \Sha256ContentHasher\ produces a \BlobRef\. A SHA-256
digest entering a Merkle tree is treated as a 32-byte leaf and combined
with keccak256 at every internal node.

### Append-only by type

\IBlobStore\ declares no delete member. This is compile-time. Enforced by
\HalalChain.Architecture.Tests.Rules.StorageRules.IBlobStore_has_no_delete_member\,
which rejects any public member named with one of four forbidden fragments:

\\\csharp
private static readonly string[] ForbiddenMemberFragments =
    ["delete", "remove", "purge", "destroy"];
\\\

Four verbs, not one. Renaming \DeleteAsync\ to \PurgeAsync\ fails the test.

### Value object guarantees

\BlobRef\ validates in the constructor:

\\\csharp
public BlobRef(string contentHash)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(contentHash);
    if (contentHash.Length != 64)
        throw new ArgumentException("SHA-256 hash must be exactly 64 hex characters.", ...);
    foreach (var c in contentHash)
        if (!Uri.IsHexDigit(c))
            throw new ArgumentException("SHA-256 hash must contain only hex characters.", ...);
    ContentHash = contentHash.ToLowerInvariant();
}
\\\

No path to an invalid \BlobRef\. The value object cannot be in a bad state.

\EvidenceId\ uses UUID v7 via \Guid.CreateVersion7()\. Time-ordered, so
record IDs sort by ingestion time and Postgres indexes stay effective for
range queries.

\Actor\ lives in \HalalChain.Application/Identity/Actor.cs\, not in
storage. It is used by storage, agents, and audit. \IngestAsync\ and
\OpenAsync\ take an explicit \Actor\ — no ambient \ICurrentUser\.

## Consequences

**Easier:** Every evidence item has a canonical, verifiable address.
Deduplication is free. Storage backend swaps without domain changes.
Content addressing maps 1:1 to IPFS CID if that becomes product-facing.
Append-only is enforced by type, not convention.

**Harder:** Two hashers to reason about. Content addressing means lookup
is by hash, not path. No delete means storage grows monotonically —
retention is metadata tombstone. \ISignedUrlIssuer\ is separate because
signed URLs are provider-specific.

## Alternatives considered

**Single \IStorage\ port with delete.** Rejected. Collapses mechanism and
domain — every new backend must reimplement immutability, logging, and
retention. Exposes \Delete\, which the domain cannot permit.

**SHA-256 everywhere, including on-chain.** Rejected. Solidity's native
hash is keccak256. SHA-256 on-chain costs more gas and requires a
precompile. The tree would not match OpenZeppelin's reference
implementation, so the parity test would fail.

**Keccak256 for storage addressing.** Rejected. Content-addressed storage
systems (IPFS, S3 integrity, .NET built-in) expect SHA-256. Using keccak256
breaks interop with existing tooling.

**UUID addressing instead of content hash.** Rejected. UUIDs don't verify
integrity. A modified file re-uploaded under the same UUID would not be
detected.

## Dissent

**Platform Engineering** argued for one hasher (SHA-256) with a translation
layer at the chain boundary. *Overruled:* the translation is not free. Leaves
would need SHA-256 while internal nodes use keccak256, which does not match
OpenZeppelin. Native keccak256 from leaves up is cleaner and matches the
ecosystem.

**Compliance** raised that content-addressing makes PII discoverable by
hash (a hash oracle). *Mitigated:* \IBlobStore\ requires an authenticated
actor for reads. There is no public hash-to-exists endpoint. Every read is
logged. Enterprise tier uses per-tenant buckets.

**Security** raised same-hash re-upload as a poisoning vector. *Accepted
and noted:* SHA-256 collisions are computationally infeasible. Migration
to SHA-3 is a config change if needed.

**Architecture Review Board** required the four-verb forbidden list, not
just "delete." *Accepted:* a renamed member fails the same test.

## Reversibility

**Hasher swap: cheap.** New \IContentHasher\, re-index existing blobs.
~1 engineer-week plus backfill.

**Port change: expensive.** Every adapter reimplements domain semantics.
Not recommended.

**Content-addressing removal: prohibitive.** Existing anchors reference
Merkle roots from content hashes. Removing it invalidates every anchor.

## References

- ADR-006 — Chain selection (depends on keccak256 tree)
- .cline_inbox/PRINCIPLES.md — P3, P5
- \HalalChain.Application/Storage/\ — shipped code
- \HalalChain.Architecture.Tests/Rules/StorageRules.cs\
"@ | Set-Content (Join-Path C:\Users\kurni\OneDrive\Desktop\waklu-keewak\docs\adr "002-storage-port-adapter-and-hashers.md")

# ═══════════════════════════════════════════════════════════════════════
# ADR-003 — MCP v2 stateless transport
# ═══════════════════════════════════════════════════════════════════════

@"
# ADR-003 — MCP v2 stateless transport

**Status:** Accepted

**Date:** 2026-09-26

**Deciders:** Platform Architecture

**Consulted:** Platform Engineering, Security

## Context

The platform exposes an MCP server so coding agents and compliant AI clients
can inspect the solution, query evidence, and evaluate policy through a
standardized tool interface.

MCP v1 requires a stateful session per client and sticky routing across
instances. MCP C# SDK v2.0 (July 2026) made the protocol stateless by
default, added Multi-Round-Trip Requests (MRTR) for approval-gated tools,
and standardized routing headers.

## Decision

**MCP C# SDK v2.0 with stateless transport.** No session affinity. No
in-memory session state. Tools are pure functions of input and ambient
authentication.

MRTR handles multi-step approvals. Routing headers standardize discovery
across hosts.

The MCP server in \HalalChain.Mcp/\ is AOT-compiled and read-only. Tools
that would mutate state require either explicit MRTR approval or execution
through the platform API — never direct MCP mutation.

Every MCP call requires authentication. There is no public discovery
endpoint.

## Consequences

**Easier:** Horizontal scaling behind any load balancer. No sticky session
config. No session expiry logic. Simpler testing — every call is
self-contained. AOT-compilable, small footprint. MRTR gives a natural
approval gate.

**Harder:** Cross-request state must persist externally. Older clients
assuming a session need adaptation. MRTR adds round trips for approvals.

## Alternatives considered

**MCP v1 with sticky sessions.** Rejected. Sticky sessions break under
autoscaling. Failure mode is subtle — pod restart drops in-flight state.

**Custom tool protocol.** Rejected. Ecosystem interop is the point.
Building bespoke means every agent client needs a bespoke adapter.

**MCP v2 with a session cache layer.** Rejected. Cache reintroduces the
affinity v2 removed.

**v1 for reads, v2 for writes.** Rejected. Two protocols, two bugs. Writes
go through the API instead.

## Dissent

**Platform Engineering** wanted to wait for 2.1 or 2.2. *Overruled:* the
stateless change is architectural. Adopting later means refactoring every
tool that assumed session state. Adopting now means never writing that code.
The SDK is small enough to vendor if needed.

**Security** raised MRTR as an approval-fatigue vector. *Mitigated:* MRTR
approval requests are rate-limited per client and per actor. Repeated
prompts are throttled. \IAccessLog\ records every request, so abuse patterns
are detectable.

**One rule held despite pressure:** no public discovery endpoint. Some
early discussion suggested unauthenticated read-only tools were acceptable.
*Overruled:* read-only tools can still leak vendor data, evidence metadata,
and policy details. Every call authenticates.

## Reversibility

Cheap. Reintroducing session state means adding a session store (Redis is
present) and sticky routing. ~2 engineer-weeks. We would only do this if a
future spec required stateful semantics for a critical tool.

## References

- \HalalChain.Mcp/Program.cs\
- MCP C# SDK v2.0 release notes (July 2026)
