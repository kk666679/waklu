# HalalChain.Mcp

> Read-only Model Context Protocol server for the HalalChain solution.
> Lets MCP-compatible AI clients inspect the platform's structure — projects,
> services, endpoints, tests, governance documents, and health — without
> writing state or issuing verdicts.

[![.NET](https://img.shields.io/badge/.NET-10.0-512bd4)](https://dotnet.microsoft.com/)
[![MCP](https://img.shields.io/badge/MCP-2025--06--18-6e57e0)](https://modelcontextprotocol.io/)
[![License](https://img.shields.io/badge/license-Proprietary-red)](../LICENSE)

**AI discovers. Policy decides. Verdicts are deterministic, never overridden by LLM.**

---

## Table of Contents

- [What this server is](#what-this-server-is)
- [What this server is not](#what-this-server-is-not)
- [Requirements](#requirements)
- [Build](#build)
- [Run](#run)
- [Client configuration](#client-configuration)
- [Tools](#tools)
- [Resources](#resources)
- [Prompts](#prompts)
- [Configuration](#configuration)
- [Protocol details](#protocol-details)
- [Project structure](#project-structure)
- [Probe test](#probe-test)
- [Development](#development)
- [Troubleshooting](#troubleshooting)
- [Architecture invariants](#architecture-invariants)
- [License](#license)

---

## What this server is

`HalalChain.Mcp` is a **stdio MCP server**. It speaks JSON-RPC 2.0 over stdin
and stdout and implements the `2025-06-18` revision of the Model Context
Protocol, negotiating down to `2025-03-26` or `2024-11-05` when a client asks
for an older one.

It answers questions about the HalalChain solution by scanning the source tree
rooted at `HALALCHAIN_SOLUTION_ROOT` and by probing local service health
endpoints.

Typical uses:

- *"what services does `HalalChain.Platform.Api` contain?"* — returns the
  concrete class list with interfaces and methods.
- *"which Blazor pages route to `/verify`?"* — returns the matching `.razor`
  files with their `@page` directives.
- *"is the platform healthy?"* — returns the HTTP status of each configured
  health endpoint.
- *"what is the architecture?"* — returns a composed diagram of services,
  projects, and data flows discovered on disk.
- *"what does ADR 0007 say?"* — returns the full Markdown of one Architecture
  Decision Record.

The server never writes state. It never issues a compliance verdict. It has no
database connection. It calls no external networks except the health endpoints
it was designed to probe.

---

## What this server is not

| Not | Why |
|---|---|
| Not a database client | No `Npgsql`, no connection string, no `appsettings.json` |
| Not a blockchain client | No `Nethereum`, no RPC URL, no contract ABI |
| Not a write tool | Every tool is annotated `readOnlyHint: true`, `destructiveHint: false` |
| Not an MCP SDK consumer | Hand-rolled JSON-RPC host, no `ModelContextProtocol` package reference |
| Not a policy engine | `tawheed` decides; this server only inspects |
| Not a compliance oracle | No tool name begins with, or contains, `approve`, `reject`, `decide`, `set_verdict`, or `override` |

The design intentionally omits anything that could be mistaken for an
authoritative decision surface. `HalalChain.Mcp` observes the shape of the
solution; it does not participate in the platform's state.

---

## Requirements

- .NET SDK 10.0.200 or later (see `global.json`).
- No database, no message broker, and no running container is required. The
  scans are filesystem reads; only `halalchain_health` needs the stack up.

---

## Build

```bash
dotnet build HalalChain.Mcp/HalalChain.Mcp.csproj -c Release --nologo
```

Expected: **0 warnings, 0 errors**. The project sets `TreatWarningsAsErrors`;
a warning is a build failure here.

The Release binary lands at:

```
HalalChain.Mcp/bin/Release/net10.0/HalalChain.Mcp.exe    (Windows)
HalalChain.Mcp/bin/Release/net10.0/HalalChain.Mcp        (Linux/macOS)
```

---

## Run

The server speaks stdio. It is **not** meant to be launched from a terminal
by hand for normal use — MCP clients launch it automatically. For manual
testing:

```bash
# Linux / macOS
./HalalChain.Mcp/bin/Release/net10.0/HalalChain.Mcp
```

Once running, the process waits for newline-delimited JSON-RPC frames on
stdin. **stdout carries only JSON-RPC frames** — `Program.cs` redirects
`Console.Out` to stderr at startup, so a stray `Console.WriteLine` anywhere in
the process cannot corrupt the stream.

**Manual smoke test:**

```bash
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"manual","version":"1.0"}}}' \
  | ./HalalChain.Mcp/bin/Release/net10.0/HalalChain.Mcp
```

Expected: a single JSON-RPC `result` frame containing `serverInfo.name` equal
to `halalchain-mcp`, then clean exit.

---

## Client configuration

Both VS Code (Cline) and Cursor are supported. The config **key differs**
between them: VS Code uses `"servers"`, Cursor uses `"mcpServers"`.

### VS Code / Cline — `.vscode/mcp.json`

```json
{
  "servers": {
    "halalchain": {
      "type": "stdio",
      "command": "/absolute/path/to/HalalChain/bin/Release/net10.0/HalalChain",
      "args": [],
      "env": {
        "HALALCHAIN_SOLUTION_ROOT": "/absolute/path/to/waklu"
      }
    }
  }
}
```

### Cursor — `.cursor/mcp.json`

```json
{
  "mcpServers": {
    "halalchain": {
      "type": "stdio",
      "command": "/absolute/path/to/HalalChain/bin/Release/net10.0/HalalChain",
      "args": [],
      "env": {
        "HALALCHAIN_SOLUTION_ROOT": "/absolute/path/to/waklu"
      }
    }
  }
}
```

**Path conventions:**

- Use forward slashes (`/`) or escaped backslashes (`\\`). Raw Windows
  backslashes in JSON are a parse error.
- `command` must resolve to a local executable. A Git URL will not work —
  MCP stdio requires a local process.
- Both files can coexist. Neither client reads the other's file.

---

## Tools

The server registers **25 tools** in `ServiceCollectionExtensions.AddHalalChainMcp`,
in three waves. `tools/list` returns them alphabetically, cursor-paginated.

Every tool is annotated `readOnlyHint: true`, `destructiveHint: false`,
`idempotentHint: true`. All are `openWorldHint: false` — local repository
inspection only — except `halalchain_health`, which is `openWorldHint: true`
because it probes the network.

Tools that need an argument validate it and return `isError: true` with an
explanatory text block rather than failing the JSON-RPC call.

### Wave 0 — platform surface (8)

| Tool | Arguments | Returns |
|---|---|---|
| `halalchain_project_status` | none | Solution name, .NET version, per-project counts |
| `halalchain_list_projects` | none | Every `.csproj` with target framework, kind, packages, references |
| `halalchain_list_pages` | `project` | Razor `@page` routes with layout and owning project |
| `halalchain_list_components` | `project` | Blazor components with `[Parameter]` names |
| `halalchain_list_services` | `project` | Service classes with interfaces and public methods |
| `halalchain_platform_overview` | `project` | Aggregate counts plus full inventories |
| `halalchain_get_architecture` | none | System diagram, service layer, data contracts |
| `halalchain_health` | none | `{ service, url, status }` for five endpoints |

### Wave 1 — code introspection (8)

| Tool | Arguments | Returns |
|---|---|---|
| `halalchain_list_endpoints` | `project` | `[HttpVerb]` routes on `[ApiController]` classes, class route composed in |
| `halalchain_list_mediator_handlers` | `project` | `IRequestHandler<,>` / `INotificationHandler<>` with request and response types |
| `halalchain_list_domain_events` | `project` | `IDomainEvent`, `INotification`, and `*Event` payloads |
| `halalchain_list_aggregate_roots` | `project` | Types whose base list names `IAggregateRoot`, `AggregateRoot`, or `Entity` |
| `halalchain_list_value_objects` | `project` | Records implementing `IValueObject`, or declared in a `ValueObjects` file or folder |
| `halalchain_list_migrations` | `project` | EF Core migrations ordered by the file-name timestamp |
| `halalchain_test_inventory` | `project` | Per-project `[Fact]` / `[Theory]` / `[InlineData]` counts |
| `halalchain_find_type` | `name` (required), `project` | Declaration sites, exact matches first |

### Wave 2 — governance introspection (9)

| Tool | Arguments | Returns |
|---|---|---|
| `halalchain_get_principles` | none | Numbered principles (`P1`, `P2`, …) with the line each is declared on |
| `halalchain_list_adrs` | none | ADR ids, titles, and paths |
| `halalchain_get_adr` | `id` (required) | One ADR's full Markdown |
| `halalchain_list_skills` | none | Skills from `skills/<slug>/manifest.yaml` and `.clinerules/<slug>.md` |
| `halalchain_get_skill` | `slug` (required) | One skill's manifest or steering document |
| `halalchain_get_controls` | none | The controls manifest, plus every path probed |
| `halalchain_get_service_manifest` | none | `service-manifest.yaml` |
| `halalchain_list_governance_workflows` | `project` | tawheed workflow files and `*StateMachine*.cs` types |
| `halalchain_verify_no_verdict_authority` | none | Verdict-authority scan: files scanned, patterns, candidates |

Each governance tool names the concrete paths it probed when it finds nothing,
so an empty result is distinguishable from a misconfigured solution root.

---

## Resources

`resources/list` returns **10 resources** against this repository: six
synthetic plus one per root file that exists on disk.

| URI | Mime | Contents |
|---|---|---|
| `halalchain://architecture` | `text/markdown` | Rendered system diagram |
| `halalchain://solution/projects` | `application/json` | Every `.csproj` |
| `halalchain://solution/pages` | `application/json` | Every `@page` route |
| `halalchain://solution/services` | `application/json` | Every `Services/` class |
| `halalchain://docs/index` | `application/json` | Every markdown file under `docs/` |
| `halalchain://runbooks/index` | `application/json` | Runbooks under `docs/runbooks/` |
| `halalchain://file/<root-file>` | by extension | `AGENTS.md`, `Modelfile`, `README.md`, `service-manifest.yaml` |

File-backed reads are allowlisted twice: the extension must be one of `.md`,
`.yaml`, `.yml`, `.json`, `.txt`, `.sln`, `.csproj`, `.props`, `.cs`, `.razor`,
`.py`, `.sol`, `.jsonc`, `.http`, and the top-level segment must be a root
file or one of `docs`, `.halalchain`, `infrastructure`, `scripts`. A `..`
segment is rejected before any filesystem access, and the fully resolved path
is re-checked against the solution root afterwards.

`resources/templates/list` returns an empty array: every resource this server
exposes is fully enumerable, so there is nothing to parameterize.

---

## Prompts

`prompts/list` returns **5 prompts**, each carrying the platform invariant
("AI discovers and interprets evidence. Deterministic systems decide…") so a
model that loads one is steered away from answering a compliance question it
has no right to answer.

| Prompt | Arguments | Purpose |
|---|---|---|
| `halal-compliance-review` | `subject` (required), `jurisdiction`, `evidence` | Structures an evidence review and routes the outcome through the Policy Engine |
| `policy-engine-audit` | `module` | Audits a module for determinism violations |
| `incident-triage` | `symptom` (required), `service` | Symptom → runbook → first diagnostic step |
| `module-onboarding` | `module` (required), `purpose` | Generates wiring for a new API module |
| `mcp-capability-audit` | none | Finds questions the MCP surface cannot yet answer |

`completion/complete` serves argument suggestions for `jurisdiction`
(`MY`, `ID`, `SG`, `BN`, `GCC`, `EU`) and `service`.

---

## Configuration

### Environment variables

| Variable | Purpose |
|---|---|
| `HALALCHAIN_SOLUTION_ROOT` | Filesystem root for every scan. Falls back to a bound `SolutionRoot`, then to walking up from the binary for `HalalChain.Platform.sln`, then to the assembly directory. |
| `HALALCHAINHALALCHAIN__<SECTION>__<KEY>` | Overrides any `HalalChainOptions` value. See the double-prefix note below. |

Options bind from the `HalalChain` configuration section. The environment
provider is registered with the prefix `HALALCHAIN`, and the section name is
also `HalalChain`, so the **prefix appears twice**:

```bash
HALALCHAINHALALCHAIN__MCP__PAGESIZE=5          # -> McpOptions.PageSize = 5
HALALCHAINHALALCHAIN__MCP__MAXREQUESTBYTES=64  # -> requests over 64 bytes are rejected
HALALCHAINHALALCHAIN__MCP__ENABLEPROMPTS=false # -> prompts capability omitted
HALALCHAINHALALCHAIN__SERVICES__TAWHEED=http://127.0.0.1:9
```

The single-underscore and colon forms documented in `.env.example`
(`HALALCHAIN:MCP:PAGESIZE`) do **not** bind — they are read by nothing in this
process.

### Options and defaults

| Option | Default |
|---|---|
| `Mcp:MaxRequestBytes` | 1 048 576 |
| `Mcp:ToolTimeoutSeconds` | 30 |
| `Mcp:ClientRequestTimeoutSeconds` | 60 |
| `Mcp:MaxResourceBytes` | 524 288 |
| `Mcp:PageSize` | 50 |
| `Mcp:LogLevel` | `Information` |
| `Mcp:EnableResources` / `EnablePrompts` / `EnableCompletions` / `EnableProgress` / `EnableLogging` / `EnableSampling` / `EnableElicitation` | all `true` |
| `Services:PlatformApi` | `http://localhost:5001` |
| `Services:HalalChain` | `http://localhost:5200` |
| `Services:Marketplace` | `http://localhost:5201` |
| `Services:Tawheed` | `http://localhost:8000` |
| `Services:AiInference` | `http://localhost:7071` |

There is no `HALALCHAIN_DEBUG` switch. Verbosity is controlled by
`Mcp:LogLevel`.

### Why `HALALCHAIN_SOLUTION_ROOT` matters

Without it the scanners walk a root discovered from the binary's location. In
a published deployment that can be the runtime folder, which contains no
`.csproj` files and yields empty results. Set it in the client's `env` block.

---

## Protocol details

| Aspect | Value |
|---|---|
| Transport | stdio |
| Framing | newline-delimited JSON |
| JSON-RPC version | 2.0 |
| Supported revisions | `2025-06-18`, `2025-03-26`, `2024-11-05` |
| Server name | `halalchain-mcp` |
| Server title | `HalalChain Platform` |
| Server version | `2.0.0` |

Capabilities depend on the negotiated revision. On `2025-06-18` the server
advertises `tools`, `prompts`, `resources`, `logging`, and `completions` (each
optional one gated on its `Enable*` option). On `2024-11-05` it advertises
`tools` only, and `tools/list` omits `annotations` — a client that negotiated
the old revision would reject those fields.

Methods handled:

```
initialize          ping                tools/list           tools/call
resources/list      resources/templates/list                 resources/read
prompts/list        prompts/get         completion/complete  logging/setLevel
notifications/*     (acknowledged, never answered)
```

Server-initiated traffic — progress, log forwarding, sampling, elicitation —
is emitted only when the client declared the matching capability *and* the
corresponding option is on.

Batches are supported: an array frame is answered with a single response
array, and an all-notification batch produces no output.

Log output goes to **stderr**, never stdout.

---

## Project structure

```
HalalChain.Mcp/
├── HalalChain.Mcp.csproj          # net10.0 Exe, TreatWarningsAsErrors
├── Program.cs                     # stdio host + JSON-RPC dispatcher
├── AssemblyInfo.cs                # InternalsVisibleTo the architecture tests
├── ServiceCollectionExtensions.cs # DI wiring and the 25 tool registrations
├── Abstractions/                  # Interfaces for every service
├── Configuration/                 # HalalChainOptions / McpOptions / ServiceEndpoints
├── Models/                        # Protocol DTOs, content blocks, JSON options
├── Services/                      # Platform, introspection, governance,
│                                  # resource, prompt, and transport services
├── Tools/                         # One class per MCP tool, plus ITool
├── README.md                      # This file
├── bin/                           # Build output (gitignored)
└── obj/                           # Intermediate (gitignored)
```

The three invariants the source obeys:

- `Program.cs` owns the stdout stream; nothing else writes to it.
- `Tools/` contains no code that mutates filesystem state.
- No file under `HalalChain.Mcp/` references a database, RPC URL, contract
  address, or API key.

---

## Probe test

A full round-trip from the repository root:

```bash
HALALCHAIN_SOLUTION_ROOT=$PWD dotnet build HalalChain.Mcp -c Release --nologo

printf '%s\n' \
  '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{"logging":true},"clientInfo":{"name":"probe","version":"1.0"}}}' \
  '{"jsonrpc":"2.0","method":"notifications/initialized"}' \
  '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}' \
  '{"jsonrpc":"2.0","id":3,"method":"resources/list","params":{}}' \
  '{"jsonrpc":"2.0","id":4,"method":"prompts/list","params":{}}' \
  | ./HalalChain.Mcp/bin/Release/net10.0/HalalChain.Mcp 2>/dev/null
```

**Expected:**

- Exit code 0.
- `id:1` → `result.serverInfo.name == "halalchain-mcp"`,
  `result.protocolVersion == "2025-06-18"`, and capabilities
  `tools`, `prompts`, `resources`, `logging`, `completions`.
- `id:2` → **25 tools**, all `readOnlyHint: true`, `nextCursor: null`
  (25 < the default page size of 50).
- `id:3` → **10 resources** (6 synthetic + the 4 root files that exist here).
- `id:4` → **5 prompts**.
- Nothing on stdout except those four response frames.

---

## Development

### Adding a tool

1. Create a class in `Tools/` implementing `ITool`. Provide `Name`,
   `Description`, `InputSchema`, and `Annotations`; override
   `ExecuteDetailedAsync` only when the tool must report `isError` or publish
   structured content.
2. Give it a `halalchain_*` name. Names without that prefix are not exposed.
3. Register it in `ServiceCollectionExtensions.AddHalalChainMcp` with
   `RegisterTool<TTool>(services)`. Registration is explicit — there is no
   attribute-based discovery.
4. Add the name to `.autoclaw/mcp/policies/tool-allowlist.yaml`.
   `AutoclawStructureTests.Allowlist_Matches_Exposed_Tools` cross-checks that
   file against `HalalChain.Mcp/Tools/*.cs` in both directions and fails the
   build on any drift.
5. Update the tool tables in this README.

### Iterating on a tool

```bash
dotnet build HalalChain.Mcp -c Release --nologo
```

Restart the MCP client (VS Code: `MCP: Restart Server`; Cursor: toggle the
server off and on in the MCP panel), then re-probe.

### Testing

```bash
dotnet test HalalChain.Mcp.Tests/HalalChain.Mcp.Tests.csproj -c Release --nologo
```

70 tests across three files: `McpTests.cs` (JSON-RPC shape, registry,
execution, configuration), `Mcp2026Tests.cs` (negotiation, annotations,
resources, prompts, options, log levels), and `IntrospectionToolsTests.cs`
(which pins the tool count at 25 and executes every introspection tool
against the real repository).

Architecture guards live in `HalalChain.Architecture.Tests` and must stay
green:

- `McpVerdictAuthorityTests` — no MCP source file declares a verdict-authority
  identifier.
- `Rules/AutoclawStructureTests` — the tool allowlist, the denylist, the
  controls manifest, and tenant-isolation declarations.
- `DependencyRulesTests` — `HalalChain.Mcp` must not reference the API project.

### Code style

- `TreatWarningsAsErrors` is on. Do not suppress without cause.
- Prefer `record` DTOs for tool responses.
- Keep stdout writes in `Program.cs` only.
- Log via `ILogger` — it goes to stderr, which is what you want.

---

## Troubleshooting

### The client shows the server as **Stopped**

Check in this order:

1. Does the binary exist at the configured path?
2. Does `dotnet build -c Release` succeed with 0 warnings?
3. Read the server's stderr — most clients capture it under a "Show logs" menu
   on the server row.
4. Confirm the config file uses the right top-level key for your client
   (`servers` for VS Code, `mcpServers` for Cursor).

### `tools/list` returns fewer tools than expected

Check `Mcp:PageSize`. The list is cursor-paginated and the response carries
`nextCursor`; a client that ignores the cursor silently sees only the first
page.

### Every introspection tool returns an empty list

`HALALCHAIN_SOLUTION_ROOT` is unset or points at a directory with no `.csproj`
files. `PlatformDataService` falls back to walking up from the binary for
`HalalChain.Platform.sln`, which works under `dotnet run` and fails for a
published deployment. The tools say so in their output and name the variable
to set.

### `halalchain_health` reports everything unreachable

The probes target loopback endpoints defined in `Services:*`. Bring the stack
up:

```bash
docker compose up -d
```

To point the tool elsewhere, override an endpoint — note the doubled prefix:

```bash
HALALCHAINHALALCHAIN__SERVICES__TAWHEED=http://127.0.0.1:9000
```

### An option override appears to do nothing

Use the `HALALCHAINHALALCHAIN__<SECTION>__<KEY>` form. The colon form in
`.env.example` is not read by this process.

### stdout contains non-JSON-RPC lines

That is a bug, and it should be impossible: `Program.cs` captures the raw
stdout stream and points `Console.Out` at stderr before any other code runs. If
a client reports corruption, capture the offending bytes and report them.

### A tool call returns `isError: true` instead of failing

That is by design. `halalchain_find_type`, `halalchain_get_adr`, and
`halalchain_get_skill` publish a readable explanation when a required argument
is missing or a lookup finds nothing. The message names what was expected.

---

## Architecture invariants

**1. No verdict authority.**
No type in `HalalChain.Mcp` may name a capability whose purpose is to decide
compliance. The forbidden identifiers are `SetVerdict`, `WriteVerdict`,
`OverrideTawheed`, `BypassTawheed`, `ApproveCertificate`, `RejectCertificate`,
`DecideCompliance`, `ForceHalal`, `MarkHalal`, `SetHalalStatus`. The list
lives in `GovernanceService.ForbiddenPatterns`; `McpVerdictAuthorityTests`
asserts an independent baseline against it, so shrinking the list fails the
build rather than silently reducing coverage. Files that must name the
forbidden identifiers — the guard itself and its test — carry the
`halalchain:verdict-authority-guard` marker, and the count of skipped files is
published in the tool's output rather than hidden.

**2. The tool surface is explicit.**
Tools are registered one line each in `ServiceCollectionExtensions`, and
`.autoclaw/mcp/policies/tool-allowlist.yaml` is cross-checked against the
source in both directions. A tool shipped without a policy entry, or a policy
entry without a tool, fails the build.

**3. No secret exposure.**
No source file under `HalalChain.Mcp/` contains a credential-shaped literal
(`sk-`, `ghp_`, `0x` + 64 hex, `postgres://`) or logs a value of that shape.

---

## License

Proprietary & Confidential. Copyright © 2024–2026 HalalChain by Kurnia Kadir. All
rights reserved. See [`LICENSE`](../LICENSE) at the repository root for the
full terms.

This Software is licensed, not sold. Unauthorized access, use, disclosure, or
distribution is strictly prohibited and may result in civil and criminal
penalties under Malaysian law.

Third-party components bundled or referenced by this project (`.NET Runtime`,
`Microsoft.Extensions.*`, and the Model Context Protocol specification) remain
subject to their own licenses. See Schedule B of the root `LICENSE` for the
complete list.

---

**HalalChain — AI discovers. Policy decides. Verdicts are deterministic, never overridden by LLM.**
