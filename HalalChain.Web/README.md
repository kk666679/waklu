# HalalChain.Web

Blazor Server customer-facing UI built on Radzen. Served on port `5200` in
the local stack.

## Layout

```
HalalChain.Web/
├── App.razor                    # Root component
├── _Imports.razor
├── Components/                  # Reusable Blazor components
│   ├── Admin/
│   ├── Data/
│   ├── Shared/
│   ├── Storefront/
│   └── Vendor/
├── Layouts/                     # AdminLayout, AuthLayout, MainLayout, …, TopBar
├── Localization/                # ResX resources + Radzen translations
├── Models/                      # Local Blazor view-models
├── Navigation/                  # Nav menu definitions
├── Pages/                       # Routable pages (Account, Admin, Cart, Checkout, …)
├── Realtime/                    # SignalR hubs (Chat, Notification) + contracts
├── Services/                    # State, API client, feature services
├── Properties/
├── wwwroot/                     # Static assets
├── Program.cs
├── appsettings.json
├── dashboard.html
└── Dockerfile                   # Container build (mcr.microsoft.com/dotnet/aspnet:10.0.11)
```

## Project references

- `HalalChain.Platform.Contracts`
- `HalalChain.Platform.Http`

## Run

```bash
dotnet run --project HalalChain.Web
```

…or via the root `docker compose up --build`.
