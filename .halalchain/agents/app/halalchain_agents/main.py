"""Main entry point for halalchain-agents service."""
from __future__ import annotations

import os
from contextlib import asynccontextmanager
from typing import Any

from fastapi import FastAPI
from pydantic import BaseModel


class HealthResponse(BaseModel):
    status: str
    service: str
    version: str


@asynccontextmanager
async def lifespan(app: FastAPI):
    # Startup
    yield
    # Shutdown


def create_app() -> FastAPI:
    app = FastAPI(
        title="HalalChain Agents Service",
        version="0.1.0",
        lifespan=lifespan,
    )

    @app.get("/health/live", response_model=HealthResponse)
    async def health_live() -> HealthResponse:
        return HealthResponse(
            status="healthy",
            service="halalchain-agents",
            version="0.1.0",
        )

    @app.get("/health/ready", response_model=HealthResponse)
    async def health_ready() -> HealthResponse:
        return HealthResponse(
            status="healthy",
            service="halalchain-agents",
            version="0.1.0",
        )

    @app.get("/")
    async def root() -> dict[str, Any]:
        return {
            "service": "halalchain-agents",
            "version": "0.1.0",
            "description": "Agent orchestration and evidence collection for HalalChain",
        }

    return app


app = create_app()

if __name__ == "__main__":
    import uvicorn

    port = int(os.getenv("PORT", "8080"))
    uvicorn.run(app, host="0.0.0.0", port=port)