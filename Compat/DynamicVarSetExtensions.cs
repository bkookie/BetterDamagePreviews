using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace BetterDamagePreviews.Compat;

internal static class DynamicVarSetExtensions
{
    extension(DynamicVarSet varSet)
    {
        public DynamicVar HitCount => varSet["HitCount"];
    }
}
