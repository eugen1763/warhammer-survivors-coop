# Warhammer Survivors local coop

A BepInEx 6 plugin that enables the hidden shared-screen cooperative mode of Warhammer Survivors for two local players.

The game uses the code of Vampire Survivors. The session code still supports more players. This plugin adds the parts that are missing:

- The join step and the character choice for player 2 on the character select page.
- The move of the second controller from player 1 to player 2, with the controller maps for the menus.
- A patch for the level-up page, which throws an exception when a run has more than one player.
- A correction of the heads-up display, which puts the equipment panels of all players in the same position.

## Status

Tested on Linux with Proton and two virtual Xbox controllers, game build 26581996:

- Player 2 joins on the character select page and selects a character.
- The run starts with two characters on one shared camera.
- Each controller moves only its own character.
- Each player confirms its own level-up.
- The run continues when one player is dead. The game over comes when all players are dead.

- The heads-up display shows one equipment panel for each player.
- Player 2 stays in the session after a run.

Known limits:

- The level-up page showed no button to pass a level-up to the other player in the tests. The check of the game for this button runs without an exception.
- A character flashes red while enemies hit it. The game does this, not the mod.

## Rules of the game for equipment

The cooperative configuration of the game has these values:

- Each player has an own set of weapons and passive items. The level-up of a player gives the item to that player only.
- The game does not offer a weapon or a passive item to a player if a different player owns it.
- A passive item of one player counts for the weapon evolutions of all players.
- The level and the experience are common to all players.

## Windows

Download the archive `WarhammerSurvivorsCoop-<version>-windows.zip` from the releases of this repository.
Unzip it into the game folder. The archive contains its own instructions.

`./package.sh` makes this archive in `dist/`.

## Install on Linux

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
