# @halalchain/cli

Operator CLI for the HalalChain platform. Talks to the platform over HTTP
and MCP. Never issues a halal verdict — verdicts come from `tawheed`.

## Install

```bash
corepack enable
pnpm install
pnpm build
```

## Usage

```bash
halalchain agent "which vendors have certificates expiring this month?"
halalchain --help
```

## Environment

| Variable | Purpose |
|---|---|
| `HALALCHAIN_MCP_URL` | MCP server endpoint, e.g. `http://localhost:5002/mcp` |
| `HALALCHAIN_MCP_TOKEN` | Bearer token, if the MCP server requires auth |
| `HALALCHAIN_API_URL` | Platform REST API base URL |
| `OPENAI_API_KEY` | Required by the agent command |

## Skills

Skill definitions live in `skills/`. Validate with:

```bash
pnpm intent:validate
```

Install into your agent config with:

```bash
pnpm intent:install
```

## Design notes

- The MCP client is a process-level singleton (`src/lib/mcp-client.ts`).
  It is never closed between agent turns. See the file header for why.
- The agent command does not decide policy. If asked "is this halal?",
  it calls the platform's policy tool and reports the output.
- The CLI is a separate runtime from `.halalchain/agents/`. Its traces
  stay local; they do not land in the platform's `AgentTrace` chain.
