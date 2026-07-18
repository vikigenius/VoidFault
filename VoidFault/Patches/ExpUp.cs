using HarmonyLib;

namespace VoidFault.Patches;

/// <summary>
/// Grants every character an unconditional EXP bonus on top of whatever the game
/// already computed for that battle, so it stacks with any vanilla EXP boosts
/// rather than replacing them. Mirrors JPUp, hooking the EXP sibling
/// BtlResultCtrl.ReviseAddEXP (bonusexp is the by-ref bonus accumulator).
/// </summary>
[HarmonyPatch(typeof(BtlResultCtrl), nameof(BtlResultCtrl.ReviseAddEXP))]
public static class ExpUp
{
    [HarmonyPrefix]
    public static void Prefix(int exp, ref int bonusexp)
    {
        if (!Plugin.ExpUpEnabled.Value) return;

        int before = bonusexp;
        bonusexp += exp * Plugin.ExpUpBonusPercent.Value / 100;

        if (Plugin.DebugLogging.Value)
            Plugin.Log.LogInfo($"[ExpUp] exp={exp} bonusexp {before} -> {bonusexp}");
    }
}
