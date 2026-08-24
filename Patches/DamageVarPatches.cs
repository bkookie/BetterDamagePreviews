using BetterDamagePreviews.PreviewVars;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace BetterDamagePreviews.Patches;

[HarmonyPatch(typeof(DamageVar), nameof(DamageVar.UpdateCardPreview))]
public static class UpdateCardPreviewPatch
{
    [HarmonyPostfix]
    private static void PostFix(DamageVar __instance, CardModel card, Creature target)
    {
        if (__instance.GetType() == typeof(DamageVar)) // Dont overwrite a DamagePreviewVar if one is already being used
        {
            if (!PreviewManager.AutoPreviewDamageLookup.TryGetValue(__instance, out IDamagePreviewVar? var))
            {
                // need update extra hits
                // need ditchextra hits and just use hitcount
                DamagePreviewVar previewVar;
                if (PreviewManager.CardPreviewVarFactory.TryGetValue(card.GetType(), out Func<DamageVar, CardModel, Creature, DamagePreviewVar>? factory))
                {
                    previewVar = factory(__instance, card, target);
                }
                else
                {
                    previewVar = new DamagePreviewVar(__instance.BaseValue, __instance.Props);
                }

                previewVar.SetOwner(card);
                var = previewVar;
                PreviewManager.AutoPreviewDamageLookup[__instance] = var;
            }

            var.PreviewValue = __instance.PreviewValue;

            if (var.ExtraHitCount == 0)
            {
                // If the hitcount is already set, then it must be hardcoded to the card, and there is no DynamicVar to look for.
                // Finding the hitcountvar here allows the hit count preview value to update on the card as well as the damage.
                // Has no effect if the card does not preview the hit count.
                string hitCountVarName = PreviewManager.CardHitCountVarNameLookup.TryGetValue(card.GetType(), out string? name) ? name : RepeatVar.defaultName;
                //if (PreviewManager.CardHitCountLookup.TryGetValue(card.GetType(), out int hitCount))
                //{
                //    var.ExtraHitCount = hitCount - 1;
                //}
                if (card.DynamicVars.TryGetValue(hitCountVarName, out DynamicVar? hitCountVar))
                {
                    var.ExtraHitCount = hitCountVar.IntValue - 1; // Only want the extra hits after the first
                    PreviewManager.AutoPreviewHitCountLookup[hitCountVar] = var;
                }
                else if (card.DynamicVars.TryGetValue(CalculatedVar.DefaultHitCountName, out DynamicVar? cVar) && cVar is CalculatedVar calculatedVar)
                {
                    var.ExtraHitCount = calculatedVar.CalculateInt(target) - 1; // If this results in -1, it will negate the default 1 hitcount for a total of 0 hits. This is the desired behaviour.
                    PreviewManager.AutoPreviewHitCountLookup[calculatedVar] = var;
                }
            }

            PreviewManager.UpdateDamagePreview(var, card, target);
        }
    }
}