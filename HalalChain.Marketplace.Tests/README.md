# HalalChain.Marketplace.Tests

xUnit tests for the vendor marketplace application in `HalalChain.Marketplace/`.

## Purpose

These tests cover the parts of the marketplace that carry the most risk: tenant
isolation (one vendor must never see another's data), dashboard authorisation,
and the caching and notification services that sit in front of them.

## Layout

```
HalalChain.Marketplace.Tests/
├── Integration/
│   └── Dashboard/
│       ├── SecurityTests.cs           # Dashboard authorisation
│       └── TenantIsolationTests.cs    # Cross-vendor isolation
├── Repositories/
│   ├── ProductRepositoryTests.cs
│   └── Dashboard/DashboardConfigRepositoryTests.cs
└── Services/
    ├── CachingServiceTests.cs
    ├── NotificationServiceTests.cs
    └── Dashboard/
        └── DashboardConfigServiceTests.cs   # Includes CacheStrategies
```

## Dependencies

| Kind | Reference |
| --- | --- |
| Project | `HalalChain.Marketplace`, `HalalChain.Platform.Contracts` |
| Package | `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `Moq` 4.20.72 |

## Run

From the repository root:

```bash
dotnet test HalalChain.Marketplace.Tests -c Release
```

## Related Components

- [HalalChain.Marketplace](../HalalChain.Marketplace/README.md) — the system under test
- [HalalChain.Platform.Http](../HalalChain.Platform.Http/README.md) — how the marketplace reaches the platform API
- [HalalChain.Architecture.Tests](../HalalChain.Architecture.Tests/README.md) — `Marketplace_ShouldNotReference_ApiProject`
- [HalalChain.Marketplace/TESTING_GUIDE.md](../HalalChain.Marketplace/TESTING_GUIDE.md) — the marketplace's own testing guidance

## Notes / Limitations

- The `Integration/Dashboard` tests are integration-style but run in-process
  against the marketplace's own data layer; they do not require `platform-api`
  to be running.
- The marketplace's `README.md` is a product and platform vision document, not
  an implementation guide. Its own guides (`COMPONENT_MIGRATION_GUIDE.md`,
  `COMPONENT_TEST_CHECKLIST.md`, `TESTING_GUIDE.md`, `VENDOR_PORTAL_GUIDE.md`,
  `VERIFICATION_FEATURE.md`, `REALTIME_FEATURES.md`) are the operationally
  accurate references.