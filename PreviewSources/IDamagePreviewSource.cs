using BetterDamagePreviews.Preview;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace BetterDamagePreviews.PreviewSources;

/// <summary>
/// A damage source used in calculating the preview damage of a card.
/// </summary>
public interface IDamagePreviewSource
{
    /// <summary>
    /// Gets a new instance of this <see cref="IDamagePreviewSource"/>. All fields should be initialized accordingly.
    /// </summary>
    /// <inheritdoc cref="Initialize(IDamagePreview, bool)"/>
    public IDamagePreviewSource GetNewInstance(IDamagePreview preview, bool isTopLevel);

    /// <summary>
    /// Initializes all relevant fields. Called once at the start of calculations with <c><paramref name="isTopLevel"/> = <see langword="true"/></c>, then again after every instance of calculated damage with a value of <see langword="false"/>.
    /// </summary>
    /// <param name="preview">The original <see cref="IDamagePreview"/> generated for the currently previewed card.</param>
    /// <param name="isTopLevel">Whether this is the first initialization. Only reset static counters when this is true.</param>
    public void Initialize(IDamagePreview preview, bool isTopLevel);

    /// <summary>
    /// The <see cref="AbstractModel"/> that is the source of this damage.
    /// </summary>
    public AbstractModel? SourceModel { get; }

    /// <summary>
    /// The damage of this <see cref="IDamagePreviewSource"/>.
    /// </summary>
    public decimal Damage { get; }

    /// <summary>
    /// How many times this will hit.
    /// </summary>
    public int HitCount { get; }

    /// <summary>
    /// How many hits remaining.
    /// </summary>
    /// <remarks>This should be reset in Initialize().</remarks>
    public int HitsRemaining { get; set; }

    /// <summary>
    /// The <see cref="ValueProp"/> associated with this damage source.
    /// </summary>
    public ValueProp Props { get; }

    /// <summary>
    /// Whether this damage source should be triggered as a result of <paramref name="previousDamageSource"/>.
    /// </summary>
    /// <remarks>For example, can trigger after any source of attack damage, a particular card, etc.</remarks>
    /// <param name="previousDamageSource">The <see cref="IDamagePreviewSource"/> that last dealt damage.</param>
    /// <returns><see langword="true"/> if this damage source should be triggered.</returns>
    public bool ShouldTriggerFrom(IDamagePreviewSource previousDamageSource);
}