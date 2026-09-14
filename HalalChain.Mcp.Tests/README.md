# HalalChain.Mcp.Tests

xUnit tests for the Model-Context-Protocol server in `HalalChain.Mcp/`.

## Layout

```
HalalChain.Mcp.Tests/
└── McpTests.cs   # xUnit test suite for the MCP host
```

## Project references

- `HalalChain.Mcp`
- `HalalChain.Platform.Contracts`

## Run

From the repository root:

```bash
dotnet test HalalChain.Mcp.Tests -c Release
```
