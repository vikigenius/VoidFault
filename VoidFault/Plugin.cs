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
            "Increase how many Passing Souls appear and can be recruited per town visit. Each recruited " +
            "soul adds 1 colony population. Only takes effect where the game already spawns souls (i.e. " +
            "past the story-progress gate); refreshes every visit, so re-entering a town lets you farm.");
        PassengerSoulsPerTown = Config.Bind("PassengerRate", "SoulsPerTown", 5,
            "Target number of Passing Souls per town visit (clamped 1-99). Note: offline, the actual number " +
            "recruited may be limited by your existing guest pool -- enable Debug logging to check.");

        DebugLogging = Config.Bind("Debug", "Enabled", false,
            "Log per-call details for JP Up / Gold Up / MAtk Scaling / Passenger Rate patches. Off by default. " +
            "Passenger Rate logs once per town entry (not spammy). " +
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
