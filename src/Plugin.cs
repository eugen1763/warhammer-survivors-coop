using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Wilson.UI;

namespace WarhammerSurvivorsCoop;

[BepInPlugin(Guid, "Warhammer Survivors Local Coop", "0.2.0")]
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
    private static readonly List<(Transform card, Transform parent, int index)> Detached = new();

    // WSLevelUpPage.CanCharacterReceivePass expects a WSLevelUpItemUI on each child of the choice container.
    // The hint card for the fourth choice has none, which throws a NullReferenceException
    // and leaves the level-up page empty. The mod removes such cards from the container during the check.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(WSLevelUpPage), nameof(WSLevelUpPage.CheckIfPassAvailable))]
    private static void DetachOtherCards(WSLevelUpPage __instance)
    {
        Detached.Clear();
        var container = __instance._ChoiceContainerGroup?.GetActiveItem();
        if (container == null) return;
        for (var i = container.childCount - 1; i >= 0; i--)
        {
            var card = container.GetChild(i);
            if (card.GetComponent<WSLevelUpItemUI>() != null) continue;
            Detached.Add((card, container, i));
            card.SetParent(container.parent, false);
        }
    }

    [HarmonyFinalizer]
    [HarmonyPatch(typeof(WSLevelUpPage), nameof(WSLevelUpPage.CheckIfPassAvailable))]
    private static System.Exception AttachOtherCards(System.Exception __exception)
    {
        for (var i = Detached.Count - 1; i >= 0; i--)
        {
            var (card, parent, index) = Detached[i];
            if (card == null || parent == null) continue;
            card.SetParent(parent, false);
            card.SetSiblingIndex(index);
        }

        Detached.Clear();
        if (__exception != null) Plugin.Logger.LogWarning($"The pass check failed: {__exception.Message}");
        return null;
    }
}
