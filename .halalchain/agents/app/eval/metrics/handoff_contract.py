"""DAGMetric for tawheed handoff contract validation."""
from __future__ import annotations

from deepeval.metrics import DAGMetric


def handoff_contract_metric() -> DAGMetric:
    """
    DAGMetric for "does this evidence bundle satisfy tawheed's input contract?"

    Conditional rubric:
    - If any required field missing → score 0, reason "missing_required_field"
    - Else evaluate quality of each field → aggregate score
    """
    return DAGMetric(
        name="tawheed_handoff_contract",
        dag="""
        REQUIRED_FIELDS = ["product_id", "certificate", "ingredients", "manufacturer"]
        
        for field in REQUIRED_FIELDS:
            if field not in bundle or not bundle[field]:
                score = 0
                reason = f"missing_required_field:{field}"
                return
        
        # All required fields present - evaluate quality
        quality_scores = []
        
        # Certificate quality
        cert = bundle["certificate"]
        if cert.get("issuer") and cert.get("expiry") and cert.get("standard"):
            quality_scores.append(1.0)
        else:
            quality_scores.append(0.5)
        
        # Ingredients quality
        ingredients = bundle["ingredients"]
        if isinstance(ingredients, list) and len(ingredients) > 0:
            quality_scores.append(1.0)
        else:
            quality_scores.append(0.3)
        
        # Manufacturer quality
        mfr = bundle["manufacturer"]
        if mfr.get("name") and mfr.get("address"):
            quality_scores.append(1.0)
        else:
            quality_scores.append(0.5)
        
        score = sum(quality_scores) / len(quality_scores)
        reason = "quality_aggregate"
        """,
        threshold=0.7,
    )