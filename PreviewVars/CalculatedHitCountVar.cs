//using BetterDamagePreviews.Hooks;
//using MegaCrit.Sts2.Core.Entities.Cards;
//using MegaCrit.Sts2.Core.Entities.Creatures;
//using MegaCrit.Sts2.Core.Localization.DynamicVars;
//using MegaCrit.Sts2.Core.Models;

//namespace BetterDamagePreviews.PreviewVars;

///// <summary>
///// A <see cref="CalculatedVar"/> suited for calculating and previewing an attack's hit count.
///// </summary>
//public class CalculatedHitCountVar : CalculatedVar
//{
//    /// <summary>
//    /// The default name to use when calling the parameterless constructor.
//    /// </summary>
//    public const string DefaultName = "CalculatedHitCount";

//    /// <summary>
//    /// Creates a new <see cref="CalculatedHitCountVar"/>.
//    /// </summary>
//    public CalculatedHitCountVar() : base(DefaultName) { }

//    /// <summary>
//    /// Creates a new <see cref="CalculatedHitCountVar"/> with the specified <paramref name="name"/>.
//    /// </summary>
//    /// <param name="name">The name of this <see cref="DynamicVar"/>.</param>
//    public CalculatedHitCountVar(string name) : base(name) { }

//    /// <inheritdoc/>
//    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
//    {
//        PreviewValue = DamagePreviewHook.ModifyHitCountForDisplay(card.Owner.Creature, target, CalculateInt(target));
//    }

//    /// <inheritdoc/>
//    protected override decimal GetBaseValueForIConvertible()
//    {
//        return PreviewValue;
//    }

//    /// <inheritdoc/>
//    public int CalculateInt(Creature? target)
//    {
//        return (int)Calculate(target);
//    }

//    /// <inheritdoc/>
//    public override string ToString()
//    {
//        return PreviewValue.ToString();
//    }
//}
