# HalalChain.Architecture.Tests

Architecture guardrails for the HalalChain platform using **NetArchTest.Rules**.

This project is the architectural safety net for the Platform Evolution v4.0 migration. It enforces dependency rules as xUnit facts so that any violation fails the build.

## Why a separate project

NetArchTest inspects the compiled assembly graph. The architecture test project intentionally references the assemblies it needs to inspect so they are loaded into the test host's `AssemblyLoadContext`.

## Current rules (Phase 0.5)

These rules are enforced today and are **green against the current code**:

| Rule | Reason |
|---|---|
| `Contracts_ShouldNotReference_AnyOtherProject` | Contracts is a DTO boundary. It must not be coupled to UI/API/Http. |
| `Mcp_ShouldNotReference_ApiProject` | MCP tools are thin adapters; they call Application use cases, not API controllers. |
| `Marketplace_ShouldNotReference_ApiProject` | Marketplace consumes the platform via `HalalChain.Platform.Http`. |
| `Http_ShouldNotReference_ApiProject` | The HTTP client is independent of the API project. |

## Future rules (tracked, not yet enforced)

As Domain, Application, Infrastructure, UI.Shared, Storefront, and Admin projects are introduced, additional rules will be added in this project. They are listed in `DependencyRulesTests.KnownUntestable` for visibility.

## Running

```bash
dotnet test HalalChain.Architecture.Tests/HalalChain.Architecture.Tests.csproj
```

The architecture tests must pass before and after every migration phase. If a new rule fails, it means a layer boundary has been crossed; do not weaken the test to accommodate it — fix the code or split the project.
