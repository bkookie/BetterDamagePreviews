using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace BetterDamagePreviews.PreviewVars;

internal static class DynamicVarExtensions
{
    extension(DynamicVarSet varSet)
    {
        public CalculatedVar Calculated(string name) => (CalculatedVar)varSet[name];
    }

    extension(CalculatedVar calculatedVar)
    {
        public static string DefaultHitCountName => "CalculatedHits";

        public int CalculateInt(Creature? target) => (int)calculatedVar.Calculate(target);
    }
}
