#!/usr/bin/env bash
set -euo pipefail

echo "Starting observability stack..."
docker compose -f infrastructure/docker-compose.observability.yml up -d

echo "Waiting for services to become ready..."
for i in {1..60}; do
  if curl -fsS http://localhost:9090/-/ready > /dev/null \
     && curl -fsS http://localhost:3000/api/health > /dev/null \
     && curl -fsS http://localhost:3200/ready > /dev/null \
     && curl -fsS http://localhost:3100/ready > /dev/null \
     && curl -fsS http://localhost:9093/-/ready > /dev/null; then
    echo "  all services up"
    break
  fi
  sleep 2
done

echo "Verifying Prometheus targets are UP..."
UP=$(curl -fsS 'http://localhost:9090/api/v1/query?query=count(up==1)' | jq -r '.data.result[0].value[1]')
if [[ "$UP" -lt 5 ]]; then
  echo "Only $UP targets up. Expected >= 5."
  curl -fsS 'http://localhost:9090/api/v1/targets' | jq '.data.activeTargets[] | {job: .labels.job, health: .health}'
  exit 1
fi

echo "Verifying rules are loaded..."
RULES=$(curl -fsS 'http://localhost:9090/api/v1/rules' | jq '.data.groups | length')
echo "  $RULES rule groups loaded"

echo "Verifying dashboards are provisioned..."
DASH=$(curl -fsS -u admin:${GRAFANA_ADMIN_PASSWORD:-admin} \
  'http://localhost:3000/api/search?type=dash-db' | jq 'length')
echo "  $DASH dashboards provisioned"

echo "Observability stack is healthy."