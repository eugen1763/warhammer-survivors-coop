#!/usr/bin/env bash
# Installs or updates the mod in Warhammer Survivors on Linux with Steam and Proton.
#
#   ./install.sh              Downloads the latest release and installs it.
#   ./install.sh --build      Builds the plugin from this repository and installs it. Needs Nix.
#   ./install.sh --uninstall  Removes BepInEx and the plugin from the game.
#
# The script finds the game in the Steam libraries. Set STEAM_LIBRARY to use a specific Steam library.
set -euo pipefail

REPO="eugen1763/warhammer-survivors-coop"
APP_ID=3669620
# Wine loads its builtin winhttp.dll by default. BepInEx starts from the winhttp.dll in the game folder.
OVERRIDE='"winhttp"="native,builtin"'

fail() { echo "$1" >&2; exit 1; }

find_library() {
    if [ -n "${STEAM_LIBRARY:-}" ]; then
        echo "$STEAM_LIBRARY"
        return
    fi
    local root index library
    for root in "$HOME/.local/share/Steam" "$HOME/.steam/steam" "$HOME/.var/app/com.valvesoftware.Steam/.local/share/Steam"; do
        index="$root/steamapps/libraryfolders.vdf"
        [ -f "$index" ] || continue
        while IFS= read -r library; do
            if [ -f "$library/steamapps/appmanifest_$APP_ID.acf" ]; then
                echo "$library"
                return
            fi
        done < <(sed -nE 's/^[[:space:]]*"path"[[:space:]]+"(.*)"[[:space:]]*$/\1/p' "$index")
    done
}

MODE="${1:-}"
case "$MODE" in
    ""|--build|--uninstall) ;;
    *) fail "Unknown option: $MODE. Use --build, --uninstall, or no option." ;;
esac

LIBRARY="$(find_library)"
[ -n "$LIBRARY" ] || fail "The script did not find Warhammer Survivors in a Steam library. Set STEAM_LIBRARY to the Steam library that contains the game."
GAME="$LIBRARY/steamapps/common/Warhammer Survivors"
USER_REG="$LIBRARY/steamapps/compatdata/$APP_ID/pfx/user.reg"

[ -d "$GAME" ] || fail "The game folder does not exist: $GAME. Set STEAM_LIBRARY to the Steam library that contains the game."
[ -f "$USER_REG" ] || fail "The Proton prefix does not exist. Start the game one time, then run this script again."
if pgrep -f '[W]arhammerSurvivors[.]exe' >/dev/null; then
    fail "Close the game, then run this script again."
fi

if [ "$MODE" = "--uninstall" ]; then
    rm -rf "$GAME/BepInEx" "$GAME/dotnet"
    rm -f "$GAME/winhttp.dll" "$GAME/doorstop_config.ini" "$GAME/.doorstop_version" "$GAME/changelog.txt" \
        "$GAME"/WarhammerSurvivorsCoop-*
    sed -i "/^$OVERRIDE\$/d" "$USER_REG"
    echo "Removed BepInEx and the plugin."
    exit 0
fi

command -v unzip >/dev/null || fail "Install unzip, then run this script again."

if [ "$MODE" = "--build" ]; then
    HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
    [ -x "$HERE/package.sh" ] || fail "The option --build needs a copy of the repository."
    ARCHIVE="$("$HERE/package.sh" | tail -n 1)"
    echo "Built $(basename "$ARCHIVE")."
else
    command -v curl >/dev/null || fail "Install curl, then run this script again."
    URL="$(curl -fsSL "https://api.github.com/repos/$REPO/releases/latest" \
        | grep -oE '"browser_download_url": *"[^"]+-windows\.zip"' | head -n 1 | sed -E 's/.*"(https[^"]+)"/\1/')"
    [ -n "$URL" ] || fail "The script did not find the archive of the latest release of $REPO."
    DOWNLOAD="$(mktemp -d)"
    trap 'rm -rf "$DOWNLOAD"' EXIT
    ARCHIVE="$DOWNLOAD/$(basename "$URL")"
    echo "Download of $(basename "$URL")."
    curl -fL --progress-bar -o "$ARCHIVE" "$URL"
fi

# The archive contains no files of the folder BepInEx/config, so the configuration stays.
unzip -q -o "$ARCHIVE" -d "$GAME"

if ! grep -qxF "$OVERRIDE" "$USER_REG"; then
    cp "$USER_REG" "$USER_REG.before-coop-mod"
    sed -i "/^\[Software\\\\\\\\Wine\\\\\\\\DllOverrides\]/a $OVERRIDE" "$USER_REG"
fi
grep -qxF "$OVERRIDE" "$USER_REG" || fail "The script could not add the winhttp override to $USER_REG"

echo "Installed $(basename "$ARCHIVE" .zip) in $GAME"
echo "Start Warhammer Survivors from Steam."
echo "The first start takes some minutes, because BepInEx generates its assemblies."
echo "Log file: $GAME/BepInEx/LogOutput.log"
