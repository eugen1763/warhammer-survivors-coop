using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;
using Wilson.UI;

namespace WarhammerSurvivorsCoop;

[BepInPlugin(Guid, "Warhammer Survivors Local Coop", "0.1.0")]
public class Plugin : BasePlugin
{
    public const string Guid = "local.warhammersurvivors.coop";

    internal static ManualLogSource Logger;
    internal static ConfigEntry<bool> KeyboardForPlayerOne;
    internal static ConfigEntry<int> LastPlayerTwoCharacter;
    internal static ConfigEntry<bool> LogInput;

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

        AddComponent<CoopLobby>();
        Harmony.CreateAndPatchAll(typeof(CharacterSelectPatches), Guid);
        Harmony.CreateAndPatchAll(typeof(LevelUpPatches), Guid);
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

[HarmonyPatch]
internal static class LevelUpPatches
{
    // With more than one player, the check throws a NullReferenceException in
    // WSLevelUpPage.CanCharacterReceivePass and the level-up page stays empty.
    // Without the check, the page behaves as in a run with one player: no pass button.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(WSLevelUpPage), nameof(WSLevelUpPage.CheckIfPassAvailable))]
    private static bool SkipPassCheck() => false;
}
