# Eval Layer

Quality gate for HalalChain AI-native components.
Runs in CI and before deploy. No deploy without passing required gates.

## Suites
- agents/      Agent correctness, tool use, policy adherence, hallucination
- skills/      Extraction F1, validation, lookup, mapping
- workflows/   End-to-end supplier/vendor/marketplace journeys
- rag/         Hit rate, MRR, faithfulness, citation accuracy
- policy/      Decision accuracy, false approval rate, conflict detection
- blockchain/  Hash determinism, anchor success, event indexing
- metrics/     Accuracy, precision/recall, latency, cost, human-review rate
- regression/  Baselines and nightly regression

## CI flow
lint → unit tests → reference checksum → agent evals → skill evals
→ workflow evals → RAG evals → policy evals → blockchain evals → deploy
