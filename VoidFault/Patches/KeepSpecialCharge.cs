using System;
using System.Reflection;
using HarmonyLib;

namespace VoidFault.Patches;

/// <summary>
/// Keeps a weapon's Special (Finisher) charge when you change equipment.
///
/// The Special is powered by the per-hand "Spirit" gauge
/// (CharacterState.SPIRIT[hand], range 0..1000); actions like Default
/// (CheckSpiritDefault), Brave, and Attack fill it, and the Special becomes
/// usable once it's full.
///
/// UIRoot.Equipment.FinisherSpiritsCheck(int[,] savedTypes) is the ONLY
/// equip-time reset (verified: the sole other CharacterState.SetSPIRIT caller is
/// the post-battle writeback BtlChara.WriteReturnCharaStatus, untouched here).
/// For each hand it compares the current weapon TYPE against a stored previous
/// type and, on a change, calls SetSPIRIT(hand, 0) -- zeroing that hand's charge.
/// Vanilla already preserves a same-type swap (katana -> katana); only a type
/// change or an unequip resets. So an accidental weapon swap in a shop wipes the
/// charge you'd built up.
///
/// This prefix skips FinisherSpiritsCheck entirely, so Spirit is never reset on
/// an equipment change. Its only effect is that reset, so skipping is safe.
///
/// Targeted via AccessTools.TypeByName because UIRoot.Equipment is awkward to
/// reference with typeof() under Il2CppInterop (same approach as the old
/// PassengerTest hook).
/// </summary>
[HarmonyPatch]
public static class KeepSpecialCharge
{
    [HarmonyTargetMethod]
    public static MethodBase TargetMethod()
    {
        Type equipmentType = AccessTools.TypeByName("UIRoot.Equipment");
        return AccessTools.Method(equipmentType, "FinisherSpiritsCheck");
    }

    [HarmonyPrefix]
    public static bool Prefix()
    {
        if (!Plugin.KeepSpecialChargeEnabled.Value) return true; // run vanilla (reset)

        if (Plugin.DebugLogging.Value)
            Plugin.Log.LogInfo("[KeepSpecialCharge] skipped Spirit reset on equipment change");

        return false; // skip original -> Spirit preserved across weapon changes
    }
}
