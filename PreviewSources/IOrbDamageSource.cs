using MegaCrit.Sts2.Core.Models;

namespace BetterDamagePreviews.PreviewSources;

/// <summary>
/// A damage source from an orb.
/// </summary>
public interface IOrbDamageSource
{
    /// <summary>
    /// The <see cref="OrbModel"/> that is the source of this damage.
    /// </summary>
    OrbModel? SourceOrb { get; }

    /// <summary>
    /// Whether this is passive or evoke damage.
    /// </summary>
    public bool IsPassiveDamage { get; }
}
