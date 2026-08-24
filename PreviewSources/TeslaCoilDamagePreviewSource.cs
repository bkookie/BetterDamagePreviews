using BetterDamagePreviews.PreviewVars;
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

    private TeslaCoilDamagePreviewSource(IDamagePreviewVar previewVar, bool isTopLevel)
    {
        Initialize(previewVar, isTopLevel);
    }

    /// <inheritdoc/>
    public IDamagePreviewSource GetNewInstance(IDamagePreviewVar previewVar, bool isTopLevel)
    {
        return new TeslaCoilDamagePreviewSource(previewVar, isTopLevel);
    }

    /// <inheritdoc/>
    public void Initialize(IDamagePreviewVar previewVar, bool isTopLevel)
    {
        SourceModel = previewVar.PreviewOwner?.Player?.PlayerCombatState?.OrbQueue.Orbs.FirstOrDefault(orb => orb is LightningOrb);
        HitsRemaining = HitCount;
        IsPassiveDamage = true;
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