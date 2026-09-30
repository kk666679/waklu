# Gate — ENT-001 → ENT-002

Authoritative criteria live in the charter: `CHARTER.md` § Exit gate criteria
(G1.1–G1.10). This file is the operational view. **Where they disagree, the
charter wins** and this file is the bug.

All ten must pass. A single ❌ halts the program.

| # | Criterion | Evidence | Verifier | Automated |
|---|---|---|---|---|
| G1.1 | Architecture doc ratified with steering minutes | Signed minutes | Program Mgmt | No |
| G1.2 | Project count matches `dotnet sln list` (18) | Diff output | Platform Eng | Yes — `project-count` |
| G1.3 | ADR-001…005 merged, each with a dissent section | Git log + inspection | Arch Review Board | Yes — `adrs-have-dissent` |
| G1.4 | ADR-006 chain network ratified | Signed ADR | Steering | No |
| G1.5 | ADR-007 PSP ratified + vendor confirmation letter | Signed ADR + letter | Steering + Finance | No |
| G1.6 | ADR-008 registrar key custody ratified w/ security review | Signed ADR | Steering + CISO | No |
| G1.7 | ADR-009 ratified, or provisional model w/ upgrade path | Signed ADR | Steering | No |
| G1.8 | Zero `latest` or bare-major image tags | grep returns empty | Platform Ops | Yes — `images-pinned` |
| G1.9 | Evidence index has ≥ 9 entries, all with named owners | `entries | length` ≥ 9 | Program Mgmt | Yes — `evidence-index-seeded` |
| G1.10 | Baseline metrics captured for all benefits targets | Filled table | Product | No |

## Automated vs manual split

Six of ten are human judgements (G1.1, G1.4, G1.5, G1.6, G1.7, G1.10). They
**cannot** be automated and must not be simulated by the gate script. The
`gate-check.mjs` run reports automated criteria only; a green run is
**necessary but not sufficient**.

## Falsification requirement

Before ratifying, an independent reviewer — not an author — attempts to
falsify each of the seven enforcement layers (P1–P7) and records one concrete
attack per layer with a `falsified` / `held` result. Report lands at
`phases/ENT-001-specify/falsification-report.md`.

Passing requires the reviewer to **fail** at falsification. A layer that was
not tested has not passed.

## Known environment dependency

`G1.8` and `G1.9` are implemented in `hooks/gate-check.mjs` using Node only —
no `yq`/`jq`/`bash`, because none of those are available on this host.
