//using BetterDamagePreviews.Preview;
//using BetterDamagePreviews.Wrappers;

//namespace BetterDamagePreviews.PreviewInitilizers;

//internal class PreviewInitializerWrapper : IPreviewInitializer, IDynamicWrapper
//{
//    public string ModId => _modId;
//    public object Instance => _instance;

//    private readonly dynamic _modId;
//    private readonly dynamic _instance;

//    public PreviewInitializerWrapper(string modId, object instance, Dictionary<string, Delegate> delegates)
//    {
//        _modId = modId;
//        _instance = instance;

//        _Initialize = delegates[nameof(Initialize)];
//    }

//    readonly dynamic _Initialize;
//    public bool Initialize(IDamagePreview preview) => _Initialize(DynamicWrapper.CreateReverseWrapper<IDamagePreview>(_modId, preview));
//}
