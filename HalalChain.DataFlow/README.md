# HalalChain.DataFlow

A leaf integration library for moving halal records between external PostgreSQL
databases and the platform's canonical shape.

## Purpose

`HalalChain.DataFlow` implements the *data* half of the ETL contract that the
modular monolith deliberately does not own: reading records from a source
database, normalising them, validating them, and upserting them into a
destination database — with anything that cannot be validated routed to a
dead-letter sink instead of failing the batch.

It is a library, not a host. It has no `Program.cs` and nothing references it
except its test project.

## Responsibilities

- Define the canonical, normalised record shape (`CanonicalHalalRecord`).
- Read from a PostgreSQL source, statically typed or dynamically via reflection.
- Normalise halal text (case folding, whitespace, Unicode) via `HalalTextNormalizer`.
- Validate canonical records and report per-record outcomes with reason codes.
- Upsert into a PostgreSQL destination with identifier resolution.
- Route rejected records to a dead-letter sink.
- Expose a reusable `DataFlowPipeline<TRecord>` that wires source → transform →
  validate → destination.

## Structure

```
HalalChain.DataFlow/
├── IDataFlowPipeline.cs                # IDataFlowPipeline<TRecord>
├── DataFlowPipeline.cs                 # Default pipeline implementation
├── Configuration/
│   └── DataFlowOptions.cs              # Global + per-source/destination options
├── DependencyInjection/
│   └── DataFlowServiceCollectionExtensions.cs   # AddHalalChainDataFlow(IConfiguration)
├── Models/
│   ├── CanonicalHalalRecord.cs
│   └── DataFlowModels.cs
├── Source/
│   ├── IDataFlowSource.cs              # IDataFlowSource<T>, IDataFlowSourceFactory
│   ├── PostgresDataFlowSource.cs       # Statically typed
│   ├── PostgresDataFlowSourceFactory.cs
│   └── DynamicPostgresDataFlowSource.cs# Reflection-based column mapping
├── Destination/
│   ├── IDataFlowDestination.cs         # IDataFlowDestination<T>, factory, DataFlowBatchResult
│   ├── PostgresDataFlowDestination.cs
│   ├── PostgresDataFlowDestinationFactory.cs
│   └── DynamicPostgresDataFlowDestination.cs
├── Transform/
│   ├── IDataFlowTransformer.cs         # IDataFlowTransformer<TSource,TDestination>
│   └── HalalTextNormalizer.cs          # Text canonicalisation helpers
└── Validation/
    ├── CanonicalHalalRecordValidator.cs
    └── FileSystemDeadLetterSink.cs     # IDeadLetterSink
```

## Architecture / Flow

```text
External PostgreSQL (source)
   ↓  IDataFlowSource<CanonicalHalalRecord>
[ DataFlowPipeline ]
   ↓  IDataFlowTransformer  → HalalTextNormalizer
   ↓  IDataFlowValidator   → CanonicalHalalRecordValidator
   ├── valid   → IDataFlowDestination → Target PostgreSQL (upsert)
   └── invalid → IDeadLetterSink       → filesystem dead-letter directory
```

## Dependencies

| Kind | Reference |
| --- | --- |
| Project | `HalalChain.Domain`, `HalalChain.Platform.Contracts` — and nothing else |
| Package | `Microsoft.EntityFrameworkCore` 10.0.11, `Microsoft.EntityFrameworkCore.Relational` 10.0.11, `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.0, `Microsoft.Data.SqlClient` 6.0.1, `FluentValidation` 12.1.1 (+ DI extensions), `AutoMapper` 16.2.0, `Polly` 8.5.2, `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Options`, `Microsoft.Extensions.Options.ConfigurationExtensions`, `Microsoft.Extensions.Logging.Abstractions` |

The project file states the intent explicitly: *"DataFlow is a leaf integration
library: it must not drag in the API host, UI, or MCP, and it does not need
Application abstractions."* `TreatWarningsAsErrors` is `false` here (it is `true`
in `HalalChain.Platform.Http`).

## Interfaces

| Interface | Role |
| --- | --- |
| `IDataFlowPipeline<TRecord>` | Orchestrates source → transform → validate → destination |
| `IDataFlowSource<TRecord> : IAsyncDisposable` | Produces batches; created by `IDataFlowSourceFactory` |
| `IDataFlowDestination<TRecord> : IAsyncDisposable` | Consumes validated batches; returns `DataFlowBatchResult` |
| `IDataFlowTransformer<TSource, TDestination>` | Maps source shape to canonical shape |
| `IDataFlowValidator<TRecord>` | Returns `ValidationOutcome` per record |
| `IEntityKeyResolver` | Resolves cross-system identifiers (`InMemoryEntityKeyResolver` ships here) |
| `IDeadLetterSink` | Receives rejected records (`FileSystemDeadLetterSink` ships here) |

### Registration

```csharp
services.AddHalalChainDataFlow(configuration);
```

This binds `DataFlowOptions` and registers — all via `TryAdd*`, so a host may
substitute any of them — `IEntityKeyResolver`, `IDataFlowValidator<CanonicalHalalRecord>`,
`IDeadLetterSink`, `IDataFlowSourceFactory`, and `IDataFlowDestinationFactory`.

## Usage

```csharp
await using var scope = serviceProvider.CreateAsyncScope();

var pipeline = scope.ServiceProvider.GetRequiredService<IDataFlowPipeline<CanonicalHalalRecord>>();
var result = await pipeline.RunAsync(cancellationToken);
```

## Configuration

Bound from the `DataFlow` section (see `Configuration/DataFlowOptions.cs`).
The dead-letter directory comes from `options.Global.DeadLetterDirectory`; when
unset it defaults to `<temp>/halalchain/deadletter`.

## Integration

- **Currently unconnected.** No other project in the solution references
  `HalalChain.DataFlow` except `HalalChain.DataFlow.Tests`, and no host calls
  `AddHalalChainDataFlow`. It is a buildable library awaiting an entry point.
- The natural consumers are `HalalChain.Automation` (a scheduled ingest job) and
  `HalalChain.Platform.Api` (on-demand vendor data sync) — neither is wired today.

## Development

```bash
dotnet build HalalChain.DataFlow/HalalChain.DataFlow.csproj
```

Do not add references to `HalalChain.Application`, the API, the UIs, or MCP.

## Testing

```bash
dotnet test HalalChain.DataFlow.Tests/HalalChain.DataFlow.Tests.csproj
```

`HalalChain.DataFlow.Tests` (xunit.v3) covers:

- `CanonicalHalalRecordValidatorTests.cs` — validation rules
- `HalalTextNormalizerTests.cs` — normalisation
- `InMemoryEntityKeyResolverTests.cs` — key resolution
- `FileSystemDeadLetterSinkTests.cs` — dead-letter routing
- `SqlConstructionTests.cs` — SQL guard rails (the library builds SQL text for
  dynamically mapped tables, so the tests assert on what it is and is not allowed
  to construct)

## Deployment

Not deployable and not referenced by any deployable service. Nothing in
`docker-compose.yml` runs it.

## Related Components

- [HalalChain.Domain](../HalalChain.Domain/README.md) — the vocabulary the canonical record normalises to
- [HalalChain.Automation](../HalalChain.Automation/README.md) — the natural host for a scheduled ingest job
- [HalalChain.Platform.Api](../HalalChain.Platform.Api/README.md) — owns the platform's PostgreSQL schema
- [HalalChain.DataFlow.Tests](../HalalChain.DataFlow.Tests/README.md)

## Notes / Limitations

- No source or destination other than PostgreSQL is implemented. `Microsoft.Data.SqlClient`
  is referenced but no SQL Server implementation ships.
- The pipeline is defined but has no production caller; behaviour described here
  comes from unit tests, not from an end-to-end run.