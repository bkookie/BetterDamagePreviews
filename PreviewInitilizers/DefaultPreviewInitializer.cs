using BetterDamagePreviews.Hooks;
using BetterDamagePreviews.Preview;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace BetterDamagePreviews.PreviewInitilizers;

/// <summary>
/// Modifies the hit count of various base game nultihit cards before calculating their damage.
/// </summary>
public sealed class DefaultPreviewInitializer : IDamagePreviewInitializer
{
    internal DefaultPreviewInitializer() { }

    /// <summary>
    /// Hides the preview value for randomly targeted attacks when there is more than one enemy.
    /// </summary>
    /// <inheritdoc/>
    public bool Initialize(IDamagePreview preview)
    {
        if (preview.Card.TargetType == TargetType.RandomEnemy && preview.PreviewTarget == null) // null target means there are multiple enemies (or it is not being hovered)
        {
            return false;
        }

        return true;
    }

    private static void RunHookAndSetHitCount(IDamagePreview preview, int hitCount)
    {
        hitCount = DamagePreviewHook.ModifyHitCountForDisplay(preview.Card.Owner.Creature, preview.PreviewTarget, hitCount);
        preview.CardDamageSource!.HitCount = hitCount;
        preview.CardDamageSource!.HitsRemaining = hitCount;
    }
}