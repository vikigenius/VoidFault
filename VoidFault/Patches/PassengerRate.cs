using HarmonyLib;

namespace VoidFault.Patches;

/// <summary>
/// Spawns extra Passing Soul ghosts per town visit.
///
/// TownFunction.UpdatePhase case 10 (the one-shot town-load phase) calls
/// PassengerManager.GetCount() once per town entry, and its return is the loop
/// count for spawning ghost NPCs (MB_PassThroughNPC.GetReady). Offline the
/// vanilla count is 0 (empty guest pool), so we override it to SoulsPerTown to
/// make ghosts actually appear.
///
/// This patch ONLY handles spawning. Turning a passed ghost into +1 village
/// population is PassengerRecruit's job -- offline, the vanilla recruit chain
/// (Doit -> COMS guest pool -> AddReinforcer) is dead because the guest system
/// is disabled without internet.
///
/// Only fires where the game already spawns souls (past its story-progress
/// gate). Runs once per town entry.
/// </summary>
[HarmonyPatch(typeof(PassengerManager), nameof(PassengerManager.GetCount))]
public static class PassengerRate
{
    private const int MaxSouls = 99;

    [HarmonyPostfix]
    public static void Postfix(ref int __result)
    {
        if (!Plugin.PassengerRateEnabled.Value) return;

        int target = Plugin.PassengerSoulsPerTown.Value;
        if (target < 1) target = 1;
        if (target > MaxSouls) target = MaxSouls;

        int vanillaCount = __result;
        __result = target;

        if (Plugin.DebugLogging.Value)
            Plugin.Log.LogInfo($"[PassengerRate] town-load: ghosts {vanillaCount} -> {__result}");
    }
}
