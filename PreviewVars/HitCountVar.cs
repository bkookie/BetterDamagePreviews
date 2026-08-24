//using BetterDamagePreviews.Hooks;
//using MegaCrit.Sts2.Core.Entities.Cards;
//using MegaCrit.Sts2.Core.Entities.Creatures;
//using MegaCrit.Sts2.Core.Localization.DynamicVars;
//using MegaCrit.Sts2.Core.Models;

//namespace BetterDamagePreviews.PreviewVars;

///// <summary>
///// A <see cref="DynamicVar"/> suited for previewing an attack's hit count.
///// </summary>
//public class HitCountVar : DynamicVar
//{
//    /// <summary>
//    /// The default name to use when calling the parameterless constructor.
//    /// </summary>
//    public const string DefaultName = "HitCount";

//    /// <summary>
//    /// Creates a new <see cref="HitCountVar"/> with the specified <paramref name="hitCount"/>.
//    /// </summary>
//    /// <inheritdoc cref="HitCountVar.HitCountVar(string, decimal)"/>
//    public HitCountVar(decimal hitCount) : base(DefaultName, hitCount) { }

//    /// <summary>
//    /// Creates a new <see cref="HitCountVar"/> with the specified <paramref name="name"/> and <paramref name="hitCount"/>.
//    /// </summary>
//    /// <param name="name">The name of this <see cref="DynamicVar"/>.</param>
//    /// <param name="hitCount">How many times the attack should hit.</param>
//    public HitCountVar(string name, decimal hitCount) : base(name, hitCount) { }

//    /// <inheritdoc/>
//    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
//    {
//        PreviewValue = DamagePreviewHook.ModifyHitCountForDisplay(card.Owner.Creature, target, IntValue);
//    }
//}
