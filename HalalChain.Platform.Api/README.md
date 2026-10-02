# HalalChain.Platform.Api

The core REST API — a .NET 10 modular monolith. Served on port `5001` in the
local stack.

## Layout

```
HalalChain.Platform.Api/
├── AI/                   # AI provider adapters (ResilientAiInferenceProvider, …)
├── Controllers/          # Top-level controllers (Auth, Chat, Customers)
├── Infrastructure/
│   ├── Repositories/     # EF Core repositories
│   ├── Security/         # Token service, current-user accessor
│   ├── docker-compose.dev.yml   # Dev-only overlay (IPFS / Polygon fork)
│   └── scripts/setup-dev.sh
├── Migrations/           # EF Core migrations
├── Modules/              # Vertical feature slices
│   ├── AI/               # AI routing and proxying
│   ├── Auth/             # Login / register
│   ├── Blockchain/       # On-chain attestation calls (Nethereum)
│   ├── Catalog/          # Product catalog
│   ├── Commerce/         # Cart, orders, checkout
│   ├── Events/           # In-process + distributed events (outbox)
│   ├── Halal/            # Halal evidence — forwards to tawheed, never assigns verdicts
│   ├── Indexer/          # Off-chain indexers
│   ├── Ipfs/             # IPFS pinning integration
│   ├── Vendors/          # Vendor onboarding and profile
│   └── Verification/     # Cross-module verification orchestration
├── Observability/        # OpenTelemetry wiring
├── Persistence/          # EF Core DbContext, entities, seed data
├── Properties/           # launchSettings.json
├── CodeGen/              # OpenAPI/Radzen page generator (config, schema, templates)
├── Program.cs            # ASP.NET Core host + module wiring
├── appsettings.json      # Local config (Jwt, AiGateway, Tawheed, …)
├── ApiLayout.razor
├── _Imports.razor
├── Radzen.Blazor.Api.Generator.csproj  # Source generator for optional API pages
└── halalchain.db, -shm, -wal          # Local SQLite files
```

The container build for this project lives at the **repository root**
(`Dockerfile`, with `ARG PROJECT=HalalChain.Platform.Api`). There is no Dockerfile
inside this directory.

## Architectural invariant

The `Modules/Halal/` module is a thin client over `TawheedHttpClient`. It
forwards evidence requests to the `tawheed` service and never assigns a halal
verdict on its own — that is the responsibility of `tawheed`'s deterministic
Policy Engine.

## Project references

- `HalalChain.Application`
- `HalalChain.Domain`
- `HalalChain.Platform.Contracts`
- `HalalChain.Agents`

## Endpoints

| Route prefix | Controller |
| --- | --- |
| `api/auth` | `POST login`, `POST register` |
| `api/ai` | `ChatController` — `POST chat` |
| `api/commerce` | `CustomersController` — `GET customers` |
| (absolute) | `AiController` — `POST /api/ai/health`, `/embeddings`, `/summarize`, `/classify`, `/ingredient-parse`, `/certificate-extract`, `/shopping/search`, `/shopping/recommend`, `/catalog/enrich`, `/vendor/copilot` |
| `api/v1/catalog` | `CatalogController`, `ProductFilterController`, `TaxonomyController` |
| `api/v1/search` | `GET semantic`, `POST embed-all`, `POST embed/{productId}`, `GET suggestions` |
| `api/v1/commerce` | `CommerceController` — cart, checkout, orders |
| `api/v1/wishlist` | `WishlistController` |
| `api/v1/halal` | `HalalController` — certificates, verify, verification, audit, decision |
| `api/v1/ipfs` | `IpfsController` — `POST upload`, `GET {cid}`, `GET {cid}/metadata` |
| `api/v1/vendors` | `VendorsController` |
| `api/v1/verify` | `VerificationController` |

Health endpoints: `/health`, `/health/live`, `/health/ready`.

## Startup behaviour worth knowing

`Program.cs` walks up from the working directory to find a `.env` and sets any
variables not already present. It then:

- Persists Data Protection keys to `DataProtection:KeysPath` or
  `<ContentRoot>/.aspnet/DataProtection-Keys` under application name
  `HalalChainPlatform`.
- Throws at startup if `Jwt:Key` is shorter than 32 characters, and throws in
  non-development if it equals the documented example value.
- Configures API versioning (`v1`, URL segment reader) and rate limits
  (fixed 100/min with queue limit 10; sliding 50/min with queue limit 5; 429 on
  rejection).
- Runs `MigrateAsync()` at startup when the database is relational.
- Registers the blockchain module only when `Blockchain:RpcUrl` is non-empty;
  otherwise it logs that the module is disabled and the verification endpoint
  will return 503.
- Selects the IPFS provider by `IPFS:Provider` (`pinata` → `PinataStorageService`,
  otherwise `LocalKuboStorageService`), falling back to `LocalFileStorageService`
  when neither `IPFS:KuboApiUrl` nor `IPFS:PinataJwt` is set.
- Honors an inbound `X-Correlation-ID` and always writes it back.
- Chooses Npgsql when `ConnectionStrings:Postgres` is set, otherwise SQLite.

## Local SQLite

By default the API persists to a local SQLite file at
`HalalChain.Platform.Api/halalchain.db` (along with the `-shm` / `-wal` files
when WAL is in use). In non-development environments the Postgres connection
string is loaded from `ConnectionStrings__Postgres`.

## Configuration

`appsettings.json` ships with `Jwt`, `AiGateway`, `Tawheed`, `ConnectionStrings:Redis`,
`Logging`, and `AllowedHosts`. These are *not* present and must be supplied
through environment variables or user secrets (`UserSecretsId` is
`halalchain-platform-api-2026`): `ConnectionStrings:Postgres`, `Cors:AllowedOrigins`,
`Agents:*`, `IPFS:*`, `Blockchain:*`, `DataProtection:KeysPath`. See
[`.env.example`](../../.env.example) for the names.

## Run

```bash
dotnet run --project HalalChain.Platform.Api
```

…or via the root `docker compose up --build`.

## Testing

Exercised end-to-end by `HalalChain.Platform.Tests` through
`WebApplicationFactory<global::Program>` — the `public partial class Program;` at
the end of `Program.cs` exists specifically to enable that. The project also
provides the assembly under inspection for the boundary rules in
`HalalChain.Architecture.Tests`.

## Related Components

- [HalalChain.Application](../HalalChain.Application/README.md) — handlers this host wires up
- [HalalChain.Domain](../HalalChain.Domain/README.md) — entities persisted here
- [HalalChain.Agents](../HalalChain.Agents/README.md) — `AddAgentRuntime` called from `Program.cs`
- [HalalChain.Platform.Http](../HalalChain.Platform.Http/README.md) — the client the UIs use to call this API
- [HalalChain.Platform.Tests](../HalalChain.Platform.Tests/README.md)
- [tawheed service](../../.halalchain/tawheed/README.md) — the compliance authority this API forwards to
- [docker-compose.yml](../../docker-compose.yml) — service definition
