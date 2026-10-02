# HalalChain.Architecture.Tests

Architecture guardrails for the HalalChain platform using **NetArchTest.Rules**.

This project is the architectural safety net for the .NET solution. It enforces
dependency and boundary rules as xUnit facts so that a violation fails the
build rather than being discovered in review.

## Why a separate project

NetArchTest inspects the compiled assembly graph. This project therefore
references the assemblies it needs to inspect so they load into the test host's
`AssemblyLoadContext`.

It references nine projects: `HalalChain.Domain`, `HalalChain.Application`,
`HalalChain.Platform.Contracts`, `HalalChain.Platform.Http`,
`HalalChain.Platform.Api`, `HalalChain.Mcp`, `HalalChain.Marketplace`,
`HalalChain.Web`, `HalalChain.Storage`, and `HalalChain.Agents`.

## Layout

```
HalalChain.Architecture.Tests/
├── DependencyRulesTests.cs            # Assembly reference + boundary rules
├── EvaluationArchitectureTests.cs    # Agent evaluation DAG rules
├── McpVerdictAuthorityTests.cs        # Scans the tree for verdict-authority identifiers
├── Rules/
│   └── AutoclawStructureTests.cs      # .autoclaw/ tree invariants
└── Rules/*.cs helpers
```

## Current rules (enforced today)

### `DependencyRulesTests.cs`

| Rule | Why |
| --- | --- |
| `GuardedAssembly_ShouldBe_Loadable` | Every guarded assembly must resolve, so a rule below cannot pass vacuously |
| `Application_ShouldNotReference_Api_Or_AnyUiProject` | Application is the behaviour layer; the host composes it |
| `Application_ShouldNotReference_ConcreteHttpClientTypes` | `HttpClient` must be behind a port (scoped to `HalalChain.Application.Catalog.Handlers`) |
| `Application_ShouldNotAssign_HalalVerdicts` | No concrete type assignable to `ComplianceStatus` may be assigned in Application — the compile-time form of "AI never assigns a verdict" |
| `Contracts_ShouldNotReference_AnyOtherProject` | Contracts is a DTO boundary, coupled to nothing |
| `Mcp_ShouldNotReference_ApiProject` | MCP tools are thin adapters, not API controllers |
| `Marketplace_ShouldNotReference_ApiProject` | Marketplace consumes the platform through `HalalChain.Platform.Http` |
| `Http_ShouldNotReference_ApiProject` | The typed HTTP client is independent of the API |
| `Domain_ShouldNotReference_EFCore_Or_AnyOtherProject` | Domain stays dependency-free |
| `NonApiProjects_ShouldNotReference_EFCore` | EF Core belongs to the API only (asserts Web, Mcp, Http) |
| `MovedEntities_ShouldNotExistIn_ApiPersistenceEntities` | Entities moved to Domain must not be duplicated under `HalalChain.Platform.Api.Persistence.Entities` |
| `MigratedTypes_ShouldResideIn_Domain` | Each migrated type lives in its expected `HalalChain.Domain.*` namespace |
| `Domain_ShouldNotReference_Storage` | The dependency arrow points Domain ← Storage only |
| `Storage_ShouldNotReference_ApiOrUiOrMcp` | Storage is host-agnostic |
| `IBlobStore_DeclaresNoDeleteMember_ArchTest` | Append-only evidence, enforced by the absence of a member |
| `Storage_AssemblyDeclaresNoMerkleTreeType` | Merge decision M1 — Keccak-256 Merkle internals belong to the blockchain module |

### `EvaluationArchitectureTests.cs`

| Rule | Why |
| --- | --- |
| `EvalResult_DeclaresNoVerdictField` | Evaluation output cannot carry a compliance verdict |
| `AgentsAssembly_ShouldNotReference_Ragas` | RAGAS is excluded (SSRF advisory PYSEC-2026-3046); metrics are reimplemented |
| `EvalExtra_NotInProductionDockerfile` | Eval dependencies stay out of the production image |
| `EveryWorkflowNodeType_HasEvalScorer` | Every DAG node type must have a scorer |
| `EvalReadsBlobOnlyThrough_IBlobStore` | The harness reads traces only via the storage port |

### `McpVerdictAuthorityTests.cs`

| Rule | Why |
| --- | --- |
| `Mcp_Sources_Declare_No_Forbidden_Verdict_Authority_Identifier` | No MCP source may name a verdict-authority identifier |
| `Guard_Exemptions_Are_Discoverable_By_Marker_And_No_Other_File_Uses_Them` | Exemptions are explicit and traceable |
| `Runtime_Pattern_List_Covers_The_Baseline` | The runtime scanner covers the static baseline |
| `Guard_Marker_Constant_Matches_The_Runtime_Scan` | The marker constant cannot drift from the scan |

### `Rules/AutoclawStructureTests.cs`

These validate the `.autoclaw/` policy tree: scaffolding present, `.autoclaw/tawheed/`
sealed, no forbidden verdict patterns outside it, every skill carries a manifest,
published skills emit no verdicts, staging skills unreferenced by agents, the
allowlist matches exposed tools, denied tools are not exposed, tenant isolation
keys declared, every agent tenant-scoped and declaring what it cannot do, BYOK
holds references not secrets, redaction config consumes the secret pattern list,
certification state transitions reference valid states and are bipartite subsets,
and anchors declare no verdict types.

## Tracked, not enforced

`DependencyRulesTests.KnownUntestable` records rules that cannot be asserted yet:

- Once `HalalChain.Infrastructure` exists: Infrastructure may reference EF Core,
  Nethereum, IPFS SDKs, and HttpClients — but must not be referenced by a UI.
- Once `HalalChain.UI.Shared` exists: UI projects may reference UI.Shared,
  Contracts, and Http only.
- Once `HalalChain.Storefront` / `HalalChain.Admin` are split from Web: Admin must
  not be referenced by Storefront.

`HalalChain.Infrastructure` does not exist in the current solution.

## Running

```bash
dotnet test HalalChain.Architecture.Tests/HalalChain.Architecture.Tests.csproj
```

Or as part of the whole solution:

```bash
dotnet test HalalChain.Platform.sln -c Release
```

These tests run in CI in the `build-and-test` job of `.github/workflows/ci.yml`.

## Conventions

If a new rule fails, a layer boundary has been crossed. Do not weaken the test
to accommodate the violation — fix the code or split the project.

## Related Components

- [HalalChain.Domain](../HalalChain.Domain/README.md) — the zero-dependency project the rules protect
- [HalalChain.Application](../HalalChain.Application/README.md)
- [HalalChain.Storage](../HalalChain.Storage/README.md)
- [HalalChain.Mcp](../HalalChain.Mcp/README.md)
- [HalalChain.Agents](../HalalChain.Agents/README.md)
- [HalalChain.Mcp.Tests](../HalalChain.Mcp.Tests/README.md)
- [Platform guard rules in the root README](../README.md#12-architecture-guard-rules)

## Notes / Limitations

- `AutoclawStructureTests` read `.autoclaw/`, which is outside this project's
  directory. They are marked by location, not by assembly.
- NetArchTest sees compiled metadata only; a rule cannot detect a violation that
  a project expresses without a reference (e.g. reflection over an assembly
  name).