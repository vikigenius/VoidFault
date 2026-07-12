using HarmonyLib;

namespace VoidFault.Patches;

/// <summary>
/// Grants every character an unconditional JP bonus on top of whatever the game
/// already computed for that battle (base JP plus any real "JP Up" ability bonus),
/// so it stacks with the vanilla ability rather than replacing it.
/// </summary>
[HarmonyPatch(typeof(BtlResultCtrl), nameof(BtlResultCtrl.ReviseAddJEXP))]
public static class JPUp
{
    [HarmonyPrefix]
    public static void Prefix(int jexp, ref int bonusjexp)
    {
        if (!Plugin.JPUpEnabled.Value) return;
        bonusjexp += jexp * Plugin.JPUpBonusPercent.Value / 100;
    }
}
