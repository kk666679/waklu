# HalalChain.Agents

.NET-side agent evaluation harness and agent runtime client.

This project answers one question: **did the agent pipeline work?** It does
not answer whether a product is halal. That is tawheed's question, and
`docs/DECISION-CONTRACT.md` invariant 1 is the reason this project exists in
the shape it does.

## What lives here

| Area | Namespace | Purpose |
|------|-----------|---------|
| Failure taxonomy | `HalalChain.Agents.Eval.Taxonomy` | 21 failure categories, wire-compatible with the Python eval taxonomy |
| DAG evaluation | `HalalChain.Agents.Eval.Dag` | Trace model, node types, per-node scorers, greedy-parent root-cause attribution |
| Trace loading | `HalalChain.Agents.Eval.Traces` | Read-only trace access via `IBlobStore` |
| Runtime client | `HalalChain.Agents.Runtime` | `IAgentWorkflow` over HTTP; `IWorkflowBudget` enforcement |
| Trace index | `HalalChain.Agents.Traces` | `IAgentTraceStore` read side; trace-id resolution |

## Three interfaces this project implements

These were declared in `HalalChain.Application` with **zero implementations**
anywhere in the solution. An interface nothing implements is documentation at
best; these are now real:

- `IAgentWorkflow` → `AgentWorkflowClient`. Calls the Python agents service.
- `IWorkflowBudget` → `WorkflowBudgetLedger`. Enforces step and wall-clock
  ceilings.
- `IAgentTraceStore` → `InMemoryAgentTraceIndex`. Read side only.

## The verdict boundary

The harness produces scores, root causes, and failure categories. It cannot
express a compliance outcome, and that is enforced structurally rather than by
convention:

- `EvalResult` declares no verdict member, and
  `HalalChain.Architecture.Tests.EvaluationArchitectureTests` checks it — over
  **properties as well as fields**, because a `record` with a `Verdict`
  property backed by a compiler-generated field would slip past a fields-only
  check.
- No type in the `HalalChain.Agents.Eval` namespace is named after a decision
  concept, and none holds a `VerdictState` or `VerdictBinding`. Asserted in
  `HalalChain.Agents.Tests.VerdictBoundaryTests`.
- `AgentRunResponse` — the DTO the agents service replies into — has no
  verdict-shaped member, so a verdict sent by an upstream agent is dropped at
  the boundary instead of travelling into the managed layers as a fact.

`TerminalScorer` deserves a note, because its name is close to the forbidden
thing. It **compares** the workflow's terminal outcome against a human label
recorded in the goldens. It produces neither. It has no policy knowledge and
no authority to decide; it measures whether the pipeline faithfully carried
tawheed's decision to the end. A workflow that routed around tawheed and
answered correctly on its own would still be a defect — one this scorer
exists to catch, not to reward.

## Why the harness reads only through `IBlobStore`

The eval harness holds a read-only credential against evidence storage — the
same credential an auditor gets. If any eval type could open a file or
directory directly, that boundary would be decorative. Enforced by
`EvalReadsBlobOnlyThrough_IBlobStore` in the architecture tests, and
independently in `HalalChain.Agents.Tests` (the architecture tests cannot run
while `HalalChain.Platform.Api` is unbuildable — see below).

`ITraceLoader` also has no `Delete`. An eval run that could destroy its own
inputs is an eval run that can launder a regression.

## Determinism

Root-cause attribution is deterministic, because a report that names a
different root cause on each run is not a regression signal. Two cases are
covered explicitly in `GreedyParentAttributionTests`:

- **Cycles.** A malformed shadow-mode trace can contain an edge cycle. A naive
  backward walk makes every failed node point at another failed node, so
  nothing is a root cause and the report says "failures, cause unknown" — the
  least actionable output possible. The cycle is broken at the
  ordinal-smallest node.
- **Dangling edges.** An edge naming a node the trace does not declare is
  dropped, not thrown on. One bad edge must not abort evaluation of the whole
  trace.

## Cross-runtime contracts

Three things are duplicated across runtimes on purpose, and must change
together:

1. **Failure categories** — `FailureCategory` here vs
   `.halalchain/agents/app/eval/dag/taxonomy.py`. Reports are diffed by wire
   name, so `PythonTaxonomyCategoryCountMatches` pins the count at 21.
2. **Required bundle fields** — `HandoffScorer.RequiredFields` vs
   `agents.handoff.REQUIRED_BUNDLE_FIELDS` vs the Python eval scorer's list.
3. **Wire names** — `WorkflowNodeType.Terminal` is `"verdict"` on the wire,
   matching the Python taxonomy.

## Building and testing

```bash
dotnet build HalalChain.Agents/HalalChain.Agents.csproj -c Release
dotnet test  HalalChain.Agents.Tests/HalalChain.Agents.Tests.csproj -c Release
```

69 tests. The agent loop itself is Python (`.halalchain/agents/`); this
project is the .NET half — sequencing, transport, budget, and evaluation.
