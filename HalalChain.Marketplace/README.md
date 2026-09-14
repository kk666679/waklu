# HalalChain.Marketplace

ASP.NET Core MVC vendor-facing UI. Served on port `5201` in the local stack.

Communicates with the core API through the typed client in
`Services/PlatformApiClient.cs` (built on `HalalChain.Platform.Http`).

## Layout

```
HalalChain.Marketplace/
├── Controllers/        # MVC controllers (Account, Admin, Cart, Catalog, Checkout,
│                       #   Home, Marketplace, Orders, Products, VendorPortal,
│                       #   Vendors, Verify)
├── Services/           # PlatformApiClient + interface
├── ViewComponents/     # Reusable view components (e.g. SiteNav)
├── Views/              # Razor views
├── wwwroot/            # Static assets
├── Program.cs          # ASP.NET Core host
├── appsettings.json    # Local config
└── Dockerfile          # Container build (mcr.microsoft.com/dotnet/aspnet:10.0.11)
```

## Project references

- `HalalChain.Platform.Contracts`
- `HalalChain.Platform.Http`

## Run

```bash
dotnet run --project HalalChain.Marketplace
```

…or via the root `docker compose up --build` (see `docker-compose.yml`).
