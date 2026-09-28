#!/usr/bin/env bash
# Wrapper invoked by the BuildScss target in Directory.Build.targets.
#
# The target's SassEnabled condition requires this file to exist, so keeping
# it here is what re-enables SCSS compilation on every .NET build. It simply
# delegates to scripts/build-scss.mjs so that `npm run scss:build` (used by
# CI and by the Docker styles stages) and `dotnet build` compile exactly the
# same set of stylesheets.
#
# If the node toolchain is unavailable the build is skipped rather than
# failed: the Sass sources are the source of truth, but a developer without
# node should still be able to compile C#.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if ! command -v node >/dev/null 2>&1; then
  echo "BuildScss: node not found on PATH — skipping stylesheet compilation." >&2
  echo "           Install Node >= 22 and run 'npm run scss:build'." >&2
  exit 0
fi

exec node "${SCRIPT_DIR}/build-scss.mjs"
