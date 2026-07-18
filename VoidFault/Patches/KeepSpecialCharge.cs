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
/// Applied MANUALLY from Plugin.Load (not auto-discovered by PatchAll): the
/// target is resolved by name via AccessTools.TypeByName (UIRoot.Equipment is
/// awkward to reference with typeof() under Il2CppInterop). Doing it manually
/// means a failed resolution or hook-init logs a clear one-line status at
/// startup instead of throwing inside PatchAll (which could abort other patches).
/// </summary>
public static class KeepSpecialCharge
{
    public static bool Prefix()
    {
        if (!Plugin.KeepSpecialChargeEnabled.Value) return true; // run vanilla (reset)

        if (Plugin.DebugLogging.Value)
            Plugin.Log.LogInfo("[KeepSpecialCharge] skipped Spirit reset on equipment change");

        return false; // skip original -> Spirit preserved across weapon changes
    }

    /// <summary>
    /// Resolve and patch the target, logging a definitive attach status.
    /// Called once from Plugin.Load. The status line is unconditional (not gated
    /// by Debug logging) so it's easy to confirm the hook took without turning on
    /// per-call spam.
    /// </summary>
    public static void Apply(Harmony harmony)
    {
        try
        {
            Type equipmentType = AccessTools.TypeByName("UIRoot.Equipment");
            MethodBase target = equipmentType != null
                ? AccessTools.Method(equipmentType, "FinisherSpiritsCheck")
                : null;

            if (target == null)
            {
                Plugin.Log.LogWarning(
                    "[KeepSpecialCharge] NOT attached: could not resolve " +
                    "UIRoot.Equipment.FinisherSpiritsCheck. Special charge will still reset on weapon change.");
                return;
            }

            harmony.Patch(target, prefix: new HarmonyMethod(typeof(KeepSpecialCharge), nameof(Prefix)));
            Plugin.Log.LogInfo("[KeepSpecialCharge] attached to UIRoot.Equipment.FinisherSpiritsCheck.");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[KeepSpecialCharge] NOT attached (hook failed): {ex}");
        }
    }
}
