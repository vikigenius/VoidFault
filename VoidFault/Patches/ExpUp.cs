using HarmonyLib;

namespace VoidFault.Patches;

/// <summary>
/// Grants a bonus to EXP earned after battle, stacking with vanilla bonuses.
///
/// Bumps ResultData.exp in BtlSequenceCtrl.CreateResultData -- the same struct
/// and approach GoldUp uses for gil -- so BOTH the results-screen total AND the
/// applied per-character reward increase consistently. BtlResultCtrl.Update
/// reads ResultData.exp into the displayed total (m_addEXP = ResultData.exp +
/// bonus) and also passes ResultData.exp as the base to ReviseAddEXP (the reward
/// applied to each character's EXP).
///
/// Previously this hooked ReviseAddEXP directly, which raised the reward but not
/// the shown total (the display total is computed from ResultData.exp *before*
/// ReviseAddEXP runs). Net reward is the same; this just also fixes the display.
/// </summary>
[HarmonyPatch(typeof(BtlSequenceCtrl), nameof(BtlSequenceCtrl.CreateResultData))]
public static class ExpUp
{
    [HarmonyPostfix]
    public static void Postfix(BtlSequenceCtrl __instance)
    {
        if (!Plugin.ExpUpEnabled.Value) return;

        ResultData result = __instance.GetResultData();
        if (result == null)
        {
            if (Plugin.DebugLogging.Value)
                Plugin.Log.LogInfo("[ExpUp] CreateResultData fired but GetResultData() was null.");
            return;
        }

        int before = result.exp;
        result.exp += result.exp * Plugin.ExpUpBonusPercent.Value / 100;

        if (Plugin.DebugLogging.Value)
            Plugin.Log.LogInfo($"[ExpUp] exp {before} -> {result.exp}");
    }
}
