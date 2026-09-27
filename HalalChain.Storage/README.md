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

**`VerifyOnRead` is off by default** because it costs a full read on every
retrieval. Enable it where integrity outweighs throughput — certificate
retrieval, audit replay.

## Wiring

In the API composition root:

1. `builder.Services.AddFileSystemStorage(builder.Configuration)` — or the S3
   variant when the storage profile is active.
2. `builder.Services.AddNdjsonAccessLog(builder.Configuration)` in dev, the
   Postgres adapter in production.
3. `builder.Services.AddHostedService<RetentionHostedService>()` — the sweep.
4. `app.MapBlobProxy()` — the `GET /_blob/{hash}` endpoint.

Steps 3 and 4 live in the API, not here, because they need the ASP.NET Core
host. This library stays host-agnostic.
