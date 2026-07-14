using System;
using HarmonyLib;

namespace VoidFault.Patches;

/// <summary>
/// TEMPORARY verification hook. TownFunction's static "instance"/"IsAliveInstance"
/// properties plus its DeleteThis() teardown override strongly suggest a new
/// TownFunction is constructed each time a town scene loads, mirroring how
/// BtlSequenceCtrl/BtlResultCtrl work per-battle. Calls PassengerManager.IncomingCOM()
/// once per construction to test empirically whether it places a recruitable Passing
/// Soul. Not the final design -- see LEARNINGS.md for the plan once this confirms
/// the mechanism works (or doesn't).
/// </summary>
[HarmonyPatch(typeof(TownFunction), MethodType.Constructor)]
public static class PassengerTest
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        if (!Plugin.PassengerTestEnabled.Value) return;

        try
        {
            int before = PassengerManager.GetCount();
            PassengerManager.IncomingCOM();
            int after = PassengerManager.GetCount();

            if (Plugin.DebugLogging.Value)
                Plugin.Log.LogInfo($"[PassengerTest] TownFunction constructed -> IncomingCOM() called. GetCount() {before} -> {after}");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[PassengerTest] IncomingCOM() threw: {ex}");
        }
    }
}
