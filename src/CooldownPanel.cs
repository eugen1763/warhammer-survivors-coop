using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using VampireSurvivors;
using VampireSurvivors.Data;
using VampireSurvivors.UI;
using Wilson.UI;

namespace WarhammerSurvivorsCoop;

/// <summary>Shows the weapon cooldown panel of the game for all weapons and adds a tick box for it to the options.</summary>
[HarmonyPatch]
internal static class CooldownPanel
{
    private static int _tickBoxFrame = -1;
    private static readonly HashSet<IntPtr> HiddenContainers = new();

    [HarmonyPostfix]
    [HarmonyPatch(typeof(DataManager), nameof(DataManager.BuildConvertedData))]
    private static void AfterBuildData(DataManager __instance) => ShowAllCooldowns(__instance);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(DataManager), nameof(DataManager.BuildConvertedDlcData))]
    private static void AfterBuildDlcData(DataManager __instance) => ShowAllCooldowns(__instance);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MainGamePage), nameof(MainGamePage.InitializeEquipment))]
    private static void AfterInitializeEquipment()
    {
        HiddenContainers.Clear();
        Apply();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(WSOptionsController), nameof(WSOptionsController.BuildQuickAccessPage))]
    private static void AfterBuildQuickAccessPage(WSOptionsController __instance) => AddTickBox(__instance);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(WSOptionsController), nameof(WSOptionsController.BuildIngamePage))]
    private static void AfterBuildIngamePage(WSOptionsController __instance) => AddTickBox(__instance);

    // The game keeps the panel of a dead character. The panel then shows on the black page that follows the death of all players.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(WSGameEquipmentCooldownPanel), nameof(WSGameEquipmentCooldownPanel.LateUpdate))]
    private static void HideForDeadCharacter(WSGameEquipmentCooldownPanel __instance)
    {
        var container = __instance._WeaponContainer;
        var character = __instance._characterController;
        if (container == null || character == null) return;

        var slots = container.gameObject;
        if (character.IsDead)
        {
            if (!slots.activeSelf) return;
            slots.SetActive(false);
            HiddenContainers.Add(container.Pointer);
        }
        else if (HiddenContainers.Remove(container.Pointer))
        {
            slots.SetActive(true);
        }
    }

    private static void AddTickBox(WSOptionsController controller)
    {
        // One page builder of the game can call the other. This check prevents a second tick box on the same page.
        if (_tickBoxFrame == Time.frameCount) return;
        _tickBoxFrame = Time.frameCount;

        controller.AddTickBox(
            "Weapon cooldowns",
            Plugin.ShowWeaponCooldowns.Value,
            (Action<bool>)(value =>
            {
                Plugin.ShowWeaponCooldowns.Value = value;
                Apply();
            }),
            false);
    }

    private static void Apply()
    {
        var show = Plugin.ShowWeaponCooldowns.Value;
        foreach (var panel in UnityEngine.Object.FindObjectsOfType<WSGameEquipmentCooldownPanel>(true))
        {
            if (!show)
            {
                panel.gameObject.SetActive(false);
                continue;
            }
            // The data manager builds its data before the plugin loads if the game starts quickly.
            if (panel._data != null) ShowAllCooldowns(panel._data);
            for (var node = panel.transform; node != null && !panel.gameObject.activeInHierarchy; node = node.parent)
            {
                if (!node.gameObject.activeSelf) node.gameObject.SetActive(true);
            }
            panel.Rebuild();
        }
    }

    private static void ShowAllCooldowns(DataManager data)
    {
        if (!Plugin.ShowWeaponCooldowns.Value) return;

        var weapons = data._weaponData;
        if (weapons == null) return;
        var changed = 0;
        foreach (var pair in weapons)
        {
            if (pair.Value == null) continue;
            foreach (var level in pair.Value)
            {
                if (level == null || level.showCooldown) continue;
                level.showCooldown = true;
                changed++;
            }
        }
        if (changed > 0) Plugin.Logger.LogInfo($"Set the cooldown display for {changed} weapon data entries.");
    }
}
