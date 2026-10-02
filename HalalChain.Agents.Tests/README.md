# HalalChain.Agents.Tests

xUnit (v3) tests for the agent evaluation DAG and runtime client in
`HalalChain.Agents/`.

## Purpose

These tests cover the parts of `HalalChain.Agents` whose correctness is not
obvious from reading them: how a failed node's blame is attributed to its
ancestors, how each node type is scored, how traces are loaded from the blob
store, and — most importantly — that no evaluation output can carry a verdict.

## Layout

```
HalalChain.Agents.Tests/
├── GreedyParentAttributionTests.cs  # Root-cause attribution across the DAG
├── NodeScorerTests.cs               # Per-node-type scoring and thresholds
├── TraceLoadingTests.cs             # Blob-store trace loading
├── GoldenExpectationSetTests.cs     # Golden-set comparison
├── AgentWorkflowClientTests.cs      # The HTTP client to the agents service
└── VerdictBoundaryTests.cs          # No verdict leaks out of the harness
```

## Dependencies

| Kind | Reference |
| --- | --- |
| Project | `HalalChain.Agents` only |
| Package | `xunit.v3` (`$(XunitV3Version)`), `xunit.runner.visualstudio` v3 |

Note this is one of the projects on xunit v3 (the rest of the older suites use
xunit v2), so its runner package differs.

## Run

From the repository root:

```bash
dotnet test HalalChain.Agents.Tests -c Release
```

## Related Components

- [HalalChain.Agents](../HalalChain.Agents/README.md) — the system under test
- [HalalChain.Architecture.Tests](../HalalChain.Architecture.Tests/README.md) — `EvaluationArchitectureTests.cs` enforces the same verdict boundary at the assembly level
- [.halalchain/agents](../../.halalchain/agents/README.md) — the Python-side evaluation harness with its own goldens

## Notes / Limitations

- The Python evaluation harness in `.halalchain/agents/app/eval/` is a separate
  implementation with its own scorers and golden files. The two are not
  cross-checked against each other by any test.