"""FastAPI application entry point for Project Tawheed."""
from __future__ import annotations
import logging
import os
from contextlib import asynccontextmanager
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from src.api.routes import router
from src.core.config import get_settings
from src.observability import init_tracing, setup_logging

setup_logging()
logging.basicConfig(level=logging.INFO, format="%(asctime)s %(name)s %(levelname)s %(message)s")
logger = logging.getLogger(__name__)
settings = get_settings()

# Initialize OpenTelemetry tracing if endpoint is configured
if settings.otlp_endpoint:
    init_tracing(service_name="tawheed", endpoint=settings.otlp_endpoint)


@asynccontextmanager
async def lifespan(app: FastAPI):
    logger.info(
        "Project Tawheed started | demo_mode=%s | policy=%s | jurisdiction=%s",
        settings.demo_mode, settings.default_policy_version, settings.default_jurisdiction,
    )
    yield
    logger.info("Project Tawheed shutting down.")


app = FastAPI(
    title="Project Tawheed — Halal Product Intelligence Platform",
    description=(
        "Evidence-driven multi-agent AI platform for halal product verification. "
        "AI agents collect evidence; the Policy Engine makes deterministic compliance decisions."
    ),
    version="1.0.0",
    docs_url="/docs",
    redoc_url="/redoc",
    lifespan=lifespan,
)

# CORS — explicit allow-list, never "*" with credentials. In dev we
# default to the local platform services; in production the operator
# MUST set TAWHEED_ALLOWED_ORIGINS.
_allowed = [
    o.strip() for o in os.getenv("TAWHEED_ALLOWED_ORIGINS",
        "http://localhost:5001,http://localhost:5200,http://localhost:5201").split(",")
    if o.strip()
]
if "*" in _allowed:
    raise RuntimeError("Wildcard origin not permitted in TAWHEED_ALLOWED_ORIGINS")
app.add_middleware(
    CORSMiddleware,
    allow_origins=_allowed,
    allow_methods=["GET", "POST", "OPTIONS"],
    allow_headers=["Authorization", "Content-Type", "X-API-Key", "X-Correlation-ID"],
    allow_credentials=True,
)
app.include_router(router)


@app.get("/health")
async def health() -> dict:
    return {
        "status": "ok",
        "service": "tawheed",
        "demo_mode": settings.demo_mode,
        "iot_enabled": settings.iot_enabled,
        "policy_version": settings.default_policy_version,
        "jurisdiction": settings.default_jurisdiction,
    }
