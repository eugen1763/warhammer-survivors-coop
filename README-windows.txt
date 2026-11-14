Warhammer Survivors local coop - Windows install

1. In Steam, right-click Warhammer Survivors, then select Manage > Browse local files.
2. Unzip all files from this archive into that folder.
   The file winhttp.dll must be in the same folder as WarhammerSurvivors.exe.
3. Start the game from Steam.
   The first start takes some minutes, because BepInEx generates its assemblies.

How to update
Close the game. Open PowerShell and run this command:
   irm https://raw.githubusercontent.com/eugen1763/warhammer-survivors-coop/main/install.ps1 | iex
The command downloads the latest release and unzips it into the game folder.

How to play
1. Open the character select page.
2. Press a button on a second controller. Do not use B.
3. Move the stick or the D-pad of player 2 to the left or to the right to change the character.
   Press B to leave.
4. Select the character of player 1 and start the run.

If player 1 uses the keyboard and only one controller is available, set
KeyboardForPlayerOne = true in BepInEx\config\local.warhammersurvivors.coop.cfg.

Weapon cooldowns
The mod shows the weapon cooldown panel of the game for all weapons.
To hide the panel, press ESC in a run, open the options, and clear the tick box "Weapon cooldowns".

More experience
The mod can multiply the experience in a run with more than one player. This option is off by default.
To get more experience, press ESC in a run, open the options, and set the tick box "Double XP in coop".
The entry XpFactor in BepInEx\config\local.warhammersurvivors.coop.cfg sets the factor. The default is 2.

Camera
The mod moves the camera away when the players are far apart.
The entry CameraMaxZoomOut in the same file sets the maximum factor. The default is 1.5. The value 1 keeps the camera of the game.

Known limits
- The automatic test covered one pass of a level-up, from player 1 to player 2.
- A character flashes red while enemies hit it. The game does this, not the mod.

How to remove the mod
Delete winhttp.dll, doorstop_config.ini, .doorstop_version, the folder BepInEx and the folder dotnet
from the game folder.

If the mod does not work, look at BepInEx\LogOutput.log in the game folder.

License
The mod has the MIT License. See WarhammerSurvivorsCoop-LICENSE.txt.
BepInEx has the GNU Lesser General Public License, version 2.1. See WarhammerSurvivorsCoop-THIRD-PARTY-NOTICES.md.
This mod is not an official product of the owners of the game.
