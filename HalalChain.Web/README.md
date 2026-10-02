# HalalChain.Web

Blazor Server customer-facing UI built on Radzen. Served on port `5200` in
the local stack.

## Purpose

This is the shopper and vendor-facing web application: storefront, product
discovery, cart and checkout, wishlist, vendor dashboard, and an admin area.
It is a **client of the platform API**, not a second backend — every piece of
domain data it shows comes from `HalalChain.Platform.Api` over HTTP.

## Responsibilities

- Render the storefront, catalog, product detail, cart, checkout, and account pages.
- Manage client-side state (cart, wishlist, notifications, theme, localisation).
- Call `HalalChain.Platform.Api` through the typed client in `HalalChain.Platform.Http`.
- Receive realtime updates over SignalR (chat, notifications, inventory).
- Provide the admin surface for catalog, customers, marketing, reports, and sales.

## Layout

```
HalalChain.Web/
├── App.razor                    # Root component
├── _Imports.razor
├── Components/
│   ├── Admin/                   # Catalog, Customers, Dashboard, Marketing,
│   │                            #   Reports, Sales, Shared, System
│   ├── Data/                    # Data-loading components
│   ├── ProductDiscovery/        # Search and filter UI
│   ├── Recommendations/         # Recommendation surfaces
│   ├── Shared/                  # Reusable UI pieces
│   ├── Storefront/              # Storefront components
│   └── Vendor/                  # Dashboard, Forms, Grids
├── Infrastructure/              # Host-level plumbing
├── Layouts/                     # AdminLayout, AuthLayout, MainLayout, …, TopBar
├── Localization/                # ResX resources + Radzen translations
├── Models/                      # Local Blazor view-models
│   └── Admin/
├── Navigation/                  # Nav menu definitions
├── Pages/                       # Routable pages (Account, Admin, Vendor)
├── Properties/
├── Realtime/                    # SignalR hubs (Chat, Notification) + contracts
├── Services/                    # State, API clients, feature services (~50 files)
├── wwwroot/                     # Static assets; scss/ is the compiled from source
├── Program.cs
├── appsettings.json
├── dashboard.html
└── Dockerfile                   # Container build (mcr.microsoft.com/dotnet/aspnet:10.0.11)
```

`Services/` holds the application services the UI depends on — `PlatformApiClient`,
`WebApiClient`, `ProductService`, `SearchService`, `QdrantVectorSearchService`,
`ChatService`, `CartState`, `WishlistService`, `VendorService`, `ReportService`,
`OrderService`, `PayoutService`, `ShipmentService`, `ReviewService`,
`LocalizationService`, `ThemeService`, and others. `QdrantVectorSearchService`
notably reaches a vector store directly rather than going through the API.

## Architecture / Flow

```text
Browser
  │  SignalR (ChatHub, NotificationHub) + HTTP
  ▼
HalalChain.Web (Blazor Server)
  │  IPlatformApiClient via HalalChain.Platform.Http
  │  + ResilientApiClient (standard resilience, correlation-id handler)
  ▼
HalalChain.Platform.Api
  │
  ├─▶ ai-inference, tawheed, local-models, postgres, redis
```

## Project references

- `HalalChain.Platform.Contracts`
- `HalalChain.Platform.Http`

Deliberately *not* `HalalChain.Domain`, `HalalChain.Application`, or
`HalalChain.Platform.Api` — the UI is separated from the backend by an HTTP
boundary only.

## Dependencies

Notable packages: `Radzen.Blazor` 11.3.2, `Microsoft.AspNetCore.SignalR.Client`,
`Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.ML` 5.0.0
(recommendations), `CsvHelper` (bulk import/export), `System.IdentityModel.Tokens.Jwt`.

`HalalChain.Architecture.Tests` asserts `NonApiProjects_ShouldNotReference_EFCore`
for this project.

## Configuration

`Api:BaseUrl` (or `PlatformApi:BaseUrl`) — required by
`AddHalalChainApiClient`; the extension throws if neither is set.

`FeatureFlags:Flags` — the feature-flag keys present in this app's
`appsettings.json` are `services.wishlist.api_backed`,
`services.cart.api_backed`, and `services.cart.merge_enabled`. These select
between the in-memory and API-backed implementations registered by
`AddRoutedService<TInterface, TInMemory, TApi, TRouter>()`.

## Usage

```bash
dotnet run --project HalalChain.Web
```

…or via the root `docker compose up --build`, where it listens on `5200`.

## Styling

SCSS under `wwwroot/scss/` is the source; `wwwroot/css/` is the build output.
Compile from the repository root:

```bash
npm run scss:build   # once
npm run scss:watch   # watch mode
```

The .NET build can invoke the same script with `-p:SassEnabled=true`, but that
is off by default because the SDK container build stage has no npm — the Docker
image compiles SCSS in a separate `node:22-alpine` stage. See
[`Directory.Build.targets`](../Directory.Build.targets).

## Testing

Covered indirectly by `HalalChain.Platform.Tests` (which references this project)
and structurally by `HalalChain.Architecture.Tests`. There is no
`HalalChain.Web.Tests`.

## Deployment

Container image built from `HalalChain.Web/Dockerfile`, mapped to host port
`5200`. The Helm chart deploys it as the `web` release. Health probe:
`/health/live`.

## Related Components

- [HalalChain.Platform.Http](../HalalChain.Platform.Http/README.md) — the typed client this UI uses
- [HalalChain.Platform.Contracts](../HalalChain.Platform.Contracts/README.md) — DTOs shared with the API
- [HalalChain.Platform.Api](../HalalChain.Platform.Api/README.md) — the backend this UI calls
- [HalalChain.Marketplace](../HalalChain.Marketplace/README.md) — the separate vendor marketplace application
- [infrastructure/scripts](../infrastructure/scripts/build-scss.mjs) — the SCSS build

## Notes / Limitations

- `QdrantVectorSearchService` performs vector search against Qdrant from the UI
  process. That is a direct backend dependency from a presentation layer and is
  listed in [`docs/architecture/tech-debt.md`](../docs/architecture/tech-debt.md).
- SCSS must be built before `dotnet run`, or the site renders unstyled.