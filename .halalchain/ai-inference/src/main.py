from fastapi import FastAPI, HTTPException, Depends
from fastapi.middleware.cors import CORSMiddleware
from contextlib import asynccontextmanager
import logging
import time

from .config import settings
from .models import *
from .cache import cache
from .health import get_health, request_count
from .embeddings import generate_embedding
from .classifier import classify_halal, classify_generic
from .summarizer import summarize
from .reranker import rerank
from .ingredient_parser import parse_and_summarize
from .certificate_extractor import extract_certificate
from .auth import verify_api_key, ensure_auth_configured
from .observability import (
    init_tracing, setup_logging, get_metrics,
    measure_request, record_llm_tokens, record_cache_hit, record_cache_miss,
)
from .llm import get_llm_provider
from .vector_store import get_vector_store
from .document_processor import processor

setup_logging()
logger = logging.getLogger(__name__)

# Initialize OpenTelemetry tracing if endpoint is configured
otel_endpoint = settings.otlp_endpoint if hasattr(settings, 'otlp_endpoint') else None
if otel_endpoint:
    init_tracing(service_name="ai-inference", endpoint=otel_endpoint)


HARAM_LABELS = {"halal", "haram", "mashbooh", "unknown"}

# Initialize services
llm_provider = get_llm_provider()
vector_store = get_vector_store()

# Fail-closed auth check: enforce at process startup so a missing/empty
# AI_GATEWAY_API_KEY can never be silently treated as "auth disabled".
ensure_auth_configured(settings)


@asynccontextmanager
async def lifespan(app: FastAPI):
    await vector_store.initialize()
    yield


app = FastAPI(title=settings.app_name, version=settings.app_version, lifespan=lifespan)

# CORS: explicit allow-list only. Wildcard origins are NEVER combined with
# credentials. In development the default list is the loopback origins of
# the local .NET and Blazor services; production must be set via
# AI_GATEWAY_ALLOWED_ORIGINS (comma-separated) and must NOT include "*".
_allowed_origins = [o.strip() for o in settings.allowed_origins.split(",") if o.strip()]
if not _allowed_origins:
    if settings.environment.lower() in ("production", "prod"):
        raise RuntimeError(
            "AI_GATEWAY_ALLOWED_ORIGINS is required in production. "
            "Wildcard origins are not allowed."
        )
    _allowed_origins = [
        "http://localhost:5001",
        "http://localhost:5200",
        "http://localhost:5201",
    ]
if "*" in _allowed_origins:
    raise RuntimeError(
        "Wildcard origin ('*') is not permitted in AI_GATEWAY_ALLOWED_ORIGINS. "
        "Specify explicit origins."
    )
app.add_middleware(
    CORSMiddleware,
    allow_origins=_allowed_origins,
    allow_methods=["GET", "POST", "OPTIONS"],
    allow_headers=["Authorization", "Content-Type", "X-API-Key", "X-Correlation-ID"],
    allow_credentials=True,
)


@app.get("/")
async def root():
    return {"service": settings.app_name, "version": settings.app_version, "docs": "/docs"}


@app.get("/health/live")
async def health_live():
    return {"status": "ok", "service": settings.app_name}


@app.get("/health/ready")
async def health_ready():
    deps = await vector_store.get_stats()
    deps_ok = bool(deps.get("enabled")) if settings.vector_db_provider != "none" else True
    status = "ok" if deps_ok else "degraded"
    code = 200 if deps_ok else 503
    from fastapi.responses import JSONResponse
    return JSONResponse(status_code=code, content={"status": status, "service": settings.app_name, "dependencies": deps})


@app.get("/metrics")
async def metrics():
    return get_metrics()


@app.post("/health")
async def health():
    stats = cache.stats() if cache else {"size": 0, "hits": 0, "misses": 0, "hitRate": "N/A"}
    return get_health(stats)


@app.post("/embeddings")
async def embeddings(req: EmbeddingRequest, auth: bool = Depends(verify_api_key)):
    global request_count
    request_count += 1
    start = time.time()
    if not req.text:
        raise HTTPException(400, "text is required")
    model = req.model or "halalchain-local-v1"
    ck = "emb:" + req.text
    if cache:
        cached = cache.get(ck)
        if cached is not None:
            record_cache_hit("/embeddings")
            record_duration("/embeddings", time.time() - start)
            return EmbeddingResponse(model=model, embedding=cached, cached=True)
    record_cache_miss("/embeddings")
    vec = generate_embedding(req.text)
    if cache:
        cache.set(ck, vec)
    record_request("/embeddings")
    record_duration("/embeddings", time.time() - start)
    return EmbeddingResponse(model=model, embedding=vec)


@app.post("/summarize")
async def summarize_endpoint(req: SummarizeRequest, auth: bool = Depends(verify_api_key)):
    global request_count
    request_count += 1
    start = time.time()
    model = req.model or "halalchain-local-v1"
    max_t = max(1, min(req.maxTokens or 80, 500))
    result = SummarizeResponse(model=model, summary=summarize(req.text, max_t))
    record_request("/summarize")
    record_duration("/summarize", time.time() - start)
    return result


@app.post("/classify")
async def classify_endpoint(req: ClassifyRequest, auth: bool = Depends(verify_api_key)):
    global request_count
    request_count += 1
    start = time.time()
    if not req.labels:
        raise HTTPException(400, "labels is required")
    model = req.model or "halalchain-local-v1"
    ck = "cls:" + req.text + "|" + ",".join(sorted(req.labels))
    if cache:
        cached = cache.get(ck)
        if cached is not None:
            record_cache_hit("/classify")
            record_duration("/classify", time.time() - start)
            return ClassifyResponse(**cached, cached=True)
    record_cache_miss("/classify")
    is_halal = all(l.lower() in HARAM_LABELS for l in req.labels)
    scores = classify_halal(req.text, req.labels) if is_halal else classify_generic(req.text, req.labels)
    best = max(scores, key=scores.get)
    result = {"model": model, "bestLabel": best, "bestScore": scores[best], "scores": scores}
    if cache:
        cache.set(ck, result)
    record_request("/classify")
    record_duration("/classify", time.time() - start)
    return ClassifyResponse(**result)


@app.post("/rerank")
async def rerank_endpoint(req: RerankRequest, auth: bool = Depends(verify_api_key)):
    global request_count
    request_count += 1
    start = time.time()
    model = req.model or "halalchain-local-v1"
    results = rerank(req.query, req.passages)
    record_request("/rerank")
    record_duration("/rerank", time.time() - start)
    return RerankResponse(model=model, results=[RankedPassage(**r) for r in results])


@app.post("/ingredient-parse")
async def ingredient_parse_endpoint(req: IngredientParseRequest, auth: bool = Depends(verify_api_key)):
    global request_count
    request_count += 1
    start = time.time()
    if not req.text:
        raise HTTPException(400, "text is required")
    model = "halalchain-local-v1"
    result = parse_and_summarize(req.text)
    record_request("/ingredient-parse")
    record_duration("/ingredient-parse", time.time() - start)
    return IngredientParseResponse(model=model, parsed=[IngredientItem(**i) for i in result["parsed"]], summary=result["summary"])


@app.post("/certificate-extract")
async def certificate_extract_endpoint(req: CertificateExtractRequest, auth: bool = Depends(verify_api_key)):
    global request_count
    request_count += 1
    start = time.time()
    if not req.text:
        raise HTTPException(400, "text is required")
    model = "halalchain-local-v1"
    parsed = extract_certificate(req.text)
    record_request("/certificate-extract")
    record_duration("/certificate-extract", time.time() - start)
    return CertificateExtractResponse(model=model, parsed=parsed, completeness=f"{len(parsed)}/6 fields extracted")


@app.post("/rag/add-documents")
async def rag_add_documents(documents: list, auth: bool = Depends(verify_api_key)):
    """Add documents to the vector store for RAG."""
    chunks = []
    for doc in documents:
        content = doc.get("content", "")
        if not content:
            continue
        text_chunks = processor.chunk_text(content, settings.rag_chunk_size, settings.rag_chunk_overlap)
        for i, chunk in enumerate(text_chunks):
            embedding = generate_embedding(chunk)
            chunks.append({
                "id": f"{doc.get('id', 'doc')}_{i}",
                "content": chunk,
                "embedding": embedding,
                "metadata": {"source": doc.get("source", ""), "chunk_index": i, **doc.get("metadata", {})}
            })
    await vector_store.add_documents(chunks)
    return {"status": "success", "chunks_added": len(chunks)}


@app.post("/rag/search")
async def rag_search(query: str, top_k: int = 5, auth: bool = Depends(verify_api_key)):
    """Search the vector store for similar documents."""
    embedding = generate_embedding(query)
    results = await vector_store.search(embedding, top_k)
    return {"query": query, "results": results}


@app.post("/rag/clear")
async def rag_clear(auth: bool = Depends(verify_api_key)):
    """Clear all documents from the vector store."""
    await vector_store.clear()
    return {"status": "success", "message": "Vector store cleared"}


@app.post("/llm/generate")
async def llm_generate(prompt: str, max_tokens: int = 100, temperature: float = 0.7, auth: bool = Depends(verify_api_key)):
    """Generate text using the configured LLM provider."""
    response = await llm_provider.generate(prompt, max_tokens, temperature)
    return {"prompt": prompt, "response": response, "provider": settings.default_llm_provider}


@app.post("/cache/clear")
async def cache_clear(auth: bool = Depends(verify_api_key)):
    if cache:
        cache.cache.clear()
    return {"status": "success", "message": "Cache cleared"}


@app.get("/cache/stats")
async def cache_stats():
    if cache:
        return cache.stats()
    return {"enabled": False}


@app.post("/process/document")
async def process_document(file_path: str, auth: bool = Depends(verify_api_key)):
    """Process a document for RAG ingestion.

    The ``file_path`` MUST point to a regular file inside the configured
    document root (or an explicitly registered document directory). The
    processor refuses symlinks, traversal outside the root, unsupported
    extensions, and files larger than ``settings.max_document_size``.
    """
    try:
        result = processor.process_file(file_path)
    except FileNotFoundError as exc:
        raise HTTPException(status_code=404, detail=str(exc)) from exc
    except PermissionError as exc:
        raise HTTPException(status_code=403, detail=str(exc)) from exc
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    if result.get("metadata", {}).get("error"):
        # Re-classify: a path-traversal / unsupported / oversize / not-a-file
        # error from the processor is a 4xx, not a 200 with an empty body.
        err = result["metadata"]["error"]
        status = 400
        if "not found" in err.lower() or "does not exist" in err.lower():
            status = 404
        elif "symlink" in err.lower() or "permission" in err.lower() or "outside" in err.lower():
            status = 403
        elif "too large" in err.lower() or "size" in err.lower():
            status = 413
        raise HTTPException(status_code=status, detail=err)
    return result


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(
    "src.main:app", host="0.0.0.0", port=settings.port
)
