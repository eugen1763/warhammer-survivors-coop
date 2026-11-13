using System;
using System.Collections.Generic;
using VampireSurvivors.Data;
using VampireSurvivors.Data.Characters;
using VampireSurvivors.Framework;
using VampireSurvivors.Objects;
using Rewired;
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
    private int _playerOnePadId = -1;
    private int _lastMenuPadId = -1;
    private float _nextInputLog;
    private string _status = "";
    private GUIStyle _style;

    public CoopLobby(IntPtr ptr) : base(ptr) { }

    private void Awake() => _instance = this;

    internal static void Open(WSCharacterSelectPage page)
    {
        if (_instance == null) return;
        _instance._page = page;
        _instance._choices.Clear();
        try
        {
            _instance.RememberPlayerOnePad();
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError(e);
        }

        var manager = MultiplayerManager.Instance;
        if (manager == null) return;
        manager.AllowPlayerJoining = true;
        manager.AllowPlayerRemoval = true;
        if (Plugin.LogInput.Value) Plugin.Logger.LogInfo($"Lobby open. The controller of player 1 is {_instance._playerOnePadId}.");
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
        if (_page == null)
        {
            TrackMenuPad();
            if (Plugin.LogInput.Value) LogSession();
            return;
        }

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

    private void LogSession()
    {
        if (!ReInput.isReady || Time.unscaledTime < _nextInputLog) return;
        _nextInputLog = Time.unscaledTime + 1f;
        var manager = MultiplayerManager.Instance;
        if (manager == null || manager.GetPlayers().Count < 2) return;

        try
        {
            var selected = manager.GetSelectedPlayer();
            var text = $"session: selected={(selected == null ? -1 : selected.id)} blocked={manager.IsUIBeingBlocked} timeScale={Time.timeScale:0.0}";
            var ui = manager._uiControllingPlayers;
            if (ui != null) text += $" uiPlayers=[{string.Join(",", ui)}]";
            foreach (var player in manager.GetPlayers())
            {
                var maps = new Il2CppSystem.Collections.Generic.List<ControllerMap>();
                player.controllers.maps.GetAllMaps(ControllerType.Joystick, maps);
                text += $" | player {player.id} pads={player.controllers.joystickCount} any={player.GetAnyButton()}"
                    + $" confirm={player.GetButton(RewiredAction.PrimaryConfirm)} submit={player.GetButton(RewiredAction.UISubmit)}"
                    + $" moveX={player.GetAxis(RewiredAction.Move_Horizontal):0.0} maps=";
                foreach (var map in maps) text += $"{map.categoryId}/{map.layoutId}:{(map.enabled ? "on" : "off")}:{map.elementMapCount} ";
            }

            foreach (var character in manager.GetAllCharacters())
            {
                text += $" | character {character._PlayerIndex} {character._currentCharacterData?.charName} hp={character.CurrentHp:0} dead={character.IsDead}"
                    + $" main={character.CountsAsMainCharacterForRevivals} revival={character.MultiplayerRevivalAllowed}";
            }

            Plugin.Logger.LogInfo(text);
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError(e);
        }
    }

    private void TrackMenuPad()
    {
        if (!ReInput.isReady) return;
        foreach (var joystick in ReInput.controllers.GetJoysticks())
        {
            if (joystick.GetAnyButtonDown()) _lastMenuPadId = joystick.id;
        }
    }

    private void Tick()
    {
        var manager = MultiplayerManager.Instance;
        var selections = Selections(manager);
        if (manager == null || selections == null) return;

        // The item list of the page is empty until the page fills it.
        if (_choices.Count == 0) ReadChoices();

        JoinNewPlayers(manager);
        if (Plugin.LogInput.Value) LogInput(manager);

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

    // Player 1 is the player who operated the menus before the character select page.
    private void RememberPlayerOnePad()
    {
        _playerOnePadId = -1;
        if (Plugin.KeyboardForPlayerOne.Value) return;

        var playerOne = ReInput.players.GetPlayer(0);
        var fallback = -1;
        foreach (var joystick in ReInput.controllers.GetJoysticks())
        {
            if (!playerOne.controllers.ContainsController(joystick)) continue;
            if (fallback < 0) fallback = joystick.id;
            if (joystick.id == _lastMenuPadId) _playerOnePadId = joystick.id;
        }

        if (_playerOnePadId < 0) _playerOnePadId = fallback;
    }

    // The join code of the game expects that Rewired already gave the controller to an extra player.
    // Warhammer Survivors gives all controllers to player 1, so the mod moves the controller itself.
    private void JoinNewPlayers(MultiplayerManager manager)
    {
        var joined = manager.GetPlayers();
        foreach (var joystick in ReInput.controllers.GetJoysticks())
        {
            if (joystick.id == _playerOnePadId || !joystick.GetAnyButtonDown()) continue;

            var inUse = false;
            for (var i = 1; i < joined.Count; i++) inUse |= joined[i].controllers.ContainsController(joystick);
            if (inUse) continue;

            RewiredPlayer free = null;
            for (var id = 1; id < ReInput.players.playerCount && free == null; id++)
            {
                var candidate = ReInput.players.GetPlayer(id);
                if (candidate != null && !joined.Contains(candidate)) free = candidate;
            }

            if (free == null) return;
            var layouts = PlayerOneLayouts(joystick);
            free.controllers.AddController(joystick, true);
            AddMissingMaps(free, joystick, layouts);
            manager.AddPlayer(free);
            Plugin.Logger.LogInfo($"Controller {joystick.id} ({joystick.name}) joined as Rewired player {free.id}.");
        }
    }

    // The Rewired data of the game gives the menu actions of Warhammer Survivors to player 1 only.
    private static List<(int category, int layout, bool enabled)> PlayerOneLayouts(Joystick joystick)
    {
        var maps = new Il2CppSystem.Collections.Generic.List<ControllerMap>();
        ReInput.players.GetPlayer(0).controllers.maps.GetAllMaps(ControllerType.Joystick, maps);

        var own = new List<(int, int, bool)>();
        var all = new List<(int, int, bool)>();
        foreach (var map in maps)
        {
            var entry = (map.categoryId, map.layoutId, map.enabled);
            if (map.controllerId == joystick.id) own.Add(entry);
            if (!all.Exists(e => e.Item1 == entry.Item1 && e.Item2 == entry.Item2)) all.Add(entry);
        }

        return own.Count > 0 ? own : all;
    }

    private static void AddMissingMaps(RewiredPlayer player, Joystick joystick, List<(int category, int layout, bool enabled)> layouts)
    {
        foreach (var (category, layout, enabled) in layouts)
        {
            if (player.controllers.maps.GetMap(joystick, category, layout) != null) continue;
            var map = ReInput.mapping.GetJoystickMapInstance(joystick, category, layout);
            if (map == null) continue;
            player.controllers.maps.AddMap(joystick, map, enabled);
            if (Plugin.LogInput.Value) Plugin.Logger.LogInfo($"Added the controller map {category}/{layout} to Rewired player {player.id}.");
        }
    }

    private void LogInput(MultiplayerManager manager)
    {
        if (Time.unscaledTime < _nextInputLog) return;
        _nextInputLog = Time.unscaledTime + 2f;

        var text = $"joined={manager.GetPlayers().Count} joining={manager.AllowPlayerJoining} playerOnePad={_playerOnePadId} choices={_choices.Count}";
        foreach (var joystick in ReInput.controllers.GetJoysticks())
        {
            text += $" | pad {joystick.id} '{joystick.name}' anyButton={joystick.GetAnyButton()} owners=";
            for (var id = 0; id < ReInput.players.playerCount; id++)
            {
                if (ReInput.players.GetPlayer(id).controllers.ContainsController(joystick)) text += id;
            }
        }

        Plugin.Logger.LogInfo(text);
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
            _names[type] = DisplayName(character.CharacterData) ?? type.ToString();
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

    private static string DisplayName(CharacterData data)
    {
        if (data == null) return null;
        var parts = new List<string>();
        foreach (var part in new[] { data.prefix, data.charName, data.surname })
        {
            if (!string.IsNullOrWhiteSpace(part)) parts.Add(part.Trim());
        }

        return parts.Count > 0 ? string.Join(" ", parts) : null;
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
