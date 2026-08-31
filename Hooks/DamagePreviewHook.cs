using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace BetterDamagePreviews.Hooks;

/// <summary>
/// Various hooks related to displaying damage preview amounts.
/// </summary>
public class DamagePreviewHook
{
    private static IEnumerable<T> CombatHookListeners<T>(ICombatState? combatState)
    {
        return combatState?.IterateHookListeners().OfType<T>() ?? [];
    }

    /// <inheritdoc cref="IHitCountModifierForDisplay.ModifyHitCountForDisplay(Creature?, Creature?, int)"/>
    public static int ModifyHitCountForDisplay(Creature attacker, Creature? target, int hitCount) 
    {
        // I dont have an AttackCommand to use the actual ModifyAttackHitCount hook
        // Iterates all combat listeners, returning any that implement either our IHitCountModifierForDisplay, or their registered version of it

        foreach (IHitCountModifierForDisplay hitCountModifier in CombatHookListeners<IHitCountModifierForDisplay>(attacker.CombatState))
        {
            hitCount = hitCountModifier.ModifyHitCountForDisplay(attacker, target, hitCount);
        }

        return hitCount;
    }
}
