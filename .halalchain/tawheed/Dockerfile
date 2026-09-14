# Pinned to a specific Python 3.11 patch release for reproducibility.
FROM python:3.11.11-slim-bookworm AS runtime

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    PIP_NO_CACHE_DIR=1 \
    PIP_DISABLE_PIP_VERSION_CHECK=1

WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends tini wget \
    && rm -rf /var/lib/apt/lists/*

COPY _shared /tmp/_shared
RUN pip install --no-cache-dir /tmp/_shared \
    && rm -rf /tmp/_shared

COPY tawheed/requirements.txt ./requirements.txt
RUN pip install --no-cache-dir -r requirements.txt

COPY tawheed/src ./src

# Create a non-root user. The stock `python:3.11-slim` image ships
# as root, so we explicitly create the user here.
RUN groupadd --system --gid 1001 halalchain \
    && useradd  --system --uid 1001 --gid 1001 --no-create-home halalchain \
    && chown -R halalchain:halalchain /app
USER halalchain

EXPOSE 8000

HEALTHCHECK --interval=30s --timeout=10s --retries=3 --start-period=20s \
    CMD wget -qO- http://localhost:8000/health || exit 1

ENTRYPOINT ["/usr/bin/tini", "--"]
CMD ["uvicorn", "src.main:app", "--host", "0.0.0.0", "--port", "8000"]
