# HalalChain Platform — Implementation Reconnaissance

**Date:** 2026-08-27
**Source of truth for the current state. The architecture document is the source of truth for the target state.**

This file is the **starting point** for the HalalChain Platform Evolution v4.0 migration. Per the master implementation prompt, no large refactoring is performed until this analysis is complete and an `implementation-status.md` is in place.

Build baseline (per `docs/architecture/workspace-health.md:128-135`): **8/8 projects build, 55/55 tests pass** on .NET 10 (10.0.200).

---

## 1. Current solution structure

`HalalChain.Platform.sln:1-132` contains **8 .NET projects**:

| Project | csproj | SDK | Maps to target arch | Notes |
|---|---|---|---|---|
| `HalalChain.Platform.Contracts` | `HalalChain.Platform.Contracts.csproj:1-2` | net10.0 lib | **Contracts** | DTOs + enums + **some EF entities** (taxonomy, reference data) |
| `HalalChain.Platform.Api` | `HalalChain.Platform.Api.csproj:1-25` | net10.0 web | **Api** (+ currently hosts Domain, Application, Infrastructure concerns) | Single project holding entities, DbContext, application services, controllers, hosted services |
| `HalalChain.Platform.Http` | `HalalChain.Platform.Http.csproj:1-18` | net10.0 web | **Http** | Typed client (FrameworkReference AspNetCore.App) |
| `HalalChain.Mcp` | `HalalChain.Mcp.csproj:1-25` | net10.0 exe | **Mcp** | JSON-RPC over stdio, 8 read-only tools |
| `HalalChain.Marketplace` | `HalalChain.Marketplace.csproj:1-10` | net10.0 web | **Marketplace** | ASP.NET Core MVC |
| `HalalChain.Web` | `HalalChain.Web.csproj:1-18` | net10.0 web | **Storefront + Admin** (single project) | Blazor Server + Radzen |
| `HalalChain.Platform.Tests` | `HalalChain.Platform.Tests.csproj:1-31` | net10.0 test | **Tests** | Integration + persistence, InMemory EF |
| `HalalChain.Mcp.Tests` | `HalalChain.Mcp.Tests.csproj:1-24` | net10.0 test | **Tests** | MCP protocol/registry/execution |

**Missing target projects (to be created):**

- `HalalChain.Domain`
- `HalalChain.Application`
- `HalalChain.Infrastructure`
- `HalalChain.UI.Shared`
- `HalalChain.Storefront` (currently fused with Admin in `HalalChain.Web`)
- `HalalChain.Admin` (currently fused with Storefront in `HalalChain.Web`)
- `HalalChain.Domain.Tests`, `HalalChain.Application.Tests`, `HalalChain.Infrastructure.Tests`
- `HalalChain.Architecture.Tests` (NetArchTest or similar)
- `HalalChain.Integration.Tests`

Helper csproj (not in solution, not deployable): `Radzen.Blazor.Api.Generator.csproj` — invoked by `HalalChain.Platform.Api` only with `-p:GenerateApiPages=true`.

---

## 2. Project dependencies (current)

`HalalChain.Platform.Api` is the only project that touches EF Core, Nethereum, Radzen, JWT, and ASP.NET Core. It also transitively owns all entities and the DbContext. **There is no separate Domain/Application/Infrastructure project.** This is the primary migration target.

The current Api project is the composition root and is the largest project. The migration will *split* it into three layers without changing the composition root's responsibilities.

---

## 3. Domain / entity locations

All EF entities live in `HalalChain.Platform.Api/Persistence/Entities/`:

- `Product.cs:1-58`
- `Vendor.cs:1-13`
- `Category.cs:1-12` (legacy, self-referential — deprecated by the new 4-level taxonomy)
- `Certificate.cs:1-23`
- `HalalVerification.cs:1-23`
- `VerificationEvidence.cs:1-17`
- `VerificationAudit.cs:1-16`
- `Order.cs:1-17`
- `VendorOrder.cs:1-20`
- `OrderItem.cs:1-16`
- `CartItem.cs:1-15`
- `ProductEmbedding.cs:1-19`
- `OutboxMessage.cs:1-18`
- `ChainTxOutbox.cs:1-19`
- `ChainMirror.cs:1-69` (`SupplierOnChain`, `ProductOnChain`, `CertificateOnChain`, `TraceabilityEventOnChain`)

**Cross-layer violation (DUPLICATION):** `HalalChain.Platform.Contracts.Catalog.Taxonomy.cs:1-79` declares `Department`, `Category`, `Subcategory`, `ProductType`, **and these are registered as DbSets in `HalalChainDbContext.cs:31-43`**. Same for `ReferenceData.cs:1-66` (`CertificationBody`, `Facility`, `Brand`, `Country`) and `ProductVariant.cs:1-34`. These types currently live in the Contracts project but are EF entities. The migration must move them into `HalalChain.Domain` (or `HalalChain.Infrastructure.Persistence.Entities` for the time being) and keep Contracts as DTO-only.

---

## 4. DbContexts

- **Single DbContext:** `HalalChain.Platform.Api.Persistence.HalalChainDbContext` (`HalalChainDbContext.cs:11-362`).
- **No `IEntityTypeConfiguration<T>` classes** — all configuration is inline in `OnModelCreating` (`HalalChainDbContext.cs:52-361`).
- **Single migration:** `Migrations/20260826191514_InitialSchema.cs` (+ `.Designer.cs` + model snapshot). Migrated at startup (`Program.cs:248-252`).
- **Provider-aware:** checks `IsNpgsql()` (`HalalChainDbContext.cs:60`) — chooses `jsonb`/`text[]`/`real[]` on Postgres vs JSON text on SQLite. Same model runs on both.
- **Production:** Postgres 17.4-alpine (`docker-compose.yml:183-191`).
- **Local dev / tests:** SQLite (`Program.cs:194-204`) and `EFCore.InMemory` (`CustomWebApplicationFactory.cs:70-73`).

---

## 5. Persistence strategy

- **Migration-on-startup.** Single migration today.
- EF Core code-first. No Dapper, no raw SQL except semantic-search cosine distance (`SearchController.cs:46`, `:96`).
- No query-side persistence (no read model, no CQRS read store).
- Outbox for **business events** (`OutboxMessage` + `OutboxBackgroundService`) **and** a separate outbox for **chain transactions** (`ChainTxOutbox` + `OutboxDispatcherService`).
- Redis cache via `AddStackExchangeRedisCache` (falls back to in-memory if no Redis configured) (`Program.cs:175-180`).

---

## 6. API architecture

- Modular monolith, organised by business capability under `HalalChain.Platform.Api/Modules/`: `AI`, `Blockchain`, `Catalog`, `Commerce`, `Events`, `Halal`, `Indexer`, `Ipfs`, `Verification`, `Vendors`.
- ASP.NET Core Controllers (no Minimal APIs). API versioning configured at `Program.cs:41-52` with `[ApiVersion("1.0")]` on most controllers.
- Three controllers are **un-versioned**: `AuthController` (`api/auth/*`), `CustomersController` (`api/commerce/customers`), `ChatController` (`api/ai/chat`).
- Per-endpoint summary in `reconnaissance-raw.md` (saved to tool output). The largest controllers — `HalalController`, `CatalogController`, `ProductFilterController`, `CommerceController` — contain **business logic inline** (halal-status derivation, order assembly, ownership checks, JSONB-aware filtering, idempotent checkout). This is the second-largest refactor target.
- `Program.cs:1-290` wires DI for: DataProtection, OpenAPI, ProblemDetails, HealthChecks, RateLimiter, CORS, JWT, AI client, Tawheed client, Event bus + outbox, Redis, EF Core (Npgsql or Sqlite), Blockchain (gated), IPFS (gated), Migrations, Correlation-ID middleware, Global exception handler.

---

## 7. MCP architecture

- `HalalChain.Mcp/Program.cs:1-230` — JSON-RPC 2.0 over stdio. Per-request DI scope, 30s per-tool timeout.
- `ServiceCollectionExtensions.cs:1-64` registers `IPlatformDataService`, `IHealthCheckService`, `IToolRegistry`, and 8 tools.
- 8 read-only tools (all are code/system inspectors — see `reconnaissance-raw.md` Section 7). **None of them call into the platform API or the Application layer.** They are not tools for end users, they are developer/agent tools for inspecting the platform itself.
- `Services/PlatformDataService.cs:1-443` scans the file system with regex; it does not query any database.
- `HealthTool.cs:1-50` is the only tool that reaches the network.

**Implication for the migration:** the existing MCP is a developer/introspection surface, not a product surface. The target architecture describes a "VerifyProductTool / TriggerAgentTool / GetAgentStatusTool" set, which does **not** exist today. Phase 7 of the migration must add these product-facing MCP tools.

---

## 8. UI architecture

- **Marketplace** (`HalalChain.Marketplace`): ASP.NET Core web app using Razor Pages + Blazor Server + SignalR. It consumes the platform via `HalalChain.Platform.Http` (`IPlatformApiClient`) and exposes a vendor workflow surface with real-time UI updates.
- **Storefront + Admin** (`HalalChain.Web`, single Blazor Server project): Radzen components, SignalR hubs (`/hubs/chat`, `/hubs/notifications`), 5 cultures, 40+ services. Admin and Vendor pages live in the same project, gated by `AdminLayout.razor` and `VendorLayout.razor`. **There is no separate Admin or Storefront project.** Splitting them is Phase 8.
- **No `UI.Shared` project.** Shared components live in `HalalChain.Web/Components/Shared/`, `Components/Data/`, etc.
- **No `HalalChain.Admin` project.**
- **Auth divergence (important):** `HalalChain.Web/Services/AuthService.cs:25-30` mints a **fake** base64 token (`base64(email:ticks)`) and never calls the platform's real `/api/auth/login`. So the Blazor app is effectively a separate identity world from the API and the Marketplace. This is one of the architectural violations to fix.

---

## 9. Authentication

- `AuthController.cs:13-64` issues JWTs directly (HS256). **No user table, no password storage.** `User.Identity.Name` is a `Guid.NewGuid()` for every issued token. Roles are taken from the request body (`role`) for register and hard-coded to `marketplace-user` for login.
- Roles defined (`AuthConstants.cs:1-10`): `admin`, `vendor`, `marketplace-user`, `verification-officer`.
- `Program.cs:105-136` enforces `Jwt:Key >= 32 chars`; non-dev rejects the example value.
- `BlazorTokenAccessor` reads the base64 token; real JWT path is unused from the Blazor host.
- Marketplace uses cookie auth (`HalalChain.Marketplace/Program.cs:1-63`).
- Test auth scheme (`CustomWebApplicationFactory.cs`) reads `X-Test-Name` (subject Guid) and `X-Test-Roles` headers.

---

## 10. Authorization

- **No custom authorization policies**, no `IAuthorizationHandler` implementations.
- Authorization is by `[Authorize(Roles = "...")]` only.
- "Multi-tenancy" is hand-rolled: `User.Identity.Name` is parsed as a Guid and compared against `Product.VendorId` / `Order.CustomerId` per controller (`HalalController.cs:34`, `CatalogController.cs:123`, `CommerceController.cs:21`). **There is no tenant model.**
- `VendorIsolationTests.cs:1-116` is the only automated check of cross-vendor access (returns 403).

---

## 11. Event / outbox architecture

- `IEventBus` + `InProcessEventBus` (`Modules/Events/IEventBus.cs:1-67`) — dual-writes to outbox + tries in-process dispatch.
- Domain events declared in `IEventBus.cs:61-67`: `ProductCreatedEvent`, `ProductUpdatedEvent`, `CertificateSubmittedEvent`, `HalalVerificationRequestedEvent`, `HalalVerificationCompletedEvent`, `OrderPlacedEvent`, `VendorApprovedEvent`.
- `OutboxBackgroundService.cs:1-143` — polls every 5s, exponential backoff to 60s, max 5 retries.
- `EventHandlerRegistry.cs:1-74` — hard-coded event→handler table; **the table is empty: no `IEventHandler<T>` implementations are registered.** Events currently go through the outbox but have no in-process consumer.
- Second outbox for blockchain: `ChainTxOutbox` + `OutboxDispatcherService` (5s poll, batch 25).
- No message bus (RabbitMQ, Kafka, etc.) — outbox + in-process only.

---

## 12. Blockchain architecture

- Foundry project: `HalalChain.Platform.Contracts/contracts/` (5 contracts: `HalalAccessControl`, `SupplierRegistry`, `HalalProductRegistry`, `HalalCertificationRegistry`, `TraceabilityEventLog`).
- C# integration: `HalalChain.Platform.Api/Modules/Blockchain/`. Nethereum 6.1.0.
- `SmartContractService.cs:1-464` — ABI fragments from `Blockchain:Abi:*` config; structs hard-coded.
- `TransactionQueue.cs:1-178` — outbox-based, fingerprint idempotency.
- `OutboxDispatcherService.cs:1-56` — durable dispatch.
- `GasPriceOracle.cs` — gas price reads.
- `ConfigurationWalletProvider` + `IWalletProvider` — wallet abstraction.
- `EventIndexerHostedService.cs:1-141` (12s poll, 64-block confirmations) + `EventDispatcher.cs:1-273` (topic match by keccak256 of event signature).
- Network: Polygon Amoy testnet / mainnet targeted.

---

## 13. IPFS architecture

- `Modules/Ipfs/`: `IStorageService`, `PinataStorageService.cs:1-142`, `LocalKuboStorageService.cs`, `ContentValidator.cs:1-97`, `StorageEncryption.cs`.
- **No controller.** HTTP uploads from the Blazor app are made via a named HttpClient `PlatformApiPfs` in `HalalChain.Web/Program.cs:43-47` (bypasses the platform API).
- Provider switch: `IPFS:Provider = "kubo-local" | "pinata"`.
- Activation gated on config presence (`Program.cs:226-240`).

---

## 14. AI architecture

- **`.halalchain/ai-inference/`** (FastAPI): embeddings, summarize, classify, rerank, ingredient-parse, certificate-extract, RAG, LLM generate. API-key authenticated.
- **`.halalchain/tawheed/`** (FastAPI): `agents/` (5 agents — certificate, document, ingredient, supplier, orchestrator) + `policy/engine.py` (deterministic Policy Engine). AI agents collect evidence; the deterministic engine decides compliance. This rule is enforced by `Modelfile` and the API design.
- **`.halalchain/_shared/`**: provider selector + LRU/Redis cache.
- .NET integration: `IAiInferenceProvider` (`HalalChain.Platform.Api/AI/IAiInferenceProvider.cs:1-14`) with `TransformersJsInferenceProvider` default. `ITawheedClient` + `TawheedHttpClient` (`Modules/Halal/TawheedHttpClient.cs:1-118`).
- 7 domain events are declared (`ProductCreatedEvent` etc.) but the in-process handler map is empty — the AI side-effects of these events are not actually wired up.

---

## 15. Agent architecture

- **`.autoclaw/`** holds the existing agent system: 10 agent YAMLs, 4 orchestrators, 11 skill YAMLs, knowledge graphs (`kg/*.yaml`), memory (SQLite), vector store, audit logs.
- Agents defined: `halal-assistant`, `policy-engine`, `blockchain-oracle`, `ingredient-analyzer`, `certificate-validator`, `supply-chain-tracker`, `verification-verifier`, `compliance-reporter`, `alert-monitor`, `smart-contract-agent`.
- **No `IAgentOrchestrationService` exists in .NET today.** The `.autoclaw` system is decoupled from the .NET API — there is no HTTP client wrapper, no trigger endpoint on the API, no `Trigger agent` use case. The migration must add this bridge.

---

## 16. Tests

- `HalalChain.Platform.Tests/` (39 passing): persistence (7+), API integration (≥22), vendor isolation (5).
- `HalalChain.Mcp.Tests/` (16 passing): protocol, tool registry, tool execution, configuration.
- **No architecture tests** (no `NetArchTest`/ArchUnit-style enforcement).
- **No tests** for: marketplace MVC controllers, outbox handler dispatch (the empty registry is not asserted), AuthController JWT issuance, blockchain code paths (no Anvil), Python services, performance/load.

---

## 17. Deployment

- `docker-compose.yml:1-251` — platform-api (5001), halalchain/Blazor (5200), marketplace (5201), ai-inference (7071), tawheed (8000), postgres, redis, neo4j, qdrant.
- `infrastructure/docker-compose.dev.yml` — IPFS + Anvil (Polygon fork) overlay with `--profile chain`.
- Docker base images are pinned to specific tags in compose.
- `.env.example` documents env vars; `.env` is gitignored (but a non-empty `.env` is checked in to this workspace — review and purge any real secrets before any commit).
- `service-manifest.yaml` lists services.
- `Dockerfile` at repo root for the API.
- CI: `.github/workflows/` (to be inspected during the CI phase).
- `Modelfile` at repo root (Ollama/OpenClaw system prompt for `halalchain-assistant`).

---

## 18. Duplicate concepts (consolidation list)

| Concept | Locations | Decision |
|---|---|---|
| `Department` / `Category` / `Subcategory` / `ProductType` | `HalalChain.Platform.Contracts/Catalog/Taxonomy.cs` (as EF entity) + `HalalChainDbContext` | Move to `HalalChain.Domain.Catalog` |
| `CertificationBody` / `Facility` / `Brand` / `Country` | `HalalChain.Platform.Contracts/Catalog/ReferenceData.cs` | Move to `HalalChain.Domain.Catalog` |
| `ProductVariant` | `HalalChain.Platform.Contracts/Catalog/ProductVariant.cs` | Move to `HalalChain.Domain.Catalog` |
| `Product` | `HalalChain.Platform.Api/Persistence/Entities/Product.cs` (entity) **and** `HalalChain.Web/Models/Product.cs` (Blazor view model) **and** `HalalChain.Platform.Contracts/Catalog/Dto/CatalogDtos.cs` (ProductDto) | Keep entity in `HalalChain.Domain.Catalog`, view-model in `HalalChain.Web/Models`, DTO in Contracts |
| `Vendor` | entity in Api + Blazor model in Web + DTO in Contracts | Same pattern |
| `Order` / `CartItem` | entity in Api + Blazor model + DTO | Same pattern |
| `Certificate` / `HalalVerification` | entity in Api + DTO in Contracts | Entity moves to Domain |
| `Category` | legacy self-referential `Category.cs` AND new `Contracts.Catalog.Category` (different class — namespace is the same `HalalChain.Platform.Contracts.Catalog`) | Legacy `Category` is deprecated; new `Category` is the taxonomy category. **Naming collision — must be disambiguated during migration.** |
| `AuthService` (Blazor) vs `AuthController` (Api) | Two independent auth systems | The Blazor fake-token `AuthService.cs:25-30` must be replaced with a real call to `/api/auth/login`. Phased. |
| `ProductEmbedding` (entity) | Api | Move to Domain |
| `OutboxMessage` (entity) | Api | Move to Domain (or Infrastructure) |
| `ChainTxOutbox` (entity) | Api | Move to Domain (or Infrastructure) |
| `ChainMirror` (4 entities) | Api | Move to Domain (or Infrastructure) |

---

## 19. Architectural violations (current)

1. **No Domain/Application/Infrastructure projects.** Everything in Api.
2. **Contracts hosts EF entities** (taxonomy, reference data, ProductVariant). Contracts must be DTO-only.
3. **Business logic in controllers.** `HalalController`, `CatalogController`, `ProductFilterController`, `CommerceController`, `AuthController`.
4. **No authorization policies.** Role strings on attributes only.
5. **No tenant model.** Vendor isolation by per-controller Guid compare.
6. **Two outbox patterns** (business + chain) that need consolidation under one abstraction.
7. **Empty event handler map.** Domain events fire but have no consumers.
8. **Two independent auth systems** (real JWT in Api/Marketplace, fake base64 token in Web).
9. **MCP tools are file inspectors, not product tools.** Need to add product-facing tools.
10. **IPFS upload bypasses the API** (direct from Blazor to Pinata). Should be a `IContentStorageService` upload endpoint behind a controller.
11. **No `IAgentOrchestrationService`**. .autoclaw is not wired into the .NET API.
12. **No `IAiInferenceService` boundary.** Direct `IHttpClientFactory` calls to `ai-inference` (semantic search, etc.) — no resilience policy.
13. **`Directory.Build.props` / `Directory.Build.targets`** exist (not in target arch doc but referenced for builds).

---

## 20. Migration risks

1. **Single InitialSchema migration** spanning both SQLite and Postgres: any column split that changes provider-specific column types must be carefully added without breaking the SQLite path used by tests.
2. **Blazor admin and storefront are fused** in `HalalChain.Web`. Splitting them risks breaking navigation, layouts, and SignalR hubs.
3. **`AuthService` (Blazor) currently works** for the local Blazor app because it never hits the platform API. Replacing it with the real JWT requires changes in every page that calls `IAuthService`.
4. **Outbox handler map is empty** — adding handlers in a new layer must not silently start double-firing events.
5. **MCP `PlatformDataService`** scans the file system at runtime. Moving projects will change the file paths it sees; tests that depend on project names may need updating.
6. **Migrations:** we have one monolithic InitialSchema. Future schema changes need new migrations; we will **never delete** InitialSchema.
7. **The fake `AuthService` in Blazor means many of its "services" (Order, Product, etc.) might be calling the local Blazor API, not the platform API.** This needs verification during Phase 1.
8. **`Directory.Build.props` and `global.json`** must be respected by any new project.

---

## 21. Recommended migration sequence

Strictly incremental, one bounded domain at a time, build+test after each step.

1. **Reconnaissance & status docs (this file + `implementation-status.md`).** ✅ Done.
2. **Architecture guardrail project:** create `HalalChain.Architecture.Tests` with NetArchTest, assert the dependency rules against the **current** codebase. This locks in the starting state.
3. **Phase 1 — Domain consolidation, by bounded context:**
   1. Create `HalalChain.Domain` (empty lib, BCL-only). Catalog first (`Product`, `Category`, `Vendor`, `Certificate`, etc.) by moving entity types from `HalalChain.Platform.Api/Persistence/Entities/`. Re-point `HalalChainDbContext` to the new namespace. **No new behaviour.**
   2. Move `Department`, `Category`, `Subcategory`, `ProductType`, `CertificationBody`, `Facility`, `Brand`, `Country`, `ProductVariant` out of `Contracts` into `HalalChain.Domain` and re-point `HalalChainDbContext`.
   3. Vendor, Commerce, Certification, Blockchain, Assets, Agents, Marketplace, Identity/Governance, Usage.
4. **Phase 2 — Application layer.** Create `HalalChain.Application` with CQRS/MediatR. Start with Catalog. Move controller logic from `CatalogController`/`ProductFilterController` into use cases.
5. **Phase 3 — Infrastructure.** Create `HalalChain.Infrastructure`. Move the DbContext and EF configuration into it. Add `IAiInferenceService`, `IContentStorageService`, `IAgentOrchestrationService`, `IBlockchainService` abstractions. Wire concrete impls.
6. **Phase 4 — Refactor API controllers to be thin.** Mediator only.
7. **Phase 5 — Refactor MCP to be thin.** Add product-facing tools.
8. **Phase 6 — Shared UI.** Extract `HalalChain.UI.Shared` from `HalalChain.Web`.
9. **Phase 7 — Split UI** into `HalalChain.Storefront` and `HalalChain.Admin`.
10. **Phase 8 — Cross-cutting:** auth unification, multi-tenancy, rate limiting per tier, webhooks, payments.

After each step: `dotnet restore && dotnet build && dotnet test` must remain green, and architecture tests must remain green.

---

## 22. Stable subsystems (do not touch in early phases)

- The single `InitialSchema` migration.
- `Foundry` contracts under `HalalChain.Platform.Contracts/contracts/`.
- The Python services in `.halalchain/` (must not be rewritten in C#).
- The agent definitions in `.autoclaw/`.
- The existing `OutboxMessage` and `ChainTxOutbox` (move location, do not redesign semantics).
- The `IPFS:Provider` switch and AES-256-GCM `StorageEncryption`.
- The CORS allow-list, RateLimiter, JWT key validation rules in `Program.cs`.
- The `VendorIsolationTests` test contract (cross-vendor must remain 403).
- The MCP protocol/registry tests.
- `Modelfile` (the LLM system prompt is intentional architecture).

---

## 23. What we will NOT do

- Rewrite the Python AI in C#.
- Rewrite the Python agents in C#.
- Delete the single InitialSchema migration.
- Replace the existing outbox.
- Replace the existing JWT/auth scheme wholesale in one go.
- Introduce microservices.
- Bake secrets into images or commit `.env`.
- "Refactor" `Directory.Build.props` to add warnings-as-errors globally during the migration.
- Force CQRS on trivial reads.
- Add architecture tests that are too strict to enforce on the starting codebase (the first commit of `HalalChain.Architecture.Tests` must be green against the *current* code).
