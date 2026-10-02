# HalalChain.Platform.Http

Typed `HttpClient` library used by the two frontends (`HalalChain.Web` and
`HalalChain.Marketplace`) to talk to the core REST API.

## Purpose

This library is the HTTP boundary between the UIs and `HalalChain.Platform.Api`.
It centralises base-address configuration, bearer-token acquisition, correlation
IDs, resilience policies, response-envelope handling, feature flags, and
result types so no UI has to reimplement them.

## Responsibilities

- Register typed `HttpClient`s for the platform API, with or without cookie token forwarding.
- Attach a correlation ID to every outgoing request and honour the inbound one.
- Apply the standard resilience handler (retry, circuit breaker, timeout).
- Expose a canonical `ApiResult<T>` envelope so callers branch on
  success/failure instead of parsing HTTP status codes.
- Provide `IFeatureFlag` with stable bucketing, letting a UI ship in-memory and
  API-backed implementations of the same service behind one flag.
- Provide `IPlatformTokenAccessor` for server-side bearer-token forwarding.

## Layout

```
HalalChain.Platform.Http/
├── Abstractions/       # IApiClient, IApiNotifier, ICurrentUserAccessor,
│                      #   IFeatureFlag, IPlatformTokenAccessor
├── Extensions/         # ServiceCollectionExtensions,
│                      #   PlatformApiClientServiceCollectionExtensions,
│                      #   HalalChainApiClientExtensions
├── Flags/              # FlagNames, StableBucketing, ConfigFeatureFlag,
│                      #   FeatureFlagOptions
├── Http/               # ResilientApiClient
├── Models/             # ApiResult<T>, Result
├── Resilience/         # ApiErrorMapper, CorrelationIdHandler
├── Services/           # IPlatformApiClient, PlatformApiClient, ProductQuery
├── TokenAccessors/     # CookieTokenAccessor
├── Utils/              # QueryString helpers
├── Meters.cs           # Meter names for resilience/outcome instrumentation
└── PlatformApiSender.cs
```

## Architecture / Flow

```text
HalalChain.Web  ─┐
                 ├─▶ IPlatformApiClient ─▶ CorrelationIdHandler
HalalChain.Marketplace ─┘                     └─▶ AddStandardResilienceHandler
                                                      └─▶ HalalChain.Platform.Api
```

`HalalChain.Platform.Api` must not reference this project, and this project must
not reference the API. Both directions are enforced by
`HalalChain.Architecture.Tests`
(`Marketplace_ShouldNotReference_ApiProject`, `Http_ShouldNotReference_ApiProject`).

## Dependencies

| Kind | Reference |
| --- | --- |
| Project | `HalalChain.Platform.Contracts` |
| Framework | `Microsoft.AspNetCore.App` |
| Package | `Microsoft.Extensions.Http.Resilience` 9.0.0 |

`TreatWarningsAsErrors` is `true` for this project.

## Interfaces

### Registration

| Method | Effect |
| --- | --- |
| `AddPlatformHttpClient(Action<HttpClient>?)` | Registers `PlatformApiSender`, `IHttpContextAccessor`, `Accept: application/json` |
| `AddPlatformHttpClientWithCookieToken(...)` | The above plus `IPlatformTokenAccessor`→`CookieTokenAccessor` |
| `AddPlatformApiClient()` | Registers `IPlatformApiClient`→`PlatformApiClient` (scoped). Must be paired with `AddPlatformHttpClient` |
| `AddHalalChainApiClient(IConfiguration)` | Base address from `Api:BaseUrl` ?? `PlatformApi:BaseUrl` (**throws if neither is set**), infinite client timeout, `X-Client: HalalChain/1.0` header, `CorrelationIdHandler`, standard resilience. Also binds `FeatureFlagOptions` from `FeatureFlags` and registers `IFeatureFlag`→`ConfigFeatureFlag` |
| `AddRoutedService<TInterface, TInMemory, TApi, TRouter>()` | Registers the in-memory implementation, the API-backed implementation, and the flag-driven router between them |

### Contracts

`IApiClient`, `IApiNotifier`, `ICurrentUserAccessor`, `IFeatureFlag`,
`IPlatformTokenAccessor`, and the `ApiResult<T>` / `Result` result types.

## Usage

```csharp
builder.Services.AddHalalChainApiClient(builder.Configuration);
builder.Services.AddRoutedService<IWishlistService, InMemoryWishlist, ApiWishlist, WishlistRouter>();
```

## Configuration

| Key | Required | Notes |
| --- | --- | --- |
| `Api:BaseUrl` or `PlatformApi:BaseUrl` | Yes | `AddHalalChainApiClient` throws if neither is set |
| `FeatureFlags:Flags:<name>` | No | Keys consumed by `ConfigFeatureFlag` |

Feature-flag keys currently present in the two UIs' `appsettings.json`:
`services.wishlist.api_backed`, `services.cart.api_backed`,
`services.cart.merge_enabled`.

## Integration

- `HalalChain.Web` and `HalalChain.Marketplace` both depend on this project.
- `HalalChain.Platform.Api` is the server side; there is no compile-time
  reference in either direction.
- `HalalChain.Mcp` does **not** use this library — it calls the API over its own
  `HttpClient`.

## Build

```bash
dotnet build HalalChain.Platform.Http
```

## Testing

There is no dedicated test project. Coverage is structural only, via
`HalalChain.Architecture.Tests`
(`Http_ShouldNotReference_ApiProject`, `NonApiProjects_ShouldNotReference_EFCore`).

## Deployment

Not deployable. Ships as an assembly inside `HalalChain.Web` and
`HalalChain.Marketplace`.

## Related Components

- [HalalChain.Platform.Api](../HalalChain.Platform.Api/README.md) — the server this calls
- [HalalChain.Web](../HalalChain.Web/README.md) — consumer
- [HalalChain.Marketplace](../HalalChain.Marketplace/README.md) — consumer
- [HalalChain.Platform.Contracts](../HalalChain.Platform.Contracts/README.md) — shared DTOs
- [HalalChain.Mcp](../HalalChain.Mcp/README.md) — the other API consumer, via its own transport

## Notes / Limitations

- `AddHalalChainApiClient` sets `Timeout = Timeout.InfiniteTimeSpan` on the
  client. Deadlines come from the resilience pipeline and from callers, not from
  `HttpClient.Timeout`.
- Resilience behaviour is configured through the standard
  `Microsoft.Extensions.Http.Resilience` handler, so per-endpoint overrides are
  added at registration time rather than per request.