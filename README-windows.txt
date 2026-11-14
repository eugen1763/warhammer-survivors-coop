Warhammer Survivors local coop - Windows install

1. In Steam, right-click Warhammer Survivors, then select Manage > Browse local files.
2. Unzip all files from this archive into that folder.
   The file winhttp.dll must be in the same folder as WarhammerSurvivors.exe.
3. Start the game from Steam.
   The first start takes some minutes, because BepInEx generates its assemblies.

How to play
1. Open the character select page.
2. Press a button on a second controller. Do not use B.
3. Move the stick or the D-pad of player 2 to the left or to the right to change the character.
   Press B to leave.
4. Select the character of player 1 and start the run.

If player 1 uses the keyboard and only one controller is available, set
KeyboardForPlayerOne = true in BepInEx\config\local.warhammersurvivors.coop.cfg.

Known limits
- The automatic test covered one pass of a level-up, from player 1 to player 2.
- A character flashes red while enemies hit it. The game does this, not the mod.

How to remove the mod
Delete winhttp.dll, doorstop_config.ini, .doorstop_version, the folder BepInEx and the folder dotnet
from the game folder.

If the mod does not work, look at BepInEx\LogOutput.log in the game folder.
