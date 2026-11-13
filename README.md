# Warhammer Survivors local coop

A BepInEx 6 plugin that enables the hidden shared-screen cooperative mode of Warhammer Survivors for two local players.

The game uses the code of Vampire Survivors. The session code still supports more players. This plugin adds the join step and the character choice for player 2 on the character select page.

## Status

The plugin builds. A test in the game is not done.

## Install

The script expects Linux, Steam with Proton, and Nix for the .NET SDK.

1. Start the game one time without the mod, so that Steam creates the Proton prefix.
2. Set `STEAM_LIBRARY` if the game is not in `/mnt/nvme2/SteamLibrary`.
3. Run `./install.sh`.
4. Start the game from Steam. The first start takes some minutes.

`./install.sh --uninstall` removes BepInEx and the plugin from the game.

## Build references

The folder `lib/` is not in the repository, because it contains assemblies that BepInEx generates from the game.

1. Unzip the BepInEx archive from `vendor/` into the game folder.
2. Add the `winhttp` override. See `install.sh`.
3. Start the game one time.
4. Copy `BepInEx/interop/*.dll` from the game folder to `lib/interop/`.
5. Copy `BepInEx/core/*.dll` from the game folder to `lib/bepinex-core/`.

## Use

1. Open the character select page.
2. Press a button on a second controller. Do not use B.
3. Move the stick or the D-pad of player 2 to the left or to the right to change the character. Press B to leave.
4. Select the character of player 1 and start the run.

If player 1 uses the keyboard and only one controller is available, set `KeyboardForPlayerOne = true` in `BepInEx/config/local.warhammersurvivors.coop.cfg`.
