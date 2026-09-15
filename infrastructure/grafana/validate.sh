#!/usr/bin/env bash
set -euo pipefail

for f in grafana/dashboards/*.json; do
  echo "Validating $f"
  jq -e '.title and .panels' "$f" > /dev/null || { echo "Invalid dashboard: $f"; exit 1; }
  jq -e '[.panels[].targets[]?.expr // empty] | length > 0' "$f" > /dev/null \
    || { echo "Dashboard has no panel targets: $f"; exit 1; }
done

echo "Dashboards valid."