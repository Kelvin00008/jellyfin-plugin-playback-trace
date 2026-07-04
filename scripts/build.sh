#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
dotnet publish "$ROOT/Jellyfin.Plugin.PlaybackTrace/Jellyfin.Plugin.PlaybackTrace.csproj" \
  -c Release \
  -o "$ROOT/dist/PlaybackTrace"

