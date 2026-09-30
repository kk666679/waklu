# HalalChain-Cli

Operator CLI. Node 22+, npm 10+ (this repo is an npm workspace), TypeScript 6.0.

## Build

```bash
npm install          # from the repository root (npm workspace)
npm run typecheck
npm run build
npm test
```

Run the scripts through npm so they resolve from the workspace root:

```bash
npm run typecheck --workspace HalalChain-Cli
npm test --workspace HalalChain-Cli
```

## Conventions

- Commands live in `src/commands/`, one file per command, exported as
  `create<Name>Command()`, and registered in `src/index.ts`.
- Shared utilities live in `src/lib/`.
- The agent loop and Intent wiring live in `src/runtime/`.
- Skills live in `skills/`, one directory per skill, each with `SKILL.md`.
- No browser packages. The CLI has no DOM. CI fails if any React or
  TanStack React package appears in `dependencies` or `devDependencies`.
- The MCP client is a process-level singleton. Never close it inside a
  command handler — register cleanup at process level instead.
- `src/lib/api-client.ts` is the only place that talks to `.halalchain/*`.
  Every route there is verified against the FastAPI services; do not add a
  call to an endpoint that does not exist in `docker-compose.yml`.

## Adding a command

1. Create `src/commands/<name>.ts` exporting `create<Name>Command(): Command`.
2. Register it in `buildProgram()` in `src/index.ts`.
3. Add a matching skill under `skills/halalchain-<name>/SKILL.md` if the
   command has non-obvious usage patterns.
4. Run `npm run intent:validate` before committing.
