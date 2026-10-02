# HalalChain.Platform.Tests

xUnit test suite for the core REST API, persistence layer, AI resilience, and
per-vendor data isolation.

## Purpose

These are the behavioural tests that prove the API actually works and that the
platform's trust boundaries hold at runtime — as opposed to
`HalalChain.Architecture.Tests`, which proves the layering is right at compile
time.

## Layout

```
HalalChain.Platform.Tests/
├── CustomWebApplicationFactory.cs   # WebApplicationFactory bootstrap (SQLite, overridden config)
├── ApiIntegrationTests.cs            # End-to-end HTTP tests against the real host
├── PersistenceTests.cs               # EF Core / repository behaviour
├── VendorIsolationTests.cs           # Cross-vendor data isolation
├── TrustBoundaryTests.cs             # What the API will and will not do
├── SecurityPolicyTests.cs            # Auth, JWT, CORS, rate limiting
├── SemanticSearchFallbackTests.cs    # Degradation when the AI gateway is unavailable
├── AiInferenceResilienceTests.cs     # Retry / circuit-breaker behaviour
└── Observability/
    └── AlertCoverageTests.cs         # Every Prometheus alert rule has a test behind it
```

The API exposes `public partial class Program;` at the end of `Program.cs`
specifically so `WebApplicationFactory<global::Program>` can boot it.

## Dependencies

| Kind | Reference |
| --- | --- |
| Project | `HalalChain.Platform.Api`, `HalalChain.Application`, `HalalChain.Domain`, `HalalChain.Web` |
| Package | `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `FluentAssertions` 8.10.0 |

Note that this project references the **UI** (`HalalChain.Web`). That is
intentional: `TrustBoundaryTests` asserts behaviour visible to the web client.

## Run

From the repository root:

```bash
dotnet test HalalChain.Platform.Tests -c Release
```

…or as part of the full solution:

```bash
dotnet test HalalChain.Platform.sln -c Release --no-build
```

CI runs the whole solution in the `build-and-test` job of
`.github/workflows/ci.yml`.

## Configuration

Tests run against SQLite via `CustomWebApplicationFactory`, which overrides
connection strings and the AI-gateway base URL so no external service is
required. `AiInferenceResilienceTests` and `SemanticSearchFallbackTests`
substitute the AI gateway.

## Related Components

- [HalalChain.Platform.Api](../HalalChain.Platform.Api/README.md) — the system under test
- [HalalChain.Web](../HalalChain.Web/README.md) — referenced so trust-boundary tests can assert the client's view
- [HalalChain.Architecture.Tests](../HalalChain.Architecture.Tests/README.md) — compile-time layer rules
- [infrastructure/prometheus/rules](../infrastructure/prometheus/rules) — the alerts `Observability/AlertCoverageTests.cs` covers
- [HalalChain.Platform.Http](../HalalChain.Platform.Http/README.md) — the client the trust-boundary tests mirror