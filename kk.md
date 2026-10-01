
# HalalChain Platform

AI-native halal commerce and compliance platform for supplier verification,
evidence collection, policy evaluation, and multivendor commerce.

**Status:** Architecture ahead of implementation. See [Current status](#current-status).

**Stack (as actually referenced in the tree):** .NET 10 (`net10.0`) ·
ASP.NET Core / EF Core 10.0.11 · Npgsql 10.0.3 · Nethereum.Web3 6.1.0 ·
Radzen.Blazor 11.3.2 · AutoMapper 16.2.0 · StackExchange.Redis 3.1.31 ·
Python 3.11+ (FastAPI) · Node 22+ (npm workspace) · Foundry (forge-std
vendored as a git submodule)

> Earlier revisions of this document listed Aspire, Microsoft Agent Framework,
> and a C# MCP SDK as stack pins. None of them appear in any `.csproj`, `.props`,
> or `.targets` file, so they are not claimed here. The only MCP SDK in the tree
> is the JavaScript `@modelcontextprotocol/client` 2.0.0 used by the Node CLI.
>
> Three version details worth knowing, all verified:
>
> - The Npgsql / `Npgsql.EntityFrameworkCore.PostgreSQL` pins are not uniform —
>   one project pair sits on 10.0.0 while others are on 10.0.3.
> - The package manager is npm (root `workspaces` field, `package-lock.json`,
>   and `npm install` in `AGENTS.md`). `HalalChain-Cli/package.json` still
>   declares `"packageManager": "pnpm@10.18.0"`, and there is no
>   `pnpm-workspace.yaml` or `pnpm-lock.yaml`. That declaration is stale.
> - Root `package.json` defines `scss:build` and `scss:watch` against
>   `scripts/build-scss.mjs`, but no root `scripts/` directory exists; the
>   script lives at `infrastructure/scripts/build-scss.mjs`. Both npm scripts
>   currently fail.

---

## Table of contents

- [HalalChain Platform](#halalchain-platform)
  - [Table of contents](#table-of-contents)
  - [Architectural principle](#architectural-principle)
  - [The problems this platform addresses](#the-problems-this-platform-addresses)
    - [Certification is expensive in ways the fee schedule doesn't show](#certification-is-expensive-in-ways-the-fee-schedule-doesnt-show)
    - [The bottleneck is discovery, not bureaucracy](#the-bottleneck-is-discovery-not-bureaucracy)
    - [Certification is a snapshot, not a state](#certification-is-a-snapshot-not-a-state)
    - [Buyers can't verify what they can't see](#buyers-cant-verify-what-they-cant-see)
    - [Cross-border transaction compliance is a separate problem](#cross-border-transaction-compliance-is-a-separate-problem)
  - [What HalalChain offers](#what-halalchain-offers)
    - [For suppliers](#for-suppliers)
    - [For vendors](#for-vendors)
    - [For buyers](#for-buyers)
  - [How HalalChain differs](#how-halalchain-differs)
  - [Current status](#current-status)
  - [The trust boundary](#the-trust-boundary)
  - [Repository overview](#repository-overview)

---

## Architectural principle

> **AI gathers evidence. Deterministic systems decide business and compliance outcomes.**

The LLM and AI services observe, classify, summarize, and enrich evidence. The
deterministic policy engine in `tawheed` owns the halal verdict and operating
decisions. The rule is enforced at seven layers, not asserted.

Each layer below names the artifact that enforces it and how strongly it is
gated today. "In CI" means a failure breaks the build; "not gated" means the
artifact exists and is correct but nothing runs it automatically.

> **Superseded 2026-09-30.** The three caveats below were accurate when
> written and **no longer are**. Verified against commit `bac0444`:
>
> 1. ~~CI aborts on stale `scripts/` paths~~ — **fixed.** `ci.yml` now runs
>    `npm run scss:build` (which resolves to `node infrastructure/scripts/build-scss.mjs`)
>    and `python3 infrastructure/scripts/check-docs.py`. Both paths are correct.
> 2. ~~`VerdictBoundaryTests` cannot compile~~ — **fixed.** `VerdictState` and
>    `VerdictBinding` now exist in `HalalChain.Domain/Halal/ValueObjects.cs`, and
>    `dotnet build HalalChain.Platform.sln -c Release` succeeds for all 18
>    projects.
> 3. The **marketplace** half of caveat 2 is still open and is now the
>    load-bearing gap: `Product.Status` is `public ... { get; set; }`, so P4
>    ("only Compliance mutates `ProductStatus`") is **not enforced by
>    anything**. Tracked as `finding F-ENT001-01`.
>
> The original text is kept below for the audit trail. Read the correction
> first; do not act on the superseded text.

| Layer | Enforcement mechanism | Proof | Gate |
|---|---|---|---|
| Prompt | The root `Modelfile` confines the local assistant to the evidence-collector role and hands the decision to the Policy Engine | `Modelfile` | Not gated — and see caveat below |
| API | `Modules/Halal/` holds only a controller and a `tawheed` client; it forwards for evidence and never evaluates policy | `Modules/Halal/HalalController.cs`, `ITawheedClient.cs`, `TawheedHttpClient.cs` | In CI (compiles; no policy types available to call) |
| Storage | `IBlobStore` exposes `Put`/`OpenRead`/`Exists` and deliberately no `Delete` — append-only is a compile-time property | `HalalChain.Application/Storage/IBlobStore.cs` | In CI (type system) |
| Agent runtime | The eval namespace may not declare, name, or accept a verdict-shaped member, and may not touch a verdict type | `HalalChain.Agents.Tests/VerdictBoundaryTests.cs` | In CI via `dotnet test` — see caveat below |
| Application layer | No concrete verdict/compliance type may be defined in `HalalChain.Application`; status is projected, never fabricated | `HalalChain.Architecture.Tests/DependencyRulesTests.cs` → `Application_ShouldNotAssign_HalalVerdicts` | In CI (NetArchTest) |
| Marketplace | ADR-005 binds a verdict to a product instead of storing a halal boolean; only the compliance path drives `ProductStatus` | `docs/adr/005-verdict-binding-not-boolean.md`, `HalalChain.Application/Halal/StateMachine/ProductStatusMachine.cs`, `IVerdictBindingRepository.cs` | **Not enforced** — see caveat 2 |
| Blockchain | Contracts record existence, revocation, and timestamps. Never a verdict | `contracts/src/HalalCertificationRegistry.sol` + 4 others | Not gated — CI does not run `forge` |
| Contract tests | Invariants assert revocation never yields current, and no contract declares a verdict type | `contracts/test/HalalPlatform.t.sol`, `RegressionAndSecurity.t.sol` | Not gated — no Foundry job in `ci.yml` |

Three caveats stated plainly, because the point of this table is that it can be
checked:

1. **The prompt layer is a constraint, not a guarantee.** The `Modelfile` does
   say "You ONLY collect EVIDENCE. The final compliance decision is made by the
   deterministic Policy Engine," and it frames the classification as
   preliminary. But it also instructs the model to "End with [VERDICT] and the
   single word classification" and emits `[VERDICT] HALAL|HARAM|MASHBOOH`. That
   output is advisory and untyped, so nothing downstream can bind it — yet the
   vocabulary undercuts the rule it is meant to state. Renaming the token to
   `[PRELIMINARY_CLASSIFICATION]` would align it with the other six layers.
2. **The marketplace layer is decided but not built.** ADR-005 chose
   verdict-binding-over-boolean, and the Application layer has the port and the
   state machine (`IVerdictBindingRepository`, `ProductStatusMachine`). But
   `HalalChain.Domain/Catalog/Product.cs` carries **no** `VerdictBinding` and
   **does** expose a settable `HalalProfile`, whose `Status` is a `HalalStatus`
   enum that includes `SelfDeclared` and `Certified`. So a vendor-facing write
   path can still assert a halal status on the product today. `VerdictBinding`
   and `VerdictState` are not defined anywhere in the domain model.
3. **`VerdictBoundaryTests.cs` cannot compile as committed.** It references
   `HalalChain.Domain.Catalog.VerdictState` and
   `HalalChain.Domain.Catalog.VerdictBinding` via `typeof(...)`, and neither
   type exists. The test logic is sound and worth keeping; it just needs those
   types to land (which is the same work as caveat 2). Confirm with
   `dotnet build HalalChain.Platform.sln`.

The intent is deliberate at every layer: a future contributor should not be
able to reintroduce an LLM-decides-halal path without a build failure.

How much of that holds today, stated precisely (**as of 2026-09-30**;
see the supersession note above — two of these have since changed):

- **Holds now.** The type system (`IBlobStore` has no `Delete`), the
  `Application_ShouldNotAssign_HalalVerdicts` architecture guard, and the
  structure of `Modules/Halal/`, which contains no policy types to call.
- **Now compiling.** `VerdictBoundaryTests` builds and runs; the
  `VerdictState` / `VerdictBinding` types it referenced now exist.
- **Written, not gated.** The Solidity invariant tests — no Foundry job exists
  in CI.
- **Not enforced.** P4. `Product.Status` is a public settable property and no
  architecture test constrains who writes it. See `finding F-ENT001-01`.

The remaining gap is therefore narrower than it first looked: the pipeline
runs, the C# guards compile, and what remains unproven is the Foundry job and
the P4 write-path rule. Two fixes close it: add a Foundry job, and constrain
the `Product.Status` write path.

---

## The problems this platform addresses

Halal certification is not an administrative task. For a supplier it is a
survival-level cost and a discovery problem wrapped inside a compliance
requirement.

### Certification is expensive in ways the fee schedule doesn't show

JAKIM's official annual fee for food premises is approximately RM100 —
deliberately accessible. The costs it doesn't charge are the ones that decide
whether a business applies.

- Ingredient lab testing for uncertified suppliers: **RM300–1,500 per ingredient**
- Halal awareness training for staff: **RM200–500**
- Consultants, because the application fee is non-refundable and a failed
  audit means starting over: **RM1,500–4,000**

A home-based seller faces **RM2,000–4,000** all-in. A small packaged food
manufacturer: **RM5,000–8,000**. A medium manufacturer with multiple lines:
**RM8,000–15,000 or more**.

The fee is not the barrier. The barrier is the cost of preparing an
application that will pass on the first attempt — and the cost of discovering
what "passing" means in practice.

### The bottleneck is discovery, not bureaucracy

A Malaysian entrepreneur wants to open a JAKIM-certified Japanese restaurant.
The wasabi must come from a supplier certified by a body recognised by JAKIM.
JAKIM recognises **97 foreign halal certification bodies globally** — the
supply network exists. But the supplier's product listing is in Japanese. The
entrepreneur searches *"halal wasabi supplier"* and gets noise.

Every certified supplier whose information exists only in their native
language is *effectively removed from the global Halal supply chain — not by
policy, not by cost, but by discoverability*.

### Certification is a snapshot, not a state

Certification is verified at onboarding and then not again until renewal.
Between audits, nothing enforces it. A product with an expired certificate
sells exactly like a product without one. A buyer has no way to tell the
difference without independently contacting the certifying body.

### Buyers can't verify what they can't see

Wholesale buyers need specific verifiable facts — certifying body,
certificate number, expiry date, scope, export licences, cold chain
capability. Generic marketplaces don't provide these. Certificate recognition
also varies: a JAKIM certificate is recognised differently from an IFANCA
certificate depending on the import country, and most platforms collapse that
distinction rather than recording it.

### Cross-border transaction compliance is a separate problem

Even when a supplier is certified and a buyer is found, the transaction
structure itself may violate halal finance constraints. Delayed payouts that
accrue interest. Escrow arrangements without a documented agency basis.
Commingled zakat or sadaqah line items. These are not edge cases — they are
the default in general-purpose commerce platforms.

---

## What HalalChain offers

### For suppliers

**Continuous compliance, not a one-time upload.** Every existing halal
platform treats certification as a document you upload once. HalalChain treats
it as a state that determines whether your product is listed at all. The
Compliance gate runs `Draft → PendingVerification → Active → ExpiringSoon →
Suspended`. When your certificate lapses, the product leaves the catalog
automatically. When you renew, it returns. You never risk the reputational
damage of selling with an expired certificate you didn't notice.

**Agent-assisted evidence gathering.** The `agents` service discovers
candidate documents, classifies them, verifies certificates against issuer
registries, and identifies which policy requirements lack evidence. A supplier
who would otherwise spend RM1,500–4,000 on a consultant to translate
guidelines into practical steps gets that discovery work done in minutes.
Critically: the agents produce *proposals*, never verdicts. `tawheed` decides.

**Discoverability across languages.** The evidence store is content-addressed
and the classifier agent types every document, so a supplier's certificate,
product description and capability data are structured from the moment they
enter the platform. Where a listing exists only in Malay or Arabic, the
platform can surface it to English-language buyers — because the classifier
has typed the document and the search index has structured it.

**A transaction structure that is itself halal-compliant.** The Payments
module is designed against explicit halal finance constraints. No riba on
delayed payouts. No gharar — delivery terms stated before capture.
Wakala-based escrow, where funds are held under agency, documented. Zakat and
sadaqah as 100% pass-through with no commingling.

**Immutable, auditable evidence that survives a dispute.** Certificates,
verification traces, dispute evidence and order documents all land in
`IEvidenceStore`, which is append-only and content-addressed. A certificate
cannot be deleted or altered — the type makes it impossible. When a dispute
arises, the evidence trail is the same one assembled at onboarding.

### For vendors

**Compliance re-verified at checkout, not just at listing.** A product that
was `Active` when a buyer added it to the cart may have been `Suspended` while
it sat there. Checking at placement is the last chance to prevent a
halal-claim violation from becoming a completed sale. This protects the
vendor from the reputational cost of an inadvertent violation.

**Cross-vendor checkout with per-vendor fulfilment.** A buyer checks out with
items from multiple vendors. One payment capture splits into per-vendor
sub-orders and per-vendor payouts. Each vendor sees only their own sub-order.
The buyer sees one order. This is standard in general commerce but absent from
every halal-specific platform, which are mostly catalogs or single-vendor
storefronts.

**Vendor isolation by default.** Every vendor-scoped query carries a tenant
filter, enforced by a per-module integration test and an architecture guard
rule. A vendor cannot see another vendor's catalog, orders, or evidence.

### For buyers

**Verification you can inspect, not just a badge.** The platform surfaces the
certifying body, certificate number, expiry date, and scope — not a generic
*verified* badge. Where a JAKIM certificate is recognised differently from an
IFANCA certificate depending on the import country, the platform records the
distinction rather than collapsing it.

**One cart, many vendors, one compliant transaction.** Buyers purchase from
multiple certified suppliers without managing separate accounts, separate
payments, or separate compliance checks.

**On-chain corroboration, off-chain privacy.** Certificate existence,
revocation, and expiry are anchored on-chain as a public record. Evidence
contents, vendor PII, and halal verdicts are not. The chain records what
happened, not what was decided or why.

---

## How HalalChain differs

| What suppliers need | Salaam Market | DagangHalal | HalalChain |
|---|---|---|---|
| Halal-certified product listing | Yes | Yes | Yes |
| Continuous compliance verification | Onboarding only | Partial, separate SaaS | Yes, automatic |
| Agentic evidence gathering | No | No | Yes |
| Multilingual discovery | Partial | Partial | Yes, by design |
| Halal-compliant transaction structure | Yes, Maybank Islamic | No | Yes, enforced |
| Cross-vendor checkout | No | No | Yes |
| Immutable auditable evidence | No | Partial | Yes |
| Automatic delisting on expiry | No | No | Yes |
| On-chain certificate registry | No | No | Yes |

Salaam Market is Malaysia's first B2B halal marketplace, launched August 2024
as a Maybank Islamic and Borong partnership. It verifies certification at
onboarding and charges sellers up to 3% per order.

DagangHalal, founded 2007, is a B2B directory and marketplace covering 90+
countries, with a separate SaaS compliance product.

Neither verifies certification continuously. Neither does agentic evidence
gathering. Neither offers cross-vendor checkout. HalalChain's differentiation
is the compliance gate and the agent layer — not the catalog.

---

## Current status

The architecture described in this document is more complete than the
implementation. The offers above describe what the platform is **designed** to
do.

The pieces that make these offers real are:

1. The Compliance gate and its state machine
2. The certificate expiry sweep
3. Vendor onboarding with one real `tawheed` rule

Everything else — the blockchain module, the eval DAG, the skills runtime —
is infrastructure in service of those three. They are worth building when the
first three hurt without them, and not before.

See `docs/ARCHITECTURE.md` §14 (Architectural Decisions & Blocking Milestones)
for the decisions that block the next milestones, and §16 (Implementation
Status) for the mechanical list of what is and is not yet in the tree. The
numbered architecture document `README.md` carries a parallel §1–§16 numbering
with its own §14 (Decisions requiring input) and §16 (Repository status); the
two documents number independently, so cite the file as well as the section.

---

## The trust boundary

The design intent is enforced at the storage layer, not only in code:
`ai-inference` and `agents` hold read-only blob credentials, and only `tawheed`
holds the write key.

Three enforcement layers are specified, in order of reliability. Their status
differs, and the difference matters:

| # | Layer | Status in this tree |
|---|---|---|
| 1 | **IAM and bucket policy** — reasoning services get `GetObject` only; no `PutObject`, no `DeleteObject`. This is the real boundary; everything below is defense in depth | Deployment-time. Not expressible as a repo artifact — verified when the bucket is provisioned |
| 2 | **Separate config keys** — `BLOB__S3__ACCESS_KEY` versus `BLOB__S3__READONLY_KEY`, so the reasoning services never see the write key | **Not present.** Neither key name appears anywhere in the tree |
| 3 | **CI config-lint** — the pipeline fails if a reasoning service is wired to the write key | **Not present.** `ci.yml` has build-and-test, python-tests, security-scan, and docker-build jobs; none inspects blob credential wiring |

The compile-time half of this boundary *is* real and gated: `IBlobStore` has no
`Delete` member, and the eval namespace is architecturally barred from opening
a path or filesystem handle directly (asserted in
`HalalChain.Agents.Tests/VerdictBoundaryTests.cs`). The credential half is
design intent until rows 2 and 3 land.

A related gap to be aware of: `HalalChain.Architecture.Tests/EvaluationArchitectureTests.cs`
contains `EvalExtra_NotInProductionDockerfile`, which is an unconditional
`Assert.True(true)` deferring to `.github/workflows/python-locks.yml` — but that
workflow only verifies `uv` lock files and contains no such check. The rule is
currently guarded by nothing.

The blockchain equivalent remains design intent: the registrar keypair is held
by `tawheed`, not by agents and not by the API, and the API submits anchor
transactions through a service account with `anchorBatch` permission and nothing
else. See `docs/adr/008-registrar-key-custody.md`.

For the enforcement layer table, see `docs/ARCHITECTURE.md` §8 (Enforcement
Layers); for the trust-boundary diagram, see `README.md` §8 (Trust boundary).
The two documents use independent §1–§16 numbering.

---

## Repository overview

```text
.
├── AGENTS.md                        # Contributor and build guide
├── CODEOWNERS
├── LICENSE
├── HalalChain.Platform.sln          # Main .NET solution — 18 projects
├── Directory.Build.props            # Shared .NET defaults (net10.0, nullable, implicit usings)
├── Directory.Build.targets
├── global.json                      # Pins the .NET SDK (10.0.200, latestFeature roll-forward)
├── docker-compose.yml               # Full local stack
├── Dockerfile                       # Platform API image
├── service-manifest.yaml            # Canonical service inventory and ports
├── Modelfile                        # Local assistant system prompt for Ollama/OpenClaw
├── Makefile
├── gates.yaml                       # Quality gate definitions
├── verdicts.jsonl                   # Sample decision records (MY-v3 / ID-v1 policy versions)
├── package.json + package-lock.json # npm workspace root (Node CLI + Sass tooling)
├── .env.example / .env.observability.example
├── README.md                        # Numbered architecture doc (§1–§16)
├── kk.md                            # This document
├── .github/workflows/               # ci, cli, observability, python-locks,
│                                    #   generate-admin-pages, release
├── docs/
│   ├── ARCHITECTURE.md              # Numbered architecture doc (§1–§16, §7 absent;
│   │                                #   §8 Enforcement Layers, §16 Implementation Status)
│   ├── DECISION-CONTRACT.md         # Verdict record contract
│   ├── SECURITY-BASELINE.md
│   ├── RUNTIME-MATRIX.md
│   ├── SERVICE_MANIFEST.md
│   ├── CATALOG_MODERNIZATION_2026.md
│   ├── ENHANCED_ARCHITECTURE_SUMMARY.md
│   ├── due-diligence.md             # Source for the market and cost figures
│   ├── local-development.md
│   ├── slo.yaml                     # SLO / error-budget definitions
│   ├── adr/                         # ADR-001 … ADR-009 (accepted decisions)
│   ├── architecture/                # Architecture, data placement, threat model,
│   │                                #   API, tech-debt register, workspace health
│   └── runbooks/                    # 27 operational procedures
├── infrastructure/                  # Compose overlays and observability config
│   ├── docker-compose.dev.yml       # Chain profile overlay
│   ├── docker-compose.observability.yml
│   ├── grafana/ · prometheus/ · loki/ · tempo/ · otel/ · alertmanager/
│   └── scripts/                     # check-docs.py, build-scss, setup-dev
├── deploy/                          # Deployment assets
├── HalalChain-Cli/                  # Node operator and CLI toolkit
├── HalalChain.Domain/               # Domain model — aggregates, value objects, events
├── HalalChain.Application/          # Use cases, orchestration, storage ports, state machine
├── HalalChain.Storage/              # Blob and evidence storage adapters
├── HalalChain.Platform.Api/         # ASP.NET Core modular monolith
│   └── Modules/                     # AI, Auth, Blockchain, Catalog, Commerce,
│                                    #   Events, Halal, Indexer, Ipfs, Vendors,
│                                    #   Verification
├── HalalChain.Platform.Contracts/   # Shared DTOs and Solidity artifacts
│   └── contracts/                   # Foundry project — 5 src + 2 test suites
├── HalalChain.Platform.Http/        # Typed HTTP client
├── HalalChain.Agents/               # .NET agent runtime + eval DAG
├── HalalChain.Automation/           # Scheduled jobs (compliance sweep, cleanup)
├── HalalChain.DataFlow/             # PostgreSQL source/destination components
├── HalalChain.Web/                  # Customer-facing Blazor Server UI
├── HalalChain.Marketplace/          # Vendor marketplace UI
├── HalalChain.Mcp/                  # MCP server
└── .halalchain/                     # Python services
    ├── _shared/                     # Shared package (LLM provider, cache)
    ├── ai-inference/                # FastAPI AI gateway
    ├── tawheed/                     # FastAPI evidence ingestion + Policy Engine
    ├── agents/                      # FastAPI agentic reasoning layer
    ├── local-models/                # Local model hosting (source + Dockerfile)
    ├── requirements/                # Pinned lock files with hashes
    └── config.json                  # Local-only shared config placeholder
```

Test projects (`HalalChain.Platform.Tests`, `HalalChain.Storage.Tests`,
`HalalChain.Mcp.Tests`, `HalalChain.Agents.Tests`, `HalalChain.DataFlow.Tests`,
`HalalChain.Architecture.Tests`) are part of the same 18-project solution.
