using BetterDamagePreviews.Preview;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace BetterDamagePreviews.Patches;

[HarmonyPatch]
public static class UpdateCardPreviewPatch
{
    [HarmonyPatch(typeof(DamageVar), nameof(DamageVar.UpdateCardPreview))]
    [HarmonyPostfix]
    private static void Postfix_DamageVar(DamageVar __instance, CardModel card, Creature target)
    {
        Helper.Postfix(__instance, card, target);
    }

    [HarmonyPatch(typeof(CalculatedDamageVar), nameof(CalculatedDamageVar.UpdateCardPreview))]
    [HarmonyPostfix]
    private static void Postfix_CalculatedDamageVar(CalculatedDamageVar __instance, CardModel card, Creature target)
    {
        Helper.Postfix(__instance, card, target);
    }
}

internal static class Helper
{
    public static void Postfix(DynamicVar __instance, CardModel card, Creature target)
    {
        if (!PreviewManager.PreviewCache.TryGetValue(__instance, out IDamagePreview? preview))
        {
            // Try finding the hitcountvar here, if one exists.
            string? hitCountVarName = RepeatVar.defaultName;
            string? calculatedHitCountVarName = CalculatedVar.DefaultHitCountName;
            if (PreviewManager.CardHitCountVarNameLookup.TryGetValue(card.GetType(), out HitCountVarName? lookup))
            {
                switch (lookup.VarType)
                {
                    case DynamicVarType.Normal:
                        hitCountVarName = lookup.HitCountName;
                        calculatedHitCountVarName = null;
                        break;
                    case DynamicVarType.Calculated:
                        hitCountVarName = null;
                        calculatedHitCountVarName = lookup.CalculatedHitCountName;
                        break;
                    case DynamicVarType.Either:
                        hitCountVarName = lookup.HitCountName;
                        calculatedHitCountVarName = lookup.CalculatedHitCountName;
                        break;
                }
            }

            DynamicVar? hitCountVar = null;
            foreach (var kvp in card.DynamicVars)
            {
                if (kvp.Key == hitCountVarName || kvp.Value is CalculatedVar && kvp.Key == calculatedHitCountVarName)
                {
                    hitCountVar = kvp.Value;
                    break;
                }
            }

            foreach (var kvp in PreviewManager.CardPreviewFactory)
            {
                Type cardType = card.GetType();
                Type lookupType = kvp.Key;

                if (cardType == lookupType || lookupType.IsInterface && cardType.IsAssignableTo(lookupType))
                {
                    Func<DynamicVar, Func<Creature?, int>, CardModel, IDamagePreview> factory = kvp.Value;
                    preview = factory(__instance, PreviewManager.HitCountFromDynamicVarFunc(hitCountVar), card);
                    break;
                }
            }
            preview ??= new DamagePreview(__instance, PreviewManager.HitCountFromDynamicVarFunc(hitCountVar), card); // will be null if there is no registered factory method for this card.

            if (hitCountVar != null)
            {
                PreviewManager.HitCountVarLookup[hitCountVar] = preview;
            }

            PreviewManager.PreviewCache[__instance] = preview;
        }

        preview.UpdateDamagePreview(target);
    }
}