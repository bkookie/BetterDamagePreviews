using MegaCrit.Sts2.Core.Entities.Creatures;

namespace BetterDamagePreviews.Hooks;

/// <summary>
/// Interface for a combat hook listener that modifies the hit count of an attack, used for a card's damage preview only.
/// </summary>
public interface IHitCountModifierForDisplay
{
    /// <summary>
    /// Modifies the hit count of an attack for display purposes only.
    /// </summary>
    /// <remarks>Need to use this rather than the built in <see cref="MegaCrit.Sts2.Core.Models.AbstractModel.ModifyAttackHitCount(MegaCrit.Sts2.Core.Commands.Builders.AttackCommand, int)"/>, since we wont have an attack command until actually playing the card.</remarks>
    /// <param name="attacker">The <see cref="Creature"/> performing the attack.</param>
    /// <param name="target">The target of the attack.</param>
    /// <param name="hitCount">How many times the attack is currently hitting for.</param>
    /// <returns>The modified hit count.</returns>
    int ModifyHitCountForDisplay(Creature? attacker, Creature? target, int hitCount);
}