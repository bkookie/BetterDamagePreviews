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
}
