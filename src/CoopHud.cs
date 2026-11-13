using UnityEngine;
using UnityEngine.UI;
using VampireSurvivors;
using VampireSurvivors.Framework;

namespace WarhammerSurvivorsCoop;

/// <summary>Corrects the equipment panels of the heads-up display in a run with more than one player.</summary>
internal static class CoopHud
{
    private const float PanelScale = 0.85f;
    private const float PanelGap = 6f;
    private const float Interval = 0.25f;
    // The game uses this sprite as a placeholder. It shows as a magenta square.
    private const string PlaceholderSprite = "no16";

    private static MainGamePage _page;
    private static float _nextTick;

    internal static void Tick()
    {
        if (Time.unscaledTime < _nextTick) return;
        _nextTick = Time.unscaledTime + Interval;

        var manager = MultiplayerManager.Instance;
        if (manager == null || manager.GetPlayers().Count < 2) return;
        if (_page == null) _page = Object.FindObjectOfType<MainGamePage>();
        var container = _page == null ? null : _page._EquipmentPanelContainer;
        if (container == null || container.childCount < 2) return;

        StackPanels(container);
        for (var i = 0; i < container.childCount; i++) ReplacePlaceholders(container.GetChild(i));
    }

    // Each panel has a height of zero, so the layout group of the game puts all panels in the same position.
    private static void StackPanels(RectTransform container)
    {
        var group = container.GetComponent<VerticalLayoutGroup>();
        if (group != null && group.enabled) group.enabled = false;

        var first = container.GetChild(0).TryCast<RectTransform>();
        if (first == null) return;
        var offset = 0f;
        for (var i = 0; i < container.childCount; i++)
        {
            var panel = container.GetChild(i).TryCast<RectTransform>();
            if (panel == null || panel.childCount == 0) continue;
            panel.localScale = new Vector3(PanelScale, PanelScale, 1f);
            if (i > 0) panel.anchoredPosition = first.anchoredPosition - new Vector2(0f, offset);

            var content = panel.GetChild(0).TryCast<RectTransform>();
            if (content != null) offset += content.rect.height * PanelScale + PanelGap;
        }
    }

    private static void ReplacePlaceholders(Transform panel)
    {
        foreach (var image in panel.GetComponentsInChildren<Image>(true))
        {
            if (image.sprite == null || image.sprite.name != PlaceholderSprite) continue;
            if (image.name == "DeadIcon")
            {
                image.sprite = null;
                image.color = new Color(0.6f, 0f, 0f, 0.6f);
            }
            else if (image.color.a > 0f)
            {
                image.color = Color.clear;
            }
        }
    }
}
