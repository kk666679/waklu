# HalalChain-Cli

Node 22 operator and developer CLI for the HalalChain platform. Exposed as
the `halalchain` binary.

## Layout

```
HalalChain-Cli/
├── bin/halalchain.js   # Entry point
├── commands/           # Individual subcommand modules (Commander.js)
└── lib/                # Shared helpers (API client, config, auth, logger, …)
```

## Available subcommands

`ai-context`, `batch`, `classify`, `config`, `embedding`, `env`, `evaluate`,
`ingredient`, `llm`, `ml`, `monitor`, `pipeline`, `rag`, `server`,
`summarize`, `vector`.

## Usage

The CLI exposes a `halalchain` binary (`package.json` -> `bin/halalchain`).

From the repository root:

```bash
node HalalChain-Cli/bin/halalchain.js --help
```

Or via the npm script:

```bash
npm run halalchain:cli -- --help
```

## Configuration

The CLI reads configuration via the canonical `ConfigManager` exposed by
`lib/store.js`. Secret values (`jwt.key`, `ai-inference.api-key`) are
redacted in every printed view and the JSON file is created with `0600`
permissions. Service endpoints can be overridden through environment
variables documented in the root `.env.example`.
