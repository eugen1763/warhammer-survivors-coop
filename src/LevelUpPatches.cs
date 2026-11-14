using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using VampireSurvivors;
using VampireSurvivors.Framework;
using VampireSurvivors.Objects;
using CharacterController = VampireSurvivors.Objects.Characters.CharacterController;
using Wilson.UI;

namespace WarhammerSurvivorsCoop;

/// <summary>Makes the pass of a level-up to a different player work.</summary>
[HarmonyPatch]
internal static class LevelUpPatches
{
    private const float MinSecondsBetweenPasses = 0.3f;

    private static readonly List<(Transform card, Transform parent, int index)> Detached = new();
    private static bool _lentSkips;
    private static WSLevelUpPage _passPage;
    private static float _lastPass;

    // WSLevelUpPage.CanCharacterReceivePass expects a WSLevelUpItemUI on each child of the choice container.
    // The hint card for the fourth choice has none, which throws a NullReferenceException
    // and leaves the level-up page empty. The mod removes such cards from the container during the call.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(WSLevelUpPage), nameof(WSLevelUpPage.CanCharacterReceivePass))]
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
    [HarmonyPatch(typeof(WSLevelUpPage), nameof(WSLevelUpPage.CanCharacterReceivePass))]
    private static Exception AttachOtherCards(Exception __exception)
    {
        for (var i = Detached.Count - 1; i >= 0; i--)
        {
            var (card, parent, index) = Detached[i];
            if (card == null || parent == null) continue;
            card.SetParent(parent, false);
            card.SetSiblingIndex(index);
        }

        Detached.Clear();
        if (__exception != null) Plugin.Logger.LogWarning($"The pass check of the game failed: {__exception.Message}");
        return null;
    }

    // The game permits a pass only if the player has a Skip. The check reads WSLevelUpPage._hasSkips.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(WSLevelUpPage), nameof(WSLevelUpPage.CheckIfPassAvailable))]
    private static void LendSkip(WSLevelUpPage __instance)
    {
        _lentSkips = Plugin.FreePass.Value && !__instance._hasSkips;
        if (_lentSkips) __instance._hasSkips = true;
    }

    [HarmonyFinalizer]
    [HarmonyPatch(typeof(WSLevelUpPage), nameof(WSLevelUpPage.CheckIfPassAvailable))]
    private static Exception ReturnSkip(WSLevelUpPage __instance, Exception __exception)
    {
        if (_lentSkips) __instance._hasSkips = false;
        _lentSkips = false;
        if (__exception != null) Plugin.Logger.LogWarning($"The pass check of the game failed: {__exception.Message}");
        return null;
    }

    // The game hides the pass button when the player has no Skip.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(WSLevelUpPage), nameof(WSLevelUpPage.UpdateActionsRemainingButtons))]
    private static void ShowPassButton(WSLevelUpPage __instance)
    {
        if (Plugin.LogInput.Value && __instance._canPass && _passPage == null) Plugin.Logger.LogInfo("Pass available.");
        _passPage = __instance._canPass ? __instance : null;
        if (!__instance._canPass || __instance._hasSkips) return;
        var buttons = __instance._PassButtonGroup?.items;
        if (buttons == null) return;
        foreach (var button in buttons)
        {
            if (button == null) continue;
            var holder = button.transform.parent;
            if (holder != null) holder.gameObject.SetActive(true);
            button.interactable = true;
        }
    }

    // The button input of the game reads Rewired player 1 only.
    // The mod reads the action of the pass button from the player who has the level-up.
    internal static void ReadPassInput()
    {
        var page = _passPage;
        if (page == null) return;
        if (!page.isActiveAndEnabled || !page._canPass)
        {
            _passPage = null;
            return;
        }

        var player = MultiplayerManager.Instance?.GetSelectedPlayer();
        var buttons = page._PassButtonGroup?.items;
        if (player == null || buttons == null || !page.IsInputActive()) return;
        foreach (var button in buttons)
        {
            if (button == null || !button.gameObject.activeInHierarchy) continue;
            var glyph = button.GetComponent<GlyphInput>()?._glyphImage;
            if (glyph == null || !player.GetButtonDown(glyph.actionId)) continue;
            page.Pass();
            return;
        }
    }

    // WSLevelUpPage.Pass casts each child transform of the choice container to WSLevelUpItemUI,
    // which always throws an InvalidCastException. The mod does the steps of that method itself.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(WSLevelUpPage), nameof(WSLevelUpPage.Pass))]
    private static bool ReplacePass(WSLevelUpPage __instance)
    {
        // The button input of the game and the input of the mod can start a pass in the same frame.
        if (Time.unscaledTime - _lastPass < MinSecondsBetweenPasses) return false;
        _lastPass = Time.unscaledTime;
        try
        {
            PassToNextPlayer(__instance);
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError(e);
        }

        return false;
    }

    private static void PassToNextPlayer(WSLevelUpPage page)
    {
        var game = GM.Core;
        var session = page._gameSession;
        var manager = MultiplayerManager.Instance;
        var giver = session?.ActiveCharacter;
        if (game == null || manager == null || giver == null) return;

        var count = manager.GetAllCharacters().Count;
        CharacterController receiver = null;
        for (var i = 0; i < count && receiver == null; i++)
        {
            game.CycleActivePlayer();
            var candidate = session.ActiveCharacter;
            if (candidate == null || candidate.Pointer == giver.Pointer || candidate.IsDead) continue;
            if (page.CanCharacterReceivePass(candidate)) receiver = candidate;
        }

        if (receiver == null)
        {
            for (var i = 0; i < count && session.ActiveCharacter?.Pointer != giver.Pointer; i++) game.CycleActivePlayer();
            return;
        }

        if (!Plugin.FreePass.Value)
        {
            var skips = giver.PlayerStats?.Skips;
            if (skips != null) skips.SetValue(Math.Max(0f, skips.GetValue() - 1f));
        }

        var vibration = game.CoopConfig == null ? -1f : game.CoopConfig._levelupVibrationMilliseconds;
        page.EnterMultiplayerControl(receiver._player, vibration);
        page.ClearChoiceAnimators();
        page.SpawnWeaponLevelUpChoices(page._currentWeaponChoices);
        page.ValidateButtonStates();

        WSLevelUpItemUI first = null;
        var container = page._ChoiceContainerGroup?.GetActiveItem();
        var hints = new List<Transform>();
        for (var i = 0; container != null && i < container.childCount; i++)
        {
            var card = container.GetChild(i);
            var choice = card.GetComponent<WSLevelUpItemUI>();
            if (choice == null)
            {
                hints.Add(card);
            }
            else if (CanTake(receiver, choice) && !page._levelUpFactory.IsBlockedDueToCoop(choice._type, receiver))
            {
                if (first == null) first = choice;
            }
            else
            {
                // WSLevelUpItemUI.DisableSelection throws, because the card has no component of the type that it expects.
                var blocked = choice.GetComponent<Selectable>();
                if (blocked != null) blocked.interactable = false;
            }
        }

        foreach (var hint in hints) hint.SetAsLastSibling();
        page.AnimateChoices();

        var selectable = first == null ? null : first.GetComponent<Selectable>();
        if (selectable != null) page.StartCoroutine(page.SelectElementLater(selectable));
        var row = page._EquipmentRowPanel?.GetActiveItem();
        if (row != null) row.SetupPanels(receiver, null);
        if (Plugin.LogInput.Value) Plugin.Logger.LogInfo($"Pass from character {giver._PlayerIndex} to character {receiver._PlayerIndex}.");
    }

    // The same rule as in WSLevelUpPage.CanCharacterReceivePass, for one choice.
    private static bool CanTake(CharacterController character, WSLevelUpItemUI choice)
    {
        var type = choice._type;
        var data = choice._data;
        if (type != 0 && data != null && !data.isPowerUp)
        {
            var weapons = character._weaponsManager;
            var hasFreeSlot = weapons.GetActiveCountExcludingGifts() < character._maxWeaponCount + character._maxWeaponBonus;
            if ((hasFreeSlot && choice._isNew) || Owns(weapons, type)) return true;
        }

        if (!choice.IsPowerUp()) return false;
        var accessories = character._accessoriesManager;
        var hasFreeAccessorySlot = accessories.ActiveEquipment.Count < character._maxAccessoryCount + character._maxAccessoryBonus;
        return (hasFreeAccessorySlot && choice._isNew) || Owns(accessories, type);
    }

    private static bool Owns(EquipmentManager manager, VampireSurvivors.Data.WeaponType type)
    {
        foreach (var equipment in manager.ActiveEquipment)
        {
            if (equipment != null && equipment._equipmentType == type) return true;
        }

        return false;
    }
}
