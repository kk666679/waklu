# HalalChain-Cli

Operator CLI. Node 22+, pnpm 10+, TypeScript 6.0.

## Build

```bash
pnpm install
pnpm build
pnpm test
```

## Conventions

- Commands live in `src/commands/`, one file per command, named `<command>.ts`.
- Shared utilities live in `src/lib/`.
- The agent loop and Intent wiring live in `src/runtime/`.
- Skills live in `skills/`, one directory per skill, each with `SKILL.md`.
- No browser packages. The CLI has no DOM. CI fails if any React or
  TanStack React package appears in `dependencies` or `devDependencies`.
- The MCP client is a process-level singleton. Never close it inside a
  command handler — register cleanup at process level instead.

## Adding a command

1. Create `src/commands/<name>.ts` exporting a `Command` from `commander`.
2. Register it in `src/index.ts`.
3. Add a matching skill under `skills/halalchain-<name>/SKILL.md` if the
   command has non-obvious usage patterns.
4. Run `pnpm intent:validate` before committing.
