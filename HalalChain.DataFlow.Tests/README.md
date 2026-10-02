# HalalChain.DataFlow.Tests

xUnit (v3) tests for the data-flow source/validate/transform/destination
library in `HalalChain.DataFlow/`.

## Purpose

These tests cover normalisation, validation, key resolution, dead-letter
routing, and — because the library builds SQL text for dynamically mapped
tables — the SQL construction guard rails.

## Layout

```
HalalChain.DataFlow.Tests/
├── HalalTextNormalizerTests.cs                 # Text canonicalisation
├── CanonicalHalalRecordValidatorTests.cs       # Validation rules and reason codes
├── InMemoryEntityKeyResolverTests.cs           # Cross-system identifier resolution
├── FileSystemDeadLetterSinkTests.cs            # Rejected-record routing
└── SqlConstructionTests.cs                     # What the SQL builder is and is not allowed to build
```

`SqlConstructionTests.cs` exists because `DynamicPostgresDataFlowSource` and
`DynamicPostgresDataFlowDestination` map columns by reflection. Identifier
interpolation is the risk; these tests pin the allowed shapes.

## Dependencies

| Kind | Reference |
| --- | --- |
| Project | `HalalChain.DataFlow` only |
| Package | `xunit.v3`, `xunit.runner.visualstudio` v3, `Microsoft.NET.Test.Sdk` |

## Run

From the repository root:

```bash
dotnet test HalalChain.DataFlow.Tests -c Release
```

## Related Components

- [HalalChain.DataFlow](../HalalChain.DataFlow/README.md) — the system under test
- [HalalChain.Domain](../HalalChain.Domain/README.md) — the vocabulary the canonical record normalises to
- [HalalChain.Automation](../HalalChain.Automation/README.md) — the intended host for scheduled ingestion

## Notes / Limitations

- `HalalChain.DataFlow` has no production caller, so these tests exercise the
  library in isolation rather than through a pipeline run.