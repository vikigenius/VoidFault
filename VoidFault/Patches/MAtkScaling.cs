using System;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace VoidFault.Patches;

/// <summary>
/// Ported from the MAtk proficiency mod (matk_ref). Recomputes CharacterState.GetMATK
/// so equipment MAtk, support abilities, and the Late Bloomer job-level bonus are applied
/// the same way the original mod did, instead of the base game formula.
/// </summary>
[HarmonyPatch(typeof(CharacterState), nameof(CharacterState.GetMATK))]
public static class MAtkScaling
{
    private static int _callCount;

    [HarmonyPrefix]
    public static bool Prefix(CharacterState __instance, bool ignoreManjuu, ref int __result)
    {
        int mAtk = __instance.GetINT(ignoreManjuu);

        ItemState[] equipment =
        [
            __instance.RHAND, __instance.LHAND, __instance.HEAD,
            __instance.BODY, __instance.AC, __instance.COSTUME
        ];

        foreach (ItemState item in equipment)
        {
            if (item == null || !item.IsExist()) continue;

            ItemTable itemTable = item.GetParam();
            int itemMatk = itemTable.MATK;

            if (itemTable.IsWeapon())
            {
                int correctedMAtkMult = __instance.GetItemProperATK(itemTable.ITEM_ID);
                itemMatk = itemMatk * correctedMAtkMult / 100;
            }

            mAtk += itemMatk;
        }

        bool pAbilink;
        if (__instance.IsEnableSupportAbility(1007, out pAbilink))
            mAtk = mAtk * 110 / 100;

        if (__instance.IsEnableSupportAbility(1146, out pAbilink))
            mAtk = mAtk * 120 / 100;

        if (__instance.IsEnableSupportAbility(1012, out pAbilink))
            mAtk = mAtk * 130 / 100;

        if (__instance.IsEnableSupportAbility(1108, out pAbilink) && !pAbilink)
        {
            Il2CppReferenceArray<JobState> jobArr = __instance.m_JobStateArray;
            if (jobArr == null)
            {
                Plugin.Log.LogInfo("[VoidFault] GetMATK failed: JobStateArray was null.");
                return true;
            }

            int lateBloomerMult = 100;
            for (int jobIndex = 0; jobIndex < jobArr.Length; jobIndex++)
            {
                JobState jobState = jobArr[jobIndex];
                if (jobState == null || jobState.m_JobParam == null)
                {
                    Plugin.Log.LogInfo($"[VoidFault] GetMATK failed: invalid JobState at index {jobIndex}.");
                    return true;
                }

                if (jobState.m_JobParam.LV >= 14)
                    lateBloomerMult++;
            }

            mAtk = mAtk * lateBloomerMult / 100;
        }

        __result = Math.Clamp(mAtk, 0, 999);

        if (_callCount++ < 5)
            Plugin.Log.LogInfo($"[MAtkScaling] GetMATK -> {__result}");

        return false;
    }
}
