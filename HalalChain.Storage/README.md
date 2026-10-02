# HalalChain.Storage

Storage adapters behind the ports in `HalalChain.Application/Storage/`.

This library is a **leaf**. It references Application and Domain only, and is
referenced by exactly one project: `HalalChain.Platform.Api`, the composition
root. That is enforced by `HalalChain.Architecture.Tests`.

## Layout

```text
HalalChain.Storage/
├─ Adapters/FileSystem/{FileSystemBlobStore,FileSystemOptions,LocalProxySignedUrlIssuer}.cs
├─ Integrity/{IContentHasher impl, Sha256ContentHasher, HashMismatchException}.cs
├─ Evidence/{EvidenceStore,RetentionEvaluator,FileSystemEvidenceMetadataStore,
│            NdjsonAccessLogger,NullAccessLogger}.cs
└─ DependencyInjection/StorageServiceCollectionExtensions.cs
```

## The hasher boundary (merge decision M1)

This module uses **SHA-256 only**, and only to address storage:

- `Sha256ContentHasher` → `BlobRef.ContentHash` → the on-disk filename and the
  `{hash[0..2]}/{hash}` shard layout.
- Keccak256 has **no place here**. On-chain Merkle internal nodes are a separate
  concern and belong to `HalalChain.Platform.Api/Modules/Blockchain/Infrastructure/Merkle/`.

The two hashers both consume bytes, so a contributor may assume they belong
together. They do not. One addresses storage; the other proves on-chain
inclusion. Conflating them breaks every inclusion proof on the first
`verifyInclusion` call, because the roots can never match.

The guard test is `Storage_ShouldNotDeclare_AnyMerkleTreeType` in
`HalalChain.Architecture.Tests`.

## Content addressing

`PutAsync` cannot know the storage address until it has read the last byte, and
the bytes still have to land on disk. The filesystem adapter therefore spools to
a temp file while hashing in a single pass, then moves the file to its final
`{shard}/{hash}` path. This is correct for non-seekable streams and keeps peak
memory flat regardless of evidence size.

Writes are atomic: the move is the commit point. If a concurrent writer wins the
race, the move fails with `IOException`, the temp file is discarded, and the
winner's file stands — which is safe, because content addressing guarantees the
bytes are identical.

## Deliberate omissions

Append-only is enforced by the type system, not by convention. `IBlobStore` has
no `Delete` or `Remove` member, and `BlobStoreContractTests` asserts this
structurally so a future contributor sees the rule.

## Deferred adapters

| Adapter | Blocked by |
| --- | --- |
| `S3BlobStore` / `S3SignedUrlIssuer` | Step 30 in the build order |
| `AzureBlobStore`, `IpfsBlobStore` | No concrete consumer |
| `PostgresEvidenceMetadataStore` | API composition root (build order step 8) |
| `PostgresAccessLogger` | API composition root (build order step 8) |
| `BlobProxyEndpoint` (`GET /_blob/{hash}`) | API composition root — validates the HMAC token and streams |
| `RetentionHostedService` | API composition root — the scheduled sweep |

## Known constraints

**Ingest buffers the stream in memory.** `EvidenceStore.IngestAsync` copies the
caller's stream into a `MemoryStream` so it can hash and then put without
requiring a rewind-capable stream. That is fine for certificates and lab reports
(bounded by policy, well under 100 MB). If supplier audit videos are ever
ingested, this needs a two-pass contract or a spooled temp file — decide before
the first large upload, not after.

**`LocalProxySignedUrlIssuer` is a shared symmetric secret.** Any service holding
`Blob:LocalProxy:SigningKey` can mint a URL for any blob. Fine for dev and
tests; in production the S3 adapter's IAM-scoped presigned URLs are the correct
answer and this type must never be deployed. Its constructor throws if the key
is blank so it cannot fail open.

**No project references this one.** `HalalChain.Storage` is a `ProjectReference`
of `HalalChain.Agents` only. It is *not* referenced by
`HalalChain.Platform.Api`, despite earlier revisions of this document claiming
so.

The assembly guard is `Storage_AssemblyDeclaresNoMerkleTreeType` in
`HalalChain.Architecture.Tests/DependencyRulesTests.cs` — merge decision M1
reserves Keccak-256 Merkle internals for the blockchain module, so Storage must
not declare a Merkle type of its own.

**`VerifyOnRead` is off by default** because it costs a full read on every
retrieval. Enable it where integrity outweighs throughput — certificate
retrieval, audit replay.

## Wiring

`StorageServiceCollectionExtensions` exposes three registration methods:

| Method | Registers |
|---|---|
| `AddFileSystemStorage(IConfiguration)` | `IContentHasher`→`Sha256ContentHasher`, `IBlobStore`→`FileSystemBlobStore`, `IEvidenceMetadataStore`→`FileSystemEvidenceMetadataStore`, `ISignedUrlIssuer`→`LocalProxySignedUrlIssuer`, `RetentionEvaluator`, `IEvidenceStore`→`EvidenceStore` (all singletons) |
| `AddNdjsonAccessLog(IConfiguration)` | `IAccessLog`→`NdjsonAccessLogger` |
| `AddNullAccessLog()` | internal `NullAccessLogger` |

**These are not currently called anywhere in production code.** A search across
`HalalChain.Platform.Api`, `HalalChain.Marketplace`, and `HalalChain.Web` for
`AddFileSystemStorage`, `AddNdjsonAccessLog`, `AddNullAccessLog`, `IBlobStore`,
and `IEvidenceStore` returns nothing. The ports are declared in
`HalalChain.Application/Storage/` and implemented here, but no composition root
registers them, so the adapters do not run in any deployable service today.

Consequences of that gap, recorded here so they are not mistaken for working
features:

- `RetentionEvaluator` exists but no hosted service calls it. There is no
  `RetentionHostedService` in this repository.
- `LocalProxySignedUrlIssuer` exists but no `GET /_blob/{hash}` endpoint is
  mapped. There is no `MapBlobProxy()` extension here.
- The S3 / Azure / IPFS / Postgres variants listed above do not exist in the
  code; only the filesystem adapters ship.

`HalalChain.Agents` references this project, but only to satisfy the
`IBlobStore` / `IAgentTraceStore` *ports* in its type signatures
(`BlobStoreTraceLoader`). It does not register these DI extensions either.
