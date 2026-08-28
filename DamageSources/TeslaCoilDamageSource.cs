using BetterDamagePreviews.Preview;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.ValueProps;

namespace BetterDamagePreviews.DamageSources;

/// <summary>
/// Calculates the lightning damage that all Lightning Orbs will do after the <see cref="TeslaCoil"/> card has been played.
/// </summary>
public sealed class TeslaCoilDamageSource : IDamageSource, IOrbDamageSource
{
    internal TeslaCoilDamageSource() { }

    private TeslaCoilDamageSource(IDamagePreview preview, bool isTopLevel)
    {
        Initialize(preview, isTopLevel);
    }

    /// <inheritdoc/>
    public IDamageSource GetNewInstance(IDamagePreview preview, bool isTopLevel)
    {
        return new TeslaCoilDamageSource(preview, isTopLevel);
    }

    /// <inheritdoc/>
    public void Initialize(IDamagePreview preview, bool isTopLevel)
    {
        SourceModel = preview.Card.Owner.PlayerCombatState?.OrbQueue.Orbs.FirstOrDefault(orb => orb is LightningOrb);
        IsPassiveDamage = true;
        HitsRemaining = HitCount;
    }

    /// <inheritdoc/>
    public AbstractModel? SourceModel { get; private set; }

    /// <inheritdoc/>
    public OrbModel? SourceOrb => SourceModel as OrbModel;

    /// <inheritdoc/>
    public decimal Damage
    {
        get => ((LightningOrb?)SourceOrb)?.PassiveVal ?? 0;
        set => throw new NotSupportedException();
    }

    /// <inheritdoc/>
    public int HitCount
    {
        get => SourceOrb?.Owner.PlayerCombatState?.OrbQueue.Orbs.Count(orb => orb is LightningOrb) ?? 0;
        set => throw new NotSupportedException();
    }

    /// <inheritdoc/>
    public int HitsRemaining { get; set; }

    /// <inheritdoc/>
    public ValueProp Props => ValueProp.Unpowered;

    /// <inheritdoc/>
    public bool ShouldTriggerFrom(IDamageSource previousDamageSource)
    {
        if (previousDamageSource.SourceModel is TeslaCoil)
        {
            IsPassiveDamage = true;
            return true;
        }
        return false;
    }

    /// <inheritdoc/>
    public bool IsPassiveDamage { get; private set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return SourceModel?.ToString() ?? base.ToString() ?? "";
    }
}