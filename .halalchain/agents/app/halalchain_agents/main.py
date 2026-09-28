"""Main entry point for halalchain-agents service.

Exposes the run endpoint the .NET side calls. The response envelope has no
verdict-shaped field on purpose: an agent that tried to return a halal
decision would have it dropped at this boundary rather than carried into the
managed layers as a fact.
"""
from __future__ import annotations

import logging
import os
from contextlib import asynccontextmanager
from typing import Any

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field

from agents.base import EvidenceKind, EvidenceProposal, RunBudget
from agents.collection import EvidenceSource, StaticSource, source_ref
from runtime.dag import WorkflowRunner
from workflows import build

VERSION = "0.1.0"

logging.basicConfig(level=os.getenv("LOG_LEVEL", "INFO"))
logger = logging.getLogger(__name__)


class HealthResponse(BaseModel):
    status: str
    service: str
    version: str


class RunRequest(BaseModel):
    """Body posted by ``HalalChain.Agents.Runtime.AgentWorkflowClient``.

    Field names are camelCase to match System.Text.Json's web defaults on the
    .NET side.
    """

    workflow: str = Field(..., description="Workflow name, e.g. supplier_onboarding")
    input: dict[str, Any] = Field(
        default_factory=dict,
        description="Workflow input. Evidence identifiers, never a verdict.",
    )


@asynccontextmanager
async def lifespan(app: FastAPI):
    logger.info("halalchain-agents %s starting", VERSION)
    yield
    logger.info("halalchain-agents shutting down")


def _default_sources(payload: dict[str, Any]) -> list[EvidenceSource]:
    """Sources derived from the request payload.

    Deliberately conservative: this service does not invent integrations. A
    workflow run only sees evidence the caller identified, which keeps the
    collector's recall measurable against its goldens instead of depending on
    whichever third-party API happened to be reachable.
    """
    sources: list[EvidenceSource] = []

    for item in payload.get("evidence", []) or []:
        if not isinstance(item, dict):
            continue
        kind = EvidenceKind.parse(item.get("kind"))
        key = str(item.get("source") or kind.value.lower())
        proposal = EvidenceProposal(
            kind=kind,
            summary=str(item.get("summary") or f"{kind.value} evidence"),
            confidence=float(item.get("confidence", 0.9)),
            source_refs=[
                source_ref(
                    key,
                    str(item.get("reference") or ""),
                    note="supplied by caller",
                )
            ],
            proposed_evidence=dict(item.get("data") or {}),
        )
        sources.append(
            StaticSource(key=key, proposals=[proposal])
        )

    return sources


def create_app() -> FastAPI:
    app = FastAPI(
        title="HalalChain Agents Service",
        version=VERSION,
        lifespan=lifespan,
    )

    @app.get("/health/live", response_model=HealthResponse)
    async def health_live() -> HealthResponse:
        return HealthResponse(
            status="healthy",
            service="halalchain-agents",
            version=VERSION,
        )

    @app.get("/health/ready", response_model=HealthResponse)
    async def health_ready() -> HealthResponse:
        return HealthResponse(
            status="healthy",
            service="halalchain-agents",
            version=VERSION,
        )

    @app.get("/")
    async def root() -> dict[str, Any]:
        return {
            "service": "halalchain-agents",
            "version": VERSION,
            "description": "Agent orchestration and evidence collection for HalalChain",
        }

    @app.get("/api/v1/workflows")
    async def list_workflows() -> dict[str, Any]:
        """Workflows this deployment can run.

        A workflow with no goldens file has failures nobody will notice, so
        the registry is the allow-list and it is explicit.
        """
        return {
            "workflows": [
                "supplier_onboarding",
                "certificate_review",
                "product_verification",
            ]
        }

    @app.post("/api/v1/workflows/{workflow}/run")
    async def run_workflow(workflow: str, request: RunRequest) -> dict[str, Any]:
        """Run a workflow and return the evidence it collected.

        Returns proposals, not conclusions. There is no verdict field here and
        there will not be one: compliance is tawheed's to decide, and this
        service's job is to hand tawheed evidence worth evaluating.
        """
        try:
            spec = build(workflow, _default_sources(request.input))
        except KeyError as exc:
            raise HTTPException(status_code=404, detail=str(exc)) from exc

        budget = RunBudget(
            max_steps=int(os.getenv("AGENTS_MAX_STEPS", "20")),
            max_wall_seconds=int(os.getenv("AGENTS_MAX_WALL_SECONDS", "300")),
        )

        run = await WorkflowRunner().run(spec, request.input, budget=budget)
        logger.info(
            "workflow=%s run=%s proposals=%d status=%s",
            workflow,
            run.run_id,
            len(run.proposals),
            run.trace.get("status"),
        )

        return run.to_response()

    return app


app = create_app()

if __name__ == "__main__":
    import uvicorn

    port = int(os.getenv("PORT", "8080"))
    uvicorn.run(app, host="0.0.0.0", port=port)
