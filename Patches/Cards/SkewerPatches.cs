//using HarmonyLib;
//using MegaCrit.Sts2.Core.Localization.DynamicVars;
//using MegaCrit.Sts2.Core.Models.Cards;

//namespace DamagePreviewVars.Patches.Cards;

//[HarmonyPatch(typeof(Skewer), "CanonicalVars", MethodType.Getter)]
//public static class SkewerPatch
//{
//    [HarmonyPostfix]
//    private static void Postfix(ref IEnumerable<DynamicVar> __result)
//    {
//        __result = PatchHelper.PatchDamagePreviewVars_GetCanonicalVars(__result, damageVarIndex: 0);
//    }
//}