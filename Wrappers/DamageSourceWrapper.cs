//using BetterDamagePreviews.DamageSources;
//using BetterDamagePreviews.Preview;
//using MegaCrit.Sts2.Core.Models;
//using MegaCrit.Sts2.Core.ValueProps;

//namespace BetterDamagePreviews.Wrappers;

//internal class DamageSourceWrapper : IDamageSource, IDynamicWrapper
//{
//    public string ModId => _modId;
//    public object Instance => _instance;

//    private readonly dynamic _modId;
//    private readonly dynamic _instance;

//    public DamageSourceWrapper(string modId, object instance, Dictionary<string, Delegate> delegates)
//    {
//        _modId = modId;
//        _instance = instance;

//        _GetNewInstance = delegates[nameof(GetNewInstance)];
//        _Initialize = delegates[nameof(Initialize)];
//        _get_SourceModel = delegates[$"get_{nameof(SourceModel)}"];
//        _get_Damage = delegates[$"get_{nameof(Damage)}"];
//        _set_Damage = delegates[$"set_{nameof(Damage)}"];
//        _get_HitCount = delegates[$"get_{nameof(HitCount)}"];
//        _set_HitCount = delegates[$"set_{nameof(HitCount)}"];
//        _get_HitsRemaining = delegates[$"get_{nameof(HitsRemaining)}"];
//        _set_HitsRemaining = delegates[$"set_{nameof(HitsRemaining)}"];
//        _get_Props = delegates[$"get_{nameof(Props)}"];
//        _ShouldTriggerFrom = delegates[nameof(ShouldTriggerFrom)];
//    }

//    readonly dynamic _GetNewInstance;
//    public IDamageSource GetNewInstance(IDamagePreview preview, bool isTopLevel) => DynamicWrapper.CreateWrapper<IDamageSource>(_GetNewInstance(_instance, DynamicWrapper.CreateReverseWrapper<IDamagePreview>(_modId, preview), isTopLevel));

//    readonly dynamic _Initialize;
//    public void Initialize(IDamagePreview preview, bool isTopLevel) => _Initialize(_instance, DynamicWrapper.CreateReverseWrapper<IDamagePreview>(_modId, preview), isTopLevel);

//    readonly dynamic _get_SourceModel;
//    public AbstractModel? SourceModel => _get_SourceModel(_instance);

//    readonly dynamic _get_Damage;
//    readonly dynamic _set_Damage;
//    public decimal Damage { get => _get_Damage(_instance); set => _set_Damage(_instance, value); }

//    readonly dynamic _get_HitCount;
//    readonly dynamic _set_HitCount;
//    public int HitCount { get => _get_HitCount(_instance); set => _set_HitCount(_instance, value); }

//    readonly dynamic _get_HitsRemaining;
//    readonly dynamic _set_HitsRemaining;
//    public int HitsRemaining { get => _get_HitsRemaining(_instance); set => _set_HitsRemaining(_instance, value); }

//    readonly dynamic _get_Props;
//    public ValueProp Props => _get_Props(_instance);

//    readonly dynamic _ShouldTriggerFrom;
//    public bool ShouldTriggerFrom(IDamageSource previousDamageSource) => _ShouldTriggerFrom(_instance, DynamicWrapper.CreateReverseWrapper<IDamageSource>(_modId, previousDamageSource));
//}