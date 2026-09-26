#!/usr/bin/env bash
# prop01-five-deployments.sh
#
# Property 1: All five services render a Deployment for any valid values
#
# For any valid values.yaml where all five services have non-"latest" image tags,
# executing `helm template` must produce exactly one Deployment resource per service
# (five total) when neither blueGreen.enabled nor canary.enabled is true.
#
# Validates: Requirements 1.1

set -euo pipefail

CHART_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TEST_TAG="abc123sha"
RELEASE_NAME="halalchain"

# Collect results — track pass/fail independently so we always print full output
PASS_COUNT=0
FAIL_COUNT=0

pass() {
  echo "  PASS: $*"
  (( PASS_COUNT++ )) || true
}

fail() {
  echo "  FAIL: $*"
  (( FAIL_COUNT++ )) || true
}

echo "========================================"
echo "Property 1: Five standard Deployments"
echo "========================================"
echo

echo "Running helm template in standard mode"
echo "  blueGreen.enabled=false  canary.enabled=false"
echo "  image.tag=${TEST_TAG} for all five services"
echo

# Run helm template with all five services using a valid (non-"latest") tag
TEMPLATE_OUTPUT=$(helm template "${RELEASE_NAME}" "${CHART_DIR}" \
  --set blueGreen.enabled=false \
  --set canary.enabled=false \
  --set "platformApi.image.tag=${TEST_TAG}" \
  --set "web.image.tag=${TEST_TAG}" \
  --set "marketplace.image.tag=${TEST_TAG}" \
  --set "aiInference.image.tag=${TEST_TAG}" \
  --set "tawheed.image.tag=${TEST_TAG}" \
  2>&1)

HELM_EXIT=$?

if [[ ${HELM_EXIT} -ne 0 ]]; then
  echo "FATAL: helm template failed (exit ${HELM_EXIT}):"
  echo "${TEMPLATE_OUTPUT}"
  exit 1
fi

echo "----------------------------------------"
echo "Assertion 1: Deployment count equals 5"
echo "----------------------------------------"

# Count lines that are exactly "kind: Deployment" (the YAML key)
DEPLOYMENT_COUNT=$(echo "${TEMPLATE_OUTPUT}" | grep -c '^kind: Deployment$' || true)

if [[ "${DEPLOYMENT_COUNT}" -eq 5 ]]; then
  pass "Deployment count = ${DEPLOYMENT_COUNT} (expected 5)"
else
  fail "Deployment count = ${DEPLOYMENT_COUNT} (expected 5)"
fi

echo
echo "----------------------------------------"
echo "Assertion 2: Expected Deployment names present"
echo "----------------------------------------"

EXPECTED_NAMES=(
  "platform-api"
  "web"
  "marketplace"
  "ai-inference"
  "tawheed"
)

for service in "${EXPECTED_NAMES[@]}"; do
  expected_name="${RELEASE_NAME}-${service}"
  if echo "${TEMPLATE_OUTPUT}" | grep -q "name: ${expected_name}$"; then
    pass "Deployment name '${expected_name}' found"
  else
    fail "Deployment name '${expected_name}' NOT found"
  fi
done

echo
echo "----------------------------------------"
echo "Assertion 3: No extra unexpected Deployments"
echo "----------------------------------------"

# Extract all Deployment names from the rendered output.
# We look for the metadata.name field immediately following a "kind: Deployment" line.
RENDERED_NAMES=$(echo "${TEMPLATE_OUTPUT}" | awk '
  /^kind: Deployment$/ { capture = 1; next }
  capture && /^  name:/ { print $2; capture = 0 }
')

while IFS= read -r rendered_name; do
  [[ -z "${rendered_name}" ]] && continue
  found=false
  for service in "${EXPECTED_NAMES[@]}"; do
    if [[ "${rendered_name}" == "${RELEASE_NAME}-${service}" ]]; then
      found=true
      break
    fi
  done
  if [[ "${found}" == true ]]; then
    pass "Rendered Deployment '${rendered_name}' is in the expected set"
  else
    fail "Rendered Deployment '${rendered_name}' is UNEXPECTED"
  fi
done <<< "${RENDERED_NAMES}"

echo
echo "========================================"
if [[ "${FAIL_COUNT}" -eq 0 ]]; then
  echo "OVERALL: PASS (${PASS_COUNT} assertions passed, ${FAIL_COUNT} failed)"
  echo "========================================"
  exit 0
else
  echo "OVERALL: FAIL (${PASS_COUNT} assertions passed, ${FAIL_COUNT} failed)"
  echo "========================================"
  exit 1
fi
