#!/usr/bin/env bash
# Builds the plugin and installs BepInEx 6 and the plugin into Warhammer Survivors.
# Use "./install.sh --uninstall" to remove all files that this script installed.
set -euo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
LIBRARY="${STEAM_LIBRARY:-$HOME/.local/share/Steam}"
GAME="$LIBRARY/steamapps/common/Warhammer Survivors"
USER_REG="$LIBRARY/steamapps/compatdata/3669620/pfx/user.reg"
BEPINEX_ZIP="$HERE/vendor/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.793.zip"
# Wine loads its builtin winhttp.dll by default. BepInEx starts from the winhttp.dll in the game folder.
OVERRIDE='"winhttp"="native,builtin"'

[ -d "$GAME" ] || { echo "The game folder does not exist: $GAME. Set STEAM_LIBRARY to the Steam library that contains the game." >&2; exit 1; }
[ -f "$USER_REG" ] || { echo "The Proton prefix does not exist. Start the game one time, then run this script again." >&2; exit 1; }
if pgrep -f 'WarhammerSurvivors.exe' >/dev/null; then
    echo "Close the game, then run this script again." >&2
    exit 1
fi

if [ "${1:-}" = "--uninstall" ]; then
    rm -rf "$GAME/BepInEx" "$GAME/dotnet"
    rm -f "$GAME/winhttp.dll" "$GAME/doorstop_config.ini" "$GAME/.doorstop_version" "$GAME/changelog.txt"
    sed -i "/^$OVERRIDE\$/d" "$USER_REG"
    echo "Removed BepInEx and the plugin."
    exit 0
fi

cd "$HERE"
DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 \
    nix --extra-experimental-features 'nix-command flakes' shell nixpkgs#dotnet-sdk_8 -c \
    dotnet build -c Release --nologo -v quiet

# -n keeps an existing BepInEx configuration.
unzip -q -n "$BEPINEX_ZIP" -d "$GAME"
mkdir -p "$GAME/BepInEx/plugins"
cp "$HERE/bin/Release/net6.0/WarhammerSurvivorsCoop.dll" "$GAME/BepInEx/plugins/"

if ! grep -qxF "$OVERRIDE" "$USER_REG"; then
    cp "$USER_REG" "$USER_REG.before-coop-mod"
    sed -i "/^\[Software\\\\\\\\Wine\\\\\\\\DllOverrides\]/a $OVERRIDE" "$USER_REG"
fi
grep -qxF "$OVERRIDE" "$USER_REG" || { echo "The script could not add the winhttp override to $USER_REG" >&2; exit 1; }

echo "Installed. Start Warhammer Survivors from Steam."
echo "The first start takes some minutes, because BepInEx generates its assemblies."
echo "Log file: $GAME/BepInEx/LogOutput.log"
