#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

echo "▸ Building API with generation enabled..."
dotnet build HalalChain.Platform.Api/HalalChain.Platform.Api.csproj \
  -c Release -p:GenerateApiPages=true --nologo

echo "▸ Verifying Web still compiles..."
dotnet build HalalChain.Web/HalalChain.Web.csproj -c Release --nologo

echo "▸ Generated files:"
git status --short HalalChain.Web/Components/Admin/Generated/ || true