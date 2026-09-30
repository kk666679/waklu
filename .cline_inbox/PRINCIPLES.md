# Non-Negotiable Principles

Violations fail the build. No exceptions without a signed ADR.

Enforced by `hooks/gate-check.mjs` and `hooks/principles.mjs`.

## P1 — No verdict field on agent output

`EvidenceProposal` (and any derivative) must not **declare** a property named
`verdict`, `decision`, `isHalal`, `halalStatus`, `approved`, or `rejected`.

Enforced by: `hooks/principles.mjs` (reflection on declarations, comment- and
string-literal-aware) and `HalalChain.Agents.Tests/VerdictBoundaryTests.cs`.

> The check matches **declarations**, not text. The type's own doc comment
> names the forbidden fields on purpose; a naive text grep would block edits to
> the file that most carefully enforces this principle. See
> `hooks/principles.mjs` for the exact grammar.

## P2 — Only `tawheed` holds blob write credentials

`ai-inference` and `agents` hold `GetObject` only. Enforced by IAM bucket
policy, distinct `BLOB__S3__ACCESS_KEY` vs `BLOB__S3__READONLY_KEY`, and
config-lint in CI.

## P3 — `IBlobStore` has no `Delete`

Evidence is append-only. Retention tombstones metadata; it never purges bytes
as a side effect. Enforced by interface inspection in
`HalalChain.Application/Storage/IBlobStore.cs`.

## P4 — Only `Compliance` mutates `ProductStatus`

Every other module reads. Enforced by the write-path architecture test in
`HalalChain.Architecture.Tests/`.

## P5 — Two hashers, two concerns

SHA-256 for storage addressing. Keccak256 for on-chain Merkle nodes.
`KeccakMerkleTree` is the only `IMerkleTree` implementation;
`Sha256MerkleTree` must not exist. Enforced by a repo-wide filename check.

## P6 — No verdict type on-chain

No Solidity contract in `contracts/src/` declares `verdict`, `halalStatus`,
`isHalal`, or `approved`. Enforced by a token-level scan of `*.sol`.

> **Verified clean 2026-09-26** across all 5 contracts. Re-run to confirm.

## P7 — Registrar key never in a hot wallet in production

Certificate registry authority is KMS-backed. Amoy testnet may use a hot
wallet; production may not. Enforced by the deployment checklist sign-off and
[`ADR-008`](../../docs/adr/008-registrar-key-custody.md).

## P8 — Every workflow runs under an explicit budget

No agent loop without `max_steps`, `max_tokens`, `max_wall_seconds`,
`max_cost_usd`. Exceeding any bound halts and escalates.

## P9 — Compliance re-verification at checkout is not optional

Not cacheable. The last chance to stop a halal-claim violation from becoming a
completed sale.

## P10 — Every anchor receipt is cited by the verdict it supports

Provenance is bidirectional. A verdict without a trace hash is incomplete.

## Waivers

A waiver is an ADR that names the principle, the scope, the expiry date, and
the compensating control. There is currently **no waiver for any principle**.

Halal-assurance controls `H-001`–`H-006` in
`.cline_inbox/bau/COMPLIANCE/controls-matrix.md` are **not waivable** for cost or schedule.
