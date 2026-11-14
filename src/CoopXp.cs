using HarmonyLib;
using VampireSurvivors.Framework;

namespace WarhammerSurvivorsCoop;

/// <summary>Multiplies the experience by the option XpFactor in a run with more than one player, if the option DoubleXp is true.</summary>
[HarmonyPatch]
internal static class CoopXp
{
    // GameManager.AddPlayerXp gives the same value to each character, so one change here applies to all players.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.AddPlayerXp))]
    private static void BeforeAddPlayerXp(ref float xp)
    {
        if (!Plugin.DoubleXp.Value) return;
        var manager = MultiplayerManager.Instance;
        if (manager == null || manager.GetPlayers().Count < 2) return;
        xp *= Plugin.XpFactor.Value;
    }
}
