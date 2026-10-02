# HalalChain.Storage.Tests

xUnit (v3) tests for the filesystem blob and evidence storage adapters in
`HalalChain.Storage/`.

## Purpose

Storage is the system of record for evidence, so its tests are written as
**contract** tests as well as unit tests: the append-only rule, the
content-addressing rule, and the retention rule are properties of the adapter
family, not of one implementation.

## Layout

```
HalalChain.Storage.Tests/
├── BoundaryTests.cs               # Reflection checks on the adapter surface
├── BlobStoreContractTests.cs      # Contract tests any IBlobStore must satisfy
│   (Contract/)
├── EvidenceStoreTests.cs          # EvidenceStore ingest/open/retention behaviour
├── FileSystemBlobStoreTests.cs    # The filesystem adapter specifically
├── RetentionEvaluatorTests.cs     # Retention policy evaluation
└── Sha256ContentHasherTests.cs    # Content hashing and HashMismatchException
```

## Dependencies

| Kind | Reference |
| --- | --- |
| Project | `HalalChain.Storage` only |
| Package | `xunit.v3`, `xunit.runner.visualstudio` v3, `Microsoft.NET.Test.Sdk` |

## Run

From the repository root:

```bash
dotnet test HalalChain.Storage.Tests -c Release
```

## Related Components

- [HalalChain.Storage](../HalalChain.Storage/README.md) — the system under test
- [HalalChain.Application](../HalalChain.Application/README.md) — declares the ports under test
- [HalalChain.Architecture.Tests](../HalalChain.Architecture.Tests/README.md) — `IBlobStore_DeclaresNoDeleteMember_ArchTest` and `Storage_AssemblyDeclaresNoMerkleTreeType`
- [ADR-002: storage port, adapter, and hashers](../docs/adr/002-storage-port-adapter-and-hashers.md) — the merge decision these tests encode

## Notes / Limitations

- `BlobStoreContractTests.cs` is written as a reusable contract suite so a future
  S3 or Azure adapter can run the same assertions. No such adapter exists today.
- No host registers `AddFileSystemStorage`, so these tests are the only exercise
  the adapters receive. See the "Wiring" section of the
  [Storage README](../HalalChain.Storage/README.md).