# HalalChain.Application

Use cases, orchestration, and the storage/agent ports that the API and UIs
implement.

## Purpose

This project is the application's behaviour layer. It owns the MediatR
commands, queries, and handlers, the FluentValidation validators, the AutoMapper
profiles, the pipeline behaviours, the repository and storage *interfaces*, and
the deterministic compliance decision engine that routes work between the
deterministic path and the agentic path.

It is not a host. There is no `Program.cs` and no `OutputType`; the
composition root is `HalalChain.Platform.Api/Program.cs`.

## Responsibilities

- Define commands and queries for the catalog, vendors, and halal verification.
- Handle those commands and queries, applying domain rules.
- Validate input before handlers run (`ValidationBehavior`).
- Authorise handlers before they run (`AuthorizationBehavior`).
- Declare the outbound ports the infrastructure must implement: `IRepository<T>`,
  `IProductRepository`, `IVendorRepository`, `ICertificateRepository`,
  `IVerdictBindingRepository`, `ITenantContext`, `IClock`, `ICurrentUser`.
- Declare the storage ports (`IBlobStore`, `IEvidenceStore`,
  `IEvidenceMetadataStore`, `IAccessLog`, `IAgentTraceStore`, `IContentHasher`,
  `ISignedUrlIssuer`) — implemented by `HalalChain.Storage`.
- Declare the agentic ports (`IAgentWorkflow`, `IWorkflowBudget` /
  `IBudgetRun`).
- Own `DeterministicComplianceDecisionEngine`, which picks `AUTONOMOUS`,
  `ASSISTED`, or `ESCALATED` and emits the audit trail.
- Wrap the `tawheed` HTTP API in `ITawheedClient` / `TawheedClient`.

## Structure

```
HalalChain.Application/
├── GlobalUsings.cs
├── Agentic/
│   ├── Abstractions/          # IAgentWorkflow, IWorkflowBudget, IBudgetRun
│   ├── Handlers/              # RunSupplierOnboardingHandler
│   └── Models/                # EvidenceProposal, EvidenceKind, SourceRef,
│                              #   WorkflowBudget, Escalation
├── Catalog/
│   ├── Commands/  Queries/  Validators/  Handlers/
├── Common/
│   ├── Abstractions/          # IClock, ICurrentUser
│   ├── Behaviors/             # ValidationBehavior, AuthorizationBehavior,
│   │                          #   CachingBehavior, ICacheableQuery
│   ├── Exceptions/            # BadRequest/Forbidden/NotFound exceptions
│   ├── Interfaces/            # IRepository<T>, IProductRepository
│   └── Mappings/              # CatalogProfile, ProductResponseProfile,
│                              #   MappingConfiguration
├── Decision/
│   └── Models/                # IComplianceDecisionEngine, IEvidenceCollector,
│                              #   DeterministicComplianceDecisionEngine
├── Halal/
│   ├── Commands/              # BindVerdictCommand, SweepExpiringCertificatesCommand
│   ├── Handlers/  Interfaces/ # ICertificateRepository, IVerdictBindingRepository
│   └── StateMachine/          # ProductStatusMachine
├── Policies/                  # CatalogPolicies.Register, VendorPolicies.Register
├── Storage/                   # Blob + evidence ports and their value types
├── Tawheed/
│   └── Models/                # ITawheedClient, TawheedClient, request/response DTOs
├── Tenancy/                   # ITenantContext
└── Vendors/                   # Commands, Handlers, Queries, Validators, Interfaces
```

## Architecture / Flow

```text
HTTP request
   ↓
HalalChain.Platform.Api (controller)
   ↓
MediatR pipeline: AuthorizationBehavior → ValidationBehavior → CachingBehavior
   ↓
Handler (this project)
   ↓
Outbound port  ──▶ IProductRepository   → HalalChain.Platform.Api EF Core
                ├─▶ ITawheedClient      → tawheed (HTTP)
                ├─▶ IAgentWorkflow      → agents service (HTTP)
                └─▶ IBlobStore/IEvidenceStore → HalalChain.Storage
   ↓
Response DTO
```

## Dependencies

| Kind | Reference |
| --- | --- |
| Project | `HalalChain.Domain`, `HalalChain.Platform.Contracts` |
| Package | `MediatR` 14.2.0, `FluentValidation` 12.1.1, `FluentValidation.DependencyInjectionExtensions` 12.1.1, `AutoMapper` 16.2.0, `Microsoft.EntityFrameworkCore` 10.0.11 |
| Framework | `Microsoft.AspNetCore.App` |

Deliberately *not* referenced: `HalalChain.Platform.Api`, `HalalChain.Platform.Http`,
`HalalChain.Marketplace`, `HalalChain.Web`, `HalalChain.Mcp`. Enforced by
`HalalChain.Architecture.Tests`.

## Interfaces

### Outbound ports (implement these in the host)

| Interface | Implemented by |
| --- | --- |
| `IProductRepository` | `EfProductRepository` (API) |
| `IVendorRepository` | `EfVendorRepository` (API) |
| `ICertificateRepository` | `EfCertificateRepository` (API) |
| `IVerdictBindingRepository` | `InMemoryVerdictBindingRepository` (API) |
| `ITawheedClient` | `TawheedHttpClient` (API) |
| `IBlobStore`, `IEvidenceStore`, `IEvidenceMetadataStore`, `IAccessLog`, `IContentHasher`, `ISignedUrlIssuer`, `IAgentTraceStore` | `HalalChain.Storage` (via `AddFileSystemStorage`) |
| `IAgentWorkflow` | `AgentWorkflowClient` (in `HalalChain.Agents`) |

### Entry points consumed by the API

- `CreateProductHandler` / `CreateProductRequestValidator` — the assemblies the
  API scans for MediatR and FluentValidation registration.
- `CatalogPolicies.Register(options)` and `VendorPolicies.Register(options)` —
  called from `HalalChain.Platform.Api/Program.cs`.
- `ProductStatusMachine` — registered as a singleton by the API.

### `IBlobStore` has no `Delete`

The port exposes only `PutAsync`, `OpenReadAsync`, and `ExistsAsync`. Deleting
is not a member, which is a compile-time enforcement of append-only evidence.
See [ADR-002](../docs/adr/002-storage-port-adapter-and-hashers.md).

## Usage

MediatR handlers are registered by scanning this assembly:

```csharp
cfg.RegisterServicesFromAssembly(typeof(CreateProductHandler).Assembly);
AddValidatorsFromAssembly(typeof(CreateProductRequestValidator).Assembly);
```

`DeterministicComplianceDecisionEngine` returns one of:

| Outcome | Meaning |
| --- | --- |
| `AUTONOMOUS` | Confidence cleared the threshold; reason `AUTO_CONFIDENCE_OK` |
| `ASSISTED` | A human must assist; reason `HUMAN_ASSIST_REQUIRED` |
| `ESCALATED` | Escalated for review; reason `ESCALATE_FOR_REVIEW` |

## Configuration

No `appsettings.json` of its own. Configuration is supplied by the host through
the port interfaces and the `IClock` / `ICurrentUser` registrations.

## Integration

- Consumed by `HalalChain.Platform.Api`, `HalalChain.Marketplace`, and
  `HalalChain.Automation`.
- Its `Tawheed` models are shaped against the `tawheed` service's HTTP contract.
- Its `Storage` ports are satisfied by `HalalChain.Storage`, which in turn is
  referenced by `HalalChain.Agents`.

## Development

```bash
dotnet build HalalChain.Application/HalalChain.Application.csproj
```

Keep handlers thin: they orchestrate ports and map to DTOs. Anything requiring a
new external dependency belongs in the host, behind a new port declared here.

## Testing

Covered by `HalalChain.Platform.Tests` (behaviour, via `WebApplicationFactory`)
and `HalalChain.Architecture.Tests` (structure):

- `Application_ShouldNotReference_Api_Or_AnyUiProject`
- `Application_ShouldNotReference_ConcreteHttpClientTypes`
- `Application_ShouldNotAssign_HalalVerdicts` — no concrete type assignable to
  `HalalChain.Domain.Halal.ComplianceStatus` may be assigned anywhere in this
  assembly. This is the compile-time expression of "AI never assigns a verdict".

There is no `HalalChain.Application.Tests` project.

## Deployment

Not deployable. Compiled into the API, Marketplace, and Automation assemblies.

## Related Components

- [HalalChain.Domain](../HalalChain.Domain/README.md) — entities and enums used here
- [HalalChain.Platform.Api](../HalalChain.Platform.Api/README.md) — implements most ports
- [HalalChain.Storage](../HalalChain.Storage/README.md) — implements the storage ports
- [HalalChain.Agents](../HalalChain.Agents/README.md) — implements `IAgentWorkflow`
- [.halalchain/tawheed](../../.halalchain/tawheed/README.md) — the service behind `ITawheedClient`

## Notes / Limitations

- `HalalChain.Storage`'s DI extensions are **not** currently called by
  `HalalChain.Platform.Api`, `HalalChain.Marketplace`, or `HalalChain.Web`.
  The ports compile but have no runtime registration in production code.
- `IProductRepository`/`IVendorRepository` implementations exist only in the API;
  Marketplace references this project but registers its own repositories.