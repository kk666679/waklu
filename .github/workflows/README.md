# `.github/workflows/`

Continuous integration, delivery, and dependency-reproducibility gates for the
HalalChain monorepo.

## Purpose

Six workflows cover the five toolchains in this repository — .NET, Node, Python,
Solidity-adjacent Docker images, and the observability configuration — plus the
release pipeline that signs and promotes images.

## Structure

```
.github/workflows/
├── ci.yml                     # The main gate: SCSS, docs, .NET build+test, Python tests, Docker build
├── cli.yml                    # HalalChain-Cli: typecheck, skill validation, tests, two guards
├── python-locks.yml           # Re-compiles 8 Python locks and byte-diffs them
├── observability.yml          # amtool check-config + AlertManager secret wiring
├── generate-admin-pages.yml   # Runs the Radzen/OpenAPI page generator and commits output
└── release.yml                # Build, push to GHCR, cosign sign, Helm lint/package, staged promotion
```

## Dependencies

`actions/checkout@v4`, `actions/setup-dotnet@v4`, `actions/setup-python@v5`,
`actions/setup-node@v4`, `docker/setup-buildx-action@v3`,
`docker/login-action@v3`, `docker/build-push-action@v6`,
`astral-sh/setup-uv@v4` (pinned to `version: "0.12.9"`),
`sigstore/cosign-installer@v3.7.0`, `azure/setup-helm@v4`,
`prom/alertmanager:v0.28.0`.

## Workflows

### `ci.yml` — HalalChain CI

Triggers: push to `main`/`develop`, pull request to `main`. Permissions:
`contents: read`.

| Job | Steps |
| --- | --- |
| `build-and-test` | checkout → setup .NET `10.0.x` → setup Node 22 → `npm install` → `npm run scss:build` → `python3 infrastructure/scripts/check-docs.py` → `dotnet restore` → `dotnet build -c Release` → `dotnet test --no-build -c Release` |
| `python-tests` | Matrix `[ai-inference, tawheed, local-models]`, `fail-fast: false`. Per service: install deps (base lock for `local-models`, `requirements.txt` otherwise), `../requirements-test.txt`, `../_shared`; then assert `pytest`/`fastapi` (and `prometheus_client`/OTLP for non-`local-models`) are importable; then `python -m pytest` |
| `security-scan` | `needs: build-and-test`. `dotnet list … package --vulnerable --include-transitive` |
| `docker-build` | `needs: build-and-test`, `if: github.ref == 'refs/heads/main'`. `docker build -t halalchain/platform-api:$SHA .` then `docker compose build` |

The import-verification step exists because a comment-only `requirements.txt`
previously installed nothing and surfaced much later as
`pytest: command not found`.

### `cli.yml` — CLI

Triggers: PR touching `HalalChain-Cli/**`, `package.json`, or `package-lock.json`.

Steps: `npm ci` → `npm run typecheck --workspace HalalChain-Cli` →
`npm run intent:validate` → `npm test --workspace HalalChain-Cli` → two guards:
no browser-only packages in `package.json`, and `src/runtime/chat.ts` must not
call `closeMCPClient`.

A comment records that this workflow previously installed with
`pnpm --frozen-lockfile` against files that do not exist in this repository.

### `python-locks.yml` — Verify Python Locks

Matrix of 8 locks: `base.txt`, `mcp.txt`, `dev.txt`, `ai-inference.txt`,
`tawheed.txt`, `agents.txt`, `local-models.txt`, `local-models-gguf.txt`.

Each entry re-runs the `uv pip compile` command recorded in that lock's own
header, normalises the `-o` path the tool echoes into the header, and
`diff -u`s the result against the committed file. On mismatch the job emits a
`::error::` annotation with the exact regeneration command.

`uv` is pinned to `0.12.9` **on purpose** — an unpinned `uv` rewrites locks
wholesale and produces false failures.

Not verified: `vector.txt` (embeds a machine-specific `file:///` path) and
`llm.txt` (documented as deliberately unverifiable).

### `observability.yml` — Observability Stack

Triggers: push/PR touching `infrastructure/**`, `docs/runbooks/**`, or
`docs/slo.yaml`.

Substitutes a placeholder Slack webhook into `alertmanager.yml`, runs
`amtool check-config` against `prom/alertmanager:v0.28.0`, then asserts every
`*_file:` secret path in the config has a matching entry in
`infrastructure/scripts/write-alertmanager-secrets.py`. A missing secrets
directory is expected and is not an error.

### `generate-admin-pages.yml` — Generate Admin Pages

Installs `Swashbuckle.AspNetCore.Cli`, then builds with `-p:GenerateApiPages=true`,
which activates three gated MSBuild targets in `HalalChain.Platform.Api`:

- `GenerateRadzenApiPages` — runs `Radzen.Blazor.Api.Generator.csproj` over the
  Radzen assembly to emit Radzen-wrapped API pages
- `GenerateOpenApiSpec` — `dotnet swagger tofile … v1 --validate`
- `BuildAdminPages` — runs the generator with `CodeGen/generator.json`, writing
  to `HalalChain.Web/Components/Admin/Generated`

It then verifies the generated output compiles, and commits to `main` if anything
changed, otherwise annotating the PR with the local equivalent command.

### `release.yml` — HalalChain Release

Permissions: `contents: read`, `packages: write`, `id-token: write`,
`deployments: write`.

Four gated jobs:

1. `build-and-publish` — buildx, GHCR login as the actor, build and push images,
   install cosign and sign them, install Helm, `helm lint`,
   `helm package --destination ./dist`
2. `deploy-staging` — `needs: build-and-publish`
3. `smoke-gate` — `needs: deploy-staging`. Health checks before promotion
4. `deploy-production` — `needs: smoke-gate`. Deploys, then records deployment
   metadata

This mirrors the promotion flow in
[`deploy/DEPLOYMENT-STRATEGY.md`](../../deploy/DEPLOYMENT-STRATEGY.md).

## Usage

```bash
# Replicate ci.yml locally
npm install
npm run scss:build
python3 infrastructure/scripts/check-docs.py
dotnet restore HalalChain.Platform.sln
dotnet build   HalalChain.Platform.sln -c Release
dotnet test    HalalChain.Platform.sln -c Release --no-build

# One Python service
cd .halalchain/tawheed && python -m pytest

# The page generator
dotnet build HalalChain.Platform.Api -p:GenerateApiPages=true
```

## Configuration

`ci.yml` sets `DOTNET_VERSION: 10.0.x`, `NODE_VERSION: 22`, and
`SOLUTION: HalalChain.Platform.sln`. There is no repository-level secrets file
here; the release workflow consumes GHCR and Kubernetes credentials from the
environment.

## Integration

- Validates the artifacts produced by every build directory in the repository.
- `release.yml` produces the images [`deploy/`](../../deploy/README.md) deploys.
- `generate-admin-pages.yml` writes into `HalalChain.Web/Components/Admin/Generated`,
  making a UI project a consumer of an API project's MSBuild targets.

## Notes / Limitations

- `HalalChain.Automation`, `HalalChain.DataFlow`, and `HalalChain.Storage` have
  no dedicated workflow. `HalalChain.DataFlow` and `HalalChain.Storage` are
  built and tested by `ci.yml` through the solution; `HalalChain.Automation` is
  in the solution but has no tests to run.
- `.halalchain/agents` and `.halalchain/_shared` are not in the `python-tests`
  matrix.
- No workflow runs `deploy/helm/halalchain/tests/*.sh`. Chart properties are
  validated by `helm lint` in `release.yml` and by hand otherwise.
- No Solidity/Foundry job runs, despite `HalalChain.Platform.Contracts/contracts/`
  containing a Foundry project.