using BetterDamagePreviews.Preview;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.ValueProps;

namespace BetterDamagePreviews.PreviewSources;

/// <summary>
/// Calculates the lightning damage that all Lightning Orbs will do after the <see cref="TeslaCoil"/> card has been played.
/// </summary>
public sealed class TeslaCoilDamagePreviewSource : IDamagePreviewSource, IOrbDamageSource
{
    internal TeslaCoilDamagePreviewSource() { }

    private TeslaCoilDamagePreviewSource(IDamagePreview preview, bool isTopLevel)
    {
        Initialize(preview, isTopLevel);
    }

    /// <inheritdoc/>
    public IDamagePreviewSource GetNewInstance(IDamagePreview preview, bool isTopLevel)
    {
        return new TeslaCoilDamagePreviewSource(preview, isTopLevel);
    }

    /// <inheritdoc/>
    public void Initialize(IDamagePreview preview, bool isTopLevel)
    {
        SourceModel = preview.PreviewOwner?.Player?.PlayerCombatState?.OrbQueue.Orbs.FirstOrDefault(orb => orb is LightningOrb);
        IsPassiveDamage = true;
        HitsRemaining = HitCount;
    }

    /// <inheritdoc/>
    public AbstractModel? SourceModel { get; private set; }

    /// <inheritdoc/>
    public OrbModel? SourceOrb => SourceModel as OrbModel;

    /// <inheritdoc/>
    public decimal Damage => ((LightningOrb?)SourceOrb)?.PassiveVal ?? 0;

    /// <inheritdoc/>
    public int HitCount => SourceOrb?.Owner.PlayerCombatState?.OrbQueue.Orbs.Count(orb => orb is LightningOrb) ?? 0;

    /// <inheritdoc/>
    public int HitsRemaining { get; set; }

    /// <inheritdoc/>
    public ValueProp Props => ValueProp.Unpowered;

    /// <inheritdoc/>
    public bool ShouldTriggerFrom(IDamagePreviewSource previousDamageSource)
    {
        if (previousDamageSource.SourceModel is TeslaCoil)
        {
            IsPassiveDamage = true;
            return true;
        }
        return false;
    }

    /// <inheritdoc/>
    public bool IsPassiveDamage {  get; private set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return SourceModel?.ToString() ?? base.ToString() ?? "";
    }
}