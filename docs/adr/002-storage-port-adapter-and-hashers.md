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
