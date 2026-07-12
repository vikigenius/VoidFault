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

    public override void Load()
    {
        Log = base.Log;
        Log.LogInfo($"Hello from {PluginInfo.NAME} v{PluginInfo.VERSION}!");

        JPUpEnabled = Config.Bind("JPUp", "Enabled", true,
            "Grant every character a bonus to JP earned after battle, stacking with the vanilla JP Up ability.");
        JPUpBonusPercent = Config.Bind("JPUp", "BonusPercent", 20,
            "Percentage of earned JP added as a bonus (e.g. 20 = +20%).");

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
