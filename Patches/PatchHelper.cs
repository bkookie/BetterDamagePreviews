using System.Reflection;
using System.Reflection.Emit;
using BetterDamagePreviews.PreviewVars;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace BetterDamagePreviews.Patches;

/// <summary>
/// Helper methods for patching cards with replacement PreviewVars.
/// </summary>
[Obsolete("Patching individual cards should no longer be necessary.")]
public static class PatchHelper
{
    /// <inheritdoc cref="PatchDamagePreviewVars_GetCanonicalVars(IEnumerable{DynamicVar}, int, int, int)"/>
    /// <remarks>Assumes an index of 0 for the <see cref="DamageVar"/> and 1 for the <see cref="RepeatVar"/>.</remarks>
    public static IEnumerable<DynamicVar> PatchDamagePreviewVars_GetCanonicalVars(IEnumerable<DynamicVar> __result)
    {
        return PatchDamagePreviewVars_GetCanonicalVars(__result, damageVarIndex: 0, extraHitCount: 0, hitCountVarIndex: 1);
    }

    /// <inheritdoc cref="PatchDamagePreviewVars_GetCanonicalVars(IEnumerable{DynamicVar}, int, int, int)"/>
    /// <remarks>Does not look for or replace the <see cref="RepeatVar"/>.</remarks>
    public static IEnumerable<DynamicVar> PatchDamagePreviewVars_GetCanonicalVars(IEnumerable<DynamicVar> __result, int damageVarIndex)
    {
        return PatchDamagePreviewVars_GetCanonicalVars(__result, damageVarIndex, extraHitCount: 0, hitCountVarIndex: -1);
    }

    /// <inheritdoc cref="PatchDamagePreviewVars_GetCanonicalVars(IEnumerable{DynamicVar}, int, int, int)"/>
    public static IEnumerable<DynamicVar> PatchDamagePreviewVars_GetCanonicalVars(IEnumerable<DynamicVar> __result, int damageVarIndex, int hitCountVarIndex)
    {
        return PatchDamagePreviewVars_GetCanonicalVars(__result, damageVarIndex, extraHitCount: 0, hitCountVarIndex);
    }

    /// <summary>
    /// Replaces <see cref="DamageVar"/> with <see cref="DamagePreviewVar"/> and <see cref="RepeatVar"/> with <see cref="HitCountVar"/>
    /// </summary>
    /// <param name="__result">The original CanonicalVars.</param>
    /// <param name="damageVarIndex">The index of the <see cref="DamageVar"/> among the CanonicalVars.</param>
    /// <param name="extraHitCount">Additional hit count for the attack. Set this value when the card does not use a <see cref="DynamicVar"/> for the hit count (eg. <see cref="MegaCrit.Sts2.Core.Models.Cards.Maul"/>).</param>
    /// <param name="hitCountVarIndex">The index of the <see cref="RepeatVar"/> among the CanonicalVars (note that the card may use a different var for this). Supply a negative number when not present.</param>
    /// <returns></returns>
    public static IEnumerable<DynamicVar> PatchDamagePreviewVars_GetCanonicalVars(IEnumerable<DynamicVar> __result, int damageVarIndex, int extraHitCount, int hitCountVarIndex)
    {
        List<DynamicVar> vars = [.. __result];

        // Need to keep the same localization names
        if (damageVarIndex >= 0)
            vars[damageVarIndex] = new DamagePreviewVar(vars[damageVarIndex].Name, vars[damageVarIndex].BaseValue, ((DamageVar)vars[damageVarIndex]).Props, extraHitCount);
        //if (hitCountVarIndex >= 0)
        //    vars[hitCountVarIndex] = new HitCountVar(vars[hitCountVarIndex].Name, vars[hitCountVarIndex].BaseValue);

        return [.. vars];
    }

    /// <inheritdoc cref="PatchDamagePreviewVars_GetHitCountVar(IEnumerable{CodeInstruction}, string)"/>
    public static IEnumerable<CodeInstruction> PatchDamagePreviewVars_GetHitCountVar(IEnumerable<CodeInstruction> instructions)
    {
        const string DefaultHitCountVarPropertyName = nameof(DynamicVarSet.Repeat);
        return PatchDamagePreviewVars_GetHitCountVar(instructions, DefaultHitCountVarPropertyName);
    }

    /// <summary>
    /// Replaces calls to the property DynamicVars.Repeat with the indexer DynamicVars["Repeat"] (or whichever property the card uses).
    /// </summary>
    /// <remarks>Not required to patch this if the card already uses the direct string indexer to retrieve the DynamicVar, or if the hit count is hardcoded and not using a DynamicVar at all.</remarks>
    /// <param name="instructions">The code instructions to patch.</param>
    /// <param name="originalHitCountVarPropertyName">The name of the property to replace with an indexer.</param>
    /// <returns></returns>
    public static IEnumerable<CodeInstruction> PatchDamagePreviewVars_GetHitCountVar(IEnumerable<CodeInstruction> instructions, string originalHitCountVarPropertyName)
    {
        MethodInfo referenceMethod = AccessTools.PropertyGetter(typeof(DynamicVarSet), originalHitCountVarPropertyName);
        MethodInfo indexGetter = AccessTools.IndexerGetter(typeof(DynamicVarSet), [typeof(string)]);

        List<CodeInstruction> codes = [.. instructions];

        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].Calls(referenceMethod))
            {
                codes[i] = new CodeInstruction(OpCodes.Ldstr, originalHitCountVarPropertyName);
                codes.Insert(++i, new CodeInstruction(OpCodes.Callvirt, indexGetter));
                break;
            }
        }

        return codes;
    }
}