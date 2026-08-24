//using HarmonyLib;
//using MegaCrit.Sts2.Core.Localization.DynamicVars;
//using MegaCrit.Sts2.Core.Models.Cards;

//namespace DamagePreviewVars.Patches.Cards;

//[HarmonyPatch(typeof(SwordBoomerang), "CanonicalVars", MethodType.Getter)]
//public static class SwordBoomerangPatch
//{
//    [HarmonyPostfix]
//    private static void Postfix(ref IEnumerable<DynamicVar> __result)
//    {
//        __result = PatchHelper.PatchDamagePreviewVars_GetCanonicalVars(__result);
//    }
//}

//[HarmonyPatch(typeof(SwordBoomerang), "OnPlay", MethodType.Async)]
//public static class SwordBoomerangOnPlayPatch
//{
//    [HarmonyTranspiler]
//    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
//    {
//        return PatchHelper.PatchDamagePreviewVars_GetHitCountVar(instructions);
//    }
//}