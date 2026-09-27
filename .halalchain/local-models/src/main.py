"""FastAPI application entry point for HalalChain Local Models service."""
from __future__ import annotations
from contextlib import asynccontextmanager
from fastapi import FastAPI, HTTPException, Depends
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from typing import List, Optional
import logging
import os
import time

from halalchain_shared.ai_backend import resolve_ai_backend, resolve_ai_provider_config
from halalchain_shared.cache import build_cache

setup_logging()
logger = logging.getLogger(__name__)

# Settings
class Settings:
    app_name: str = "halalchain-local-models"
    app_version: str = "0.1.0"
    port: int = int(os.getenv("PORT", "8080"))
    environment: str = os.getenv("ENVIRONMENT", "development")
    log_level: str = os.getenv("LOG_LEVEL", "INFO")
    ai_backend: str = os.getenv("AI_BACKEND", "local")
    allowed_origins: str = os.getenv("LOCAL_MODELS_ALLOWED_ORIGINS", "http://localhost:5001,http://localhost:5200,http://localhost:5201,http://localhost:7071,http://localhost:8000")
    enable_cache: bool = os.getenv("ENABLE_CACHE", "true").lower() == "true"
    cache_strategy: str = os.getenv("CACHE_STRATEGY", "lru")
    cache_max_size: int = int(os.getenv("CACHE_MAX_SIZE", "10000"))
    cache_ttl: int = int(os.getenv("CACHE_TTL", "3600"))
    redis_url: str = os.getenv("REDIS_URL", "redis://redis:6379/0")

settings = Settings()

# Initialize cache
cache = None
if settings.enable_cache:
    cache = build_cache(
        strategy=settings.cache_strategy,
        max_size=settings.cache_max_size,
        ttl=settings.cache_ttl,
    )

# Initialize AI backend
backend = resolve_ai_backend(default=settings.ai_backend)
provider_config = resolve_ai_provider_config(default_backend=backend)

# Model registry
_models = {}
_current_model = None

class EmbeddingRequest(BaseModel):
    text: str
    model: Optional[str] = None

class EmbeddingResponse(BaseModel):
    model: str
    embedding: List[float]
    cached: bool = False

class ClassifyRequest(BaseModel):
    text: str
    labels: List[str]
    model: Optional[str] = None

class ClassifyResponse(BaseModel):
    model: str
    bestLabel: str
    bestScore: float
    scores: dict
    cached: bool = False

class GenerateRequest(BaseModel):
    prompt: str
    max_tokens: int = 100
    temperature: float = 0.7
    model: Optional[str] = None

class GenerateResponse(BaseModel):
    model: str
    response: str

class HealthResponse(BaseModel):
    status: str
    service: str
    version: str
    backend: str
    models_loaded: int

@asynccontextmanager
async def lifespan(app: FastAPI):
    logger.info("Local Models service starting up | backend=%s", backend)
    # Lazy-load models on first request
    yield
    logger.info("Local Models service shutting down")

app = FastAPI(
    title=settings.app_name,
    version=settings.app_version,
    lifespan=lifespan,
)

# CORS
_allowed_origins = [o.strip() for o in settings.allowed_origins.split(",") if o.strip()]
if not _allowed_origins:
    if settings.environment.lower() in ("production", "prod"):
        raise RuntimeError("LOCAL_MODELS_ALLOWED_ORIGINS is required in production")
    _allowed_origins = [
        "http://localhost:5001",
        "http://localhost:5200",
        "http://localhost:5201",
        "http://localhost:7071",
        "http://localhost:8000",
    ]
if "*" in _allowed_origins:
    raise RuntimeError("Wildcard origin ('*') is not permitted")
app.add_middleware(
    CORSMiddleware,
    allow_origins=_allowed_origins,
    allow_methods=["GET", "POST", "OPTIONS"],
    allow_headers=["Authorization", "Content-Type", "X-API-Key", "X-Correlation-ID"],
    allow_credentials=True,
)

def get_model(model_name: Optional[str] = None):
    """Get or load a model by name."""
    global _current_model
    name = model_name or "default"
    if name in _models:
        return _models[name]
    # Lazy load - in production this would load actual models
    _models[name] = {"name": name, "loaded": True}
    return _models[name]

@app.get("/")
async def root():
    return {"service": settings.app_name, "version": settings.app_version, "docs": "/docs"}

@app.get("/health/live")
async def health_live():
    return {"status": "ok", "service": settings.app_name}

@app.get("/health/ready")
async def health_ready():
    return {"status": "ok", "service": settings.app_name, "backend": backend, "models_loaded": len(_models)}

@app.get("/health", response_model=HealthResponse)
async def health():
    return HealthResponse(
        status="ok",
        service=settings.app_name,
        version=settings.app_version,
        backend=backend,
        models_loaded=len(_models),
    )

@app.post("/embeddings", response_model=EmbeddingResponse)
async def embeddings(req: EmbeddingRequest):
    if not req.text:
        raise HTTPException(400, "text is required")
    model_name = req.model or "default"
    ck = f"emb:{model_name}:{req.text}"
    if cache:
        cached = cache.get(ck)
        if cached is not None:
            return EmbeddingResponse(model=model_name, embedding=cached, cached=True)
    # Generate embedding (placeholder - replace with actual model inference)
    model = get_model(model_name)
    embedding = [0.1] * 384  # Placeholder
    if cache:
        cache.set(ck, embedding)
    return EmbeddingResponse(model=model_name, embedding=embedding)

@app.post("/classify", response_model=ClassifyResponse)
async def classify(req: ClassifyRequest):
    if not req.labels:
        raise HTTPException(400, "labels is required")
    model_name = req.model or "default"
    ck = f"cls:{model_name}:{req.text}|{','.join(sorted(req.labels))}"
    if cache:
        cached = cache.get(ck)
        if cached is not None:
            return ClassifyResponse(**cached, cached=True)
    # Classify (placeholder)
    model = get_model(model_name)
    scores = {label: 0.5 for label in req.labels}
    best = req.labels[0]
    result = {"model": model_name, "bestLabel": best, "bestScore": 0.5, "scores": scores}
    if cache:
        cache.set(ck, result)
    return ClassifyResponse(**result)

@app.post("/generate", response_model=GenerateResponse)
async def generate(req: GenerateRequest):
    model_name = req.model or "default"
    model = get_model(model_name)
    # Generate text (placeholder)
    response = f"Generated response for: {req.prompt[:50]}..."
    return GenerateResponse(model=model_name, response=response)

@app.post("/cache/clear")
async def cache_clear():
    if cache:
        cache._cache.clear()
        cache._expiry.clear()
        cache.hits = 0
        cache.misses = 0
    return {"status": "success", "message": "Cache cleared"}

@app.get("/cache/stats")
async def cache_stats():
    if cache:
        return cache.stats()
    return {"enabled": False}

@app.get("/models")
async def list_models():
    return {"models": list(_models.keys()), "backend": backend}

if __name__ == "__main__":
    import uvicorn
    uvicorn.run("main:app", host="0.0.0.0", port=settings.port)