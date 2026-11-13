using System.Reflection;
using BetterDamagePreviews.Preview;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace BetterDamagePreviews.Patches;

[HarmonyPatch]
internal static class DamageVarPatches
{
    internal static void CompatPatch(Harmony harmony)
    {
        MethodInfo? getBaseValueForIConvertable = AccessTools.Method(typeof(CalculatedVar), "GetBaseValueForIConvertible");
        if (getBaseValueForIConvertable != null)
        {
            HarmonyMethod postfix = new(AccessTools.Method(typeof(DamageVarPatches), nameof(Postfix_GetBaseValueForIConvertible)));
            harmony.Patch(getBaseValueForIConvertable, postfix: postfix);
        }
    }

    [HarmonyPatch(typeof(DamageVar), nameof(DamageVar.UpdateCardPreview))]
    [HarmonyPostfix]
    internal static void Postfix_DamageVar(DamageVar __instance, CardModel card, Creature target)
    {
        Helper.Postfix(__instance, card, target);
    }

    [HarmonyPatch(typeof(CalculatedDamageVar), nameof(CalculatedDamageVar.UpdateCardPreview))]
    [HarmonyPostfix]
    internal static void Postfix_CalculatedDamageVar(CalculatedDamageVar __instance, CardModel card, Creature target)
    {
        Helper.Postfix(__instance, card, target);
    }

    internal static void Postfix_GetBaseValueForIConvertible(CalculatedVar __instance, ref decimal __result)
    {
        // Ive forgotten why this patch was even needed
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
            DynamicVar? hitCountVar = card.DynamicVars.Values.FirstOrDefault(dvar => dvar.GetType().Name == "HitCountVar" || dvar.GetType().Name == "CalculatedHitCountVar"); // Use reflection for main-branch compat

            bool multihitDetected;

            int constHitCount = -1;
            string? hitCountVarName = null;
            Type? dynamicVarType = null;
            Func<Creature?, int>? hitCountFunc = null;
            string? errorMsg = null;

            if (hitCountVar != null)
            {
                multihitDetected = true;
            }
            else
            {
                multihitDetected = HitCountILScanner.TryDetectHitCount(card, out constHitCount, out hitCountVarName, out dynamicVarType, out hitCountFunc, out errorMsg);

                if (!(PreviewManager.CardHitCountVarNameLookup.TryGetValue(card.GetType(), out HitCountVarName? lookup)
                            && (!string.IsNullOrEmpty(lookup.HitCountName) && card.DynamicVars.TryGetValue(lookup.HitCountName, out hitCountVar)
                                || !string.IsNullOrEmpty(lookup.CalculatedHitCountName) && card.DynamicVars.TryGetValue(lookup.CalculatedHitCountName, out hitCountVar)))
                    && multihitDetected
                    && (hitCountVarName == null || !card.DynamicVars.TryGetValue(hitCountVarName, out hitCountVar))
                    && hitCountFunc == null)
                {
                    foreach (var (name, dvar) in card.DynamicVars)
                    {
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
            }

            if ((multihitDetected || errorMsg != null) && LoggedCards.Add(card.Id))
            {
                if (!multihitDetected)
                {
                    MainFile.Logger.LogMessage(LogLevel.Warn, $"Cannot read card '{card.Title}': {errorMsg}", 1);
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

            foreach (var (lookupType, factory) in PreviewManager.CardPreviewFactory)
            {
                Type cardType = card.GetType();

                if (cardType == lookupType || lookupType.IsInterface && cardType.IsAssignableTo(lookupType))
                {
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