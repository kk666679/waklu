from __future__ import annotations
from fastapi import APIRouter, HTTPException, status
from src.api.schemas import (
    SensorObservationRequest,
    SensorObservationResponse,
    VerifyProductRequest,
    VerifyProductResponse,
)
from src.adapters.iot import ObservationRejected, ObservationStore, ObservationVerifier
from src.core.models import EvidenceSignals, RiskSignals
from src.core.config import get_settings
from src.policy import engine
from src.agents.orchestrator import AgentOrchestrator
from src.observability import iot_observations_accepted_total, iot_observations_rejected_total

router = APIRouter(prefix="/v1")

# Singleton orchestrator
_orchestrator = AgentOrchestrator()
_settings = get_settings()
_observation_store = ObservationStore(
    ObservationVerifier(_settings.iot_device_keys, _settings.iot_max_clock_skew_seconds),
    _settings.iot_observation_store_path,
)


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


@router.post(
    "/evidence/sensor-observations",
    response_model=SensorObservationResponse,
    status_code=status.HTTP_202_ACCEPTED,
)
async def ingest_sensor_observation(
    observation: SensorObservationRequest,
) -> SensorObservationResponse:
    """Accept signed device telemetry as evidence; never evaluate a verdict here."""
    if not _settings.iot_enabled:
        raise HTTPException(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
            detail="IoT evidence ingestion is disabled",
        )
    try:
        _observation_store.accept(observation)
    except ObservationRejected as exc:
        reason = str(exc)
        iot_observations_rejected_total.labels(reason=reason).inc()
        raise HTTPException(status_code=status.HTTP_422_UNPROCESSABLE_ENTITY, detail=reason) from exc

    iot_observations_accepted_total.labels(metric=observation.metric).inc()
    return SensorObservationResponse(observation_id=str(observation.id))
