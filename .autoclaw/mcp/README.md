# `.autoclaw/mcp/` — MCP Gateway

Binds the agent fleet to `HalalChain.Mcp`. The server exposes eight read-only
tools. This directory decides which of them any given caller may invoke, at what
rate, and records what happened.

## Layout

```
servers/     server definitions (halalchain.yaml, _template.yaml)
clients/     which client can launch the server, and which agents it drives
policies/    allowlist, denylist, rate limits — the enforcement point
audit/       tool-invocations.jsonl and its schema
```

## The boundary

Two independent layers refuse verdict-writing calls:

1. **This policy layer.** `hooks/mcp-tool-invoked/check.sh` runs before the
   request is marshalled and exits 403 for anything not on the allowlist, on
   the denylist, or matching a denied prefix.
2. **The server.** `HalalChain.Mcp` implements none of the six denied tools.
   There is nothing to call even if the policy were bypassed.

The duplication is deliberate. A boundary that depends on the thing it is
protecting is not a boundary.

## Allowlist maintenance

`mcp/policies/tool-allowlist.yaml` and the server's tool list are cross-checked
in both directions by
`AutoclawStructureTests.Allowlist_Matches_Exposed_Tools`:

- a tool the server exposes but the allowlist omits fails the build
- a tool on the allowlist the server does not implement also fails the build

So the list cannot drift in either direction silently.

## Adding a tool

1. Implement it in `HalalChain.Mcp` as read-only.
2. Add it to `mcp/policies/tool-allowlist.yaml` with its rate limits.
3. Update `exposed_tools.readonly` in `mcp/servers/halalchain.yaml`.
4. Run `dotnet test HalalChain.Architecture.Tests`.

A tool that decides anything does not go in this directory. It belongs in the
Policy Engine, behind `tawheed/`.

## Audit

`mcp/audit/tool-invocations.jsonl` — one record per invocation, schema in
`mcp/audit/tool-invocations.schema.json`. Arguments are recorded as a sha256
over the canonicalised form, never raw: they carry tenant identifiers and
sometimes credential references. Tenant id comes from the `X-Tenant-Id` header
after validation against the auth claim, never from the argument payload.
