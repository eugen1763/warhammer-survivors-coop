using Com.LuisPedroFonseca.ProCamera2D;
using UnityEngine;
using VampireSurvivors.Framework;

namespace WarhammerSurvivorsCoop;

/// <summary>Moves the shared camera away when the players are far apart, so that the players have more room.</summary>
internal static class CoopCamera
{
    // The part of the screen width or height between two players at which the camera starts to move away.
    private const float StartSpan = 0.5f;
    private const float Rate = 4f;

    private static float _factor = 1f;
    private static Vector2 _baseSize;

    internal static void Tick()
    {
        // ProCamera2D.Instance throws an exception when no camera exists, for example in the menus.
        if (!ProCamera2D.Exists)
        {
            _factor = 1f;
            return;
        }

        var camera = ProCamera2D.Instance;
        // The game sets the camera size for the window size and for its own effects. The mod reads the size while it does not change it.
        if (_factor == 1f) _baseSize = camera.ScreenSizeInWorldCoordinates;
        var target = TargetFactor(_baseSize);
        if (_factor == 1f && target == 1f) return;

        _factor = Mathf.Lerp(_factor, target, 1f - Mathf.Exp(-Rate * Time.unscaledDeltaTime));
        if (Mathf.Abs(_factor - target) < 0.002f) _factor = target;
        camera.SetScreenSize(_baseSize.y * 0.5f * _factor);
    }

    private static float TargetFactor(Vector2 size)
    {
        var max = Plugin.CameraMaxZoomOut.Value;
        var manager = MultiplayerManager.Instance;
        if (max <= 1f || manager == null || size.x <= 0f || size.y <= 0f) return 1f;

        var alive = 0;
        Vector2 low = default, high = default;
        foreach (var character in manager.GetAllCharacters())
        {
            if (character == null || character.IsDead) continue;
            Vector2 position = character.transform.position;
            low = alive == 0 ? position : Vector2.Min(low, position);
            high = alive == 0 ? position : Vector2.Max(high, position);
            alive++;
        }
        if (alive < 2) return 1f;

        var span = high - low;
        return Mathf.Clamp(Mathf.Max(span.x / (size.x * StartSpan), span.y / (size.y * StartSpan)), 1f, max);
    }
}
