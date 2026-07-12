using System;
using BepInEx;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace BD1MagicScaling
{
	// Token: 0x02000004 RID: 4
	[BepInPlugin("com.bd1.magicscaling", "MATk Magic Scaling", "1.0.0")]
	public class Plugin : BasePlugin
	{
		// Token: 0x06000003 RID: 3 RVA: 0x0000206C File Offset: 0x0000026C
		public override void Load()
		{
			Plugin.Log = base.Log;
			ManualLogSource log = Plugin.Log;
			bool flag;
			BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(14, 2, ref flag);
			if (flag)
			{
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("Hello from ");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted<string>("MATk Magic Scaling");
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" v");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted<string>("1.0.0");
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("!");
			}
			log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
			Harmony harmony = new Harmony("com.bd1.magicscaling");
			harmony.PatchAll();
			Plugin.Log.LogInfo("Patches applied.");
		}

		// Token: 0x04000002 RID: 2
		internal static ManualLogSource Log;
	}
}
