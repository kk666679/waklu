# HalalChain.Mcp.Tests

xUnit tests for the Model-Context-Protocol server in `HalalChain.Mcp/`.

## Purpose

These tests exercise the MCP server's protocol surface and its introspection
tools — the code that reads the repository and the running services so an agent
can navigate the platform.

## Layout

```
HalalChain.Mcp.Tests/
├── McpTests.cs             # Protocol-level tests: initialize, tools/list,
│                           #   tools/call, resources, prompts
├── Mcp2026Tests.cs         # Tests for the 2026 protocol revision surface
└── IntrospectionToolsTests.cs  # Behaviour of the code-introspection tools
```

## Project references

- `HalalChain.Mcp`
- `HalalChain.Platform.Contracts`

`Moq` 4.20.72 is used for the service doubles the tools depend on
(`IToolRegistry`, `IResourceRegistry`, `IPromptRegistry`, `IClientMessenger`,
`IHealthCheckService`).

## Run

From the repository root:

```bash
dotnet test HalalChain.Mcp.Tests -c Release
```

## Related Components

- [HalalChain.Mcp](../HalalChain.Mcp/README.md) — the server under test
- [HalalChain.Architecture.Tests](../HalalChain.Architecture.Tests/README.md) — `Mcp_ShouldNotReference_ApiProject` and `McpVerdictAuthorityTests`
- [HalalChain-Cli](../HalalChain-Cli/README.md) — the Node client that consumes this server

## Notes / Limitations

- `HalalChain.Architecture.Tests/McpVerdictAuthorityTests.cs` scans MCP *sources*
  for verdict-authority identifiers. That is a different kind of check from the
  behavioural tests here, and both must pass.