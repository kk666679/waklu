# HalalChain.Mcp

.NET 10 console host implementing a Model-Context-Protocol (MCP) server over
JSON-RPC 2.0 on stdio. It exposes the HalalChain solution — projects, pages,
components, services, architecture, live service health, documentation, and
reusable prompt templates — to MCP clients such as IDEs and agent runners.

The server is read-only by construction. Every tool is annotated
`readOnlyHint: true`, `destructiveHint: false`, so a host can auto-approve any
call it offers.

## Protocol support

The server implements the **2025-06-18** MCP revision and negotiates down for
older clients.

| Revision    | Notes                                                        |
|-------------|--------------------------------------------------------------|
| 2025-06-18  | Advertised by default. Structured output, resources, prompts. |
| 2025-03-26  | Tool annotations.                                            |
| 2024-11-05  | Text-only tool results (the original surface).                |

### Methods

| Method                        | Purpose                                          |
|-------------------------------|--------------------------------------------------|
| `initialize`                  | Handshake; returns the negotiated revision.      |
| `ping`                        | Liveness.                                        |
| `tools/list`                  | Tool descriptors with annotations (paginated).   |
| `tools/call`                  | Invoke a tool; returns a content-block array.    |
| `resources/list`              | Enumerable resources (paginated).                |
| `resources/templates/list`    | Parameterized resource templates (none).         |
| `resources/read`              | Read one resource.                               |
| `prompts/list`                | Prompt templates (paginated).                    |
| `prompts/get`                 | Render a prompt.                                 |
| `completion/complete`         | Argument completion for prompts.                |
| `logging/setLevel`            | Set the client log threshold.                    |

The transport also accepts **JSON-RPC batches** (an array of requests in one
frame) and emits `notifications/progress` when a request carries
`_meta.progressToken`.

### Bidirectional primitives

`tools/call` handlers may call back into the client, gated on both a server
option and the capability the client declared at `initialize`:

- `sampling/createMessage` — ask the client's model to sample.
- `elicitation/create` — ask the user for structured input.
- `notifications/message` — forward logs to the client.

## Layout

```
HalalChain.Mcp/
├── Abstractions/         # IMcpSession, IClientMessenger, IToolRegistry,
│                         # IResourceRegistry, IPromptRegistry,
│                         # IHealthCheckService, IPlatformDataService
├── Configuration/        # HalalChainOptions (HALALCHAIN__* env / appsettings)
├── Models/               # JSON-RPC wire models + MCP protocol models
├── Services/             # Implementations, plus the stderr logger and the
│                         # gated stdio transport writer
├── Tools/                # One file per MCP tool
├── Program.cs            # Host, DI wiring, stdio read loop, method dispatch
└── ServiceCollectionExtensions.cs
```

## Tools exposed

- `halalchain_get_architecture`
- `halalchain_health` — the only tool that touches the network
- `halalchain_list_components`
- `halalchain_list_pages`
- `halalchain_list_projects`
- `halalchain_list_services`
- `halalchain_platform_overview`
- `halalchain_project_status`

## Resources

Synthetic, computed from the solution scan:

- `halalchain://architecture`
- `halalchain://solution/projects`
- `halalchain://solution/pages`
- `halalchain://solution/services`
- `halalchain://docs/index`
- `halalchain://runbooks/index`

File-backed, allowlisted by directory and extension:

- `file://<root file>` — `AGENTS.md`, `Modelfile`, `README.md`, `service-manifest.yaml`
- `file://docs/...`
- `file://.halalchain/...`
- `file://infrastructure/...`
- `file://scripts/...`

File reads re-validate the resolved path against the solution root, reject
`..` segments, and are capped at `Mcp:MaxResourceBytes`.

## Prompts

- `halal-compliance-review` — evidence review scaffold for a product or certificate
- `policy-engine-audit` — hunt determinism violations
- `incident-triage` — symptom to runbook to first diagnostic
- `module-onboarding` — wire a new API module
- `mcp-capability-audit` — find the gaps in this server's own surface

Each template restates the platform invariant: AI collects evidence, the
deterministic Policy Engine assigns every verdict.

## Configuration

`HalalChainOptions` binds from configuration. Environment variables use the
`HALALCHAIN_` prefix with `__` as the section separator, for example
`HALALCHAIN_MCP__ENABLEPROMPTS=false`.

| Key                                    | Default | Purpose                                  |
|----------------------------------------|---------|------------------------------------------|
| `SolutionRoot`                         | discovered | Repository root to scan                |
| `Services:*`                           | localhost ports | Service base URLs                 |
| `Mcp:MaxRequestBytes`                  | 1 MiB    | Inbound frame ceiling                    |
| `Mcp:ToolTimeoutSeconds`               | 30       | Per-tool budget                          |
| `Mcp:ClientRequestTimeoutSeconds`      | 60       | Wait for sampling/elicitation replies    |
| `Mcp:MaxResourceBytes`                 | 512 KiB  | Resource payload ceiling                 |
| `Mcp:PageSize`                         | 50       | Cursor page size                         |
| `Mcp:EnableResources`                  | true     | Serve the resource methods               |
| `Mcp:EnablePrompts`                    | true     | Serve the prompt methods                 |
| `Mcp:EnableCompletions`                | true     | Serve `completion/complete`              |
| `Mcp:EnableProgress`                   | true     | Emit progress notifications              |
| `Mcp:EnableLogging`                    | true     | Forward logs to the client               |
| `Mcp:EnableSampling`                   | true     | Allow `sampling/createMessage`           |
| `Mcp:EnableElicitation`                | true     | Allow `elicitation/create`               |
| `Mcp:LogLevel`                         | Information | stderr log threshold                  |

Point the server at a checkout with `HALALCHAIN_SOLUTION_ROOT`. When it is
unset the server walks up from its own binary looking for
`HalalChain.Platform.sln`. If the root cannot be resolved, enumeration returns
empty rather than failing, and `halalchain://architecture` explains why.

## Transport hygiene

stdout is the frame channel, so the server captures the raw stdout stream and
redirects `Console.Out` to stderr before any handler runs. All writes go
through a single gated writer, so a progress notification emitted while a
response is flushing cannot interleave mid-line.

## Project references

- `HalalChain.Platform.Contracts`

## Run

```bash
dotnet run --project HalalChain.Mcp
```

The server reads JSON-RPC requests from stdin and writes responses to stdout.

## Test

```bash
dotnet test HalalChain.Mcp.Tests
```
