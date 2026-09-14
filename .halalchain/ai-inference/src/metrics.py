from prometheus_client import Counter, Histogram, Gauge, generate_latest
from fastapi import Response
import time

# Prometheus metrics
REQUEST_COUNT = Counter(
    'halalchain_requests_total',
    'Total requests processed',
    ['endpoint', 'method', 'status']
)

REQUEST_DURATION = Histogram(
    'halalchain_request_duration_seconds',
    'Request duration in seconds',
    ['endpoint'],
    buckets=[0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1.0, 2.5, 5.0, 10.0]
)

ACTIVE_REQUESTS = Gauge(
    'halalchain_active_requests',
    'Number of active requests'
)

CACHE_HITS = Counter(
    'halalchain_cache_hits_total',
    'Total cache hits',
    ['endpoint']
)

CACHE_MISSES = Counter(
    'halalchain_cache_misses_total',
    'Total cache misses',
    ['endpoint']
)


def record_request(endpoint: str, method: str = "POST", status: str = "success"):
    """Record a completed request."""
    REQUEST_COUNT.labels(endpoint=endpoint, method=method, status=status).inc()


def record_duration(endpoint: str, duration: float):
    """Record request duration."""
    REQUEST_DURATION.labels(endpoint=endpoint).observe(duration)


def record_cache_hit(endpoint: str):
    """Record a cache hit."""
    CACHE_HITS.labels(endpoint=endpoint).inc()


def record_cache_miss(endpoint: str):
    """Record a cache miss."""
    CACHE_MISSES.labels(endpoint=endpoint).inc()


def get_metrics() -> Response:
    """Get Prometheus metrics."""
    return Response(
        content=generate_latest(),
        media_type="text/plain"
    )
