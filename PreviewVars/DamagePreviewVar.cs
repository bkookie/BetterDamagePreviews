using BetterDamagePreviews.PreviewSources;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace BetterDamagePreviews.PreviewVars;

/// <summary>
/// A <see cref="DamageVar"/> that will include additional calculations when previewing it's damage value.
/// </summary>
public class DamagePreviewVar : DamageVar, IDamagePreviewVar
{
    /// <inheritdoc cref="DamagePreviewVar.DamagePreviewVar(string, decimal, ValueProp, int)"/>
    public DamagePreviewVar(decimal damage, ValueProp props) : this(defaultName, damage, props) { }

    /// <inheritdoc cref="DamagePreviewVar.DamagePreviewVar(string, decimal, ValueProp, int)"/>
    public DamagePreviewVar(string name, decimal damage, ValueProp props) : this(name, damage, props, extraHitCount: 0) { }

    /// <inheritdoc cref="DamagePreviewVar.DamagePreviewVar(string, decimal, ValueProp, int)"/>
    public DamagePreviewVar(decimal damage, ValueProp props, int extraHitCount) : this(defaultName, damage, props, extraHitCount) { }

    /// <summary>
    /// Creates a new <see cref="DamagePreviewVar"/>.
    /// </summary>
    /// <param name="name">The name of this <see cref="DynamicVar"/>.</param>
    /// <param name="damage">The base damage.</param>
    /// <param name="props">The <see cref="ValueProp"/> associated with the damage.</param>
    /// <param name="extraHitCount">How many additional times this will hit, that does not appear on any <see cref="DynamicVar"/>.<para/> NOTE: This value is intended for patching existing cards only.
    /// </param>
    public DamagePreviewVar(string name, decimal damage, ValueProp props, int extraHitCount) : base(name, damage, props)
    {
        ExtraHitCount = extraHitCount;
    }

    /// <inheritdoc/>
    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
    {
        base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);

        UpdateDamagePreview(card, target);
    }

    /// <summary>
    /// Performs the additional calculations required to display a more accurate damage preview value.
    /// </summary>
    /// <remarks>Override this method if you need to update your custom fields (making sure to sill call the base method).</remarks>
    /// <inheritdoc cref="DamagePreviewVar.UpdateCardPreview(CardModel, CardPreviewMode, Creature?, bool)"/>
    protected virtual void UpdateDamagePreview(CardModel card, Creature? target)
    {
        PreviewManager.UpdateDamagePreview(this, card, target);
    }

    /// <inheritdoc/>
    public AbstractModel? Owner => _owner;

    /// <inheritdoc/>
    decimal IDamagePreviewVar.PreviewValue {get => PreviewValue; set => PreviewValue = value; }

    /// <inheritdoc/>
    public Creature? PreviewOwner { get; set; }

    /// <inheritdoc/>
    public Creature? PreviewTarget { get; set; }

    /// <inheritdoc/>
    public virtual int ExtraHitCount { get; set; }

    /// <inheritdoc/>
    public Accuracy Accuracy { get; set; }

    /// <inheritdoc/>
    public DefaultDamagePreviewSource? CardDamageSource { get ; set ; }

    /// <inheritdoc/>
    public bool ShouldDisplayValue { get; set; }
    AbstractModel? IDamagePreviewVar.Owner { get => Owner; set => throw new NotImplementedException(); }
}