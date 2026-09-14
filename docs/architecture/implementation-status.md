# HalalChain Platform — Implementation Status

Tracks the HalalChain Platform Evolution v4.0 migration. Source of truth for phase progress.

**Statuses:** `NOT_STARTED` · `IN_PROGRESS` · `BLOCKED` · `COMPLETE` · `DEFERRED`

## Phases

### Phase 0 — Reconnaissance
| Task | Status | Notes |
|---|---|---|
| Inspect solution, projects, entities, DbContexts, migrations, DI, controllers, MCP, UI, tests, Python services, .autoclaw, blockchain, IPFS, auth, event/outbox | COMPLETE | See `implementation-reconnaissance.md` |
| Produce `implementation-reconnaissance.md` | COMPLETE | This commit |
| Produce `implementation-status.md` (this file) | COMPLETE | This commit |
| Identify high-risk migrations | COMPLETE | See `implementation-reconnaissance.md` §20 |

### Phase 0.5 — Architecture guardrail
| Task | Status | Notes |
|---|---|---|
| Create `HalalChain.Architecture.Tests` with NetArchTest | COMPLETE | Project added to solution; references all .NET projects to load their assemblies into the test host |
| Rule: `Contracts_ShouldNotReference_AnyOtherProject` | COMPLETE | Green against current code |
| Rule: `Mcp_ShouldNotReference_ApiProject` | COMPLETE | Green against current code |
| Rule: `Marketplace_ShouldNotReference_ApiProject` | COMPLETE | Green against current code |
| Rule: `Http_ShouldNotReference_ApiProject` | COMPLETE | Green against current code |
| Rule: `Domain_ShouldNotReference_EFCore_Or_AnyOtherProject` | COMPLETE | Green — Domain is currently empty (just `IAggregateRoot`) |
| Rule: API may not reference EF Core DbContext types | COMPLETE | Enforced via `MoveToDomain` migration; both moved entities are now in Domain. The API still hosts the DbContext (delegated to Infrastructure in Phase 3). |
| Rule: `NonApiProjects_ShouldNotReference_EFCore` | COMPLETE | Green against current code |
| Rule: `Mcp_ShouldNotReference_EFCore` | COMPLETE | Green against current code (was listed under Domain phase, now covered by the above) |
| Rule: Moved entities must not leak back into `Persistence.Entities` | COMPLETE | `MovedEntities_ShouldNotExistIn_ApiPersistenceEntities` covers `ProductEmbedding`, `OutboxMessage`, `SupplierOnChain`, `ProductOnChain`, `CertificateOnChain`, `TraceabilityEventOnChain`, `ChainTxOutbox`. `MigratedTypes_ShouldResideIn_Domain` covers all of them in the correct Domain sub-namespace, plus `IAggregateRoot`. |

### Phase 1 — Domain consolidation
| Bounded context | Status | Notes |
|---|---|---|
| `HalalChain.Domain` project created (BCL-only) | COMPLETE | Contains `IAggregateRoot` (root namespace), `Common.OutboxMessage`, `Catalog.ProductEmbedding` |
| `Common` (`OutboxMessage`) | **COMPLETE** | Moved from `Api/Persistence/Entities/OutboxMessage.cs`. Backed by additive migration `MoveToDomain` (schema preserved; fixes pre-existing missing `CreatedAtUtc` column). |
| `Catalog.ProductEmbedding` | **COMPLETE** | Moved from `Api/Persistence/Entities/ProductEmbedding.cs`. Navigation `Product` removed from entity (FK configured via `HasOne().WithMany().HasForeignKey()` without nav property). Backed by same `MoveToDomain` migration. |
| Catalog (`Product`, `ProductVariant`, `Department`, `Category` (new), `Subcategory`, `ProductType`, `CertificationBody`, `Facility`, `Brand`, `Country`, `Taxonomy*`) | NOT_STARTED | Move from `HalalChain.Platform.Api/Persistence/Entities/` and out of `Contracts.Catalog.Taxonomy`/`ReferenceData`/`ProductVariant`. Disambiguate legacy `Category` vs new `Category`. |
| Vendor | NOT_STARTED | Move `Vendor.cs` |
| Commerce (`Order`, `OrderItem`, `VendorOrder`, `CartItem`) | NOT_STARTED | |
| Certification (`Certificate`, `HalalVerification`, `VerificationEvidence`, `VerificationAudit`) | **COMPLETE** | All four moved to `Domain.Halal`. `CertificateStatus` and `ComplianceStatus` enums are now defined in `Domain.Halal` (numeric values preserved to match the DTO copies in Contracts). API has a `GlobalUsings.cs` that aliases the Contracts enum names to the Domain versions. Backed by additive migration `MoveHalalToDomain` (schema preserved; only seed-data timestamp diffs). |
| Blockchain (`ChainTxOutbox`, `ChainMirror`, `ProductOnChain`, `CertificateOnChain`, `TraceabilityEventOnChain`) | **PARTIAL** | `ChainTxOutbox`, `SupplierOnChain`, `ProductOnChain`, `CertificateOnChain`, `TraceabilityEventOnChain` moved to `Domain.Blockchain`. Backed by additive migration `MoveBlockchainToDomain` (schema preserved; only seed-data timestamp diffs). |
| Assets / Agents / Marketplace / Identity / Governance / Usage | NOT_STARTED | |

### Phase 2 — Application layer (CQRS/MediatR)
| Bounded context | Status | Notes |
|---|---|---|
| Catalog use cases (`CreateProductCommand`, `UpdateProductCommand`, `SearchProductsQuery`, …) | NOT_STARTED | |
| Commerce use cases | NOT_STARTED | |
| Certification use cases | NOT_STARTED | |
| Agent / AI use cases | NOT_STARTED | |
| Validators (FluentValidation) | NOT_STARTED | |
| Authorization policies | NOT_STARTED | |

### Phase 3 — Infrastructure consolidation
| Task | Status | Notes |
|---|---|---|
| Create `HalalChain.Infrastructure` | NOT_STARTED | |
| Move `HalalChainDbContext` + EF configs into Infrastructure | NOT_STARTED | |
| Add `IAiInferenceService` + `AiInferenceClient` (resilience) | NOT_STARTED | |
| Add `IContentStorageService` boundary over Pinata/kubo | NOT_STARTED | |
| Add `IAgentOrchestrationService` over `.autoclaw` | NOT_STARTED | |
| Add `IEventBus` (consolidate) | NOT_STARTED | |
| Add Outbox publisher (idempotency) | NOT_STARTED | |

### Phase 4 — API refactor
| Task | Status | Notes |
|---|---|---|
| API versioning `/api/v1/...` for all public endpoints | NOT_STARTED | Some are un-versioned today |
| Controllers become thin (Mediator) | NOT_STARTED | Start with `CatalogController` |
| `ProblemDetails` everywhere | NOT_STARTED | |
| `idempotency-key` for write endpoints | NOT_STARTED | |
| API key hashing, scopes, rate-limit tiers | NOT_STARTED | |
| Webhook system | NOT_STARTED | |
| Cross-tenant tests | NOT_STARTED | |

### Phase 5 — MCP refactor
| Task | Status | Notes |
|---|---|---|
| Add product-facing tools (`GetProductTool`, `VerifyProductTool`, `TriggerAgentTool`, `GetAgentStatusTool`) | NOT_STARTED | Existing 8 read-only tools preserved |
| MCP tools become thin adapters over Application | NOT_STARTED | |

### Phase 6 — Shared UI
| Task | Status | Notes |
|---|---|---|
| Create `HalalChain.UI.Shared` | NOT_STARTED | |
| Extract shared components / theme / localization / auth helpers | NOT_STARTED | |

### Phase 7 — Three UI applications
| Task | Status | Notes |
|---|---|---|
| `HalalChain.Storefront` (customer-facing) | NOT_STARTED | Today fused in `HalalChain.Web` |
| `HalalChain.Admin` (platform/vendor/compliance) | NOT_STARTED | Today fused in `HalalChain.Web` |
| `HalalChain.Marketplace` already exists | COMPLETE | |
| Cross-app SSO | NOT_STARTED | |
| Unify Blazor `AuthService` with platform JWT | NOT_STARTED | |

### Phase 8 — Cross-cutting
| Task | Status | Notes |
|---|---|---|
| OpenTelemetry, structured logging, health checks | NOT_STARTED | |
| Multi-tenancy model | NOT_STARTED | |
| Webhook delivery + retries | NOT_STARTED | |
| Payment gateway abstraction (`IPaymentGateway`, `IPayoutProvider`) | NOT_STARTED | |
| AI recommendations | NOT_STARTED | |
| API sandbox + SDK generation | NOT_STARTED | |
| DID / Verifiable Credentials abstraction | NOT_STARTED | Staged design only |
| Bulk import/export | NOT_STARTED | |

## High-level risks
See `implementation-reconnaissance.md` §20.
