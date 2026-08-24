using BetterDamagePreviews.Hooks;
using BetterDamagePreviews.PreviewSources;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.ValueProps;

namespace BetterDamagePreviews.PreviewVars;

/// <summary>
/// Utility methods for calculating damage preview values.
/// </summary>
public static class PreviewManager
{
    private static readonly Dictionary<Type, Func<DamageVar, CardModel, Creature, DamagePreviewVar>> _cardPreviewVarFactory = [];
    internal static IReadOnlyDictionary<Type, Func<DamageVar, CardModel, Creature, DamagePreviewVar>> CardPreviewVarFactory => _cardPreviewVarFactory;

    ///// <summary>
    ///// For cards with hardcoded hitcounts.
    ///// </summary>
    //public static readonly Dictionary<Type, int> CardHitCountLookup = [];

    /// <summary>
    /// For cards that dont use "Repeat" for their hitcount var name, you can specify the actual name to use here.
    /// </summary>
    public static readonly Dictionary<Type, string> CardHitCountVarNameLookup = [];

    internal static readonly Dictionary<DynamicVar, IDamagePreviewVar> AutoPreviewDamageLookup = [];
    internal static readonly Dictionary<DynamicVar, IDamagePreviewVar> AutoPreviewHitCountLookup = [];

    static PreviewManager()
    {
        AddPreviewVarFactory<AstralPulse>(1);
        AddPreviewVarFactory<DaggerSpray>(1);
        AddPreviewVarFactory<Refract>(1);
        AddPreviewVarFactory<Maul>(1);
        AddPreviewVarFactory<Thrash>(1);
        AddPreviewVarFactory<TwinStrike>(1);
        AddPreviewVarFactory<Uproar>(1);

        //CardHitCountLookup.Add(typeof(AstralPulse), 2);
        //CardHitCountLookup.Add(typeof(DaggerSpray), 2);
        //CardHitCountLookup.Add(typeof(Refract), 2); // Uses a RepearVar for channelling orbs instead.
        //CardHitCountLookup.Add(typeof(Maul), 2);
        //CardHitCountLookup.Add(typeof(Thrash), 2);
        //CardHitCountLookup.Add(typeof(TwinStrike), 2);
        //CardHitCountLookup.Add(typeof(Uproar), 2);

        CardHitCountVarNameLookup.Add(typeof(MadScience), MadScience.violenceHitsKey);
    }

    /// <summary>
    /// Provide a factory method to create a <see cref="DamagePreviewVar"/> for cards of type <typeparamref name="T"/>, with the specified <paramref name="extraHitCount"/>.
    /// </summary>
    /// <typeparam name="T">The type of the card that this is for.</typeparam>
    /// <param name="extraHitCount">How many extra times the damage hits for (not including the first hit).</param>
    public static void AddPreviewVarFactory<T>(int extraHitCount) where T : CardModel
    {
       _cardPreviewVarFactory.Add(typeof(T), (damageVar, card, target) => new DamagePreviewVar(damageVar.Name, damageVar.BaseValue, damageVar.Props, extraHitCount));
    }

    /// <summary>
    /// Provide a factory method to create a custom <see cref="DamagePreviewVar"/> for cards of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the card that this is for.</typeparam>
    /// <remarks>The <see cref="CardModel"/> passed into the Func() will always be of type <typeparamref name="T"/>.</remarks>
    public static void AddPreviewVarFactory<T>(Func<DamageVar, CardModel, Creature, DamagePreviewVar> factory) where T : CardModel
    {
        _cardPreviewVarFactory.Add(typeof(T), factory);
    }

    /// <summary>
    /// Prepares the suppliedvar for final calculations. Should be called from <see cref="DynamicVar.UpdateCardPreview(CardModel, MegaCrit.Sts2.Core.Entities.Cards.CardPreviewMode, Creature?, bool)"/>.
    /// </summary>
    /// <inheritdoc cref="DynamicVar.UpdateCardPreview(CardModel, MegaCrit.Sts2.Core.Entities.Cards.CardPreviewMode, Creature?, bool)"/>
    public static void UpdateDamagePreview(IDamagePreviewVar damagePreviewVar, CardModel card, Creature? target)
    {
        int hitCount = 1;

        //foreach (DynamicVar var in card.DynamicVars.Values)
        //{
        //    if (var is HitCountVar hitCountVar)
        //    {
        //        if (card is MadScience madScience && madScience.TinkerTimeRider != MegaCrit.Sts2.Core.Models.Events.TinkerTime.RiderEffect.Violence)
        //            break;

        //        hitCount = hitCountVar.IntValue;
        //        break;
        //    }
        //    else if (var is CalculatedHitCountVar calculatedHitCountVar)
        //    {
        //        hitCount = calculatedHitCountVar.CalculateInt(target);
        //        break;
        //    }
        //}

        hitCount += damagePreviewVar.ExtraHitCount;
        hitCount = DamagePreviewHook.ModifyHitCountForDisplay(card.Owner.Creature, target, hitCount);

        damagePreviewVar.PreviewOwner = card.Owner.Creature;
        damagePreviewVar.PreviewTarget = target;
        damagePreviewVar.Accuracy = Accuracy.Accurate;
        damagePreviewVar.ShouldDisplayValue = true;

        damagePreviewVar.CardDamageSource = new DefaultDamagePreviewSource(card, damagePreviewVar.PreviewValue, hitCount);
    }

    /// <summary>
    /// Performs the final damage calculation.
    /// <br/>Takes into account base game interactions such as <see cref="IntangiblePower"/> or <see cref="SlipperyPower"/>, and any custom damage sources supplied by other mods.
    /// </summary>
    /// <param name="previewVar">The var to calculate.</param>
    /// <returns>The total calculated damage, after all modifiers have been applied.</returns>
    public static int CalculateTotalDamage(IDamagePreviewVar previewVar)
    {
        if (previewVar.Owner is CardModel card && !card.IsInCombat || previewVar.CardDamageSource == null)
        {
            previewVar.ShouldDisplayValue = false;
            return -1;
        }

        foreach (IDamagePreviewInitializer initializer in BeforeAttackInitialization)
        {
            if (!initializer.Initialize(previewVar))
            {
                previewVar.ShouldDisplayValue = false;
                return -1;
            }
        }

        int totalDamage = 0;

        if (previewVar.PreviewOwner != null && previewVar.PreviewTarget != null)
        {
            int hardToKillCap = previewVar.PreviewTarget.GetPowerAmount<HardToKillPower>(); // ModifyDamageCap
            HardenedShellPower? hardenedShell = previewVar.PreviewTarget.GetPower<HardenedShellPower>();
            int hardenedShellAmount = previewVar.PreviewTarget.GetPower<HardenedShellPower>()?.DisplayAmount ?? 0; // ModifyHpLostBeforeOstyLate (ticks down in AfterDamageReceived)
            int intangibleCap = previewVar.PreviewTarget.HasPower<IntangiblePower>() ? 1 : 0; // ModifyHpLostAfterOsty (uses ModifyDamageCap for display purposes)
            int slipperyAmount = previewVar.PreviewTarget.GetPowerAmount<SlipperyPower>(); // ModifyHpLostAfterOsty (ticks down in AfterDamageReceived)
            bool haveTheBoot = previewVar.PreviewOwner.Player?.Relics.Any(relic => relic is TheBoot) ?? false;
            int bootDamage = haveTheBoot ? 5 : 0; // ModifyHpLostAfterOstyLate
            int flutterAmount = previewVar.PreviewTarget.GetPowerAmount<FlutterPower>(); // AfterDamageReceived

            foreach (IDamagePreviewSource damageSource in DamageSources(previewVar))
            {
                int damage = (int)damageSource.Damage;

                if (hardToKillCap > 0)
                {
                    damage = Math.Min(damage, hardToKillCap);
                }

                if (hardenedShell != null)
                {
                    damage = Math.Min(damage, hardenedShellAmount);
                    hardenedShellAmount -= damage;
                }

                if (intangibleCap > 0)
                {
                    damage = Math.Min(damage, intangibleCap);
                }

                if (slipperyAmount > 0 && damage > 0)
                {
                    damage = 1;
                    slipperyAmount--;
                }

                if (bootDamage > 0 && damage > 0 && damageSource.Props.IsPoweredAttack())
                {
                    damage = Math.Max(bootDamage, damage);
                }

                if (flutterAmount > 0 && damage > 0 && damageSource.Props.IsPoweredAttack())
                {
                    flutterAmount--;
                    if (flutterAmount == 0)
                    {
                        previewVar.CardDamageSource.Damage = (int)(previewVar.CardDamageSource.Damage * 2); // Remove the 50% damage reduction of Flutter
                    }
                }

                totalDamage += damage;
            }
        }
        else
        {
            totalDamage = (int)previewVar.CardDamageSource.Damage * previewVar.CardDamageSource.HitCount;
        }

        return totalDamage;
    }

    /// <summary>
    /// The set of <see cref="IDamagePreviewInitializer"/> that will be initialized once at the start of the attack.
    /// </summary>
    /// <remarks>Use this to modify the damage or hit count of the existing attack.</remarks>
    public static readonly HashSet<IDamagePreviewInitializer> BeforeAttackInitialization = [];

    /// <summary>
    /// The set of <see cref="IDamagePreviewSource"/> that will be probed after every instance of damage during the calculation.
    /// </summary>
    public static readonly HashSet<IDamagePreviewSource> AfterHitListeners = [];

    /// <summary>
    /// The set of <see cref="IDamagePreviewSource"/> that will be probed once, after all hits of the attack have been calculated.
    /// </summary>
    public static readonly HashSet<IDamagePreviewSource> AfterAttackListeners = [];

    private static IEnumerable<IDamagePreviewSource> DamageSources(IDamagePreviewVar previewVar)
    {
        DefaultDamagePreviewSource? attackDamageSource = previewVar.CardDamageSource;

        if (attackDamageSource != null)
        {
            foreach (IDamagePreviewSource source in AfterHitListeners.Concat(AfterAttackListeners))
            {
                source.Initialize(previewVar, isTopLevel: true);
            }

            while (attackDamageSource.HitsRemaining > 0)
            {
                bool isFirstHit = attackDamageSource.HitsRemaining == attackDamageSource.HitCount;

                attackDamageSource.HitsRemaining--;
                yield return attackDamageSource;

                foreach (IDamagePreviewSource previewSource in DamageSources(previewVar, attackDamageSource, AfterHitListeners, isTopLevel: isFirstHit))
                {
                    yield return previewSource;
                }
            }

            foreach (IDamagePreviewSource previewSource in DamageSources(previewVar, attackDamageSource, AfterAttackListeners, isTopLevel: true))
            {
                yield return previewSource;
            }
        }
    }

    private static IEnumerable<IDamagePreviewSource> DamageSources(IDamagePreviewVar previewVar, IDamagePreviewSource previousDamageSource, IEnumerable<IDamagePreviewSource> listeners, bool isTopLevel)
    {
        foreach (IDamagePreviewSource listener in listeners)
        {
            IDamagePreviewSource source = isTopLevel ? listener : listener.GetNewInstance(previewVar, isTopLevel);
            if (source.ShouldTriggerFrom(previousDamageSource))
            {
                while (source.HitsRemaining > 0)
                {
                    source.HitsRemaining--;
                    yield return source;

                    foreach (IDamagePreviewSource nestedListener in DamageSources(previewVar, source, listeners, isTopLevel: false))
                    {
                        yield return nestedListener;
                    }
                }
            }
        }
    }
}