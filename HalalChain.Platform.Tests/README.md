# HalalChain.Platform.Tests

xUnit test suite for the core REST API, persistence layer, and per-vendor data
isolation.

## Layout

```
HalalChain.Platform.Tests/
├── ApiIntegrationTests.cs        # End-to-end API tests
├── CustomWebApplicationFactory.cs # WebApplicationFactory bootstrap
├── PersistenceTests.cs            # EF Core / repository tests
└── VendorIsolationTests.cs       # Cross-vtenant data isolation tests
```

## Project references

- `HalalChain.Platform.Api`

## Run

From the repository root:

```bash
dotnet test HalalChain.Platform.Tests -c Release
```

…or as part of the full solution:

```bash
dotnet test HalalChain.Platform.sln -c Release --no-build
```
