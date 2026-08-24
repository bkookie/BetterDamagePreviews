//using HarmonyLib;
//using MegaCrit.Sts2.Core.Localization.DynamicVars;
//using MegaCrit.Sts2.Core.Models.Cards;

//namespace DamagePreviewVars.Patches.Cards;

//[HarmonyPatch(typeof(Exterminate), "CanonicalVars", MethodType.Getter)]
//public static class ExterminatePatch
//{
//    [HarmonyPostfix]
//    private static void Postfix(ref IEnumerable<DynamicVar> __result)
//    {
//        __result = PatchHelper.PatchDamagePreviewVars_GetCanonicalVars(__result);
//    }
//}

//[HarmonyPatch(typeof(Exterminate), "OnPlay", MethodType.Async)]
//public static class ExterminateOnPlayPatch
//{
//    [HarmonyTranspiler]
//    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
//    {
//        return PatchHelper.PatchDamagePreviewVars_GetHitCountVar(instructions);
//    }
//}