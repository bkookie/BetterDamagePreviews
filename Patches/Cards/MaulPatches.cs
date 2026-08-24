//using HarmonyLib;
//using MegaCrit.Sts2.Core.Localization.DynamicVars;
//using MegaCrit.Sts2.Core.Models.Cards;

//namespace DamagePreviewVars.Patches.Cards;

//[HarmonyPatch(typeof(Maul), "CanonicalVars", MethodType.Getter)]
//public static class MaulPatch
//{
//    [HarmonyPostfix]
//    private static void Postfix(ref IEnumerable<DynamicVar> __result)
//    {
//        __result = PatchHelper.PatchDamagePreviewVars_GetCanonicalVars(__result, damageVarIndex: 0, extraHitCount: 1, hitCountVarIndex: -1);
//    }
//}