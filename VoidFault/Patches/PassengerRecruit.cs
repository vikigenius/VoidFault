using HarmonyLib;

namespace VoidFault.Patches;

/// <summary>
/// Grants +1 village population each time the player passes a Passing Soul,
/// working offline where the vanilla recruit path is disabled.
///
/// Vanilla flow: MB_FieldUI.OnPassingEachOther -> PassengerManager.Doit() ->
/// ColonyShare.DataAccessor.AddReinforcer(1). Offline that dies at Doit() (the
/// COMS guest pool is empty because the guest system is disabled without
/// internet), so nothing is recruited. And AddReinforcer only increments
/// ColonyData.reinforcer -- a *pending* queue -- not the visible
/// ColonyData.population (which COLONYDATA_POPULATIONMAX caps at 999); a separate
/// CommitReinforcer() moves reinforcer -> population.
///
/// Rather than revive the offline-disabled guest system, we add to the village
/// count directly: postfix OnPassingEachOther (fires once per completed pass,
/// offline included) and bump ColonyData.population by 1. Gated to offline so it
/// never double-grants alongside the vanilla online flow.
///
/// OnPassingEachOther is private, so it's targeted by name. If Harmony can't
/// resolve it under Il2CppInterop, the public MB_FieldUI.PassingEachOther(npc)
/// is the fallback hook (fires at pass-start instead of completion).
/// </summary>
[HarmonyPatch(typeof(MB_FieldUI), "OnPassingEachOther")]
public static class PassengerRecruit
{
    private const int PopulationMax = 999; // ColonyData.COLONYDATA_POPULATIONMAX

    [HarmonyPostfix]
    public static void Postfix()
    {
        if (!Plugin.PassengerRateEnabled.Value) return;
        if (PassengerManager.IsOnline) return; // let the vanilla flow handle online

        Hikari gameData = Hikari.GetGameData();
        if (gameData == null) return;

        ColonyData colony = gameData.GetColonyData();
        if (colony == null) return;

        if (colony.population >= PopulationMax) return;

        int before = colony.population;
        colony.population = before + 1;

        if (Plugin.DebugLogging.Value)
            Plugin.Log.LogInfo($"[PassengerRecruit] passed a soul: population {before} -> {colony.population}");
    }
}
