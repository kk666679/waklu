"""DAGMetric for explanation faithfulness - every claim must trace to evidence or trace hash."""
from __future__ import annotations

from deepeval.metrics import DAGMetric


def explanation_faithfulness_metric() -> DAGMetric:
    """
    DAGMetric for verdict explainer faithfulness.

    Gate: every claim in the explanation must trace to:
    - A piece of evidence (by evidence_id)
    - A trace hash (from agent execution)
    - A policy rule citation

    If any claim is ungrounded → score 0, reason "ungrounded_claim"
    """
    return DAGMetric(
        name="explanation_faithfulness",
        dag="""
        # Extract all claims from explanation
        claims = extract_claims(explanation)
        
        # Available grounding sources
        evidence_ids = set(trace.evidence_ids)
        trace_hashes = set(trace.node_hashes)
        policy_rules = set(trace.policy_rules)
        
        ungrounded = []
        for claim in claims:
            # Check if claim references valid grounding
            has_evidence = any(eid in claim for eid in evidence_ids)
            has_trace = any(th in claim for th in trace_hashes)
            has_rule = any(pr in claim for pr in policy_rules)
            
            if not (has_evidence or has_trace or has_rule):
                ungrounded.append(claim)
        
        if ungrounded:
            score = 0
            reason = f"ungrounded_claims:{len(ungrounded)}"
        else:
            score = 1.0
            reason = "all_claims_grounded"
        """,
        threshold=1.0,  # Gate - must be perfect
    )