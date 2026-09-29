# ENT-007 — Enterprise Node Palette (@xyflow/react / reactflow)

**Role:** Author
**Owner:** UI Platform
**Approver:** Architecture Review Board
**Duration:** 8 weeks
**Exit:** Gate `gate-007-008`

## Objective

Author the enterprise node palette for the platform's workflow editor, built
on `@xyflow/react`. The palette lets ops and compliance staff compose,
inspect, and audit agent workflows visually.

## Why this exists

The agent workflows in `.halalchain/agents/workflows/` are deterministic
Python, but compliance staff need to *see* them, trace them, and cite them
in audits. The palette is the visual surface for that.

## The rule this palette must not break

**No node in the palette emits a verdict.** The palette is a *composition*
surface, not a decision surface. The terminal node `EmitVerdictRequest`
hands evidence to `tawheed` and receives a verdict — it never computes one.

Enforced three ways:

1. Palette grep test in CI (P1)
2. Node contract schema (closed `emits` enum)
3. Serializer round-trip test proves the emitted Python has no verdict path

## Node categories

| Category | Nodes | Purpose | Emits |
|---|---|---|---|
| **Evidence** | `FetchDocument`, `ClassifyDocument`, `ExtractFields` | Evidence gathering | `EvidenceProposal` |
| **Verification** | `VerifyCertificate`, `CheckRegistry`, `CrossReference` | Verification | `EvidenceProposal` |
| **Policy** | `EvaluatePolicy`, `CheckRequirement`, `ResolveGap` | Deterministic calls | Policy result |
| **Agents** | `AgentInvoke`, `AgentEscalate`, `BudgetCheck` | Agent orchestration | `EvidenceProposal` |
| **Control** | `Branch`, `Parallel`, `Merge`, `Loop`, `Halt` | Flow control | Flow directive |
| **Human** | `ApprovalGate`, `ReviewTask`, `Override` | Human-in-the-loop | `Escalation` or approval |
| **Chain** | `AnchorBatch`, `VerifyInclusion` | Blockchain | `AnchorReceipt` |
| **Terminal** | `EmitVerdictRequest`, `End` | Handoff to `tawheed` | Verdict request only |

## Node contract

Every node declares:

```python
class NodeContract(BaseModel):
    node_type: str
    version: str
    inputs: list[PortSpec]
    outputs: list[PortSpec]
    tools: list[ToolCapability]          # declared capability
    budget_contribution: BudgetDelta     # what it costs
    emits: Literal["EvidenceProposal", "PolicyResult", "FlowDirective",
                   "Escalation", "AnchorReceipt", "VerdictRequest"]
    # Deliberately absent, enforced by CI grep:
    #   verdict, is_halal, approved, status
```

The `emits` field is closed. A node that wants to emit anything else is a
node that should not exist.

## Non-negotiable constraints

1. Every node's output schema validates against `EvidenceProposal` or its
   declared `emits` type
2. The palette refuses to instantiate nodes that would bypass `tawheed`
3. Every composed workflow is serializable to `.halalchain/agents/` Python
   format and round-trippable byte-for-byte
4. Every node displays its tool capability declaration on hover
5. No node may be authored that lacks a `NodeContract`
6. Palette CI runs the same grep as `.halalchain/` for forbidden fields

## Deliverables

| # | Deliverable | Location | Owner |
|---|---|---|---|
| D1 | Node library (all 8 categories) | `HalalChain.Web/Components/Workflow/Nodes/` | UI Platform |
| D2 | Palette panel | `HalalChain.Web/Components/Workflow/Palette/` | UI Platform |
| D3 | Serializer (React Flow JSON ↔ Python workflow) | `HalalChain.Web/Services/Workflow/` | UI Platform |
| D4 | Schema validation layer | `HalalChain.Web/Services/Workflow/Validation/` | UI Platform |
| D5 | Node contract tests | `HalalChain.Platform.Tests/Unit/Workflow/` | UI Platform |
| D6 | Round-trip test suite | `HalalChain.Platform.Tests/Unit/Workflow/Roundtrip/` | UI Platform |
| D7 | Palette documentation | `docs/WORKFLOW-EDITOR.md` | UI Platform |
| D8 | Forbidden-node CI grep | `infrastructure/ci/check-palette.sh` | Platform Eng |

## Exit gate criteria

| # | Criterion | Evidence | Verifier |
|---|---|---|---|
| G7.1 | All 8 node categories implemented | Directory listing | Architecture Review Board |
| G7.2 | Zero verdict-emitting nodes | CI grep clean | Platform Eng |
| G7.3 | Round-trip test passes for 20+ composed workflows | Test report | UI Platform |
| G7.4 | Serializer emits valid Python accepted by `.halalchain/agents/` | Integration test | Platform Eng |
| G7.5 | Every node has a `NodeContract` | Schema validation | Architecture Review Board |
| G7.6 | Node hover shows tool capability | Manual test | UI Platform |
| G7.7 | Palette refuses to compose verdict-bypassing workflows | Negative tests | UI Platform |
| G7.8 | Palette documentation complete | Manual review | UI Platform |

## Risks

| Risk | Likelihood | Impact | Mitigation | Owner |
|---|---|---|---|---|
| A node is added that emits a verdict | Medium | Critical | CI grep; contract schema; round-trip test | UI Platform |
| Serializer drift from `.halalchain/` format | High | High | Round-trip test on every commit | UI Platform |
| Node contract schema too loose | Medium | High | Closed `emits` enum; schema reviewed by Arch Board | UI Platform |
| xyflow API breaking change | Medium | Medium | Pin version; abstract behind wrapper | UI Platform |
| Palette becomes a shadow workflow engine | Low | Critical | All nodes call deterministic services; no local execution | Architecture Review Board |
| Missing tool capability declaration | Medium | Medium | Schema requires `tools` field | UI Platform |

## RACI

| Activity | UI Platform | Platform Arch | Platform Eng | Compliance | Arch Review Board |
|---|---|---|---|---|---|
| Node library | R | A | C | C | C |
| Palette panel | R | A | I | I | I |
| Serializer | R | A | C | I | C |
| Schema validation | R | A | C | C | C |
| Round-trip tests | R | C | C | I | I |
| Forbidden-node CI | C | A | R | C | I |
| Documentation | R | A | I | C | I |

## Out of scope

- Auto-layout (ENT-008)
- Editor completion (ENT-008)
- Workflow execution (runs in `.halalchain/agents/`)

## Handoff to ENT-008

At phase close, ENT-008 receives:

- Complete node library with contracts
- Palette panel with capability display
- Serializer with round-trip guarantee
- Schema validation layer
- All tests passing
- Documentation

Verification:

```bash
.cline_inbox/hooks/gate-check.sh gate-007-008
```

A failed gate means the palette is incomplete. Extend the phase; do not
proceed to editor completion.
