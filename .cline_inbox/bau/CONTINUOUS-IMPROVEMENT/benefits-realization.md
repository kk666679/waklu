# Benefits Realization — Baseline

**Owner:** Product · **Approver:** Enterprise Steering Committee
**Gate:** G1.10 (ENT-001 exit) · **Captured:** 2026-09-30

## Why this file exists

G1.10 requires a baseline for every benefits target **before** the program
spends anything, so that "we improved things" is later a measurement rather
than a recollection. ENT-009 reads this file to compute realized benefit.

**Rule:** a target with no measured baseline cannot be claimed at close. If
the baseline cell says `not-yet-baselined`, the benefit is unprovable and
ENT-009 must report it as such rather than estimate it.

## How to read the numbers

Every value below was **measured on 2026-09-30 against commit `bac0444`**
unless marked otherwise. Values marked `not-yet-baselined` are genuinely
absent — no honest figure exists yet — and each carries an owner and a due
date. They are not placeholders to be filled in with a guess.

Reproduce any row with the command in its `Source` column.

---

## 1. Engineering baseline (measured)

| # | Metric | Baseline | Source |
|---|---|---|---|
| E1 | .NET projects in solution | 18 | `dotnet sln list \| grep -c '\.csproj'` |
| E2 | Production C# files | 547 | `Get-ChildItem -Recurse -Filter *.cs` excluding `obj`/`bin` |
| E3 | Production C# lines | 106,022 | same scan, `Measure-Object -Line` |
| E4 | Test files | 39 | 7 `*.Tests` projects, excluding `obj`/`bin` |
| E5 | Test attributes (`[Fact]`/`[Theory]`) | 378 | `Select-String '^\s*\[(Fact\|Theory)'` |
| E6 | ADRs accepted | 9 | `docs/adr/00*.md`, all `**Status:** Accepted` |
| E7 | Solidity contracts (first-party) | 5 | `contracts/src/*.sol` |
| E8 | Solidity test files | 2 | `contracts/test/*.sol` |
| E9 | Release build (cold) | 147.1s / 18 projects | `dotnet build -c Release` |
| E10 | Pinned image references | 28 across 4 files | `infrastructure/scripts/audit-image-tags.sh` |
| E11 | Unpinned image references | 0 | same — G1.8 satisfied |
| E12 | Evidence-index entries | 18 | `.cline_inbox/manifests/evidence-index.yaml` |

> E9 was measured on an idle Windows dev box, not in CI. It is a
> single observation, not a distribution. Treat it as an order of
> magnitude and re-measure before using it as a target.

---

## 2. Assurance baseline (measured)

These are the properties ENT-001 was chartered to establish. "enforced"
means a check exists and fails the build; "not verifiable in-repo" means the
control lives in infrastructure this repository cannot observe.

| # | Control | Enforced by | Status |
|---|---|---|---|
| A1 | P1 no verdict field on agent output | `VerdictBoundaryTests`, `principles.mjs` | enforced |
| A2 | P2 only tawheed holds blob write creds | IAM policy + config-lint | **not verifiable in-repo** |
| A3 | P3 `IBlobStore` has no `Delete` | type system + `DependencyRulesTests` | enforced |
| A4 | P4 only Compliance mutates `ProductStatus` | — | **NOT ENFORCED (see gap below)** |
| A5 | P5 no SHA-256 Merkle tree | `Storage_AssemblyDeclaresNoMerkleTreeType` | enforced |
| A6 | P6 no verdict type on-chain | token scan of `contracts/src` | enforced, but CI does not run `forge` |
| A7 | P7 registrar key KMS-backed | ADR-008 + deployment sign-off | documentary only |

<!-- SPLIT -->
