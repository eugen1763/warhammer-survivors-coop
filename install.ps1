# Installs or updates the mod in Warhammer Survivors on Windows.
#
#   irm https://raw.githubusercontent.com/eugen1763/warhammer-survivors-coop/main/install.ps1 | iex
#
# The script finds the game in the Steam libraries, downloads the latest release and unzips it into the game folder.
# Set $env:WARHAMMER_SURVIVORS_DIR to the game folder if the script does not find the game.

& {
    $ErrorActionPreference = 'Stop'
    # The progress bar of Windows PowerShell 5.1 makes the download slow.
    $ProgressPreference = 'SilentlyContinue'
    [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

    $repo = 'eugen1763/warhammer-survivors-coop'
    $appId = 3669620

    function Find-Game {
        if ($env:WARHAMMER_SURVIVORS_DIR) { return $env:WARHAMMER_SURVIVORS_DIR }

        $steam = $null
        foreach ($key in 'HKCU:\Software\Valve\Steam', 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam', 'HKLM:\SOFTWARE\Valve\Steam') {
            $values = Get-ItemProperty -Path $key -ErrorAction SilentlyContinue
            if (-not $values) { continue }
            foreach ($name in 'SteamPath', 'InstallPath') {
                $value = $values.$name
                if ($value -and (Test-Path $value)) { $steam = $value; break }
            }
            if ($steam) { break }
        }
        if (-not $steam) { return $null }

        $libraries = @($steam)
        $index = Join-Path $steam 'steamapps\libraryfolders.vdf'
        if (Test-Path $index) {
            foreach ($match in [regex]::Matches((Get-Content -Raw $index), '"path"\s+"([^"]+)"')) {
                $libraries += $match.Groups[1].Value.Replace('\\', '\')
            }
        }
        foreach ($library in $libraries) {
            if (Test-Path (Join-Path $library "steamapps\appmanifest_$appId.acf")) {
                return Join-Path $library 'steamapps\common\Warhammer Survivors'
            }
        }
        return $null
    }

    $game = Find-Game
    if (-not $game) {
        throw 'The script did not find Warhammer Survivors in a Steam library. Set $env:WARHAMMER_SURVIVORS_DIR to the game folder, then run the script again.'
    }
    if (-not (Test-Path (Join-Path $game 'WarhammerSurvivors.exe'))) {
        throw "The file WarhammerSurvivors.exe is not in the folder $game."
    }
    if (Get-Process -Name 'WarhammerSurvivors' -ErrorAction SilentlyContinue) {
        throw 'Close the game, then run the script again.'
    }

    $release = Invoke-RestMethod -Uri "https://api.github.com/repos/$repo/releases/latest" -Headers @{ 'User-Agent' = 'warhammer-survivors-coop-installer' }
    $asset = $release.assets | Where-Object { $_.name -like '*-windows.zip' } | Select-Object -First 1
    if (-not $asset) { throw "The script did not find the archive of the latest release of $repo." }

    $archive = Join-Path ([IO.Path]::GetTempPath()) $asset.name
    try {
        Write-Host "Download of $($asset.name)."
        Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $archive -UseBasicParsing
        # The archive contains no files of the folder BepInEx\config, so the configuration stays.
        Expand-Archive -Path $archive -DestinationPath $game -Force
    }
    finally {
        Remove-Item $archive -ErrorAction SilentlyContinue
    }

    Write-Host "Installed $($release.tag_name) in $game"
    Write-Host 'Start Warhammer Survivors from Steam.'
    Write-Host 'The first start takes some minutes, because BepInEx generates its assemblies.'
}
