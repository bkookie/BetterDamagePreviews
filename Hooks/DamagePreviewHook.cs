using BetterDamagePreviews.Preview;
using BetterDamagePreviews.Wrappers;
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
    public static int ModifyHitCountForDisplay(Creature attacker, Creature? target, int hitCount) // I dont have an AttackCommand to use the actual ModifyAttackHitCount hook
    {
        //foreach (IHitCountModifierForDisplay hitCountModifier in CombatHookListeners<IHitCountModifierForDisplay>(attacker.CombatState))
        //{
        //    hitCount = hitCountModifier.ModifyHitCountForDisplay(attacker, target, hitCount);
        //}

        // Iterates all combat listeners, returning any that implement either our IHitCountModifierForDisplay, or their registered version of it (and wrapping it in the latter case)
        foreach (IHitCountModifierForDisplay hitCountModifier in attacker.CombatState?.IterateHookListeners().Where(model =>
        {
            Type cardType = model.GetType();
            
            foreach (Type type in (Type[])[typeof(IHitCountModifierForDisplay), .. PreviewManager.AdditionalHitCountModifierTypes])
            {
                if (cardType.IsAssignableTo(type))
                    return true;
            }
            return false;
        }).Select(model =>
        {
            if (model is IHitCountModifierForDisplay modifier)
                return modifier;
            else 
                return DynamicWrapper.CreateWrapper<IHitCountModifierForDisplay>(model);
        }) ?? [])
        {
            hitCount = hitCountModifier.ModifyHitCountForDisplay(attacker, target, hitCount);
        }

        return hitCount;
    }
}
