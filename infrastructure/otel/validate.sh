#!/usr/bin/env bash
set -euo pipefail

for cfg in agent-config.yml gateway-config.yml; do
  echo "Validating $cfg..."
  docker run --rm \
    -v "$PWD/otel/$cfg:/etc/otel/$cfg:ro" \
    otel/opentelemetry-collector-contrib:0.115.0 \
    validate --config=/etc/otel/$cfg
done

echo "OTel configs valid."