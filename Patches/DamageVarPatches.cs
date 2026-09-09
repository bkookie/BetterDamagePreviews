using BetterDamagePreviews.Preview;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
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

    [HarmonyPatch(typeof(CalculatedVar), "GetBaseValueForIConvertible")]
    [HarmonyPostfix]
    private static void Postfix_GetBaseValueForIConvertible(CalculatedVar __instance, ref decimal __result)
    {
        __result = __instance.PreviewValue; // for plural formatter
    }
}

internal static class Helper
{
    private static readonly HashSet<ModelId> LoggedCards = [];
    private static readonly HashSet<string> CommonHitCountVarNames = [];

    static Helper()
    {
        CommonHitCountVarNames.Add("HitCount");
        CommonHitCountVarNames.Add("Hits");
        //CommonHitCountVarNames.Add("Repeat");
    }

    public static void Postfix(DynamicVar __instance, CardModel card, Creature target)
    {
        if (!card.IsInCombat)
            return;

        if (!PreviewManager.PreviewCache.TryGetValue(__instance, out IDamagePreview? preview))
        {
            DynamicVar? hitCountVar = null;
            bool multihitDetected = CardILReader.TryDetectHitCount(card, out int constHitCount, out string? hitCountVarName, out Type? dynamicVarType, out Func<Creature?, int>? hitCountFunc, out string? errorMsg);

            if (!(PreviewManager.CardHitCountVarNameLookup.TryGetValue(card.GetType(), out HitCountVarName? lookup)
                        && (!string.IsNullOrEmpty(lookup.HitCountName) && card.DynamicVars.TryGetValue(lookup.HitCountName, out hitCountVar)
                            || !string.IsNullOrEmpty(lookup.CalculatedHitCountName) && card.DynamicVars.TryGetValue(lookup.CalculatedHitCountName, out hitCountVar)))
                && multihitDetected
                && (hitCountVarName == null || !card.DynamicVars.TryGetValue(hitCountVarName, out hitCountVar))
                && hitCountFunc == null)
            {
                foreach (var kvp in card.DynamicVars)
                {
                    string name = kvp.Key;
                    DynamicVar dvar = kvp.Value;

                    if (dynamicVarType != null && !dvar.GetType().IsAssignableTo(dynamicVarType) || dvar is DamageVar || dvar is ExtraDamageVar || dvar is CalculationBaseVar || dvar is CalculationExtraVar)
                        continue;

                    foreach (string s in CommonHitCountVarNames)
                    {
                        if (name.Contains(s))
                        {
                            hitCountVar = dvar;
                            break;
                        }
                    }
                }
            }

            if ((multihitDetected || errorMsg != null) && LoggedCards.Add(card.Id))
            {
                if (!multihitDetected)
                {
                    MainFile.Logger.LogMessage(LogLevel.Warn, $"Error reading card '{card.Title}': {errorMsg}", 1);
                }
                else
                {
                    LogLevel logLevel = LogLevel.Debug;
                    string result;

                    if (constHitCount >= 0)
                    {
                        result = constHitCount.ToString();
                    }
                    else if (hitCountVar != null)
                    {
                        result = $"{hitCountVar.GetType().Name} \"{hitCountVar.Name}\"";
                    }
                    else if (dynamicVarType != null)
                    {
                        logLevel = LogLevel.Warn;
                        result = $"hitCountVar detected but not found: {dynamicVarType?.Name} \"{hitCountVarName ?? "<UnknownName>"}\"";
                    }
                    else if (hitCountVarName != null)
                    {
                        logLevel = LogLevel.Warn;
                        result = $"hitCountVar detected but not found: \"{hitCountVarName}\"";
                    }
                    else if (hitCountFunc != null)
                    {
                        result = "Func";
                    }
                    else if (errorMsg != null)
                    {
                        logLevel = LogLevel.Warn;
                        result = $"<Unknown> ({errorMsg})";
                    }
                    else
                    {
                        logLevel = LogLevel.Warn;
                        result = "<Unknown>";
                    }

                    MainFile.Logger.LogMessage(logLevel, $"Detected multi-hit card '{card.Title}': {result}", 1);
                }
            }

            foreach (var kvp in PreviewManager.CardPreviewFactory)
            {
                Type cardType = card.GetType();
                Type lookupType = kvp.Key;

                if (cardType == lookupType || lookupType.IsInterface && cardType.IsAssignableTo(lookupType))
                {
                    Func<DynamicVar, Func<Creature?, int>, CardModel, IDamagePreview> factory = kvp.Value;
                    preview = factory(__instance, hitCountFunc ?? PreviewManager.HitCountFromDynamicVarFunc(hitCountVar), card);
                    break;
                }
            }
            if (preview == null) // will be null if there is no registered factory method for this card.
            {
                preview = new DamagePreview(__instance, hitCountFunc ?? PreviewManager.HitCountFromDynamicVarFunc(hitCountVar), card);

                if (multihitDetected && hitCountVar == null && hitCountFunc == null)
                {
                    preview.Accuracy = Accuracy.Unknown;
                }
            }

            if (hitCountVar != null)
            {
                PreviewManager.HitCountVarLookup[hitCountVar] = preview;
            }

            PreviewManager.PreviewCache[__instance] = preview;
        }

        preview.UpdateDamagePreview(target);
    }
}