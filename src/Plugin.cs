using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;
using Wilson.UI;

namespace WarhammerSurvivorsCoop;

[BepInPlugin(Guid, "Warhammer Survivors Local Coop", "0.5.1")]
public class Plugin : BasePlugin
{
    public const string Guid = "local.warhammersurvivors.coop";

    internal static ManualLogSource Logger;
    internal static ConfigEntry<bool> KeyboardForPlayerOne;
    internal static ConfigEntry<int> LastPlayerTwoCharacter;
    internal static ConfigEntry<bool> LogInput;
    internal static ConfigEntry<bool> FreePass;
    internal static ConfigEntry<bool> ShowWeaponCooldowns;
    internal static ConfigEntry<bool> DoubleXp;

    public override void Load()
    {
        Logger = Log;
        KeyboardForPlayerOne = Config.Bind(
            "Input", "KeyboardForPlayerOne", false,
            "If true, player 1 uses the keyboard and the first controller is free for player 2.");
        LastPlayerTwoCharacter = Config.Bind(
            "State", "LastPlayerTwoCharacter", 0,
            "The character that player 2 selected last. The mod writes this value.");

        LogInput = Config.Bind(
            "Debug", "LogInput", false,
            "If true, the mod writes the controller state and the character state to the log each second.");

        FreePass = Config.Bind(
            "Gameplay", "FreePass", true,
            "If true, a player can pass a level-up to a different player without the Skip power-up. If false, a pass costs one Skip, as the game defines it.");

        DoubleXp = Config.Bind(
            "Gameplay", "DoubleXp", false,
            "If true, a run with more than one player gives double experience. The options page of the pause menu changes this value.");

        ShowWeaponCooldowns = Config.Bind(
            "Display", "ShowWeaponCooldowns", true,
            "If true, the weapon cooldown panel of the game shows all weapons. If false, the mod hides the panel. The options page of the pause menu changes this value.");

        AddComponent<CoopLobby>();
        Harmony.CreateAndPatchAll(typeof(CharacterSelectPatches), Guid);
        Harmony.CreateAndPatchAll(typeof(LevelUpPatches), Guid);
        Harmony.CreateAndPatchAll(typeof(CooldownPanel), Guid);
        Harmony.CreateAndPatchAll(typeof(CoopXp), Guid);
        Log.LogInfo("Local coop is active. Open the character select page to let player 2 join.");
    }
}

[HarmonyPatch]
internal static class CharacterSelectPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(WSCharacterSelectPage), nameof(WSCharacterSelectPage.OnShowStart))]
    private static void AfterShow(WSCharacterSelectPage __instance) => CoopLobby.Open(__instance);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(WSCharacterSelectPage), nameof(WSCharacterSelectPage.OnHideFinish))]
    private static void AfterHide() => CoopLobby.Close();
}
