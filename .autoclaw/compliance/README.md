# `.autoclaw/compliance/`

Deterministic compliance rules, the jurisdiction matrix, and the scheme
definitions. Everything here is evaluated by the Policy Engine and by nothing
else.

## The rule

No agent, skill, connector, or MCP tool reads a file in this directory. They
are the evaluator's input, not the fleet's. An agent that wants to know whether
something is compliant calls `/tawheed/evaluate` and receives a result; it never
reads `compliance/rules/*.yaml` and reasons about them itself. That is P2 and
P3 in one sentence, and it is why `rules/index.yaml` lists
`never_consumed_by`.

## Determinism

Every rule is a pure function of its declared inputs. Same inputs, same output,
no sampling, no model in the loop, no network call, no clock read other than
the `evaluated_at` the request supplies. A rule that cannot satisfy all five of
those is not a rule in this directory; it is a skill, and it belongs in
`skills/`.

## Layout

```
rules/          the rule definitions, plus index.yaml as the load order
jurisdictions/  per-authority parameters: evidence age, required accreditations
schemes/        MS-1500, OIC/SMIIC, GSO-2055, HAS-23000
schemas/        rule-result.schema.json — the shape every rule emits
```

## The six rules

| Rule | Blocking | Answers |
|------|----------|---------|
| `delegated-authority-check` | yes | may this actor do this at all? |
| `evidence-freshness` | yes | is this evidence still valid? |
| `certificate-scope-match` | yes | does this certificate cover this product? |
| `cold-chain-threshold` | yes | did the shipment stay in range? |
| `laboratory-accreditation` | yes | is the lab accredited for this test? |
| `supplier-traceability` | yes | can every ingredient be traced? |

## Adding a rule

1. Write the file against the common schema: `id`, `version`, `severity`,
   `applies_to`, `inputs`, `rule`, `on_violation`, `audit`.
2. Register it in `rules/index.yaml` and place it in `evaluation_order`.
3. If it is a preflight concern, mark it `evaluated_by: tawheed-preflight`.
4. Document any new guard name in
   `governance/certification/guards.md`.
5. Run the architecture tests.

A rule that reads a field not listed in `inputs` is a bug: the input list is
what makes the evaluation reproducible by hand, and an undeclared read breaks
that.
