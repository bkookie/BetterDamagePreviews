using BetterDamagePreviews.PreviewSources;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace BetterDamagePreviews.PreviewVars;

/// <summary>
/// A <see cref="CalculatedDamageVar"/> that will include additional calculations when previewing it's damage value.
/// </summary>
public class CalculatedDamagePreviewVar : CalculatedDamageVar, IDamagePreviewVar
{
    /// <inheritdoc cref="CalculatedDamagePreviewVar.CalculatedDamagePreviewVar(ValueProp, int)"/>
    public CalculatedDamagePreviewVar(ValueProp props) : this(props, 0) { }

    /// <summary>
    /// Creates a new <see cref="DamagePreviewVar"/>.
    /// </summary>
    /// <inheritdoc cref="DamagePreviewVar.DamagePreviewVar(string, decimal, ValueProp, int)"/>
    public CalculatedDamagePreviewVar(ValueProp props, int extraHitCount) : base(props)
    {
        ExtraHitCount = extraHitCount;
    }

    /// <inheritdoc/>
    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
    {
        base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);

        UpdateDamagePreview(card, target);
    }

    /// <inheritdoc cref="DamagePreviewVar.UpdateDamagePreview(CardModel, Creature?)"/>
    protected virtual void UpdateDamagePreview(CardModel card, Creature? target)
    {
        PreviewManager.UpdateDamagePreview(this, card, target);
    }


    /// <inheritdoc/>
    public AbstractModel? Owner => _owner;

    /// <inheritdoc/>
    decimal IDamagePreviewVar.PreviewValue { get => PreviewValue; set => PreviewValue = value; }

    /// <inheritdoc/>
    public Creature? PreviewOwner { get; set; }

    /// <inheritdoc/>
    public Creature? PreviewTarget { get; set; }

    /// <inheritdoc/>
    public int ExtraHitCount { get; set; }

    /// <inheritdoc/>
    public Accuracy Accuracy { get; set; }

    /// <inheritdoc/>
    public DefaultDamagePreviewSource? CardDamageSource { get; set; }

    /// <inheritdoc/>
    public bool ShouldDisplayValue { get; set; }
    AbstractModel? IDamagePreviewVar.Owner { get => Owner; set => throw new NotImplementedException(); }
}