# Tech Debt Register

**Date:** 2026-09-26
**Operator:** Kilo coding agent
**Branch:** `main`
**Commit:** `1771f90` — Merge pull request #1 from supportHalal/diligence/2026-08-hardening
**Method:** read-only inspection + baseline build/test execution. No source changes in this pass.

Companion documents: `workspace-health.md` (disk/cleanup baseline),
`implementation-status.md` (migration phase tracker), `implementation-reconnaissance.md`,
`../due-diligence.md` (external diligence narrative).

## 0. Baseline verification (this pass)

| Check | Command | Result |
|---|---|---|
| Solution restore | `dotnet restore HalalChain.Platform.sln` | 11 projects restored |
| Solution build | `dotnet build HalalChain.Platform.sln -c Release` | **succeeded, 0 warnings, 0 errors** (5m52s) |
| Solution tests | `dotnet test HalalChain.Platform.sln -c Release --no-build` | **95/95 passed** — Platform.Tests 47, Architecture.Tests 32, Mcp.Tests 16 |
| Docs/service inventory gate | `python3 scripts/check-docs.py` | pass (9 services) |
| SCSS build | `npm run scss:build` | **fails — MODULE_NOT_FOUND** (TD-02) |
| CLI tests | `npm test` | fails locally (deps not installed); not wired into CI (TD-11) |
| Lint | `npm run lint` | **fails — `eslint: not found`** (TD-03) |
| Python tests | `pytest` (both services) | not runnable locally — pytest absent; only CI installs it (TD-09) |
| Container build | `dotnet restore`/`build` replay of root `Dockerfile` layer set | **fails — NETSDK1004** (TD-01) |

Codebase size: 636 `.cs`, 378 `.razor`, 160 `.py` files (excluding `bin`/`obj`/`.kilo`).

Legend — **S1** blocks deploy/CI, **S2** correctness or guardrail gap, **S3** hygiene/consistency,
**S4** documentation drift only. Effort is a rough engineering estimate, not a commitment.

## 1. Register

| ID | Sev | Area | Finding | Effort |
|---|---|---|---|---|
| TD-01 | S1 | Build/deploy | Root `Dockerfile` cannot build the API image — `HalalChain.Domain` and `HalalChain.Application` `.csproj` files are not in the restore layer | S |
| TD-02 | S1 | Build/CI | SCSS pipeline is dead: `npm run scss:build` and MSBuild `BuildScss` both point at scripts that do not exist | M |
| TD-03 | S2 | Quality gate | `npm run lint` is unrunnable (eslint not a declared dependency); no lint/format gate for any language | S |
| TD-04 | S2 | Solution hygiene | `HalalChain.Application` is absent from `HalalChain.Platform.sln` and from `HalalChain.Architecture.Tests` | S |
| TD-05 | S2 | Dead code | MediatR/CQRS layer (731 LOC) is registered but has zero dispatch sites; controllers query EF directly | M |
| TD-06 | S2 | Security | `AuthorizationBehavior` fails open for requests without a resolvable policy | S |
| TD-07 | S2 | Duplication | Three overlapping platform API clients (`Web/Services/PlatformApiClient`, `Platform.Http/Services/PlatformApiClient`, `Web/Services/WebApiClient` with 3 `NotSupportedException` stubs) | L |
| TD-08 | S2 | Security | Web UI persists the platform JWT in `localStorage`; Blazor `AuthService` remains a second, un-unified auth path | M |
| TD-09 | S2 | Supply chain | Python requirements pinned without hashes (contradicts `AGENTS.md`); duplicated package lists; Python tests absent from the documented local loop | S |
| TD-10 | S3 | Duplication | `.halalchain` services duplicate `observability.py` and `config.py` instead of using `_shared` | M |
| TD-11 | S2 | Test coverage | No test project for `HalalChain.Web` (4.1k LOC, 378 razor files repo-wide) or `HalalChain.Marketplace`; `npm test` not in CI | L |
| TD-12 | S2 | CI/CD | No coverage threshold, no failing vulnerability gate, no EF migration-drift check, Helm lint runs only on tag | M |
| TD-13 | S3 | Build weight | ~2.6 MB of EF migration `Designer.cs`/snapshot, 304 KB single-file `InitialSchema`, 71 KB hardcoded `TaxonomySeed.cs` | L |
| TD-14 | S4 | Docs drift | `workspace-health.md` / `implementation-status.md` / `AGENTS.md` / `Dockerfile` header all describe a repository state that no longer exists (see §3) | S |
| TD-15 | S3 | API consistency | `AiController` uses absolute `/api/ai/*` routes, bypassing the `/api/v1/*` convention used by the other 10 controllers | S |
| TD-16 | S3 | Workspace | `.kilo/` (26 MB of agent worktrees, including full copies of `Migrations`) is untracked and not ignored | S |
| TD-17 | S3 | Repo hygiene | 41 compiled `.css` files plus `app.css.map` are tracked; `.gitignore` only excludes `*.scss.map` | S |
| TD-18 | S2 | Deploy | Helm chart is a stub: `deployment.yaml` only, no Service/ConfigMap/Secret/probes, and references an image (`ghcr.io/supporthalal/halalchain`) that `release.yml` never publishes | M |

## 2. Detail and evidence

### TD-01 — Root `Dockerfile` cannot build the API image (S1, verified failure)

`HalalChain.Platform.Api.csproj` references `HalalChain.Domain` and `HalalChain.Application`
(both added after the Dockerfile was written), but the restore layer copies only
`HalalChain.Platform.Contracts`, `HalalChain.Platform.Http` and `${PROJECT}`.
`HalalChain.Web/Dockerfile` and `HalalChain.Marketplace/Dockerfile` are unaffected — those apps
reference only `Contracts` + `Http`.

Replayed the exact layer set in a scratch tree:

```
Skipping project "/tmp/dockersim/HalalChain.Application/HalalChain.Application.csproj" because it was not found.
Skipping project "/tmp/dockersim/HalalChain.Domain/HalalChain.Domain.csproj" because it was not found.
error NETSDK1004: Assets file '.../HalalChain.Domain/obj/project.assets.json' not found.
error NETSDK1004: Assets file '.../HalalChain.Application/obj/project.assets.json' not found.
Build FAILED.
```

Note the restore step *exits 0* with "Skipping project" warnings, so the failure surfaces late at
build time — easy to misread as a network/NuGet problem.

Impact: `docker compose up --build` (platform-api), CI `docker-build` job, and the `platform-api`
image in `release.yml` all fail today.

Fix: add the two `.csproj` files to the `COPY` list and restore them ahead of the API project.

### TD-02 — SCSS pipeline is dead (S1)

- `.github/workflows/ci.yml` runs `npm run scss:build` → `node scripts/build-scss.mjs`. That file
  does not exist, and it has never existed (`git log --all -- scripts` lists only `check-docs.py`
  and the now-deleted `regen-admin-pages.sh`). The CI `build-and-test` job fails at this step.
- `Directory.Build.targets` gates its `BuildScss` target on `Exists('scripts/build-scss.sh')`, which
  is also missing — the target is silently skipped, so `dotnet build` succeeds while SCSS sources in
  `HalalChain.Web/wwwroot/scss` (40+ files) and `HalalChain.Marketplace/wwwroot/scss` are never
  compiled. Compiled CSS is committed instead (see TD-17).
- AGENTS.md documents the Sass step implicitly through `npm run scss:build`; there is no documented
  alternative path.

Fix: restore a real sass build script (or delete the target, the npm script, and the CI step and
treat committed CSS as the source of truth — then fix the docs).

### TD-03 — No lint/format gate (S2)

`package.json` declares `"lint": "eslint HalalChain-Cli"`, but root `devDependencies` contains only
`sass`, and `package-lock.json` has no `eslint` entry. The script cannot succeed in a clean install.
There is no `dotnet format --verify-no-changes`, no analyzer package, and no `ruff`/`mypy`
invocation anywhere (despite `.gitignore` referencing `.ruff_cache`/`.mypy_cache`).

### TD-04 — `HalalChain.Application` is outside the solution (S2)

`HalalChain.Platform.sln` declares 10 projects; `HalalChain.Application` is not one of them. It is
built only transitively as a `ProjectReference` of the API. Consequences:

- `dotnet test HalalChain.Platform.sln` does not load its assembly, so
  `HalalChain.Architecture.Tests` (which does not reference it either) cannot guard it. The Phase 0.5
  guardrail set in `implementation-status.md` has a hole exactly where the newest code lives.
- During `dotnet build HalalChain.Platform.sln -c Release`, the project emitted to
  `bin/Debug/net10.0/` — an unconfigured transitive build that hides configuration problems.
- `AGENTS.md` says "10 .NET projects" and omits `HalalChain.Application` entirely.

Fix: add to the `.sln` and to `HalalChain.Architecture.Tests` references; re-run the build to confirm
Release output.

### TD-05 — CQRS layer is registered but unused (S2)

`HalalChain.Application` contains Catalog commands, queries, handlers, validators, an
`EfProductRepository` implementation and two MediatR pipeline behaviors; `Program.cs:175` registers
all of it. `grep` for `ISender`/`IMediator` across `HalalChain.Platform.Api` returns nothing, and
`CatalogController` has no mediator usage — it talks to the DbContext directly. `Program.cs` even
declares `global using HalalChain.Application.Catalog.Handlers;` for types nothing references.

This is the single largest block of unexercised production code in the repo and it directly
contradicts `implementation-status.md` Phase 2 (`NOT_STARTED`) while the code already exists.

### TD-06 — `AuthorizationBehavior` fails open (S2)

`HalalChain.Application/Common/Behaviors/AuthorizationBehavior.cs`:

```csharp
if (httpContextAccessor.HttpContext is null) return await next();          // no caller → proceed
var authorizeAttribute = ...;
if (authorizeAttribute is null) return await next();                        // no [Authorize] → proceed
var policyName = authorizeAttribute.Policy ?? authorizeAttribute.Roles ?? authorizeAttribute.AuthenticationSchemes;
if (string.IsNullOrEmpty(policyName)) return await next();                 // [Authorize] w/o policy → proceed
```

A request decorated with a bare `[Authorize]` (no policy, roles, or scheme) is authorized without any
check. Harmless today because nothing dispatches through MediatR (TD-05); a live bypass the moment
CQRS is adopted. Also, failures do not surface a challenge, so any future failure mode is a 500
rather than 401/403.

### TD-07 — Three overlapping platform API clients (S2)

| Client | Size | Notes |
|---|---|---|
| `HalalChain.Web/Services/PlatformApiClient.cs` | 29.5 KB | local DTO mapping; used by `AppState`, `CartState`, storefront/vendor pages |
| `HalalChain.Platform.Http/Services/PlatformApiClient.cs` | 13.9 KB | shared typed client behind `IPlatformApiClient`; `Web` references the project but uses it in 8 places, `Marketplace` in 2 |
| `HalalChain.Web/Services/WebApiClient.cs` | 8.2 KB | registered in `Web/Program.cs`, throws `NotSupportedException` for 3 members (lines 113, 120, 128) |

`Web` therefore carries its own HTTP stack while already depending on the shared one. `Platform.Http`
also has no test project.

### TD-08 — JWT in `localStorage`, dual auth path (S2)

`HalalChain.Web/Services/AuthService.cs:30,44,55` writes/removes the platform JWT via
`localStorage`, so any XSS in the Blazor host exfiltrates a bearer token valid for up to 60 minutes
(`Jwt:ExpiresMinutes`). `implementation-status.md` Phase 7 still lists "Unify Blazor `AuthService`
with platform JWT" as `NOT_STARTED`. Note this is inherent to the current Blazor Server + browser
token model; the remediation is an HttpOnly cookie or a BFF pattern, not a config change.

### TD-09 — Python dependency and test discipline (S2)

- `AGENTS.md`: "Python `requirements.txt` files should be re-pinned with hashes after every
  functional change." Neither `.halalchain/ai-inference/requirements.txt` nor
  `.halalchain/tawheed/requirements.txt` carries `--hash` entries.
- The two files duplicate 10 identical pins (fastapi, uvicorn, pydantic, pydantic-settings, httpx,
  prometheus, opentelemetry ×3, python-dotenv, json-logger). A bump in one file silently diverges
  from the other.
- Python tests exist and look healthy (`.halalchain/tawheed/tests/test_verdict_invariant.py`,
  `test_policy_engine.py`, `test_orchestrator.py`, `test_iot_observations.py`; ai-inference has
  auth/document/ingredient tests) and CI runs them — but `AGENTS.md`'s test section documents only
  `dotnet test`, and pytest is not installed in the dev container, so the verdict invariant is
  unverified locally.

### TD-10 — Python duplication despite `_shared` (S3)

`.halalchain/ai-inference/src/observability.py` (4.9 KB) and `.halalchain/tawheed/src/observability.py`
(5.2 KB) are parallel implementations, as are the two `config.py` files (3.2 KB / 5.8 KB), while
`.halalchain/_shared` holds only backend selection, cache abstraction and env normalization
(per `AGENTS.md`). Log/metric field names will drift between the two services, which matters because
both feed one OTLP collector and one set of SLOs (`docs/slo.yaml`).

### TD-11 — UI test coverage and unwired CLI tests (S2)

No test project targets `HalalChain.Web` or `HalalChain.Marketplace`; the customer-facing app is
4,139 LOC of C# plus a large Razor surface with no automated coverage. `package.json` defines
`"test": "node --test HalalChain-Cli/test/*.test.mjs"` and those tests exist, but CI never invokes
`npm test` — only `npm install` and `npm run scss:build`.

### TD-12 — CI/CD gate gaps (S2)

- `security-scan` runs `dotnet list package --vulnerable --include-transitive`, which prints findings
  but does not gate the build (no threshold/`--format json` + assertion), and it is wired `needs:
  build-and-test` so it only runs after the job that is already red from TD-02.
- No coverage collection or threshold.
- No `dotnet ef migrations has-pending-model-changes` check, so schema drift can only be caught at
  deploy time despite 4 additive migrations in the tree.
- `helm lint`/`helm package` run only on `v*` tags (`release.yml`), not on PRs.

### TD-13 — Migration and seed bulk (S3)

Four migrations ship `Designer.cs` snapshots of ~650 KB each plus a 650 KB model snapshot, and
`InitialSchema.cs` is a single 304 KB file. `HalalChain.Platform.Api/Persistence/Seed/TaxonomySeed.cs`
(71 KB) hardcodes taxonomy rows. Build time is 5m52s; review and merge conflicts on these files are
correspondingly noisy. Consider IDENTITY-based seeding for taxonomy and a squash strategy for the
pre-production migration chain.

### TD-14 — Documentation drift (S4, cheap to fix)

| Document | Says | Reality (verified this pass) |
|---|---|---|
| `workspace-health.md` | 8/8 projects, 55 tests, branch `master`, commit `9c3a94a` | 10 in sln / 11 built, 95 tests, branch `main`, commit `1771f90` |
| `implementation-status.md` | Domain "is currently empty (just `IAggregateRoot`)" | `HalalChain.Domain` is 710 LOC across Common/Catalog/Halal/Blockchain |
| `implementation-status.md` | Phase 2 `NOT_STARTED`; API "some are un-versioned today" | `HalalChain.Application` implements the Catalog slice; all 10 module controllers except `AiController` are `/api/v1/*` |
| `AGENTS.md` | "10 .NET projects" table | 11 build; `HalalChain.Application` unlisted (TD-04) |
| root `Dockerfile` | "Source of truth: docker/Dockerfile.template" | no `docker/` directory in the repo or its history |
| `AGENTS.md` | hashed Python pins (TD-09) | no hashes present |

`scripts/check-docs.py` validates the service inventory only, not the narrative claims above, so
this class of drift is invisible to CI.

### TD-15 — Route versioning inconsistency (S3)

`Modules/AI/AiController.cs` declares 9 absolute routes under `/api/ai/*` (`health`, `embeddings`,
`summarize`, `classify`, `ingredient-parse`, `certificate-extract`, …) while every other controller
uses `[Route("api/v1/...")]`. Consumers written against `/api/v1` will 404 on AI endpoints, and
version migration later will be a breaking change for those clients.

### TD-16 — Untracked agent worktrees in the workspace (S3)

`/workspaces/waklu/.kilo` is 26 MB of git worktrees (including full copies of the 650 KB migration
files, which is why repo-wide file searches return duplicate hits) and shows as untracked in
`git status`. Adding `.kilo/` to `.gitignore` keeps status output meaningful and stops accidental
staging.

### TD-17 — Committed build output (S3)

41 tracked `wwwroot/css/*.css` files plus `HalalChain.Web/wwwroot/css/app.css.map`. `.gitignore`
covers `wwwroot/css/*.scss.map` only, so the CSS sourcemap is tracked. This is a direct consequence
of TD-02 (no working Sass build) and needs to be decided together with it.

### TD-18 — Helm chart is a stub (S2)

`deploy/helm/halalchain/templates/` contains only `deployment.yaml`. `values.yaml` defines
`service.type`/`service.port`, `platformApi.env`, and `image.repository: ghcr.io/supporthalal/halalchain`,
but there is no Service, ConfigMap, Secret, Ingress, PDB, or probe template; the deployment
references an image `release.yml` never publishes (it publishes `-platform-api`, `-web`,
`-marketplace`). `helm lint` will pass, so this will not be caught until a real cluster deploy, and
`../due-diligence.md`'s "full production deployment manifests are not clearly proven" gap is exactly
this.

## 3. What is verified healthy (do not regress)

- Solution builds with **0 warnings / 0 errors** on .NET 10 (`Directory.Build.props` pins TFM,
  nullable, implicit usings for every project).
- **95/95 tests pass**, including 32 architecture rules and `TrustBoundaryTests`.
- The architectural principle in `AGENTS.md` holds: `HalalChain.Platform.Api` contains no verdict
  assignment; compliance status is mapped from the Tawheed response
  (`Modules/Halal/HalalController.cs:106-112`), and the invariant is covered by
  `.halalchain/tawheed/tests/test_verdict_invariant.py`.
- Secrets hygiene is good: no committed credentials found in a repo-wide scan; `Jwt:Key` is empty in
  `appsettings.json`, the development example value is rejected outside Development
  (`Program.cs:125-127`), and CORS falls back to `AllowAnyOrigin` only in Development
  (`Program.cs:110-115`).
- Images are pinned to specific tags (`postgres:17.4-alpine`, `redis:7.4.2-alpine`, `neo4j:5.26.0-community`,
  `qdrant/qdrant:v1.19.0`, `ipfs/kubo:v0.27.0`, pinned .NET SDK/runtime ARGs), and Foundry is pinned by
  digest.
- `scripts/check-docs.py` and `.env.example` are exemplary; the compose stack fails fast on missing
  required env vars.

## 4. Suggested remediation order

1. **TD-01** — fix the Dockerfile restore layer (one `COPY` block; unblocks compose, CI docker-build, release).
2. **TD-02 + TD-17** — decide the SCSS story (restore the build script, or delete target/script/step and document committed CSS), then align `.gitignore`.
3. **TD-04** — add `HalalChain.Application` to the solution and to `HalalChain.Architecture.Tests`.
4. **TD-03 + TD-12** — make lint and the vulnerability scan real gates; add `npm test` to CI.
5. **TD-06** — close the `AuthorizationBehavior` fail-open path before any MediatR adoption.
6. **TD-05** — adopt the Catalog slice end-to-end or delete it; do not leave both paths warm.
7. **TD-14** — refresh the three drifted documents in the same change that touches each area.
8. **TD-18, TD-07, TD-08, TD-09, TD-10, TD-11, TD-13, TD-15, TD-16** — scheduled work.
