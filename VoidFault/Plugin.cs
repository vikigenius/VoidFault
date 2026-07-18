using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace VoidFault;

[BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
public class Plugin : BasePlugin
{
    internal static new ManualLogSource Log;

    internal static ConfigEntry<bool> JPUpEnabled;
    internal static ConfigEntry<int> JPUpBonusPercent;

    internal static ConfigEntry<bool> GoldUpEnabled;
    internal static ConfigEntry<int> GoldUpBonusPercent;

    internal static ConfigEntry<bool> PassengerRateEnabled;
    internal static ConfigEntry<int> PassengerSoulsPerTown;

    internal static ConfigEntry<bool> DebugLogging;

    public override void Load()
    {
        Log = base.Log;
        Log.LogInfo($"Hello from {PluginInfo.NAME} v{PluginInfo.VERSION}!");

        JPUpEnabled = Config.Bind("JPUp", "Enabled", true,
            "Grant every character a bonus to JP earned after battle, stacking with the vanilla JP Up ability.");
        JPUpBonusPercent = Config.Bind("JPUp", "BonusPercent", 20,
            "Percentage of earned JP added as a bonus (e.g. 20 = +20%).");

        GoldUpEnabled = Config.Bind("GoldUp", "Enabled", true,
            "Grant a bonus to Gil earned after battle.");
        GoldUpBonusPercent = Config.Bind("GoldUp", "BonusPercent", 20,
            "Percentage of earned Gil added as a bonus (e.g. 20 = +20%).");

        PassengerRateEnabled = Config.Bind("PassengerRate", "Enabled", true,
            "Spawn extra Passing Souls per town visit and grant +1 village population for each one you pass. " +
            "Works offline by adding to the colony directly (the vanilla guest-recruit system is disabled " +
            "without internet). Only takes effect where the game already spawns souls (past the story-progress " +
            "gate); refreshes every visit, so re-entering a town lets you farm. Population caps at 999.");
        PassengerSoulsPerTown = Config.Bind("PassengerRate", "SoulsPerTown", 5,
            "Number of Passing Soul ghosts spawned per town visit (clamped 1-99). Pass each one to gain a villager.");

        DebugLogging = Config.Bind("Debug", "Enabled", false,
            "Log per-call details for JP Up / Gold Up / MAtk Scaling patches. Off by default. " +
            "Note: MAtk Scaling logs on every GetMATK call, which fires constantly outside battle too (menus, tooltips) — expect a lot of log lines while this is on.");

        // Harmony will auto-discover all [HarmonyPatch] classes in this assembly
        var harmony = new Harmony(PluginInfo.GUID);
        harmony.PatchAll();

        Log.LogInfo("Patches applied.");
    }
}

internal static class PluginInfo
{
    public const string GUID    = "com.voidfault.mod";
    public const string NAME    = "VoidFault";
    public const string VERSION = "1.0.0";
}
