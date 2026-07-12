using HarmonyLib;

namespace VoidFault.Patches;

/// <summary>
/// Grants a bonus to Gil earned after battle. Hooks BtlSequenceCtrl.CreateResultData,
/// which populates the ResultData the game uses for both the results screen and the
/// actual reward — same struct the struktured-labs BravelyMod project found via
/// unsafe pointer offsets (gil at +0x10), exposed here as a normal typed field.
/// </summary>
[HarmonyPatch(typeof(BtlSequenceCtrl), nameof(BtlSequenceCtrl.CreateResultData))]
public static class GoldUp
{
    [HarmonyPostfix]
    public static void Postfix(BtlSequenceCtrl __instance)
    {
        if (!Plugin.GoldUpEnabled.Value) return;

        ResultData result = __instance.GetResultData();
        if (result == null)
        {
            if (Plugin.DebugLogging.Value)
                Plugin.Log.LogInfo("[GoldUp] CreateResultData fired but GetResultData() was null.");
            return;
        }

        int before = result.gil;
        result.gil += result.gil * Plugin.GoldUpBonusPercent.Value / 100;

        if (Plugin.DebugLogging.Value)
            Plugin.Log.LogInfo($"[GoldUp] gil {before} -> {result.gil}");
    }
}
