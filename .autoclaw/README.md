# `.autoclaw`

Agent-orchestration framework for HalalChain, mapped to the platform
architecture. The layout is not decorative: each directory boundary is the
physical expression of one of the fifteen principles in
[PRINCIPLES.md](PRINCIPLES.md), and the sealed boundary is enforced by a test.

## The invariant

> AI gathers evidence. Deterministic systems decide.

Everything here follows from that. `evidence/` is a first-class domain because
evidence precedes decision. `tawheed/` is sealed because the Policy Engine is
the only component allowed to conclude. `anchors/` produces a hash receipt and
nothing else, because a chain proves integrity, not authority. There is no
`verdicts/` directory to write to.

## Start here

| File | What it is |
|------|------------|
| [PRINCIPLES.md](PRINCIPLES.md) | P1–P15, the source of truth |
| [MANIFEST.yaml](MANIFEST.yaml) | machine-readable index; what a bootstrap script reads |
| [CONTROLS.yaml](CONTROLS.yaml) | control catalogue: which mechanism enforces which principle |
| [tawheed/README.md](tawheed/README.md) | the sealed deterministic boundary |
| [compliance/README.md](compliance/README.md) | deterministic rules, consumed only by tawheed |
| [governance/README.md](governance/README.md) | certification + skill state machines |
| [mcp/README.md](mcp/README.md) | gateway config and the tool allow/deny boundary |
| [tenants/README.md](tenants/README.md) | isolation model |

## The six HalalChain domains

`participants`, `evidence`, `tawheed`, `governance`, `byok`, `tenants`.
`MANIFEST.yaml` declares which of them accept writes and from whom. `tawheed`
declares `writers: []`.

## Layout

```
agents/            participant personas; each declares can and cannot
anchors/           hash receipts (formerly blockchain/)
autobuild/         build directives, harnesses, pipelines
byok/              bring-your-own-key lifecycle; sealed refs only
cloud/             auth, federation, observability
comms/             messaging, signals
compliance/        deterministic rules, jurisdictions, schemes
connectors/        one folder per external provider class
contracts/         API and event schemas between subsystems
daemon/            health, ipc, lifecycle
deploy/            topology, including residency-aware zones
diagnostics/       classification, correlation, remediation
eval/              fixtures, scorers, reporters, traces
evidence/          lifecycle, types, chain-of-custody, capsule
fabric/            agent fabric: bus, registry, reputation, routing
fleet/             seats, presence, election, federation
governance/        certification + skill state machines
harness/           runtime harness
hooks/             pre-commit, pre-push, post-merge, mcp-tool-invoked
intelligence/      kg, memory, vector, metrics, sources
kg/, memory/       aliases to intelligence/kg and intelligence/memory
mcp/               MCP gateway: servers, clients, policies, audit
observability/     redaction, trace fields, dashboards
orchestrator/      audit, comms, sprints
participants/      registry cache, capabilities, delegated authority
plugins/           named tool bundles
prompts/           HalalChain-specific prompts
reference/         API contracts, ingredients, standards, jurisdictions
safety/, spine/, steering/, vector/, runtime/, schemas/, git/
skills/            published | staging | suspended | implementations
tawheed/           SEALED. Nothing writes here.
tenants/           isolation keys and residency policies
workflows/         the only caller permitted to invoke tawheed
```

`kg/` and `memory/` are aliases. The bootstrap creates them as directories
holding a `POINTER.md` by default; pass `-JunctionAliases` to make them real
NTFS junctions. The default is deliberate — git records a junction as a
gitlink rather than tracking its contents, which would break every other clone.

## Setup

```bash
./scripts/init-autoclaw.ps1
```

Idempotent. Creates the tree, the `tawheed/SEALED` marker, `.gitkeep`
placeholders in empty directories, and the MCP audit sink. Re-running it does
not clobber content. It exits non-zero if the tree exists but the required
artifacts do not, so a half-built scaffold is loud rather than quiet.

## Enforcement

Three layers, and they are deliberately redundant:

1. **Pre-commit** — `hooks/pre-commit/run.sh` scans for verdict-authority
   identifiers and for secrets in staged files. Install with
   `git config core.hooksPath .githooks`, or symlink
   `.autoclaw/hooks/pre-commit/run.sh` into `.git/hooks/`.
2. **MCP boundary** — `hooks/mcp-tool-invoked/check.sh` refuses any tool not on
   `mcp/policies/tool-allowlist.yaml` before the request reaches
   `HalalChain.Mcp.exe`. The server does not implement the forbidden tools
   either; two independent layers agree.
3. **Architecture tests** —
   `HalalChain.Architecture.Tests/Rules/AutoclawStructureTests.cs` asserts the
   seal, the forbidden-pattern boundary, manifest presence, and the tenancy and
   anchor invariants. Runs with `dotnet test`.

The set of tools the allowlist names is the set `HalalChain.Mcp` actually
exposes. If the server grows a tool, the allowlist must grow with it — the
architecture test cross-checks both directions.

## Reading order for a new agent

1. Your persona in `agents/`. Declare `can` and `cannot`; `cannot` is the part
   that gets enforced.
2. The MCP tools you may call — `mcp/policies/tool-allowlist.yaml`, filtered
   by your agent name.
3. The skills you may use — `skills/published/`. `staging/` is inert.
4. The workflow that will invoke you — `workflows/`. Workflows are the only
   thing permitted to call tawheed, and you are not a workflow.
