# .halalchain/local-models/Dockerfile
# Multi-stage build for local model inference service

FROM python:3.11-slim AS builder

WORKDIR /app

# Install build dependencies
RUN apt-get update && apt-get install -y --no-install-recommends \
    gcc \
    g++ \
    && rm -rf /var/lib/apt/lists/*

# Copy pyproject.toml and install dependencies
COPY local-models/pyproject.toml ./
COPY _shared/pyproject.toml _shared/

# Install package with inference extras
RUN pip install --no-cache-dir --upgrade pip setuptools wheel && \
    pip install --no-cache-dir -e . --extra inference --extra quantization --extra gguf --extra optimized

FROM python:3.11-slim AS runtime

WORKDIR /app

# Install runtime dependencies
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Copy installed packages from builder
COPY --from=builder /usr/local/lib/python3.11/site-packages /usr/local/lib/python3.11/site-packages
COPY --from=builder /usr/local/bin /usr/local/bin

# Copy application code
COPY local-models/src ./src
COPY _shared/src/halalchain_shared /usr/local/lib/python3.11/dist-packages/halalchain_shared

# Create non-root user
RUN useradd --no-create-home --uid 1000 appuser && \
    mkdir -p /models && chown -R appuser:appuser /models /app
USER appuser

EXPOSE 8080

ENV PYTHONUNBUFFERED=1 \
    PYTHONDONTWRITEBYTECODE=1 \
    PATH="/usr/local/bin:${PATH}"

HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health/live || exit 1

CMD ["python", "-m", "halalchain_local_models.main"]