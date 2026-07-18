using HarmonyLib;

namespace VoidFault.Patches;

/// <summary>
/// Grants a bonus to JP (job EXP) earned after battle, stacking with vanilla.
///
/// Bumps ResultData.jobexp in BtlSequenceCtrl.CreateResultData (same approach as
/// GoldUp/ExpUp) so both the results-screen JP total and the applied per-character
/// JP reward increase consistently: BtlResultCtrl.Update reads ResultData.jobexp
/// into the displayed total (m_addJEXP) and passes it as the base to
/// ReviseAddJEXP (the reward).
///
/// Previously this hooked ReviseAddJEXP directly, which raised the reward but
/// left the shown total stale (the total is computed from ResultData.jobexp
/// before ReviseAddJEXP runs). Net reward is the same; this also fixes display.
/// </summary>
[HarmonyPatch(typeof(BtlSequenceCtrl), nameof(BtlSequenceCtrl.CreateResultData))]
public static class JPUp
{
    [HarmonyPostfix]
    public static void Postfix(BtlSequenceCtrl __instance)
    {
        if (!Plugin.JPUpEnabled.Value) return;

        ResultData result = __instance.GetResultData();
        if (result == null)
        {
            if (Plugin.DebugLogging.Value)
                Plugin.Log.LogInfo("[JPUp] CreateResultData fired but GetResultData() was null.");
            return;
        }

        int before = result.jobexp;
        result.jobexp += result.jobexp * Plugin.JPUpBonusPercent.Value / 100;

        if (Plugin.DebugLogging.Value)
            Plugin.Log.LogInfo($"[JPUp] jobexp {before} -> {result.jobexp}");
    }
}
