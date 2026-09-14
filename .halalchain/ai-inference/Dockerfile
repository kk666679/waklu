# Pinned to a specific Python 3.11 patch release for reproducibility.
# digest pinning is left to the build registry (Cosign / GHCR) so the
# image signature can be verified.
#
# Build context is the parent .halalchain/ directory (so the _shared
# package and the service source are both available). The image is
# then responsible for putting the service sources at /app/src.
FROM python:3.11.11-slim-bookworm AS runtime

# Run as a non-root user. The standard `python:3.11-slim` base ships
# with a `python` user; we reuse it.
ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    PIP_NO_CACHE_DIR=1 \
    PIP_DISABLE_PIP_VERSION_CHECK=1

WORKDIR /app

# System dependencies: only `tini` for clean signal handling and `wget`
# for the healthcheck probe. We do NOT install build-essential in the
# final image.
RUN apt-get update \
    && apt-get install -y --no-install-recommends tini wget \
    && rm -rf /var/lib/apt/lists/*

# Install shared cross-service package first so Docker can cache it.
COPY _shared /tmp/_shared
RUN pip install --no-cache-dir /tmp/_shared \
    && rm -rf /tmp/_shared

# Service source
COPY ai-inference/requirements.txt ./requirements.txt
RUN pip install --no-cache-dir -r requirements.txt
COPY ai-inference/src ./src

# Create a non-root user and chown the app directory. The stock
# `python:3.11-slim` image ships as root, so we explicitly create the
# user here.
RUN groupadd --system --gid 1001 halalchain \
    && useradd  --system --uid 1001 --gid 1001 --no-create-home halalchain \
    && chown -R halalchain:halalchain /app
USER halalchain

EXPOSE 7071

HEALTHCHECK --interval=30s --timeout=10s --retries=3 --start-period=20s \
    CMD wget -qO- http://localhost:7071/health/live || exit 1

ENTRYPOINT ["/usr/bin/tini", "--"]
CMD ["uvicorn", "src.main:app", "--host", "0.0.0.0", "--port", "7071", "--workers", "1"]
