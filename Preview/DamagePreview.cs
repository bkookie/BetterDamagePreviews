using BetterDamagePreviews.PreviewSources;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace BetterDamagePreviews.Preview;

/// <inheritdoc cref="IDamagePreview"/>
public class DamagePreview : IDamagePreview
{
    /// <inheritdoc cref="DamagePreview(DamageVar, Func{Creature?, int}, CardModel)"/>
    public DamagePreview(DamageVar damageVar, CardModel owner) : this(damageVar, default(DynamicVar), owner) { }

    /// <inheritdoc cref="DamagePreview(DamageVar, Func{Creature?, int}, CardModel)"/>
    /// <param name="damageVar">The <see cref="DamageVar"/> that this is linked to.</param>
    /// <param name="hitCount">How many times this damage hits for.</param>
    /// <param name="owner">The <see cref="CardModel"/> that this is linked to.</param>
    public DamagePreview(DamageVar damageVar, int hitCount, CardModel owner) : this(damageVar, target => hitCount, owner) { }

    /// <inheritdoc cref="DamagePreview(DamageVar, Func{Creature?, int}, CardModel)"/>
    /// <param name="damageVar">The <see cref="DamageVar"/> that this is linked to.</param>
    /// <param name="hitCountVar">The <see cref="DynamicVar"/> providing a hit count. If set to <see langword="null"/>, a hit count of 1 is assumed.</param>
    /// <param name="owner">The <see cref="CardModel"/> that this is linked to.</param>
    public DamagePreview(DamageVar damageVar, DynamicVar? hitCountVar, CardModel owner) : this(damageVar, PreviewManager.HitCountFromDynamicVarFunc(hitCountVar), owner) { }

    /// <summary>
    /// Creates a new <see cref="DamagePreview"/>.
    /// </summary>
    /// <param name="damageVar">The <see cref="DamageVar"/> that this is linked to.</param>
    /// <param name="hitCountFunc">A function accepting a target, and returning a hit count.</param>
    /// <param name="owner">The <see cref="CardModel"/> that this is linked to.</param>
    public DamagePreview(DamageVar damageVar, Func<Creature?, int> hitCountFunc, CardModel owner)
    {
        LinkedDamageVar = damageVar;
        GetHitCount = hitCountFunc;
        Owner = owner;
    }

    /// <inheritdoc/>
    public virtual void UpdateDamagePreview(CardModel card, Creature? target)
    {
        PreviewManager.UpdateDamagePreview(this, card, target);
    }

    /// <inheritdoc/>
    public DamageVar LinkedDamageVar { get; }

    /// <inheritdoc/>
    public Func<Creature?, int> GetHitCount { get; }

    /// <inheritdoc/>
    public CardModel Owner { get; }

    /// <inheritdoc/>
    public int PreviewValue { get; set; }

    /// <inheritdoc/>
    public Creature? PreviewOwner { get; set; }

    /// <inheritdoc/>
    public Creature? PreviewTarget { get; set; }

    /// <inheritdoc/>
    public Accuracy Accuracy { get; set; }

    /// <inheritdoc/>
    public DefaultDamagePreviewSource? CardDamageSource { get; set; }

    /// <inheritdoc/>
    public bool ShouldDisplayValue { get; set; }
}