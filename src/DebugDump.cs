using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VampireSurvivors.Framework;

namespace WarhammerSurvivorsCoop;

/// <summary>Writes UI and configuration state to the log. Used only when Debug.LogInput is true.</summary>
internal static class DebugDump
{
    internal static void Hierarchy(string title, Transform root, int maxDepth = 6)
    {
        if (root == null)
        {
            Plugin.Logger.LogInfo($"dump {title}: null");
            return;
        }

        var text = new StringBuilder($"dump {title}:\n");
        Append(text, root, 0, maxDepth);
        Plugin.Logger.LogInfo(text.ToString());
    }

    private static void Append(StringBuilder text, Transform node, int depth, int maxDepth)
    {
        text.Append(' ', depth * 2).Append(node.name).Append(node.gameObject.activeSelf ? "" : " [off]");
        var rect = node.TryCast<RectTransform>();
        if (rect != null)
        {
            var size = rect.rect.size;
            text.Append($" pos=({rect.position.x:0.0},{rect.position.y:0.0}) anchored=({rect.anchoredPosition.x:0},{rect.anchoredPosition.y:0}) size=({size.x:0},{size.y:0}) scale={rect.localScale.x:0.00}");
        }

        var image = node.GetComponent<Image>();
        if (image != null)
        {
            text.Append($" image={(image.sprite == null ? "none" : image.sprite.name)} enabled={image.enabled} color={image.color}");
            var material = image.material;
            if (material != null) text.Append($" material={material.name} shader={(material.shader == null ? "none" : material.shader.name)}");
        }

        foreach (var component in node.GetComponents<MonoBehaviour>())
        {
            if (component != null) text.Append($" <{component.GetIl2CppType().Name}>");
        }

        text.Append('\n');
        if (depth >= maxDepth) return;
        for (var i = 0; i < node.childCount; i++) Append(text, node.GetChild(i), depth + 1, maxDepth);
    }

    internal static void Characters(VampireSurvivors.Framework.MultiplayerManager manager)
    {
        var text = new StringBuilder("dump characters:\n");
        foreach (var character in manager.GetAllCharacters())
        {
            text.Append($"character {character._PlayerIndex} coopColour={character.GetCoopColour()}");
            Renderer(text, "main", character._CharacterRenderer);
            Renderer(text, "indicator", character._multiplayerIndicator);
            Renderer(text, "outlineRef", character._outlineReferenceRenderer);
            var outliner = character._multiplayerOutliner;
            if (outliner == null) text.Append(" outliner=null");
            else
            {
                text.Append($" outliner={outliner.name} active={outliner.gameObject.activeInHierarchy}");
                Renderer(text, "outlineMesh", outliner._meshRenderer);
            }
            text.Append('\n');
        }
        Plugin.Logger.LogInfo(text.ToString());
    }

    private static void Renderer(StringBuilder text, string label, Renderer renderer)
    {
        if (renderer == null) { text.Append($" {label}=null"); return; }
        var material = renderer.sharedMaterial;
        text.Append($" | {label}: {renderer.name} enabled={renderer.enabled} active={renderer.gameObject.activeInHierarchy}");
        var sprite = renderer.TryCast<SpriteRenderer>();
        if (sprite != null) text.Append($" sprite={(sprite.sprite == null ? "none" : sprite.sprite.name)} color={sprite.color}");
        if (material != null) text.Append($" material={material.name} shader={(material.shader == null ? "none" : material.shader.name)} supported={(material.shader != null && material.shader.isSupported)}");
    }

    internal static void CoopConfig(CoopConfig c)
    {
        if (c == null) return;
        Plugin.Logger.LogInfo(
            $"coop config: shareEvolutionPassives={c._shareEvolutionPassives} accessoryBonusMode={c._accessoryBonusMode}"
            + $" blockWeaponsOwnedByOthers={c._blockWeaponsOwnedByOtherPlayers} blockAccessoriesOwnedByOthers={c._blockAccessoriesOwnedByOtherPlayers}"
            + $" limitAccessoriesLikeWeapons={c._limitAccessoriesLikeWeapons} goldBonusForNotSharing={c._goldBonusForNotSharingPassives}"
            + $" globalLevelNumber={c._globalLevelNumber} xpGainModifier={c._xpGainModifier} xpGainDivisionPerPlayer={c._xpGainDivisionPerPlayer}"
            + $" cameraMode={c._cameraMode} revivalRange={c._revivalRange} revivalSpeedWithRevive={c._revivalSpeedWithRevive}"
            + $" revivalSpeedWithoutRevive={c._revivalSpeedWithoutRevive} reviveAll={c._reviveAllRatherThanClosest} immediateRevivalUsage={c._immediateRevivalUsage}"
            + $" ghostUsesCharacterSprite={c._ghostUsesCharacterSprite} decompositionTime={c._decompositionTimeSeconds}"
            + $" revivalUiPrefab={(c._multiplayerRevivalUIPrefab == null ? "none" : c._multiplayerRevivalUIPrefab.name)}"
            + $" indicatorPrefab={(c._playerIndicatorUIPrefab == null ? "none" : c._playerIndicatorUIPrefab.name)}"
            + $" enemyHealthPerMinute={c._enemyHealthModifierPerMinute} extraCharmPerPlayer={c._extraCharmPerPlayer}");
    }
}
