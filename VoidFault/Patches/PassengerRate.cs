using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace VoidFault.Patches;

/// <summary>
/// Increases how many Passing Souls appear (and can be recruited) per town visit.
///
/// Mechanism (reverse-engineered, see VoidFault/RESEARCH_NOTES.md):
///   TownFunction.UpdatePhase case 10 -- the one-shot town-load phase -- calls
///   PassengerManager.GetCount() exactly ONCE per town entry, and its return N is
///   the loop count for spawning ghost NPCs (MB_PassThroughNPC.GetReady). Each
///   ghost the player crosses runs MB_FieldUI.OnPassingEachOther -> Doit() ->
///   ColonyShare.AddReinforcer(1) = +1 colony population.
///
///   Doit() only yields a recruitable soul while TOWN_PS_LEFT[town] > 0 (the
///   per-town budget the game refreshes to 2 every c_setPassengerSpan = 3 real
///   days). So we override GetCount()'s return to spawn N ghosts AND raise
///   TOWN_PS_LEFT so those N are recruitable, not merely visual.
///
/// Since GetCount() is one-shot-per-visit, this runs once per town entry (no
/// per-frame spam) and re-sets the budget each visit (leave/re-enter to farm).
/// It only fires where the game already spawns souls -- inside its story-progress
/// gate (GameData.m_ProgressId in 0x14..0x25 or >= 0x2e); it cannot force souls
/// before that window (by design -- avoids misbehaving in early-game towns).
///
/// CAVEAT (why the debug log matters): offline, Doit() also pops from COMS[town],
/// a per-town pool the game fills from your existing guests. If that pool holds
/// fewer than N, actual recruits clamp to the pool size even though N ghosts
/// spawn. The pre-override value (__result before we touch it) is the vanilla
/// min(budget, pool), so a low number there flags a small pool. Topping up COMS
/// is a deliberate follow-up, gated on seeing whether the pool is the real
/// limiter on-device rather than writing Il2Cpp Stack&lt;FriendState&gt;
/// manipulation blind.
/// </summary>
[HarmonyPatch(typeof(PassengerManager), nameof(PassengerManager.GetCount))]
public static class PassengerRate
{
    // TOWN_PS_LEFT is an sbyte[]; stay well under sbyte.MaxValue (127).
    private const int MaxSouls = 99;

    [HarmonyPostfix]
    public static void Postfix(ref int __result)
    {
        if (!Plugin.PassengerRateEnabled.Value) return;

        int target = Plugin.PassengerSoulsPerTown.Value;
        if (target < 1) target = 1;
        if (target > MaxSouls) target = MaxSouls;

        int vanillaCount = __result; // vanilla min(TOWN_PS_LEFT[town], pool)

        // Raise the per-town recruit budget so the extra souls grant population,
        // not just appear. Setting every town's slot is harmless (self-clamping:
        // the game only consumes the current town's, and re-sets all at its own
        // 3-day refresh) and avoids recomputing the current town index.
        Il2CppStructArray<sbyte> left = PassengerManager.TOWN_PS_LEFT;
        int townsSet = 0;
        if (left != null)
        {
            for (int i = 0; i < left.Length; i++)
                left[i] = (sbyte)target;
            townsSet = left.Length;
        }

        __result = target;

        if (Plugin.DebugLogging.Value)
            Plugin.Log.LogInfo(
                $"[PassengerRate] town-load: souls {vanillaCount} -> {__result} " +
                $"(budget raised on {townsSet} town slots). " +
                $"If recruits < {__result}, the guest pool (COMS) is the limiter.");
    }
}
