#!/usr/bin/env bash
set -euo pipefail

docker run --rm \
  -v "$PWD/prometheus:/etc/prometheus:ro" \
  --entrypoint promtool \
  prom/prometheus:v3.0.1 \
  check rules /etc/prometheus/rules/*.yml /etc/prometheus/recording-rules.yml

docker run --rm \
  -v "$PWD/prometheus:/etc/prometheus:ro" \
  --entrypoint promtool \
  prom/prometheus:v3.0.1 \
  check config /etc/prometheus/prometheus.yml

echo "Prometheus rules valid."