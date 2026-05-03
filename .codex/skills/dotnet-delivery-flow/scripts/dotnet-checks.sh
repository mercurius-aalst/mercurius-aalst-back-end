#!/usr/bin/env bash
set -euo pipefail

dotnet restore
dotnet build --no-restore
dotnet test --no-build

if command -v dotnet-format >/dev/null 2>&1 || dotnet format --help >/dev/null 2>&1; then
  dotnet format --verify-no-changes
fi
