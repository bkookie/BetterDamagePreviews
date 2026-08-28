using BetterDamagePreviews.Hooks;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace BetterDamagePreviews.Wrappers;

internal class HitCountModifierForDisplayWrapper : IHitCountModifierForDisplay, IDynamicWrapper
{
    public string ModId => _modId;
    public object Instance => _instance;

    private readonly dynamic _modId;
    private readonly dynamic _instance;

    public HitCountModifierForDisplayWrapper(string modId, object instance, Dictionary<string, Delegate> delegates)
    {
        _modId = modId;
        _instance = instance;

        _ModifyHitCountForDisplay = delegates[nameof(ModifyHitCountForDisplay)];
    }

    readonly dynamic _ModifyHitCountForDisplay;
    public int ModifyHitCountForDisplay(Creature? attacker, Creature? target, int hitCount) => _ModifyHitCountForDisplay(_instance, attacker, target, hitCount);
}
