#!/usr/bin/env bash
# Builds the plugin and makes the Windows archive in dist/.
# The archive contains BepInEx 6 and the plugin. A Windows user unzips it into the game folder.
set -euo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
VERSION="$(sed -nE 's:.*<Version>(.*)</Version>.*:\1:p' "$HERE/WarhammerSurvivorsCoop.csproj")"
STAGE="$(mktemp -d)"
trap 'rm -rf "$STAGE"' EXIT

cd "$HERE"
DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 \
    nix --extra-experimental-features 'nix-command flakes' shell nixpkgs#dotnet-sdk_8 -c \
    dotnet build -c Release --nologo -v quiet

unzip -q "$HERE/vendor/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.793.zip" -d "$STAGE"
rm -f "$STAGE/changelog.txt"
mkdir -p "$STAGE/BepInEx/plugins"
cp "$HERE/bin/Release/net6.0/WarhammerSurvivorsCoop.dll" "$STAGE/BepInEx/plugins/"
cp "$HERE/README-windows.txt" "$STAGE/WarhammerSurvivorsCoop-README.txt"

mkdir -p "$HERE/dist"
OUT="$HERE/dist/WarhammerSurvivorsCoop-$VERSION-windows.zip"
rm -f "$OUT"
python3 -c 'import shutil, sys; shutil.make_archive(sys.argv[1][:-4], "zip", sys.argv[2])' "$OUT" "$STAGE"
echo "$OUT"
