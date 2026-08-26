using BetterDamagePreviews.PreviewSources;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace BetterDamagePreviews.Preview;

/// <summary>
/// Links to a <see cref="DamageVar"/> and facilitates additional calculations when previewing it's damage total (including various damage modifiers such as <see cref="SlipperyPower"/> or <see cref="FlutterPower"/>).
/// </summary>
/// <remarks>To calculate more complex interactions, such as additional damage effects from the card itself or other powers, requires the use of one or more <see cref="IDamagePreviewSource"/>.</remarks>
public interface IDamagePreview
{
    /// <summary>
    /// The <see cref="DamageVar"/> that this is linked to.
    /// </summary>
    public DamageVar LinkedDamageVar { get; }

    /// <summary>
    /// A function accepting a <see cref="Creature"/> target, and returning a hit count.
    /// </summary>
    public Func<Creature?, int> GetHitCount { get; }

    /// <summary>
    /// The <see cref="CardModel"/> that that this is linked to.
    /// </summary>
    public CardModel Card { get; }

    /// <summary>
    /// The current hover target of the card being held.
    /// </summary>
    public Creature? PreviewTarget { get; set; }

    /// <summary>
    /// How accurate the calculated damage is. Approximate values will be indicated with a ? or a +.
    /// </summary>
    public Accuracy Accuracy { get; set; }

    /// <summary>
    /// Whether to display the calculated value on the card.
    /// </summary>
    public bool ShouldDisplayValue { get; set; }

    /// <summary>
    /// The calculated damage after all display hooks have executed, but before the custom calculation is run.
    /// </summary>
    public int PreviewValue { get; set; }

    /// <summary>
    /// The main damage source of the card.
    /// </summary>
    public DefaultDamagePreviewSource? CardDamageSource { get; set; }

    /// <summary>
    /// Performs the additional calculations required to display a more accurate damage preview value.
    /// </summary>
    /// <remarks>Override this method if you need to update your custom fields before calculating (make sure to still call the base method).</remarks>
    /// <inheritdoc cref="DamageVar.UpdateCardPreview(CardModel, CardPreviewMode, Creature?, bool)"/>
    public void UpdateDamagePreview(Creature? target);
}
