using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace BetterDamagePreviews.Preview;

/// <summary>
/// The type of <see cref="DynamicVar"/>.
/// </summary>
public enum DynamicVarType
{
    /// <summary>
    /// A normal <see cref="DynamicVar"/> (ie. not calculated)
    /// </summary>
    Normal,

    /// <summary>
    /// A <see cref="CalculatedVar"/>.
    /// </summary>
    Calculated,

    /// <summary>
    /// Either normal or calculated.
    /// </summary>
    Either
}