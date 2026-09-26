#!/usr/bin/env bash
# prop10-latest-tag-fails.sh
#
# Property 10: "latest" tag always causes template rendering to fail
#
# For any values.yaml where any application service image tag equals "latest",
# helm template must exit with a non-zero status code and emit the _helpers.tpl
# error message.
#
# Validates: Requirements 7.4
#
# Usage: ./prop10-latest-tag-fails.sh [--chart <path>]
#   --chart  Path to the Helm chart directory (default: resolves relative to this script)

set -euo pipefail

# ── Resolve chart path ────────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CHART_DIR="${CHART_DIR:-"${SCRIPT_DIR}/.."}"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --chart) CHART_DIR="$2"; shift 2 ;;
    *) echo "Unknown argument: $1" >&2; exit 1 ;;
  esac
done

# ── Constants ─────────────────────────────────────────────────────────────────
VALID_SHA="sha256abc1234567890abcdef"
EXPECTED_ERROR="Image tag 'latest' is not permitted"

# Five services: display name → values path for the image tag
declare -A SERVICES
SERVICES["platform-api"]="platformApi.image.tag"
SERVICES["web"]="web.image.tag"
SERVICES["marketplace"]="marketplace.image.tag"
SERVICES["ai-inference"]="aiInference.image.tag"
SERVICES["tawheed"]="tawheed.image.tag"

# Ordered list for deterministic output
SERVICE_ORDER=("platform-api" "web" "marketplace" "ai-inference" "tawheed")

# ── Helpers ───────────────────────────────────────────────────────────────────
pass_count=0
fail_count=0

run_subtest() {
  local display_name="$1"   # human-readable service name
  local latest_path="$2"    # values path being set to "latest"

  # Build --set args: target service gets "latest", all others get the valid SHA
  local set_args=()
  for svc in "${SERVICE_ORDER[@]}"; do
    local path="${SERVICES[$svc]}"
    if [[ "$path" == "$latest_path" ]]; then
      set_args+=(--set "${path}=latest")
    else
      set_args+=(--set "${path}=${VALID_SHA}")
    fi
  done

  # Capture combined output and exit code (helm writes errors to stderr)
  local output
  local exit_code=0
  output=$(helm template halalchain-test "${CHART_DIR}" "${set_args[@]}" 2>&1) || exit_code=$?

  local subtest_passed=true

  # Assert 1: exit code must be non-zero
  if [[ $exit_code -eq 0 ]]; then
    echo "  [ASSERT FAIL] exit code was 0 (expected non-zero) when ${latest_path}=latest"
    subtest_passed=false
  fi

  # Assert 2: stderr must contain the helpers error message
  if ! echo "$output" | grep -qF "$EXPECTED_ERROR"; then
    echo "  [ASSERT FAIL] error message not found in output"
    echo "  Expected substring: ${EXPECTED_ERROR}"
    echo "  Actual output:"
    echo "$output" | sed 's/^/    /'
    subtest_passed=false
  fi

  if $subtest_passed; then
    echo "  PASS: ${display_name} — helm template exited non-zero and emitted the expected error"
    (( pass_count++ )) || true
  else
    echo "  FAIL: ${display_name}"
    (( fail_count++ )) || true
  fi
}

# ── Main ──────────────────────────────────────────────────────────────────────
echo "========================================"
echo " Property 10: 'latest' tag always fails"
echo "========================================"
echo "Chart: ${CHART_DIR}"
echo ""

for svc in "${SERVICE_ORDER[@]}"; do
  path="${SERVICES[$svc]}"
  echo "Sub-test: ${svc} (${path}=latest)"
  run_subtest "$svc" "$path"
  echo ""
done

echo "========================================"
echo " Results: ${pass_count} passed, ${fail_count} failed"
echo "========================================"

if [[ $fail_count -gt 0 ]]; then
  echo "OVERALL: FAIL"
  exit 1
else
  echo "OVERALL: PASS"
  exit 0
fi
