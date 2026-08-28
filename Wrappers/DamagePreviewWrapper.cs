using BetterDamagePreviews.DamageSources;
using BetterDamagePreviews.Preview;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace BetterDamagePreviews.Wrappers;

internal class DamagePreviewWrapper : IDamagePreview, IDynamicWrapper
{
    public string ModId => _modId;
    public object Instance => _instance;

    private readonly dynamic _modId;
    private readonly dynamic _instance;

    public DamagePreviewWrapper(string modId, object instance, Dictionary<string, Delegate> delegates)
    {
        _modId = modId;
        _instance = instance;

        _get_LinkedDamageVar = delegates[$"get_{nameof(LinkedDamageVar)}"];
        _get_GetHitCount = delegates[$"get_{nameof(GetHitCount)}"];
        _get_Card = delegates[$"get_{nameof(Card)}"];
        _get_PreviewTarget = delegates[$"get_{nameof(PreviewTarget)}"];
        _set_PreviewTarget = delegates[$"set_{nameof(PreviewTarget)}"];
        _get_Accuracy = delegates[$"get_{nameof(Accuracy)}"];
        _set_Accuracy = delegates[$"set_{nameof(Accuracy)}"];
        _get_ShouldDisplayValue = delegates[$"get_{nameof(ShouldDisplayValue)}"];
        _set_ShouldDisplayValue = delegates[$"set_{nameof(ShouldDisplayValue)}"];
        _get_PreviewValue = delegates[$"get_{nameof(PreviewValue)}"];
        _set_PreviewValue = delegates[$"set_{nameof(PreviewValue)}"];
        _get_CardDamageSource = delegates[$"get_{nameof(CardDamageSource)}"];
        _set_CardDamageSource = delegates[$"set_{nameof(CardDamageSource)}"];
        _UpdateDamagePreview = delegates[nameof(UpdateDamagePreview)];
    }

    readonly dynamic _get_LinkedDamageVar;
    public DamageVar LinkedDamageVar => _get_LinkedDamageVar(_instance);

    readonly dynamic _get_GetHitCount;
    public Func<Creature?, int> GetHitCount => _get_GetHitCount(_instance);

    readonly dynamic _get_Card;
    public CardModel Card => _get_Card(_instance);

    readonly dynamic _get_PreviewTarget;
    readonly dynamic _set_PreviewTarget;
    public Creature? PreviewTarget { get => _get_PreviewTarget(_instance); set => _set_PreviewTarget(_instance, value); }

    readonly dynamic _get_Accuracy;
    readonly dynamic _set_Accuracy;
    public Accuracy Accuracy { get => _get_Accuracy(_instance); set => _set_Accuracy(_instance, value); }

    readonly dynamic _get_ShouldDisplayValue;
    readonly dynamic _set_ShouldDisplayValue;
    public bool ShouldDisplayValue { get => _get_ShouldDisplayValue(_instance); set => _set_ShouldDisplayValue(_instance, value); }

    readonly dynamic _get_PreviewValue;
    readonly dynamic _set_PreviewValue;
    public int PreviewValue { get => _get_PreviewValue(_instance); set => _set_PreviewValue(_instance, value); }

    readonly dynamic _get_CardDamageSource;
    readonly dynamic _set_CardDamageSource;
    public IDamageSource? CardDamageSource { get => DynamicWrapper.CreateWrapper<IDamageSource>(_get_CardDamageSource(_instance)); set => _set_CardDamageSource(_instance, DynamicWrapper.CreateReverseWrapper<IDamageSource>(_modId, value)); }

    readonly dynamic _UpdateDamagePreview;
    public void UpdateDamagePreview(Creature? target) => _UpdateDamagePreview(_instance, target);
}