# HalalChain.Mcp

.NET 10 console host implementing a Model-Context-Protocol (MCP) server over
JSON-RPC. It exposes read-only tools for inspecting the HalalChain solution.

## Layout

```
HalalChain.Mcp/
├── Abstractions/         # IHealthCheckService, IPlatformDataService, IToolRegistry
├── Configuration/        # HalalChainOptions (bound from HALALCHAIN__* env / appsettings)
├── Models/               # JSON-RPC and MCP wire-format models
├── Services/             # Implementations of the abstractions
├── Tools/                # Individual MCP tools (one file per tool)
│                         #   GetArchitecture, Health, ListComponents, ListPages,
│                         #   ListProjects, ListServices, PlatformOverview, ProjectStatus
├── Program.cs            # Host + DI wiring + stdio/JSON-RPC loop
└── ServiceCollectionExtensions.cs
```

## Tools exposed

- `GetArchitectureTool`
- `HealthTool`
- `ListComponentsTool`
- `ListPagesTool`
- `ListProjectsTool`
- `ListServicesTool`
- `PlatformOverviewTool`
- `ProjectStatusTool`

## Configuration

`HalalChainOptions` is bound from configuration (env vars, e.g.
`HALALCHAIN:SERVICES:PLATFORMAPI`). See the root `.env.example` for the full
list and defaults.

## Project references

- `HalalChain.Platform.Contracts`

## Run

```bash
dotnet run --project HalalChain.Mcp
```

The server reads JSON-RPC requests from stdin and writes responses to stdout.
