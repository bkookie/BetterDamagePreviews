//using HarmonyLib;
//using MegaCrit.Sts2.Core.Localization.DynamicVars;
//using MegaCrit.Sts2.Core.Models.Cards;

//namespace DamagePreviewVars.Patches.Cards;

//[HarmonyPatch(typeof(Peck), "CanonicalVars", MethodType.Getter)]
//public static class PeckCanonicalPatch
//{
//    [HarmonyPostfix]
//    private static void Postfix(ref IEnumerable<DynamicVar> __result)
//    {
//        __result = PatchHelper.PatchDamagePreviewVars_GetCanonicalVars(__result);
//    }
//}

//[HarmonyPatch(typeof(Peck), "OnPlay", MethodType.Async)]
//public static class PeckOnPlayPatch
//{
//    [HarmonyTranspiler]
//    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
//    {
//        return PatchHelper.PatchDamagePreviewVars_GetHitCountVar(instructions);
//    }
//}

//[HarmonyPatch(typeof(Peck), "OnUpgrade", MethodType.Normal)]
//public static class PeckUpgradePatch
//{
//    [HarmonyTranspiler]
//    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
//    {
//        return PatchHelper.PatchDamagePreviewVars_GetHitCountVar(instructions);
//    }
//}