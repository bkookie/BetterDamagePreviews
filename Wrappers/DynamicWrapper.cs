using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using BetterDamagePreviews.DamageSources;
using BetterDamagePreviews.Hooks;
using BetterDamagePreviews.Preview;
using BetterDamagePreviews.PreviewInitilizers;

namespace BetterDamagePreviews.Wrappers;

/// <summary>
/// Provides and manages wrappers used to communicate between this and any mods that are using the optional dependency approach (dont have access to the concrete types).
/// </summary>
/// <remarks>
/// The following types need to be cloned in your mod (ie. copy-paste the implementations):
/// <list type="bullet">
/// <item><see cref="Accuracy"/> (no wrapper required or registration required).</item>
/// <item><see cref="IPreviewInitializer"/> (and its wrapper <see cref="PreviewInitializerWrapper"/>).</item>
/// <item><see cref="IDamagePreview"/> (and it's wrapper <see cref="DamagePreviewWrapper"/>).</item>
/// <item><see cref="IDamageSource"/> (and it's wrapper <see cref="DamageSourceWrapper"/>).</item>
/// <item><see cref="IHitCountModifierForDisplay"/> (and it's wrapper <see cref="HitCountModifierForDisplayWrapper"/>).</item>
/// </list>
/// Your interfaces need to be registered against mine via <see cref="DynamicWrapper.RegisterType(string, Type, string, Func{object, Dictionary{string, Delegate}, object})"/>.
/// <br/>
/// Wrappers should remove all references to <see cref="DynamicWrapper.CreateWrapper(object)"/> and <see cref="DynamicWrapper.CreateReverseWrapper{T}(string, object?)"/> (that is handled entirely on this end).
/// </remarks>
public static class DynamicWrapper
{
    private readonly static Type[] SharedInterfaces = [typeof(IPreviewInitializer), typeof(IDamagePreview), typeof(IDamageSource), typeof(IHitCountModifierForDisplay)];
    private readonly static Dictionary<Type, (Type OurInterfaceType, string ModId, Dictionary<string, Delegate> Delegates)> InteropLookup = []; // Key == TypeToRegister
    private readonly static Dictionary<(Type OurInterfaceType, string ModId), (Func<object, Dictionary<string, Delegate>, object> ReverseFactory, Dictionary<string, Delegate> ReverseDelegates)> ReverseInteropLookup = [];

    /// <inheritdoc cref="RegisterType(string, Type, Type, Func{object, Dictionary{string, Delegate}, object})"/>
    /// <param name="modId">Your ModId.</param>
    /// <param name="interfaceType">The interface that you are registering. It must implement all methods of our interface that you are matching against, and all return types and paramter types must match exactly (except for any of our other interfaces types - they need to be implemented and registered along with this).</param>
    /// <param name="reverseFactory">A factory method to create a wrapper you can use to read our interfaces. You can copy <see cref="DamagePreviewWrapper"/> or <see cref="DamageSourceWrapper"/> (removing calls to CreateWrapper).</param>
    public static void RegisterType(string modId, Type interfaceType, Func<object, Dictionary<string, Delegate>, object> reverseFactory)
    {
        RegisterType(modId, interfaceType, interfaceType, null, reverseFactory);
    }

    /// <summary>
    /// Registers a type as matching one of our interfaces. Call this when using this mod as an optional dependency.
    /// </summary>
    /// <param name="modId">Your ModId.</param>
    /// <param name="typeToRegister">The type that you are registering. It must implement all methods of our interface that you are matching against, and all return types and paramter types must match exactly (except for any of our other interfaces types - they need to be implemented and registered along with this).</param>
    /// <param name="interfaceType">The interface that it implements (can be the same as <paramref name="typeToRegister"/>.</param>
    /// <param name="reverseFactory">A factory method to create a wrapper you can use to read our interfaces. You can copy <see cref="DamagePreviewWrapper"/> or <see cref="DamageSourceWrapper"/> (removing calls to CreateWrapper).</param>
    public static void RegisterType(string modId, Type typeToRegister, Type interfaceType, Func<object, Dictionary<string, Delegate>, object> reverseFactory)
    {
        RegisterType(modId, typeToRegister, interfaceType, null, reverseFactory);
    }

    /// <summary>
    /// Registers a type as matching one of our interfaces. Call this when using this mod as an optional dependency.
    /// </summary>
    /// <param name="modId">Your ModId.</param>
    /// <param name="interfaceType">The interface type that you are registering. It must implement all methods of our interface that you are matching against, and all return types and paramter types must match exactly (except for any of our other interfaces types - they need to be implemented and registered along with this).</param>
    /// <param name="interfaceName">The name of the interface you are matching it with. Use this if your name is different.</param>
    /// <param name="reverseFactory">A factory method to create a wrapper you can use to read our interfaces. You can copy <see cref="DamagePreviewWrapper"/> or <see cref="DamageSourceWrapper"/> (removing calls to CreateWrapper).</param>
    public static void RegisterType(string modId, Type interfaceType, string interfaceName, Func<object, Dictionary<string, Delegate>, object> reverseFactory)
    {
        RegisterType(modId, interfaceType, interfaceType, interfaceName, reverseFactory);
    }

    private static void RegisterType(string modId, Type typeToRegister, Type interfaceType, string? interfaceName, Func<object, Dictionary<string, Delegate>, object> reverseFactory)
    {
        if (InteropLookup.TryGetValue(interfaceType, out var value))
        {
            // Already generated for this interface, we can reuse the data for this type
            InteropLookup[typeToRegister] = value;
            // Reverse lookups only ever care about the interface, not concrete types, so it will already have been generated, nothing new to add here.
            return;
        }

        Type? referenceInterfaceType = null;
        interfaceName ??= interfaceType.Name;
        foreach (Type refType in SharedInterfaces)
        {
            if (interfaceName == refType.Name)
            {
                referenceInterfaceType = refType;
                break;
            }
        }
        if (referenceInterfaceType != null)
        {
            Dictionary<string, Delegate> delegates = [];
            Dictionary<string, Delegate> reverseDelegates = [];

            foreach (MethodInfo refMethod in referenceInterfaceType.GetMethods())
            {
                Type[] refParamTypes = [.. refMethod.GetParameters().Select(pInfo => pInfo.ParameterType)];
                MethodInfo? incomingMethod = typeToRegister.GetMethods().FirstOrDefault(method => method.Name == refMethod.Name && method.GetParameters().Length == refParamTypes.Length); // Dont look for exact types, just same method name & parameter count (so the incoming type can use their version of our interfaces. All other stypes still need to match exactly, or it may break during execution.)
                if (incomingMethod != null)
                {
                    Type[] incomingParamTypes = [.. incomingMethod.GetParameters().Select(pInfo => pInfo.ParameterType)];
                    Type delegateType = Expression.GetDelegateType([typeToRegister, .. incomingParamTypes, incomingMethod.ReturnType]); // Func<TParam1, ..., TReturn> -or- Func<TInstance, TParam1, ..., TReturn> if using an open (static) delegate (pass null for the target to Delegate.CreateDelegate(). Is an Action<> for void methods.
                    Delegate del = Delegate.CreateDelegate(delegateType, null, incomingMethod);
                    delegates.Add(incomingMethod.Name, del);

                    Type reverseDelegateType = Expression.GetDelegateType([referenceInterfaceType, .. refParamTypes, refMethod.ReturnType]);
                    del = Delegate.CreateDelegate(reverseDelegateType, null, refMethod);
                    reverseDelegates.Add(refMethod.Name, del);
                }
                else
                {
                    throw new ArgumentException($"The provided type '{typeToRegister}' does not implement the required interface members (missing '{referenceInterfaceType.Name}.{refMethod.Name}').");
                }
            }

            InteropLookup[typeToRegister] = (referenceInterfaceType, modId, delegates);
            ReverseInteropLookup[(referenceInterfaceType, modId)] = (reverseFactory, reverseDelegates);
        }
        else
        {
            throw new ArgumentException($"The name of the provided type '{interfaceName}' did not find a match.");
        }
    }

    // Wraps their object with our interface
    internal static T CreateWrapper<T>(object externalObject) where T : class
    {
        Type externalType = externalObject.GetType();
        object? wrapperInstance = externalObject is IDynamicWrapper wrapper ? wrapper.Instance : null;
        Type? wrapperInstanceType = wrapperInstance?.GetType();

        foreach (Type type in SharedInterfaces)
        {
            if (externalType.IsAssignableTo(type))
            {
                return (T)externalObject; // No wrapper required
            }
            else if (wrapperInstanceType?.IsAssignableTo(type) == true)
            {
                return (T)wrapperInstance!; // Strip the current wrapper
            }
        }

        if (InteropLookup.TryGetValue(externalType, out var tuple))
        {
            if (tuple.OurInterfaceType == typeof(IPreviewInitializer))
            {
                throw new NotImplementedException();
            }
            if (tuple.OurInterfaceType == typeof(IDamagePreview))
            {
                return (new DamagePreviewWrapper(tuple.ModId, externalObject, tuple.Delegates) as T)!;
            }
            else if (tuple.OurInterfaceType == typeof(IDamageSource))
            {
                return (new DamageSourceWrapper(tuple.ModId, externalObject, tuple.Delegates) as T)!;
            }
            else if (tuple.OurInterfaceType == typeof(IHitCountModifierForDisplay))
            {
                return (new HitCountModifierForDisplayWrapper(tuple.ModId, externalObject, tuple.Delegates) as T)!;
            }
        }

        throw new InvalidOperationException($"The provided type '{externalObject.GetType()}' has not been registered. You must call {nameof(DynamicWrapper)}.{nameof(RegisterType)}() first.");
    }

    // Wraps our object (or another mod's) with their interface
    internal static object? CreateReverseWrapper<T>(string modId, T? objectToWrap)
    {
        if (objectToWrap == null)
            return null;

        object obj = objectToWrap;

        if (obj is IDynamicWrapper wrapper)
        {
            obj = wrapper.Instance; // Strip the current wrapper
            if (wrapper.ModId == modId)
                return obj; // No wrapper required
        }

        if (ReverseInteropLookup.TryGetValue((typeof(T), modId), out var tuple))
        {
            object reversewrapper = tuple.ReverseFactory(obj, tuple.ReverseDelegates);
            return reversewrapper;
        }

        throw new InvalidOperationException($"The provided type '{obj.GetType()}' has not been registered with '{typeof(T).Name}'. You must call RegisterType() first.");
    }
}

// Incomplete implementation of creating dynamic wrappers at runtime

//public static class ReverseWrapperFactory
//{
//    private static AssemblyBuilder _assemblyBuilder;
//    private static ModuleBuilder _moduleBuilder;

//    static ReverseWrapperFactory()
//    {
//        _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("BetterDamagePreviewsDynamic"), AssemblyBuilderAccess.Run);
//        _moduleBuilder = _assemblyBuilder.DefineDynamicModule("Main");
//    }

//    public static object CreateWrapper(Type interfaceType, object internalLogic, Dictionary<string, Delegate> delegates)
//    {
//        // 1. Define the type implementing the REAL interface
//        TypeBuilder typeBuilder = _moduleBuilder.DefineType(interfaceType.Name + "Wrapper", TypeAttributes.Public | TypeAttributes.Class, typeof(object), [interfaceType]); // ← The actual Type from the Optional Mod

//        // 2. Add a field to hold your internal logic object
//        FieldBuilder logicField = typeBuilder.DefineField("_logic", typeof(object), FieldAttributes.Private);
//        FieldBuilder delegatesField = typeBuilder.DefineField("_delegates", typeof(Dictionary<string, Delegate>), FieldAttributes.Private);

//        // 3. Define a constructor that accepts the logic object
//        var ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, [typeof(object)]);
//        var il = ctor.GetILGenerator();
//        il.Emit(OpCodes.Ldarg_0);
//        il.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);
//        il.Emit(OpCodes.Ldarg_0);
//        il.Emit(OpCodes.Ldarg_1);
//        il.Emit(OpCodes.Stfld, logicField);
//        il.Emit(OpCodes.Ldarg_2);
//        il.Emit(OpCodes.Stfld, delegatesField);
//        il.Emit(OpCodes.Ret);

//        // 4. Implement each interface method by forwarding to a dispatcher
//        foreach (MethodInfo interfaceMethod in interfaceType.GetMethods())
//        {
//            Type[]? paramTypes = [.. interfaceMethod.GetParameters().Select(p => p.ParameterType)];
//            MethodBuilder methodBuilder = typeBuilder.DefineMethod(interfaceMethod.Name, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final, interfaceMethod.ReturnType, paramTypes);

//            // IL: load 'this', load '_logic', load all args, call dispatcher, return
//            var mIl = methodBuilder.GetILGenerator();
//            mIl.Emit(OpCodes.Ldarg_0);
//            mIl.Emit(OpCodes.Ldfld, logicField); // Push internal logic object
//            mIl.Emit(OpCodes.Ldstr, interfaceMethod.Name);
//            mIl.Emit(OpCodes.Ldfld, delegatesField);

//            for (int i = 1; i <= paramTypes.Length; i++)
//                mIl.Emit(OpCodes.Ldarg, i); // Push each argument

//            // Call: Dispatcher.Invoke(logicObject, methodName, args...)
//            MethodInfo dispatcherMethod = typeof(ReverseWrapperFactory).GetMethod("Dispatch", BindingFlags.Static | BindingFlags.NonPublic)!;
//#warning wrong order of stack?
//            mIl.Emit(OpCodes.Call, dispatcherMethod);
//            mIl.Emit(OpCodes.Ret);

//            // Mark as implementing the interface method
//            typeBuilder.DefineMethodOverride(methodBuilder, interfaceMethod);
//        }

//        // 5. Create and instantiate
//        Type createdType = typeBuilder.CreateType();
//        return Activator.CreateInstance(createdType, internalLogic);
//    }

//    // Central dispatcher: forwards to your cached delegates
//    private static object Dispatch(object logic, string methodName, Dictionary<string, Delegate> delegates, params object[] args)
//    {
//        // Use your existing wrapper/delegate cache here
//        // logic is your Base Mod's internal object
//        // methodName + args come from the Optional Mod's call
//        //return .Invoke(logic, methodName, args);
//        dynamic d = delegates[methodName];
//        d(params args);
//    }
//}
