from __future__ import annotations
from fastapi import APIRouter
from src.api.schemas import VerifyProductRequest, VerifyProductResponse
from src.core.models import EvidenceSignals, RiskSignals
from src.core.config import get_settings
from src.policy import engine
from src.agents.orchestrator import AgentOrchestrator

router = APIRouter(prefix="/v1")

# Singleton orchestrator
_orchestrator = AgentOrchestrator()


@router.post("/products/{product_id}/verify", response_model=VerifyProductResponse)
async def verify_product(product_id: str, request: VerifyProductRequest) -> VerifyProductResponse:
    """
    Full halal verification workflow.
    AI agents collect evidence signals; the Policy Engine makes the compliance decision.
    """
    settings = get_settings()

    # Build context from request
    context = {
        "certificate_number": request.certificate_number or "",
        "certification_body": request.certification_body or "",
        "jurisdiction": request.jurisdiction,
        "expiry_date": request.expiry_date or "",
        "issue_date": request.issue_date or "",
        "scope": request.scope or "",
        "ingredients": request.ingredients or "",
        "supplier_name": request.supplier_name or "",
        "supplier_country": request.supplier_country or "",
        "supplier_status": request.supplier_status or "",
        "vendor_country": request.vendor_country or "",
        "documents": request.documents or [],
    }

    # In demo mode, agents still run but with limited data
    if settings.demo_mode:
        # Run agents with available context
        evidence, risk, missing, agent_results = await _orchestrator.collect_evidence(product_id, context)
    else:
        # Full evidence collection from all agents
        evidence, risk, missing, agent_results = await _orchestrator.collect_evidence(product_id, context)

    result = engine.evaluate(
        product_id=product_id,
        evidence=evidence,
        risk=risk,
        missing_evidence=missing,
        policy_version=request.policy_version,
        jurisdiction=request.jurisdiction,
    )
    return VerifyProductResponse(result=result)


@router.get("/agents/health")
async def agent_health() -> dict:
    """Check health of all evidence collection agents."""
    return _orchestrator.get_agent_health()


@router.get("/policies")
async def list_policies() -> dict:
    return {"policies": list(engine.POLICY_VERSIONS.keys())}
