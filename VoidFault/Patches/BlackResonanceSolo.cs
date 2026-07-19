using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace VoidFault.Patches;

/// <summary>
/// Makes the Black Resonance specialty work for a lone user, completing the
/// BlackResonance data mod (which only rescales CorrectionData.magSympathy).
///
/// Ghidra (Round 7a) showed BtlCharaManager.GetMagicSympathy counts battle charas
/// with SPT_TYPE_MAGIC_SYMPATHY (33), then:
///   * hard-returns 1.0 unless the count is 2, 3, or 4  -> a SOLO user gets nothing;
///   * returns magSympathy[count - 2], clamped so count maxes at 4 -> index maxes
///     at 2, so magSympathy[3] is NEVER read.
/// So the data mod's magSympathy_0 change can't help a solo user, and its 4th
/// value is dead. This prefix re-implements the count and indexes magSympathy
/// [count - 1] with no solo guard, so 1/2/3/4 users map to slots 0/1/2/3 — i.e.
/// the mod's [110,120,130,140] = x1.1/1.2/1.3/1.4, matching the ability's text.
///
/// It reads magSympathy from the loaded CorrectionData, so it stays decoupled
/// from whatever values the data mod sets.
/// </summary>
[HarmonyPatch(typeof(BtlCharaManager), nameof(BtlCharaManager.GetMagicSympathy))]
public static class BlackResonanceSolo
{
    // BTLDEF.SupportType.SPT_TYPE_MAGIC_SYMPATHY = 33 (Black Resonance).
    private const int SptTypeMagicSympathy = 33;

    [HarmonyPrefix]
    public static bool Prefix(BtlCharaManager __instance, ref float __result)
    {
        int count = 0;
        Il2CppReferenceArray<BtlChara> charas = __instance.m_battleCharaPtrArray;
        if (charas != null)
        {
            for (int i = 0; i < charas.Length; i++)
            {
                BtlChara chara = charas[i];
                if (chara != null && chara.IsSupport(SptTypeMagicSympathy))
                    count++;
            }
        }

        if (count <= 0)
        {
            __result = 1.0f; // no one has it -> no effect (matches vanilla default)
            return false;
        }

        // Reach CorrectionData via instance methods (Singleton<T>.GetInstance is a
        // generic-base static that Il2CppInterop doesn't surface as a usable member).
        BtlFunction btlFunction = __instance.GetBtlFunction();
        BtlDataManager dataManager = btlFunction?.GetBtlDataManager();
        BtlCorrectionData correction = dataManager?.GetCorrectionData(0);
        Il2CppStructArray<int> mag = correction?.magSympathy;
        if (mag == null || mag.Length == 0)
        {
            __result = 1.0f;
            return false;
        }

        int idx = count - 1;
        if (idx >= mag.Length) idx = mag.Length - 1; // clamp (party is 4, array is 4)
        __result = mag[idx] / 100.0f;
        return false; // skip original
    }
}
