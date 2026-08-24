using BetterDamagePreviews.Hooks;
using BetterDamagePreviews.PreviewSources;
using BetterDamagePreviews.PreviewVars;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace BetterDamagePreviews.PreviewInitilizers;

/// <summary>
/// Modifies the hit count of various base game nultihit cards before calculating their damage.
/// </summary>
public sealed class BaseCardDamagePreviewInitializer : IDamagePreviewInitializer
{
    internal BaseCardDamagePreviewInitializer() { }

    /// <summary>
    /// Performs initialization for some base cards with calculated hit counts or random targeting.
    /// </summary>
    /// <inheritdoc/>
    public bool Initialize(IDamagePreviewVar previewVar)
    {
        if (previewVar.Owner is AbstractModel owner && previewVar.PreviewOwner != null && previewVar.CardDamageSource != null)
        {
            if (owner is Dismantle)
            {
                if (previewVar.PreviewTarget?.HasPower<VulnerablePower>() ?? false)
                {
                    RunHookAndSetHitCount(previewVar, 2);
                }
            }
            else if (owner is Eradicate eradicate)
            {
                RunHookAndSetHitCount(previewVar, eradicate.Owner.PlayerCombatState?.Energy ?? 0);
            }
            else if (owner is FiendFire fiendFire)
            {
                int hitCount = PileType.Hand.GetPile(fiendFire.Owner).Cards.Count - (fiendFire.Pile?.Type == PileType.Hand ? 1 : 0); // Dont count itself, it wont be in hand when actually played
                RunHookAndSetHitCount(previewVar, hitCount);
            }
            else if (owner is HeavenlyDrill heavenlyDrill)
            {
                int hitCount = heavenlyDrill.Owner.PlayerCombatState?.Energy ?? 0;
                if (hitCount >= 4)
                {
                    hitCount *= 2;
                }
                RunHookAndSetHitCount(previewVar, hitCount);
            }
            else if (owner is MadScience madScience)
            {
                if (madScience.Type != CardType.Attack || madScience.TinkerTimeRider != MegaCrit.Sts2.Core.Models.Events.TinkerTime.RiderEffect.Violence)
                {
                    // MadScience will have hitCount set to 3 by default as per its "ViolenceHits" var
                    RunHookAndSetHitCount(previewVar, 1);
                }
            }
            else if (owner is Skewer skewer)
            {
                RunHookAndSetHitCount(previewVar, skewer.Owner.PlayerCombatState?.Energy ?? 0);
            }
            else if (owner is Stardust stardust)
            {
                if (previewVar.PreviewTarget == null)
                {
                    return false; // Randomly targeted
                }
                RunHookAndSetHitCount(previewVar, stardust.Owner.PlayerCombatState?.Stars ?? 0);
            }
            else if (owner is TearAsunder tearAsunder)
            {
                // TearAsunder defines both a RepeatVar and a CalculatedVar (RepeatVar is unused). This confuses the patcher so need to set the hitcount explicitly
                RunHookAndSetHitCount(previewVar, tearAsunder.DynamicVars.Calculated(CalculatedVar.DefaultHitCountName).CalculateInt(previewVar.PreviewTarget));
            }
            else if (owner is Volley volley)
            {
                if (previewVar.PreviewTarget == null)
                {
                    return false; // Randomly targeted
                }
                RunHookAndSetHitCount(previewVar, volley.Owner.PlayerCombatState?.Energy ?? 0);
            }
            else if (owner is Whirlwind whirlwind)
            {
                RunHookAndSetHitCount(previewVar, whirlwind.Owner.PlayerCombatState?.Energy ?? 0);
            }
            else if (owner is CardModel card && card.TargetType == TargetType.RandomEnemy && previewVar.PreviewTarget == null) // null target means there are multiple enemies (or it is not being hovered)
            {
                return false;
            }
        }

        return true;
    }

    private static void RunHookAndSetHitCount(IDamagePreviewVar previewVar, int hitCount)
    {
        hitCount = DamagePreviewHook.ModifyHitCountForDisplay(previewVar.PreviewOwner!, previewVar.PreviewTarget, hitCount);
        previewVar.CardDamageSource!.HitCount = hitCount;
        previewVar.CardDamageSource!.HitsRemaining = hitCount;
    }
}