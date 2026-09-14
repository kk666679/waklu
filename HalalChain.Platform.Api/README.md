# HalalChain.Platform.Api

The core REST API — a .NET 10 modular monolith. Served on port `5001` in the
local stack.

## Layout

```
HalalChain.Platform.Api/
├── AI/                 # AI gateway integration helpers
├── Controllers/        # Top-level controllers
├── Migrations/         # EF Core migrations
├── Modules/            # Vertical feature slices
│   ├── AI/             # AI routing and proxying
│   ├── Blockchain/     # On-chain attestation calls (Nethereum)
│   ├── Catalog/        # Product catalog
│   ├── Commerce/       # Cart, orders, checkout
│   ├── Events/         # In-process + distributed events
│   ├── Halal/          # Halal evidence — forwards to tawheed, never assigns verdicts
│   ├── Indexer/        # Off-chain indexers
│   ├── Ipfs/           # IPFS pinning integration
│   ├── Vendors/        # Vendor onboarding and profile
│   └── Verification/   # Cross-module verification orchestration
├── Persistence/        # EF Core DbContext + repositories
├── Properties/         # launchSettings.json
├── infrastructure/     # Internal infra adapters
├── Program.cs          # ASP.NET Core host + module wiring
├── appsettings.json    # Local config (Jwt, AiGateway, Tawheed, …)
├── ApiLayout.razor
├── _Imports.razor
├── Radzen.Blazor.Api.Generator.csproj# Source generator for optional API pages
└── Dockerfile                        # Container build (mcr.microsoft.com/dotnet/aspnet:10.0.11)
```

## Architectural invariant

The `Modules/Halal/` module is a thin client over `TawheedHttpClient`. It
forwards evidence requests to the `tawheed` service and never assigns a halal
verdict on its own — that is the responsibility of `tawheed`'s deterministic
Policy Engine.

## Project references

- `HalalChain.Application`
- `HalalChain.Domain`
- `HalalChain.Platform.Contracts`

## Local SQLite

By default the API persists to a local SQLite file at
`HalalChain.Platform.Api/halalchain.db` (along with the `-shm` / `-wal` files
when WAL is in use). In non-development environments the Postgres connection
string is loaded from `ConnectionStrings__Postgres`.

## Run

```bash
dotnet run --project HalalChain.Platform.Api
```

…or via the root `docker compose up --build`.
