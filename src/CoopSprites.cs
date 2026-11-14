using System.Collections.Generic;
using UnityEngine;
using VampireSurvivors.Framework;
using VampireSurvivors.Graphics;

namespace WarhammerSurvivorsCoop;

/// <summary>Replaces the sprites of the cooperative mode that have no valid picture in this game.</summary>
internal static class CoopSprites
{
    private const float Interval = 0.25f;
    private const float PixelsPerUnit = 100f;
    private const string StoneName = "CoopModStone";

    private static readonly Dictionary<char, Color> Palette = new()
    {
        ['.'] = Color.clear,
        ['o'] = new Color(0.10f, 0.10f, 0.12f, 1f),
        ['g'] = new Color(0.60f, 0.60f, 0.65f, 1f),
        ['h'] = new Color(0.80f, 0.80f, 0.84f, 1f),
        ['s'] = new Color(0.40f, 0.40f, 0.46f, 1f),
        // The game multiplies white by the color of the player.
        ['w'] = Color.white,
    };

    // The game turns the indicator by 90 degrees, so an arrow that points to the right shows as an arrow that points down.
    private static readonly string[] ArrowRows =
    {
        "oo.....",
        "owo....",
        "owwo...",
        "owwwo..",
        "owwwwo.",
        "owwwwwo",
        "owwwwo.",
        "owwwo..",
        "owwo...",
        "owo....",
        "oo....."
    };

    private static readonly string[] StoneRows =
    {
        "....oooooooo....",
        "...oghhhhhgso...",
        "..oghggggggsso..",
        ".oghgggggggggso.",
        ".oggggggggggsso.",
        ".oggggggggggsso.",
        ".oggggggggggsso.",
        ".oggggggggggsso.",
        ".oggggggggggsso.",
        ".oggggggggggsso.",
        ".oggggggggggsso.",
        ".oggggggggggsso.",
        ".oggggggggggsso.",
        ".oggggggggggsso.",
        ".oggggggggggsso.",
        "oooooooooooooooo",
        "oggggggggggggsso",
        "oooooooooooooooo"
    };

    private static readonly string[] CrossRows =
    {
        "................",
        "................",
        "................",
        ".......ww.......",
        ".......ww.......",
        ".....wwwwww.....",
        ".....wwwwww.....",
        ".......ww.......",
        ".......ww.......",
        ".......ww.......",
        ".......ww.......",
        ".......ww.......",
        "................",
        "................",
        "................",
        "................",
        "................",
        "................"
    };

    private static Sprite _arrow;
    private static Sprite _stone;
    private static Sprite _cross;
    private static float _nextTick;

    internal static void Tick()
    {
        if (Time.unscaledTime < _nextTick) return;
        _nextTick = Time.unscaledTime + Interval;

        var manager = MultiplayerManager.Instance;
        if (manager == null || manager.GetPlayers().Count < 2) return;
        foreach (var character in manager.GetAllCharacters())
        {
            if (character == null) continue;
            ReplaceIndicator(character._multiplayerIndicator);
            ReplaceTombstone(character._multiplayerRevivalUI);
        }
    }

    // The frames of the arrow animation refer to positions in the UI atlas that contain no arrow in this game.
    private static void ReplaceIndicator(SpriteRenderer indicator)
    {
        if (indicator == null) return;
        _arrow ??= Create(ArrowRows, new Vector2(0.5f, 0.5f));
        if (Same(indicator.sprite, _arrow)) return;

        var animation = indicator.GetComponent<SpriteAnimation>();
        if (animation != null) animation.enabled = false;
        indicator.sprite = _arrow;
    }

    // The tombstone of a dead player has no sprites. The game shows only a rectangle in the color of the player.
    // The game sets the sprite of its coffin renderer again in each frame, so the stone has its own object.
    private static void ReplaceTombstone(VampireSurvivors.UI.Player.MultiplayerRevivalUI tombstone)
    {
        if (tombstone == null) return;
        var cross = tombstone.GetComponent<SpriteRenderer>();
        var outline = tombstone._coffinOutline;
        if (cross == null || outline == null) return;

        _cross ??= Create(CrossRows, new Vector2(0.5f, 0.3f));
        if (!Same(cross.sprite, _cross)) cross.sprite = _cross;

        var child = tombstone.transform.Find(StoneName);
        var stone = child == null ? AddStone(cross) : child.GetComponent<SpriteRenderer>();
        // The rectangle of the game is above the enemies. The stone and the cross use the same draw order, so enemies do not hide them.
        if (stone.sortingOrder != outline.sortingOrder) stone.sortingOrder = outline.sortingOrder;
        if (cross.sortingOrder != outline.sortingOrder + 1) cross.sortingOrder = outline.sortingOrder + 1;
        if (outline.enabled) outline.enabled = false;
    }

    private static SpriteRenderer AddStone(SpriteRenderer cross)
    {
        _stone ??= Create(StoneRows, new Vector2(0.5f, 0.3f));
        var stone = new GameObject(StoneName).AddComponent<SpriteRenderer>();
        stone.transform.SetParent(cross.transform, false);
        stone.gameObject.layer = cross.gameObject.layer;
        stone.sprite = _stone;
        stone.sharedMaterial = cross.sharedMaterial;
        stone.sortingLayerID = cross.sortingLayerID;
        return stone;
    }

    private static bool Same(Sprite a, Sprite b) => a != null && b != null && a.Pointer == b.Pointer;

    private static Sprite Create(string[] rows, Vector2 pivot)
    {
        var width = rows[0].Length;
        var height = rows.Length;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave,
        };
        for (var y = 0; y < height; y++)
        {
            // Row 0 of a texture is the bottom row.
            var row = rows[height - 1 - y];
            for (var x = 0; x < width; x++) texture.SetPixel(x, y, Palette[row[x]]);
        }
        texture.Apply();

        var sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), pivot, PixelsPerUnit);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }
}
