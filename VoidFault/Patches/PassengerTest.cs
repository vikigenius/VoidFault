using System;
using System.Reflection;
using HarmonyLib;

namespace VoidFault.Patches;

/// <summary>
/// TEMPORARY verification hook + diagnostic. Originally targeted TownFunction's
/// constructor (fires on entering a town), but Il2CppInterop failed to init that
/// patch at runtime ("Derived classes must provide an implementation") and fell back
/// to a handler that never actually fires -- a known category of Harmony/IL2CPP
/// interop limitation, not a compile issue (build succeeds clean either way).
///
/// Now targets DeleteThis() instead -- a regular instance method (not a constructor),
/// called when tearing down a TownFunction (i.e. leaving a town). If this initializes
/// and fires cleanly, it tells us the failure was specific to hooking .ctor(), not
/// TownFunction as a class -- and we get a still-meaningful, if inverted, trigger:
/// IncomingCOM() runs when leaving a town rather than entering one.
///
/// Resolves the type/method via AccessTools.TypeByName/AccessTools.Method rather than
/// typeof(TownFunction)/nameof(...) directly, since TownFunction's complex hierarchy
/// makes direct references to it a bit fragile to depend on.
/// </summary>
[HarmonyPatch]
public static class PassengerTest
{
    [HarmonyTargetMethod]
    public static MethodBase TargetMethod()
    {
        Type townFunctionType = AccessTools.TypeByName("TownFunction");
        return AccessTools.Method(townFunctionType, "DeleteThis");
    }

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
                Plugin.Log.LogInfo($"[PassengerTest] TownFunction.DeleteThis() fired -> IncomingCOM() called. GetCount() {before} -> {after}");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[PassengerTest] IncomingCOM() threw: {ex}");
        }
    }
}
