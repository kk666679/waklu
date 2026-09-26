#!/usr/bin/env bash
# prop06-service-selector-slot.sh
#
# Property 6: Service selector always targets the declared active slot
#
# For any activeSlot value in {blue, green} when blueGreen.enabled=true, the
# rendered Service selector for web (halalchain) and marketplace must include
# slot: <activeSlot> and must NOT include the other slot label.
#
# Also asserts that the platform-api Service selector does NOT carry any
# slot: key at all (canary/stable traffic is differentiated by track:, not
# by the Service selector).
#
# Validates: Requirements 2.2, 2.6
#
# Usage: ./prop06-service-selector-slot.sh [--chart <path>]
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
# All image tags must be non-"latest" to pass _helpers.tpl validation.
STABLE_TAG="sha-abc123"
PREVIEW_TAG="preview-xyz"

# ── Counters ──────────────────────────────────────────────────────────────────
pass_count=0
fail_count=0

# ── Helpers ───────────────────────────────────────────────────────────────────

# assert_contains <label> <context_label> <output>
# Asserts that <output> contains the string <label>. Prints PASS/FAIL.
assert_contains() {
  local expected="$1"
  local context="$2"
  local output="$3"

  if echo "$output" | grep -qF "$expected"; then
    echo "  [PASS] $context — selector contains '${expected}'"
    (( pass_count++ )) || true
  else
    echo "  [FAIL] $context — selector does NOT contain '${expected}'"
    (( fail_count++ )) || true
  fi
}

# assert_not_contains <label> <context_label> <output>
# Asserts that <output> does NOT contain the string <label>. Prints PASS/FAIL.
assert_not_contains() {
  local unexpected="$1"
  local context="$2"
  local output="$3"

  if echo "$output" | grep -qF "$unexpected"; then
    echo "  [FAIL] $context — selector unexpectedly contains '${unexpected}'"
    (( fail_count++ )) || true
  else
    echo "  [PASS] $context — selector correctly absent of '${unexpected}'"
    (( pass_count++ )) || true
  fi
}

# extract_service_selector <service-name-suffix> <helm-template-output>
#
# Extracts the selector block of the named Service resource.
# Looks for the Service whose metadata.name ends with -<service-name-suffix>,
# then captures lines between the "selector:" key and the next top-level key
# (i.e. "ports:" or end of document).
#
# Returns the selector block text on stdout.
extract_service_selector() {
  local svc_suffix="$1"
  local template_output="$2"

  # Split into individual documents on "---", find the one whose name matches,
  # then pull out the selector block using awk.
  echo "$template_output" \
    | awk -v svc="$svc_suffix" '
        BEGIN { in_doc=0; found=0; in_selector=0 }
        /^---/ {
          in_doc=1; found=0; in_selector=0
          doc=""
          next
        }
        in_doc {
          doc = doc "\n" $0
          # Detect metadata.name matching our service
          if ($0 ~ "name:.*-" svc "$") { found=1 }
        }
        END {
          # Replay doc and extract selector block
          n = split(doc, lines, "\n")
          printing=0
          for (i=1; i<=n; i++) {
            line = lines[i]
            # Start printing on the "  selector:" line
            if (line ~ /^  selector:/) { printing=1; print line; continue }
            if (printing) {
              # Stop when we hit another top-level key under spec (ports:, type:, etc.)
              if (line ~ /^  [a-z]/ && line !~ /^  selector:/) { printing=0; continue }
              print line
            }
          }
        }
      ' - 2>/dev/null || true
}

# run_slot_test <active_slot>
#
# Renders the chart with blueGreen.enabled=true and the given activeSlot, then
# runs all assertions for Property 6.
run_slot_test() {
  local active_slot="$1"

  if [[ "$active_slot" == "blue" ]]; then
    expected_slot="blue"
    absent_slot="green"
  else
    expected_slot="green"
    absent_slot="blue"
  fi

  echo "──────────────────────────────────────────"
  echo " Run: blueGreen.activeSlot=${active_slot}"
  echo "──────────────────────────────────────────"

  local output
  output=$(helm template halalchain-test "${CHART_DIR}" \
    --set blueGreen.enabled=true \
    --set "blueGreen.activeSlot=${active_slot}" \
    --set "blueGreen.previewTag=${PREVIEW_TAG}" \
    --set "platformApi.image.tag=${STABLE_TAG}" \
    --set "web.image.tag=${STABLE_TAG}" \
    --set "marketplace.image.tag=${STABLE_TAG}" \
    --set "aiInference.image.tag=${STABLE_TAG}" \
    --set "tawheed.image.tag=${STABLE_TAG}" \
    2>&1)

  local exit_code=$?
  if [[ $exit_code -ne 0 ]]; then
    echo "  [FAIL] helm template exited with code ${exit_code}"
    echo "  Output:"
    echo "$output" | sed 's/^/    /'
    (( fail_count++ )) || true
    echo ""
    return
  fi

  # ── web (halalchain) Service assertions ──────────────────────────────────
  local web_selector
  web_selector=$(extract_service_selector "web" "$output")

  echo ""
  echo "  web Service selector:"
  echo "$web_selector" | sed 's/^/    /'

  assert_contains     "slot: ${expected_slot}" "web selector has 'slot: ${expected_slot}'"       "$web_selector"
  assert_not_contains "slot: ${absent_slot}"   "web selector lacks 'slot: ${absent_slot}'"       "$web_selector"

  # ── marketplace Service assertions ────────────────────────────────────────
  local marketplace_selector
  marketplace_selector=$(extract_service_selector "marketplace" "$output")

  echo ""
  echo "  marketplace Service selector:"
  echo "$marketplace_selector" | sed 's/^/    /'

  assert_contains     "slot: ${expected_slot}" "marketplace selector has 'slot: ${expected_slot}'" "$marketplace_selector"
  assert_not_contains "slot: ${absent_slot}"   "marketplace selector lacks 'slot: ${absent_slot}'" "$marketplace_selector"

  # ── platform-api Service assertion (no slot: key at all) ─────────────────
  local api_selector
  api_selector=$(extract_service_selector "platform-api" "$output")

  echo ""
  echo "  platform-api Service selector:"
  echo "$api_selector" | sed 's/^/    /'

  assert_not_contains "slot:" "platform-api selector has no 'slot:' key" "$api_selector"

  echo ""
}

# ── Main ──────────────────────────────────────────────────────────────────────
echo "============================================================"
echo " Property 6: Service selector targets the declared active slot"
echo "============================================================"
echo "Chart: ${CHART_DIR}"
echo ""

run_slot_test "blue"
run_slot_test "green"

echo "============================================================"
echo " Results: ${pass_count} passed, ${fail_count} failed"
echo "============================================================"

if [[ $fail_count -gt 0 ]]; then
  echo "OVERALL: FAIL"
  exit 1
else
  echo "OVERALL: PASS"
  exit 0
fi
