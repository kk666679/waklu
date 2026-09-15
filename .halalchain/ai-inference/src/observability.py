"""Observability module for AI Inference Gateway.

Provides Prometheus metrics and OpenTelemetry instrumentation.
"""
from __future__ import annotations

import logging
import time
from contextlib import contextmanager
from typing import Any

from prometheus_client import Counter, Histogram, Gauge, generate_latest
from opentelemetry import trace
from opentelemetry.sdk.resources import Resource
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.sdk.trace.export import BatchSpanProcessor
from opentelemetry.exporter.otlp.proto.http.trace_exporter import OTLPSpanExporter

# ── Prometheus Metrics ──────────────────────────────────────────────────────

ai_inference_requests_total = Counter(
    "ai_inference_requests_total",
    "Total requests processed",
    ["endpoint", "status"],
)

ai_inference_request_duration_seconds = Histogram(
    "ai_inference_request_duration_seconds",
    "Request duration in seconds",
    ["endpoint"],
    buckets=[0.1, 0.5, 1, 2.5, 5, 10, 30, 60],
)

ai_inference_llm_tokens_total = Counter(
    "ai_inference_llm_tokens_total",
    "LLM tokens consumed",
    ["model", "direction"],  # prompt | completion
)

ai_inference_cache_hits_total = Counter(
    "ai_inference_cache_hits_total",
    "Total cache hits",
    ["endpoint"],
)

ai_inference_cache_misses_total = Counter(
    "ai_inference_cache_misses_total",
    "Total cache misses",
    ["endpoint"],
)

ai_inference_active_requests = Gauge(
    "ai_inference_active_requests",
    "Number of active requests",
)


# ── OpenTelemetry Tracing ───────────────────────────────────────────────────

def init_tracing(service_name: str = "ai-inference", endpoint: str = "http://localhost:4317") -> None:
    """Initialize OpenTelemetry tracing with OTLP exporter."""
    resource = Resource.create({
        "service.name": service_name,
        "service.namespace": "halalchain",
    })
    provider = TracerProvider(resource=resource)
    exporter = OTLPSpanExporter(endpoint=endpoint)
    provider.add_span_processor(BatchSpanProcessor(exporter))
    trace.set_tracer_provider(provider)


# ── Log Correlation Filter ──────────────────────────────────────────────────

class TraceContextFilter(logging.Filter):
    """Injects trace_id and span_id into log records."""

    def filter(self, record: Any) -> bool:
        ctx = trace.get_current_span().get_span_context()
        record.trace_id = format(ctx.trace_id, "032x") if ctx.is_valid else ""
        record.span_id = format(ctx.span_id, "016x") if ctx.is_valid else ""
        return True


def setup_logging() -> None:
    """Configure structured JSON logging with trace context."""
    import sys
    from pythonjsonlogger import jsonlogger

    handler = logging.StreamHandler(sys.stdout)
    handler.setFormatter(jsonlogger.JsonFormatter(
        "%(asctime)s %(levelname)s %(name)s %(message)s %(trace_id)s %(span_id)s"
    ))
    handler.addFilter(TraceContextFilter())
    logging.root.handlers = [handler]
    logging.root.setLevel(logging.INFO)


# ── Request Measurement ─────────────────────────────────────────────────────

@contextmanager
def measure_request(endpoint: str):
    """Context manager to measure request duration and count."""
    ai_inference_active_requests.inc()
    ai_inference_requests_total.labels(endpoint=endpoint, status="started").inc()
    start = time.perf_counter()
    try:
        yield
        status = "success"
    except Exception:
        status = "error"
        raise
    finally:
        duration = time.perf_counter() - start
        ai_inference_request_duration_seconds.labels(endpoint=endpoint).observe(duration)
        ai_inference_requests_total.labels(endpoint=endpoint, status=status).inc()
        ai_inference_active_requests.dec()


def get_metrics() -> Any:
    """Get Prometheus metrics as a Response."""
    from fastapi import Response
    return Response(
        content=generate_latest(),
        media_type="text/plain",
    )


def record_llm_tokens(model: str, direction: str, count: int = 1) -> None:
    """Record LLM token consumption."""
    ai_inference_llm_tokens_total.labels(model=model, direction=direction).inc(count)


def record_cache_hit(endpoint: str) -> None:
    """Record a cache hit."""
    ai_inference_cache_hits_total.labels(endpoint=endpoint).inc()


def record_cache_miss(endpoint: str) -> None:
    """Record a cache miss."""
    ai_inference_cache_misses_total.labels(endpoint=endpoint).inc()