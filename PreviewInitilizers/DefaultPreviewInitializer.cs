using BetterDamagePreviews.Preview;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace BetterDamagePreviews.PreviewInitilizers;

/// <summary>
/// Modifies the hit count of various base game nultihit cards before calculating their damage.
/// </summary>
public sealed class DefaultPreviewInitializer : IPreviewInitializer
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
}