using BetterDamagePreviews.PreviewSources;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace BetterDamagePreviews.PreviewVars;

/// <summary>
/// Interface for a <see cref="DamageVar"/> that can display a more accurate damage value, including various damage modifiers such as <see cref="IntangiblePower"/> or <see cref="SlipperyPower"/>.
/// </summary>
/// <remarks>To calculate more complex interactions, such as additional damage effects from the card itself or other powers, requires the use of one or more <see cref="IDamagePreviewSource"/>.</remarks>
public interface IDamagePreviewVar
{
    /// <summary>
    /// The card that owns the <see cref="IDamagePreviewVar"/>.
    /// </summary>
    public AbstractModel? Owner { get; internal set; }

    /// <summary>
    /// The calculated damage after all display hooks have executed, but before the custom calculation is run.
    /// </summary>
    public decimal PreviewValue { get; set; }

    /// <summary>
    /// The <see cref="Creature"/> that owns the card being previewed.
    /// </summary>
    public Creature? PreviewOwner { get; set; }

    /// <summary>
    /// The current hover target of the card being held.
    /// </summary>
    public Creature? PreviewTarget { get; set; }

    /// <summary>
    /// How accurate the calculated damage is. Approximate values will be indicated with a ? or a +.
    /// </summary>
    public Accuracy Accuracy { get; set; }

    /// <summary>
    /// How many extra times the attack will hit beyond the first. This is used when the card's hit count is hardcoded (no DynamicVar to read from).
    /// </summary>
    public int ExtraHitCount { get; set; }

    /// <summary>
    /// The main damage source of the card.
    /// </summary>
    public DefaultDamagePreviewSource? CardDamageSource { get; set; }

    /// <summary>
    /// Whether to display the calculated value on the card.
    /// </summary>
    public bool ShouldDisplayValue { get; set; }
}
