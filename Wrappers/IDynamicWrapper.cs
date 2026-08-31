//namespace BetterDamagePreviews.Wrappers;

///// <summary>
///// A wrapper object for seamless inter-mod communication.
///// </summary>
//internal interface IDynamicWrapper
//{
//    /// <summary>
//    /// The ModId that this wrapper is for (regardless of direction).
//    /// </summary>
//    public string ModId { get; }

//    /// <summary>
//    /// The underlying object that this wrapper encapsulates.
//    /// </summary>
//    /// <remarks>
//    /// If the instance belongs to ModId, then the wrapper itself must belong to BetterDamagePreviews.
//    /// Otherwise, if it is the wrapper that belongs to ModId, the instance must belong to either BetterDamagePreviews or some other mod that is also using BetterDamagePreviews.
//    /// </remarks>
//    public object Instance { get; }
//}
