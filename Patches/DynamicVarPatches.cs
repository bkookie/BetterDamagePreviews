using System.Text.RegularExpressions;
using BetterDamagePreviews.PreviewVars;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace BetterDamagePreviews.Patches;

[HarmonyPatch(typeof(DynamicVar), nameof(DynamicVar.ToHighlightedString))]
public static partial class DamagePreviewPatch
{
    [HarmonyPostfix]
    private static void PostFix(DynamicVar __instance, ref string __result)
    {
        // Append the total calculated damage after the normal damage value on the card

        IDamagePreviewVar? previewVar = __instance as IDamagePreviewVar;
        if (previewVar != null || PreviewManager.AutoPreviewDamageLookup.TryGetValue(__instance, out previewVar))
        {
            int totalDamage = PreviewManager.CalculateTotalDamage(previewVar);
            if (previewVar.ShouldDisplayValue && totalDamage >= 0 && totalDamage != (int)previewVar.PreviewValue)
            {
                string accuracyAdendum = previewVar.Accuracy switch
                {
                    Accuracy.Accurate => "",
                    Accuracy.Approximate => "?",
                    Accuracy.Minimum => "+",
                    _ => ""
                };
                __result = $"{__result} ([orange]{totalDamage}{accuracyAdendum}[/orange])";
            }
        }
        else if (PreviewManager.AutoPreviewHitCountLookup.TryGetValue(__instance, out previewVar) && previewVar.CardDamageSource != null)
        {
            // If we marked this DynamicVar as a hitcount var, we can display it's modified hitcount with highlighting.

            Match match = GetPreviewValueWithoutFormatting().Match(__result);
            if (match.Success)
            {
                if (int.TryParse(match.Groups[1].Value, out int currentHitCount))
                {
                    int newHitCount = previewVar.CardDamageSource.HitCount;
                    if (newHitCount > currentHitCount)
                    {
                        __result = $"[green]{newHitCount}[/green]";
                    }
                    else if (newHitCount < currentHitCount)
                    {
                        __result = $"[red]{newHitCount}[/red]";
                    }
                }
            }
        }
    }

    [GeneratedRegex(@".*(\d+).*", RegexOptions.None)]
    private static partial Regex GetPreviewValueWithoutFormatting();
}