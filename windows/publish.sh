#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
sdk="${DOTNET:-dotnet}"
for rid in win-x64 win-arm64; do
  output=".build/windows/$rid"
  "$sdk" publish windows/OffDutyCounter.csproj -c Release -r "$rid" --self-contained true \
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:EnableCompressionInSingleFile=true -p:DebugType=None -o "$output"
done
