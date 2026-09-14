from __future__ import annotations
from typing import Any
from src.agents.base import BaseAgent, AgentEvidence
import logging

logger = logging.getLogger(__name__)


class DocumentAgent(BaseAgent):
    """Verifies documentation completeness for halal compliance.
    
    Checks that all required documents are present and complete:
    - Halal certificate
    - Ingredient declaration
    - Supplier declaration
    - Manufacturing process documentation
    - Traceability records
    """

    name = "document_agent"
    evidence_type = "document"

    REQUIRED_DOCUMENTS = [
        "halal_certificate",
        "ingredient_declaration",
        "supplier_declaration",
    ]

    RECOMMENDED_DOCUMENTS = [
        "manufacturing_process",
        "traceability_records",
        "product_label_image",
    ]

    async def collect(self, product_id: str, context: dict[str, Any]) -> AgentEvidence:
        provided_docs = context.get("documents", [])
        if isinstance(provided_docs, str):
            provided_docs = [d.strip() for d in provided_docs.split(",") if d.strip()]

        provided_set = {d.lower().replace(" ", "_").replace("-", "_") for d in provided_docs}
        
        checks = []
        missing = []
        confidence_factors = []

        # Check required documents
        for doc in self.REQUIRED_DOCUMENTS:
            if doc in provided_set:
                checks.append(f"required:{doc}:present")
                confidence_factors.append(0.9)
            else:
                checks.append(f"required:{doc}:missing")
                missing.append(doc)
                confidence_factors.append(0.0)

        # Check recommended documents
        recommended_found = 0
        for doc in self.RECOMMENDED_DOCUMENTS:
            if doc in provided_set:
                checks.append(f"recommended:{doc}:present")
                recommended_found += 1

        # Bonus for recommended docs
        if self.RECOMMENDED_DOCUMENTS:
            rec_score = recommended_found / len(self.RECOMMENDED_DOCUMENTS)
            confidence_factors.append(0.5 + (rec_score * 0.4))

        # Calculate confidence
        confidence = sum(confidence_factors) / len(confidence_factors) if confidence_factors else 0.0
        doc_score = confidence

        # Traceability signal
        traceability = 0.3
        if "traceability_records" in provided_set:
            traceability = 0.9
        elif "manufacturing_process" in provided_set:
            traceability = 0.7

        return AgentEvidence(
            agent=self.name,
            evidence_type=self.evidence_type,
            confidence=confidence,
            signals={"document_score": doc_score, "traceability": traceability},
            source="document_verification",
            details=f"Checks: {'; '.join(checks)}",
            missing=missing,
        )
