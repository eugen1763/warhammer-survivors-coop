using System;
using System.Collections.Generic;
using VampireSurvivors.Data;
using VampireSurvivors.Framework;
using VampireSurvivors.Objects;
using UnityEngine;
using Wilson.UI;
using RewiredAction = RewiredConsts.Action;
using RewiredPlayer = Rewired.Player;

namespace WarhammerSurvivorsCoop;

/// <summary>
/// Lets extra players join on the character select page and select a character.
/// The game session code reads the result from MultiplayerManager and
/// PlayerOptionsData.MultiplayerSelections, in the same way as Vampire Survivors.
/// </summary>
public class CoopLobby : MonoBehaviour
{
    private const float StickThreshold = 0.6f;

    private static CoopLobby _instance;

    private WSCharacterSelectPage _page;
    private readonly List<CharacterType> _choices = new();
    private readonly Dictionary<CharacterType, string> _names = new();
    private readonly Dictionary<int, int> _stickDirection = new();
    private string _status = "";
    private GUIStyle _style;

    public CoopLobby(IntPtr ptr) : base(ptr) { }

    private void Awake() => _instance = this;

    internal static void Open(WSCharacterSelectPage page)
    {
        if (_instance == null) return;
        _instance._page = page;
        _instance._choices.Clear();

        var manager = MultiplayerManager.Instance;
        if (manager == null) return;
        if (Plugin.KeyboardForPlayerOne.Value) manager.SetControllerAssignedToPlayer1(false);
        manager.AllowPlayerJoining = true;
        manager.AllowPlayerRemoval = true;
    }

    internal static void Close()
    {
        if (_instance == null || _instance._page == null) return;
        try
        {
            _instance.ResolveDuplicateCharacters();
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError(e);
        }

        _instance._page = null;
        var manager = MultiplayerManager.Instance;
        if (manager != null) manager.AllowPlayerJoining = false;
    }

    private void Update()
    {
        if (_page == null) return;
        try
        {
            Tick();
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError(e);
            _page = null;
        }
    }

    private void Tick()
    {
        var manager = MultiplayerManager.Instance;
        var selections = Selections(manager);
        if (manager == null || selections == null) return;

        // The item list of the page is empty until the page fills it.
        if (_choices.Count == 0) ReadChoices();

        var players = manager.GetPlayers();
        var lines = new List<string>();
        for (var i = 1; i < players.Count; i++)
        {
            var player = players[i];
            if (player == null || !selections.ContainsKey(player)) continue;
            var selection = selections[player];

            if (player.GetButtonDown(RewiredAction.UICancelController))
            {
                manager.AddPlayerForRemoval(player);
                continue;
            }

            if (_choices.Count > 0)
            {
                var index = _choices.IndexOf(selection.SelectedCharacter);
                if (index < 0)
                {
                    index = Math.Max(0, _choices.IndexOf((CharacterType)Plugin.LastPlayerTwoCharacter.Value));
                    selection.SelectedCharacter = _choices[index];
                }

                var step = ReadStep(player);
                if (step != 0)
                {
                    index = (index + step + _choices.Count) % _choices.Count;
                    selection.SelectedCharacter = _choices[index];
                    if (i == 1) Plugin.LastPlayerTwoCharacter.Value = (int)selection.SelectedCharacter;
                }
            }

            selection.Color = manager.GetSlotColor(i);
            lines.Add($"PLAYER {i + 1}: {Name(selection.SelectedCharacter)}   [left / right: change, B: leave]");
        }

        if (players.Count < 2) lines.Add("PLAYER 2: press a button on a second controller to join");
        _status = string.Join("\n", lines);
    }

    private int ReadStep(RewiredPlayer player)
    {
        if (player.GetButtonDown(RewiredAction.DPadLeft) || player.GetButtonDown(RewiredAction.PrimaryPrevious)) return -1;
        if (player.GetButtonDown(RewiredAction.DPadRight) || player.GetButtonDown(RewiredAction.PrimaryNext)) return 1;

        var axis = player.GetAxis(RewiredAction.Move_Horizontal);
        var direction = axis > StickThreshold ? 1 : axis < -StickThreshold ? -1 : 0;
        _stickDirection.TryGetValue(player.id, out var previous);
        _stickDirection[player.id] = direction;
        return direction != previous ? direction : 0;
    }

    private void ReadChoices()
    {
        _names.Clear();
        foreach (var item in _page.GetComponentsInChildren<WSCharacterItem>(true))
        {
            var character = item.CharacterItem;
            if (character == null || !item.IsCharAvailable()) continue;
            var type = character.CharacterType;
            if (_choices.Contains(type)) continue;
            _choices.Add(type);
            _names[type] = character.CharacterData?.charName ?? type.ToString();
        }
    }

    // The Vampire Survivors lobby gives each character to one player only,
    // so the session code possibly assumes that the character types are unique.
    private void ResolveDuplicateCharacters()
    {
        var manager = MultiplayerManager.Instance;
        var selections = Selections(manager);
        if (manager == null || selections == null) return;

        var players = manager.GetPlayers();
        var taken = new List<CharacterType> { manager._playerOptions.Config.SelectedCharacter };
        for (var i = 1; i < players.Count; i++)
        {
            var player = players[i];
            if (player == null || !selections.ContainsKey(player)) continue;
            var selection = selections[player];
            if (taken.Contains(selection.SelectedCharacter))
            {
                var free = _choices.Find(c => !taken.Contains(c));
                if (_choices.Contains(free) && !taken.Contains(free))
                {
                    Plugin.Logger.LogInfo($"Player {i + 1} had the character of a different player. The mod changed it to {free}.");
                    selection.SelectedCharacter = free;
                }
                else
                {
                    Plugin.Logger.LogWarning($"Player {i + 1} uses the character of a different player because no other character is available.");
                }
            }

            taken.Add(selection.SelectedCharacter);
        }
    }

    private static Il2CppSystem.Collections.Generic.Dictionary<RewiredPlayer, CoopSelections> Selections(MultiplayerManager manager)
    {
        return manager?._playerOptions?.Config?.MultiplayerSelections;
    }

    private string Name(CharacterType type) => _names.TryGetValue(type, out var name) ? name : type.ToString();

    private void OnGUI()
    {
        if (_page == null || string.IsNullOrEmpty(_status)) return;
        _style ??= new GUIStyle(GUI.skin.box)
        {
            fontSize = Math.Max(14, Screen.height / 45),
            alignment = TextAnchor.MiddleCenter,
            wordWrap = false,
        };

        var lineCount = _status.Split('\n').Length;
        var width = Screen.width * 0.5f;
        var height = _style.fontSize * 1.6f * lineCount + 12f;
        GUI.Box(new Rect((Screen.width - width) / 2f, 8f, width, height), _status, _style);
    }
}
