# HalalChain.Platform.Http

Typed `HttpClient` library used by the two frontends (`HalalChain.Web` and
`HalalChain.Marketplace`) to talk to the core REST API.

## Layout

```
HalalChain.Platform.Http/
├── Abstractions/             # IPlatformTokenAccessor
├── Extensions/               # ServiceCollectionExtensions (DI registration)
├── Models/                   # ApiResult<T> (canonical response envelope)
├── TokenAccessors/           # CookieTokenAccessor
├── Utils/                    # QueryString helpers
└── PlatformApiSender.cs      # The typed API client
```

## Consumers

- `HalalChain.Web`
- `HalalChain.Marketplace`

## Build

```bash
dotnet build HalalChain.Platform.Http
```
