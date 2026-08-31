using BetterDamagePreviews.DamageSources;
using BetterDamagePreviews.Hooks;
using BetterDamagePreviews.PreviewInitilizers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace BetterDamagePreviews.Preview;

internal record HitCountVarName(string HitCountName, string CalculatedHitCountName, DynamicVarType VarType);

/// <summary>
/// Utility methods for calculating damage preview values.
/// </summary>
public static class PreviewManager
{
    internal static readonly Dictionary<Type, HitCountVarName> CardHitCountVarNameLookup = []; // For cards that dont use "Repeat" for their hitcount var name, you can specify the actual name to use here.
    internal static readonly Dictionary<Type, Func<DamageVar, Func<Creature?, int>, CardModel, IDamagePreview>> CardPreviewFactory = [];
    internal static readonly Dictionary<DynamicVar, IDamagePreview> PreviewCache = [];
    internal static readonly Dictionary<DynamicVar, IDamagePreview> HitCountVarLookup = []; // Allows the hit count preview value to update on the card as well as the damage. Has no effect if the card does not preview the hit count.

    internal static readonly HashSet<IPreviewInitializer> BeforeAttackInitializers = [];
    internal static readonly HashSet<IDamageSource> AfterHitListeners = [];
    internal static readonly HashSet<IDamageSource> AfterAttackListeners = [];

    /// <summary>
    /// A function that accepts a <see cref="DynamicVar"/> and returns another function, which accepts a target <see cref="Creature"/> and returns a hit count.
    /// </summary>
    /// <remarks>In other words, if the <see cref="DynamicVar"/> is a <see cref="CalculatedVar"/>, this returns the calculated hit count, otherwise it's BaseValue. If <see langword="null"/>, returns <c>1</c>.</remarks>
    public static Func<DynamicVar?, Func<Creature?, int>> HitCountFromDynamicVarFunc => hitCountVar => target => hitCountVar is CalculatedVar calculatedVar ? calculatedVar.CalculateInt(target) : hitCountVar is DynamicVar dynamicVar ? dynamicVar.IntValue : 1;



    static PreviewManager()
    {
        AddHitCountVarNameLookup<MadScience>(MadScience.violenceHitsKey, DynamicVarType.Normal);
        AddHitCountVarNameLookup<TearAsunder>(CalculatedVar.DefaultHitCountName, DynamicVarType.Calculated); // TearAsunder defines both a RepeatVar and a CalculatedVar (only the CalculatedVar is used).

        AddPreviewFactory<AstralPulse>(2);
        AddPreviewFactory<DaggerSpray>(2);
        AddPreviewFactory<Refract>(2);
        AddPreviewFactory<Maul>(2);
        AddPreviewFactory<Thrash>(2);
        AddPreviewFactory<TwinStrike>(2);
        AddPreviewFactory<Uproar>(2);

        AddPreviewFactory<Dismantle>((damageVar, _, card) => new DamagePreview(damageVar, target => target?.HasPower<VulnerablePower>() == true ? 2 : 1, card));
        AddPreviewFactory<FiendFire>((damageVar, _, card) => new DamagePreview(damageVar, _ => PileType.Hand.GetPile(card.Owner).Cards.Count - (card.Pile?.Type == PileType.Hand ? 1 : 0), card)); // Dont count itself, it wont be in hand when actually played
        AddPreviewFactory<MadScience>((damageVar, hitCountFunc, card) => new DamagePreview(damageVar, target => card is MadScience madScience && madScience.Type == CardType.Attack && madScience.TinkerTimeRider == MegaCrit.Sts2.Core.Models.Events.TinkerTime.RiderEffect.Violence ? hitCountFunc(target) : 1, card));

        AddPreviewFactoryForXHitCountCard<Eradicate>(true);
        AddPreviewFactoryForXHitCountCard<HeavenlyDrill>(true, (card, xValue) => xValue >= 4 ? xValue * 2 : xValue);
        AddPreviewFactoryForXHitCountCard<Skewer>(true);
        AddPreviewFactoryForXHitCountCard<Stardust>(false);
        AddPreviewFactoryForXHitCountCard<Volley>(true);
        AddPreviewFactoryForXHitCountCard<Whirlwind>(true);

        BeforeAttackInitializers.Add(new DefaultPreviewInitializer());
        AfterAttackListeners.Add(new TeslaCoilDamageSource());

        RunManager.Instance.RoomEntered += ClearLookups;
        RunManager.Instance.RoomExited += ClearLookups;

        static void ClearLookups()
        {
            PreviewCache.Clear();
            HitCountVarLookup.Clear();
        }
    }

    /// <summary>
    /// Prepares the suppliedvar for final calculations. Should be called from <see cref="DynamicVar.UpdateCardPreview(CardModel, CardPreviewMode, Creature?, bool)"/>.
    /// </summary>
    /// <inheritdoc cref="DynamicVar.UpdateCardPreview(CardModel, CardPreviewMode, Creature?, bool)"/>
    public static void UpdateDamagePreview(IDamagePreview preview, Creature? target)
    {
        preview.PreviewTarget = target;
        preview.Accuracy = Accuracy.Accurate;
        preview.ShouldDisplayValue = true;

        int hitCount = DamagePreviewHook.ModifyHitCountForDisplay(preview.Card.Owner.Creature, target, preview.GetHitCount(target));
        preview.CardDamageSource = new DefaultDamageSource(preview.Card, preview.LinkedDamageVar.PreviewValue, hitCount);

        preview.PreviewValue = CalculateTotalDamage(preview);
    }

    /// <summary>
    /// Performs the final damage calculation.
    /// <br/>Takes into account base game interactions such as <see cref="SlipperyPower"/> or <see cref="FlutterPower"/>, and any custom damage sources supplied by other mods.
    /// </summary>
    /// <param name="preview">The var to calculate.</param>
    /// <returns>The total calculated damage, after all modifiers have been applied.</returns>
    public static int CalculateTotalDamage(IDamagePreview preview)
    {
        if (!preview.Card.IsInCombat || preview.CardDamageSource == null)
        {
            preview.ShouldDisplayValue = false;
            return -1;
        }

        foreach (IPreviewInitializer initializer in BeforeAttackInitializers)
        {
            if (!initializer.Initialize(preview))
            {
                preview.ShouldDisplayValue = false;
                return -1;
            }
        }

        int totalDamage = 0;

        if (preview.PreviewTarget != null)
        {
            int hardToKillCap = preview.PreviewTarget.GetPowerAmount<HardToKillPower>(); // ModifyDamageCap
            HardenedShellPower? hardenedShell = preview.PreviewTarget.GetPower<HardenedShellPower>();
            int hardenedShellAmount = preview.PreviewTarget.GetPower<HardenedShellPower>()?.DisplayAmount ?? 0; // ModifyHpLostBeforeOstyLate (ticks down in AfterDamageReceived)
            int intangibleCap = preview.PreviewTarget.HasPower<IntangiblePower>() ? 1 : 0; // ModifyHpLostAfterOsty (uses ModifyDamageCap for display purposes)
            int slipperyAmount = preview.PreviewTarget.GetPowerAmount<SlipperyPower>(); // ModifyHpLostAfterOsty (ticks down in AfterDamageReceived)
            bool haveTheBoot = preview.Card.Owner.Relics.Any(relic => relic is TheBoot);
            int bootDamage = haveTheBoot ? 5 : 0; // ModifyHpLostAfterOstyLate
            int flutterAmount = preview.PreviewTarget.GetPowerAmount<FlutterPower>(); // AfterDamageReceived

            foreach (IDamageSource damageSource in DamageSources(preview))
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
                        preview.CardDamageSource.Damage = (int)(preview.CardDamageSource.Damage * 2); // Remove the 50% damage reduction of Flutter
                    }
                }

                totalDamage += damage;
            }
        }
        else
        {
            totalDamage = (int)preview.CardDamageSource.Damage * preview.CardDamageSource.HitCount; // When card is in sitting hand (not hovering a target), or there are multiple targets
        }

        return totalDamage;
    }

    private static IEnumerable<IDamageSource> DamageSources(IDamagePreview preview)
    {
        IDamageSource? attackDamageSource = preview.CardDamageSource;

        if (attackDamageSource != null)
        {
            foreach (IDamageSource source in AfterHitListeners.Concat(AfterAttackListeners))
            {
                source.Initialize(preview, isTopLevel: true);
            }

            while (attackDamageSource.HitsRemaining > 0)
            {
                bool isFirstHit = attackDamageSource.HitsRemaining == attackDamageSource.HitCount;

                attackDamageSource.HitsRemaining--;
                yield return attackDamageSource;

                foreach (IDamageSource previewSource in DamageSources(preview, attackDamageSource, AfterHitListeners, isTopLevel: isFirstHit))
                {
                    yield return previewSource;
                }
            }

            foreach (IDamageSource previewSource in DamageSources(preview, attackDamageSource, AfterAttackListeners, isTopLevel: true))
            {
                yield return previewSource;
            }
        }
    }

    private static IEnumerable<IDamageSource> DamageSources(IDamagePreview preview, IDamageSource previousDamageSource, IEnumerable<IDamageSource> listeners, bool isTopLevel)
    {
        foreach (IDamageSource listener in listeners)
        {
            IDamageSource source = isTopLevel ? listener : listener.GetNewInstance(preview, isTopLevel);
            if (source.ShouldTriggerFrom(previousDamageSource))
            {
                while (source.HitsRemaining > 0)
                {
                    source.HitsRemaining--;
                    yield return source;

                    foreach (IDamageSource nestedListener in DamageSources(preview, source, listeners, isTopLevel: false))
                    {
                        yield return nestedListener;
                    }
                }
            }
        }
    }














    /// <summary>
    /// Register the given <see cref="CardModel"/> to use the supplied <paramref name="hitCountVarName"/> when searching for it's hitcount var.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="CardModel"/> to register.</typeparam>
    /// <param name="hitCountVarName">The name to use when looking up the card's DynamicVars.</param>
    /// <param name="varType">Whether the supplied <paramref name="hitCountVarName"/> is for a <see cref="DynamicVar"/>, <see cref="CalculatedVar"/> or either.</param>
    public static void AddHitCountVarNameLookup<T>(string hitCountVarName, DynamicVarType varType) where T : CardModel
    {
        switch (varType)
        {
            case DynamicVarType.Normal:
                AddHitCountVarNameLookup(typeof(T), hitCountVarName, "", varType);
                break;
            case DynamicVarType.Calculated:
                AddHitCountVarNameLookup(typeof(T), "", hitCountVarName, varType);
                break;
            case DynamicVarType.Either:
                AddHitCountVarNameLookup(typeof(T), hitCountVarName, hitCountVarName, varType);
                break;
        }
    }

    /// <summary>
    /// Register all cards in a <see cref="CardPoolModel"/> to use the supplied lookup names.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="CardModel"/> to register.</typeparam>
    /// <param name="hitCountVarName">The name to use when looking for a <see cref="DynamicVar"/>. Supplying <see langword="null"/> will use the default name "Repeat".</param>
    /// <param name="calculatedHitCountVarName">The name to use when looking for a <see cref="CalculatedVar"/>. Supplying <see langword="null"/> will use the default name "CalculatedHits".</param>
    public static void AddHitCountVarNameLookup<T>(string? hitCountVarName, string? calculatedHitCountVarName) where T : CardPoolModel
    {
        hitCountVarName ??= RepeatVar.defaultName;
        calculatedHitCountVarName ??= CalculatedVar.DefaultHitCountName;

        foreach (CardModel card in ModelDb.CardPool<T>().AllCards)
        {
            AddHitCountVarNameLookup(card.GetType(), hitCountVarName, calculatedHitCountVarName, DynamicVarType.Either);
        }
    }

    private static void AddHitCountVarNameLookup(Type cardType, string hitCountVarName, string calculatedHitCountVarName, DynamicVarType varType)
    {
        CardHitCountVarNameLookup[cardType] = new HitCountVarName(hitCountVarName, calculatedHitCountVarName, varType);
    }

    /// <summary>
    /// Creates a factory method used to create a default <see cref="DamagePreview"/> for cards of type <typeparamref name="T"/>.
    /// </summary>
    /// <remarks> This has no actual effect - use the other overloads to define a custom factory method.</remarks>
    /// <typeparam name="T">A specific <see cref="CardModel"/>, or an <see langword="interface"/> that one or more cards implement. Any other type will have no effect.</typeparam>
    public static void AddPreviewFactory<T>()
    {
        AddPreviewFactory<T>((damageVar, hitCountFunc, card) => new DamagePreview(damageVar, target => hitCountFunc(target), card));
    }

    /// <summary>
    /// Creates a factory method used to create a <see cref="DamagePreview"/> for cards of type <typeparamref name="T"/>, with the specified <paramref name="hitCount"/>.
    /// </summary>
    /// <remarks>Used for cards with hardcoded hit counts.</remarks>
    /// <typeparam name="T">A specific <see cref="CardModel"/>, or an <see langword="interface"/> that one or more cards implement. Any other type will have no effect.</typeparam>
    /// <param name="hitCount">How many times the damage hits for.</param>
    public static void AddPreviewFactory<T>(int hitCount)
    {
        AddPreviewFactory<T>((damageVar, _, card) => new DamagePreview(damageVar, _ => hitCount, card));
    }

    /// <summary>
    /// Provide a factory method used to create a custom <see cref="DamagePreview"/> for cards of type <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// Used for cards with complex hit count calculations, or for creating classes derived from <see cref="IDamagePreview"/>.
    /// <para/>The <see cref="CardModel"/> provided to the <paramref name="factory"/> will always be of type <typeparamref name="T"/>.
    /// </remarks>
    /// <typeparam name="T">A specific <see cref="CardModel"/>, or an <see langword="interface"/> that one or more cards implement. Any other type will have no effect.</typeparam>
    public static void AddPreviewFactory<T>(Func<DamageVar, Func<Creature?, int>, CardModel, IDamagePreview> factory)
    {
        CardPreviewFactory.Add(typeof(T), factory);
    }

    /// <inheritdoc cref="AddPreviewFactoryForXHitCountCard{T}(bool, Func{CardModel, int, int}?)"/>
    public static void AddPreviewFactoryForXHitCountCard<T>(bool usesEnergy)
    {
        AddPreviewFactoryForXHitCountCard<T>(usesEnergy, null);
    }

    /// <summary>
    /// Creates a factory method used to create a <see cref="DamagePreview"/> for cards that hit X times (the X cost wont resolve until played, so can't use the card's built in calculation).
    /// </summary>
    /// <typeparam name="T">A specific <see cref="CardModel"/>, or an <see langword="interface"/> that one or more cards implement. Any other type will have no effect.</typeparam>
    /// <param name="usesEnergy">Whether the X cost is for energy or stars.</param>
    /// <param name="modifyXValueAfterHook">A function for modifying the resolved X value of the card, after executing <see cref="Hook.ModifyXValue(MegaCrit.Sts2.Core.Combat.ICombatState, CardModel, int)"/>.</param>
    public static void AddPreviewFactoryForXHitCountCard<T>(bool usesEnergy, Func<CardModel, int, int>? modifyXValueAfterHook)
    {
        AddPreviewFactory<T>((damageVar, _, card) => new DamagePreview(damageVar, _ =>
        {
            int xValue = usesEnergy ? card.Owner.PlayerCombatState?.Energy ?? 0 : card.Owner.PlayerCombatState?.Stars ?? 0;
            xValue = (card.CombatState != null) ? Hook.ModifyXValue(card.CombatState, card, xValue) : xValue;
            xValue = (modifyXValueAfterHook != null) ? modifyXValueAfterHook(card, xValue) : xValue;
            return xValue;
        }, card));
    }

    // Before Attack

    /// <summary>
    /// Add an <see cref="IPreviewInitializer"/> that will be run once at the start of a calculation. Use this to modify the damage or hit count of an attack.
    /// </summary>
    /// <param name="initializer">The <see cref="IPreviewInitializer"/> to add.</param>
    public static void AddBeforeAttackInitializer(IPreviewInitializer initializer)
    {
        BeforeAttackInitializers.Add(initializer);
    }

    // After Hit

    /// <summary>
    /// Add an <see cref="IDamageSource"/> that will be probed after every instance of damage during the calculation.
    /// </summary>
    /// <param name="listener">The <see cref="IDamageSource"/> to add.</param>
    public static void AddAfterHitListener(IDamageSource listener)
    {
        AfterHitListeners.Add(listener);
    }

    // After Attack

    /// <summary>
    /// Add an <see cref="IDamageSource"/> that will be probed once, after all hits of an attack have resolved.
    /// </summary>
    /// <param name="listener">The <see cref="IDamageSource"/> to add.</param>
    public static void AddAfterAttackListener(IDamageSource listener)
    {
        AfterAttackListeners.Add(listener);
    }
}