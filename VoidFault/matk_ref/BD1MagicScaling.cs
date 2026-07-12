using System;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace BD1MagicScaling.Patches
{
	// Token: 0x02000006 RID: 6
	[HarmonyPatch(typeof(CharacterState), "GetMATK")]
	public static class BD1MagicScaling
	{
		// Token: 0x06000005 RID: 5 RVA: 0x00002108 File Offset: 0x00000308
		[HarmonyPrefix]
		public static bool Prefix(CharacterState __instance, bool ignoreManjuu, ref int __result)
		{
			int mAtk = __instance.GetINT(ignoreManjuu);
			ItemState[] equipmentArr = new ItemState[] { __instance.RHAND, __instance.LHAND, __instance.HEAD, __instance.BODY, __instance.AC, __instance.COSTUME };
			foreach (ItemState equipment in equipmentArr)
			{
				bool flag = equipment != null && equipment.IsExist();
				if (flag)
				{
					ItemTable equipmentTable = equipment.GetParam();
					int itemMatk = equipmentTable.MATK;
					bool flag2 = equipmentTable.IsWeapon();
					if (flag2)
					{
						int correctedMAtkMult = __instance.GetItemProperATK(equipmentTable.ITEM_ID);
						itemMatk = itemMatk * correctedMAtkMult / 100;
					}
					mAtk += itemMatk;
				}
			}
			bool pAbilink = false;
			bool hasMatkAbility10 = __instance.IsEnableSupportAbility(1007, ref pAbilink);
			bool flag3 = hasMatkAbility10;
			if (flag3)
			{
				mAtk = mAtk * 110 / 100;
			}
			pAbilink = false;
			bool hasMatkAbility11 = __instance.IsEnableSupportAbility(1146, ref pAbilink);
			bool flag4 = hasMatkAbility11;
			if (flag4)
			{
				mAtk = mAtk * 120 / 100;
			}
			pAbilink = false;
			bool hasMatkAbility12 = __instance.IsEnableSupportAbility(1012, ref pAbilink);
			bool flag5 = hasMatkAbility12;
			if (flag5)
			{
				mAtk = mAtk * 130 / 100;
			}
			pAbilink = false;
			bool hasLateBloomerAbility = __instance.IsEnableSupportAbility(1108, ref pAbilink);
			bool flag6 = hasLateBloomerAbility && !pAbilink;
			if (flag6)
			{
				Il2CppReferenceArray<JobState> jobArr = __instance.m_JobStateArray;
				int jobIndex = 0;
				int lateBloomerMult = 100;
				bool flag7 = jobArr == null;
				if (flag7)
				{
					Plugin.Log.LogInfo("[BD1MagicScaling] GetAtk method failed at hasLateBloomerAbility!");
					return true;
				}
				for (;;)
				{
					bool flag8 = jobArr.Length <= jobIndex;
					if (flag8)
					{
						break;
					}
					JobState jobState = jobArr[jobIndex];
					bool flag9 = jobState == null || jobState.m_JobParam == null;
					if (flag9)
					{
						goto Block_13;
					}
					bool flag10 = jobState.m_JobParam.LV >= 14;
					if (flag10)
					{
						lateBloomerMult++;
					}
					jobIndex++;
					if (jobIndex >= 24)
					{
						goto Block_15;
					}
				}
				Plugin.Log.LogInfo("[BD1MagicScaling] GetAtk method failed due to JobStateArray being smaller than expected!");
				return true;
				Block_13:
				Plugin.Log.LogInfo("[BD1MagicScaling] GetAtk method failed due to invalid JobState at index " + jobIndex.ToString() + "!");
				return true;
				Block_15:
				mAtk = mAtk * lateBloomerMult / 100;
			}
			mAtk = Math.Clamp(mAtk, 0, 999);
			__result = mAtk;
			return false;
		}
	}
}
