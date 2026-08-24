using BetterDamagePreviews.PreviewVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace BetterDamagePreviews.PreviewSources;

/// <summary>
/// The default damage source of a card, usually an attack.
/// </summary>
/// <remarks>For internal use only.</remarks>
public sealed class DefaultDamagePreviewSource : IDamagePreviewSource
{
    /// <inheritdoc cref="DefaultDamagePreviewSource.DefaultDamagePreviewSource(AbstractModel?, decimal, int, ValueProp)"/>
    internal DefaultDamagePreviewSource(AbstractModel? source, decimal damage) : this(source, damage, hitCount: 1) { }

    /// <inheritdoc cref="DefaultDamagePreviewSource.DefaultDamagePreviewSource(AbstractModel?, decimal, int, ValueProp)"/>
    internal DefaultDamagePreviewSource(AbstractModel? source, decimal damage, int hitCount) : this(source, damage, hitCount, ValueProp.Move) { }

    /// <summary>
    /// Creates a new <see cref="DefaultDamagePreviewSource"/>.
    /// </summary>
    /// <param name="source">The model source of this damage. This will almost always be a <see cref="CardModel"/>.</param>
    /// <param name="damage">The base damage.</param>
    /// <param name="hitCount">How many times this will hit.</param>
    /// <param name="props">The <see cref="ValueProp"/> associated with the damage.</param>
    internal DefaultDamagePreviewSource(AbstractModel? source, decimal damage, int hitCount, ValueProp props)
    {
        SourceModel = source;
        Damage = damage;
        HitCount = hitCount;
        HitsRemaining = hitCount;
        Props = props;
    }

    /// <inheritdoc/>
    public IDamagePreviewSource GetNewInstance(IDamagePreviewVar previewVar, bool isTopLevel) => throw new InvalidOperationException("This class should not be used as a listener. Only used for the initial attack from a card play."); // => new AttackDamagePreviewSource(previewVar, SourceModel, Damage, HitCount);

    /// <inheritdoc/>
    public void Initialize(IDamagePreviewVar previewVar, bool isTopLevel) => throw new InvalidOperationException("This class should not be used as a listener. Only used for the initial attack from a card play.");

    /// <inheritdoc/>
    public AbstractModel? SourceModel { get; private set; }

    /// <inheritdoc/>
    public decimal Damage { get; set; }

    /// <inheritdoc/>
    public int HitCount { get; set; }

    /// <inheritdoc/>
    public int HitsRemaining { get; set; }

    /// <inheritdoc/>
    public ValueProp Props { get; private set; }

    /// <inheritdoc/>
    public bool ShouldTriggerFrom(IDamagePreviewSource previousDamageSource) => false;

    /// <inheritdoc/>
    public override string ToString()
    {
        return SourceModel?.ToString() ?? base.ToString() ?? "";
    }
}