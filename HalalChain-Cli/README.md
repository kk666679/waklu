# @halalchain/cli

> Operator CLI for the HalalChain platform — platform queries, evidence
> retrieval, vector search, and agent mode over MCP.
> **AI gathers evidence. Deterministic systems decide.**

## Install

The CLI is an npm workspace member. Install from the repository root:

```bash
npm install
npm run build --workspace HalalChain-Cli
node HalalChain-Cli/bin/halalchain.js --help
```

`bin/halalchain.js` prefers the compiled output in `dist/` and falls back to
running the TypeScript sources through `tsx`, so the CLI also works in a fresh
clone that has not been built yet.

## What it talks to

Every command is a thin client over services in this repository. The routes are
verified against the FastAPI sources, not guessed:

| Service | Default URL | Reached by |
|---|---|---|
| ai-inference (AI gateway) | `http://localhost:7071` | `embedding`, `classify`, `summarize`, `rerank`, `rag`, `llm`, `certificate`, `ingredient`, `server metrics` |
| tawheed (evidence + Policy Engine) | `http://localhost:8000` | `evaluate` |
| local-models | `http://localhost:8080` | `local` |
| agents (evidence collection) | `http://localhost:8081` | `agents` |
| Platform API / Marketplace / Web | `:5001` / `:5201` / `:5200` | `server status`, `env check` |
| MCP server | `http://localhost:5002/mcp` | `agent` |

Start the stack with `docker compose up --build`.

## Commands

| Command | Purpose | Mutates platform state? |
|---|---|---|
| `agent` | One agent turn over the MCP tool surface | no |
| `agents` | Evidence-collection workflows (`.halalchain/agents`) | no |
| `certificate` | Extract and check certificate documents | no |
| `classify` | Classify text (`halal`, `generic`, `risk`) | no |
| `config` | Manage `~/.halalchain/config.json` | local only |
| `embedding` | Generate, compare and batch embeddings | no |
| `env` | Generate `.env` files; check service health | local only |
| `evaluate` | **Deterministic** tawheed verdict for a product | delegates |
| `ingredient` | Parse ingredients; list flagged ones | no |
| `llm` | One-shot generation through the AI gateway | no |
| `local` | Locally hosted model service | no |
| `rag` | Add / search / clear the RAG store | local store only |
| `rerank` | Score passages; label them | no |
| `server` | Service health, start instructions, metrics | no |
| `summarize` | Summarise text or a file | no |
| `vector` | Semantic search and index stats | no |
| `skills` | List / validate / install the CLI skills | local only |
| `sandbox` | Run a script under the Node permission model | local only |

## Sandbox

`sandbox` runs a script with Node's permission model enabled. Verified
behaviour:

- the filesystem is confined to a throwaway temp directory plus roots you
  pass with `--read` / `--write`;
- child processes, worker threads, native addons, WASI and the inspector are
  denied;
- the parent environment is **not** inherited — only `--env KEY=VALUE` pairs
  reach the script, and credential-shaped names are refused outright.

```bash
halalchain sandbox doctor
halalchain sandbox run --inline "console.log(process.version)"
halalchain sandbox run ./job.mjs --timeout 10000 --env MODE=demo
halalchain sandbox clean
```

> **There is no network isolation.** The permission model has no egress
> control, so a sandboxed script can still open sockets. Treat it as a
> filesystem/process jail, not an air gap — which is also why `--env` refuses
> keys that look like credentials.

`doctor` reports what the current runtime can enforce; the flag moved from
`--experimental-permission` to `--permission` in Node 23.5, and the CLI
probes for whichever the host accepts.

## Guarantees

- **No verdict authority.** The CLI never decides compliance. `evaluate`
  forwards a case to tawheed and renders the response. There is no `approve`,
  `reject` or `set-verdict` command, and `test/commands.test.ts` fails the build
  if one appears.
- **No invented endpoints.** `src/lib/api-client.ts` is the only module that
  talks to `.halalchain/*`, and every route there maps to a real FastAPI route.
- **No secret leakage.** Every key in `SECRET_KEYS` is masked by `config list`,
  `config show-services` and `env`.

## Configuration

`halalchain config init` writes `~/.halalchain/config.json` with mode `0600`.
Resolution order, last wins: built-in defaults → the user store → environment
variables.

| Variable | Config key |
|---|---|
| `HALALCHAIN_API_URL` | `platform-api.url` |
| `MARKETPLACE_URL` | `marketplace.url` |
| `HALALCHAIN_WEB_URL` | `halalchain.url` |
| `AI_INFERENCE_URL` | `ai-inference.url` |
| `TAWHEED_URL` | `tawheed.url` |
| `LOCAL_MODELS_URL` | `local-models.url` |
| `AGENTS_URL` | `agents.url` |
| `HALALCHAIN_MCP_URL` | `mcp.url` |
| `HALALCHAIN_MCP_TOKEN` | `mcp.token` |
| `JWT__KEY` | `jwt.key` |
| `HALALCHAIN_JURISDICTION` | `tawheed.jurisdiction` |
| `HALALCHAIN_POLICY_VERSION` | `tawheed.policy-version` |

## Development

```bash
npm install                            # from the repository root
npm run typecheck --workspace HalalChain-Cli
npm test --workspace HalalChain-Cli
npm run build --workspace HalalChain-Cli
```

The suite runs through `tsx`; `tsc --noEmit` type-checks the shipped code.
See `AGENTS.md` for conventions.

## License

Apache-2.0
