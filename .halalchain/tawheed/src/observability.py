"""Observability module for Project Tawheed.

Provides Prometheus metrics, OpenTelemetry tracing, and structured logging
with trace/correlation context propagation.
"""
from __future__ import annotations

import logging
import time
import hashlib
import json
from contextlib import contextmanager
from typing import Any

from opentelemetry import trace
from opentelemetry.sdk.resources import Resource
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.sdk.trace.export import BatchSpanProcessor
from opentelemetry.exporter.otlp.proto.http.trace_exporter import OTLPSpanExporter
from prometheus_client import Counter, Histogram, Gauge

# ── Prometheus Metrics ──────────────────────────────────────────────────────

tawheed_verdicts_total = Counter(
    "tawheed_verdicts_total",
    "Total verdicts issued",
    ["verdict"],
)

tawheed_verdict_latency_ms = Histogram(
    "tawheed_verdict_latency_ms",
    "Verdict evaluation latency in milliseconds",
    ["verdict"],
    buckets=[10, 25, 50, 100, 250, 500, 1000, 2500, 5000, 10000],
)

tawheed_evidence_errors_total = Counter(
    "tawheed_evidence_errors",
    "Total evidence collection errors",
    ["agent"],
)

tawheed_determinism_violation_total = Counter(
    "tawheed_determinism_violation",
    "Non-determinism detected in policy engine",
    ["subject"],
)

iot_observations_accepted_total = Counter(
    "tawheed_iot_observations_accepted_total",
    "Accepted signed device observations",
    ["metric"],
)

iot_observations_rejected_total = Counter(
    "tawheed_iot_observations_rejected_total",
    "Rejected device observations",
    ["reason"],
)

# ── OpenTelemetry Tracing ───────────────────────────────────────────────────

def init_tracing(service_name: str = "tawheed", endpoint: str = "http://localhost:4317") -> None:
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


# ── Verdict Measurement Context Manager ─────────────────────────────────────

@contextmanager
def measure_verdict(verdict: str):
    """Context manager to measure verdict evaluation latency."""
    start = time.perf_counter()
    try:
        yield
    finally:
        ms = (time.perf_counter() - start) * 1000
        tawheed_verdict_latency_ms.labels(verdict=verdict).observe(ms)


# ── Determinism Invariant ───────────────────────────────────────────────────

_CACHE: dict[str, str] = {}
_CACHE_MAX = 10_000


def _fingerprint(bundle: Any, decision: Any) -> None:
    """Check determinism invariant: same evidence + policy version must produce same verdict.

    Raises RuntimeError if a determinism violation is detected.
    """
    key = hashlib.sha256(
        json.dumps({
            "subject": getattr(bundle, "subject_id", str(bundle)),
            "evidence": sorted(getattr(bundle, "evidence", []) or []),
            "policy_version": getattr(decision, "engine_version", str(decision)),
        }, sort_keys=True).encode()
    ).hexdigest()
    value = getattr(decision, "verdict", str(decision))

    if key in _CACHE:
        if _CACHE[key] != value:
            tawheed_determinism_violation_total.add(1, {"subject": getattr(bundle, "subject_id", "unknown")})
            raise RuntimeError(
                f"DETERMINISM VIOLATION: subject={getattr(bundle, 'subject_id', 'unknown')} "
                f"prior={_CACHE[key]} new={value}"
            )
    else:
        _CACHE[key] = value
        if len(_CACHE) > _CACHE_MAX:
            _CACHE.clear()