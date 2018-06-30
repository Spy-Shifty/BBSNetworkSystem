using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Unity.Entities;
using UnityEngine;

public delegate Entity NetworkInstantiationHandlerDelegate(EntityManager entityManager);

internal delegate void RefAction<S, T>(ref S instance, T value);
internal delegate T RefFunc<S, T>(ref S instance);

internal abstract class NetworkMemberInfo { }
internal abstract class NetworkMemberInfo<OBJ> : NetworkMemberInfo {
    public abstract void SetValue(ref OBJ obj, int oldValue, int newValue, float deltaTimeFrame, float deltaTimeMessage);
    public abstract int GetValue(OBJ obj);
}

internal sealed class NetworkMemberInfo<OBJ, TYPE> : NetworkMemberInfo<OBJ> {
    private readonly NetSyncBaseAttribute syncBaseAttribute;
    //private readonly MemberInfo memberInfo;
    private readonly NetworkMemberInfo parent;

    public readonly RefFunc<OBJ, TYPE> GetValueDelegate;
    public readonly RefAction<OBJ, TYPE> SetValueDelegate;

    private NetworkMath networkMath;

    public NetworkMemberInfo(MemberInfo memberInfo, NetSyncBaseAttribute syncBaseAttribute) {
        this.syncBaseAttribute = syncBaseAttribute;
        //Debug.Log(typeof(OBJ) + " --- " + typeof(TYPE));
        switch (memberInfo.MemberType) {
            case MemberTypes.Field:
                FieldInfo fieldInfo = (FieldInfo)memberInfo;
                GetValueDelegate = NetworkMemberInfoUtility.CreateGetter<OBJ, TYPE>(fieldInfo);
                SetValueDelegate = NetworkMemberInfoUtility.CreateSetter<OBJ, TYPE>(fieldInfo);
                break;

            case MemberTypes.Property:
                PropertyInfo propertyInfo = (PropertyInfo)memberInfo;
                GetValueDelegate = (RefFunc<OBJ, TYPE>)Delegate.CreateDelegate(typeof(Func<OBJ, TYPE>), propertyInfo.GetGetMethod());
                SetValueDelegate = (RefAction<OBJ, TYPE>)Delegate.CreateDelegate(typeof(Action<OBJ, TYPE>), propertyInfo.GetSetMethod());
                break;

            default:
                throw new NotImplementedException(memberInfo.MemberType.ToString());
        }

        Type typeType = typeof(TYPE);
        if (typeType == typeof(int)) {
            networkMath = new NetworkMathInteger();
        } else if (typeType == typeof(float)) {
            networkMath = new NetworkMathFloat(syncBaseAttribute.Accuracy, syncBaseAttribute.LerpSpeed, syncBaseAttribute.JumpThreshold);
        } else if (typeType == typeof(boolean)) {
            networkMath = new NetworkMathBoolean();
        }       
    }

    public NetworkMemberInfo(MemberInfo memberInfo, NetworkMemberInfo parent, NetSyncBaseAttribute syncBaseAttribute) {
        this.syncBaseAttribute = syncBaseAttribute;
        this.parent = parent;
    }

    public override void SetValue(ref OBJ obj, int oldValue, int newValue, float deltaTimeFrame, float deltaTimeMessage) {
        SetValueDelegate(ref obj, (TYPE)networkMath.IntegerToNative(GetValueDelegate(ref obj), oldValue, newValue, deltaTimeFrame, deltaTimeMessage));
    }
    
    public override int GetValue(OBJ obj) {
        return networkMath.NativeToInteger(GetValueDelegate(ref obj));
    }
}

internal sealed class NetworkMemberInfo<Parent_OBJ, OBJ, TYPE> : NetworkMemberInfo<Parent_OBJ> {
    private readonly NetSyncBaseAttribute syncBaseAttribute;
    //private readonly MemberInfo memberInfo;
    private readonly NetworkMemberInfo<Parent_OBJ, OBJ> parent;


    public readonly RefFunc<OBJ, TYPE> GetValueDelegate;
    public readonly RefAction<OBJ, TYPE> SetValueDelegate;
    private NetworkMath networkMath;

    public NetworkMemberInfo(MemberInfo memberInfo, NetworkMemberInfo parent, NetSyncBaseAttribute syncBaseAttribute) {
        this.syncBaseAttribute = syncBaseAttribute;
        this.parent = (NetworkMemberInfo<Parent_OBJ, OBJ>)parent;
        //Debug.Log(typeof(Parent_OBJ) + " --- " + typeof(OBJ) + " --- " + typeof(TYPE));

        switch (memberInfo.MemberType) {
            case MemberTypes.Field:
                FieldInfo fieldInfo = (FieldInfo)memberInfo;
                GetValueDelegate = NetworkMemberInfoUtility.CreateGetter<OBJ, TYPE>(fieldInfo);
                SetValueDelegate = NetworkMemberInfoUtility.CreateSetter<OBJ, TYPE>(fieldInfo);
                break;

            case MemberTypes.Property:
                PropertyInfo propertyInfo = (PropertyInfo)memberInfo;
                GetValueDelegate = (RefFunc<OBJ, TYPE>)Delegate.CreateDelegate(typeof(RefFunc<OBJ, TYPE>), propertyInfo.GetGetMethod());
                SetValueDelegate = (RefAction<OBJ, TYPE>)Delegate.CreateDelegate(typeof(RefAction<OBJ, TYPE>), propertyInfo.GetSetMethod());
                break;

            default:
                throw new NotImplementedException(memberInfo.MemberType.ToString());
        }

        Type typeType = typeof(TYPE);
        if (typeType == typeof(int)) {
            networkMath = new NetworkMathInteger();
        } else if (typeType == typeof(float)) {
            networkMath = new NetworkMathFloat(syncBaseAttribute.Accuracy, syncBaseAttribute.LerpSpeed, syncBaseAttribute.JumpThreshold);
        } else if (typeType == typeof(boolean)) {
            networkMath = new NetworkMathBoolean();
        }
    }


    public override void SetValue(ref Parent_OBJ parentObj, int oldValue, int newValue, float deltaTimeFrame, float deltaTimeMessage) {
        OBJ obj = parent.GetValueDelegate(ref parentObj);
        SetValueDelegate(ref obj, (TYPE)networkMath.IntegerToNative(GetValueDelegate(ref obj), oldValue, newValue, deltaTimeFrame, deltaTimeMessage));
        parent.SetValueDelegate(ref parentObj, obj);
    }

    public override int GetValue(Parent_OBJ parentObj) {
        OBJ obj = parent.GetValueDelegate(ref parentObj);
        return networkMath.NativeToInteger(GetValueDelegate(ref obj));
    }
}




internal sealed class NetworkMethodInfo<T> {
    public NetworkMethodInfo(MethodInfo methodInfo) {
        actionDelegate = (Action<T>)Delegate.CreateDelegate(typeof(Action<T>), methodInfo);
    }
    private readonly Action<T> actionDelegate;

    public void Invoke(T obj) {
        actionDelegate(obj);
    }
}


internal sealed class NetworkMethodInfo<T, Param> {
    public NetworkMethodInfo(MethodInfo methodInfo) {
        actionDelegate = (Action<T, Param>)Delegate.CreateDelegate(typeof(Action<T, Param>), methodInfo);
    }
    private readonly Action<T, Param> actionDelegate;

    public void Invoke(T obj, Param arg) {
        actionDelegate(obj, arg);
    }
}

internal sealed class NetworkMethodInfo<T, Param1 ,Param2> {
    public NetworkMethodInfo(MethodInfo methodInfo) {
        actionDelegate = (Action<T, Param1, Param2>)Delegate.CreateDelegate(typeof(Action<T, Param1, Param2>), methodInfo);
    }
    private readonly Action<T, Param1, Param2> actionDelegate;

    public void Invoke(T obj, Param1 arg1, Param2 arg2) {
        actionDelegate(obj, arg1, arg2);
    }
}

internal sealed class NetworkInOutMethodInfo<T, RefParam, OutParam> {
    internal delegate bool NetworkInOutDelegate(T obj, ref RefParam refArg, out OutParam outArg);

    public NetworkInOutMethodInfo(MethodInfo methodInfo) {
        functionDelegate = (NetworkInOutDelegate)Delegate.CreateDelegate(typeof(NetworkInOutDelegate), methodInfo);
    }
    private readonly NetworkInOutDelegate functionDelegate;

    public bool Invoke(T obj, ref RefParam refArg, out OutParam outArg) {
        return functionDelegate(obj,ref refArg, out outArg);
    }

}


internal static class NetworkMemberInfoUtility {

    public static RefFunc<S, T> CreateGetter<S, T>(FieldInfo field) {
        ParameterExpression instance = Expression.Parameter(typeof(S).MakeByRefType(), "instance");
        MemberExpression memberAccess = Expression.MakeMemberAccess(instance, field);
        Expression<RefFunc<S, T>> expr =
            Expression.Lambda<RefFunc<S, T>>(memberAccess, instance);
        return expr.Compile();
    }

    public static RefAction<S, T> CreateSetter<S, T>(FieldInfo field) {        
        ParameterExpression instance = Expression.Parameter(typeof(S).MakeByRefType(), "instance");
        ParameterExpression value = Expression.Parameter(typeof(T), "value");
        Expression<RefAction<S, T>> expr =
            Expression.Lambda<RefAction<S, T>>(
                Expression.Assign(
                    Expression.Field(instance, field),
                    Expression.Convert(value, field.FieldType)),
                instance,
                value);

        return expr.Compile();
    }
}

internal class ReflectionUtility {

    private Dictionary<ComponentType, NetworkMemberInfo[]> cashedNetworkMemberInfo = new Dictionary<ComponentType, NetworkMemberInfo[]>();

    private readonly Dictionary<ComponentType, int> componentTypeToIdMap = new Dictionary<ComponentType, int>();
    private readonly Dictionary<int, ComponentType> idToComponentTypeMap = new Dictionary<int, ComponentType>();
    private readonly Dictionary<ComponentType, int> componentTypeMemberCount = new Dictionary<ComponentType, int>();

    public readonly ComponentType[] ComponentTypes;
    //public static readonly MethodInfo[] NetworkFactoryMethods;

    private Dictionary<int, NetworkInstantiationHandlerDelegate> entityFactoryMethodMap = new Dictionary<int, NetworkInstantiationHandlerDelegate>();

    public ReflectionUtility() {

        byte componentTypeId = 0;
        var componentTypes = new List<ComponentType>();
        //var networkFactoryMethods = new List<MethodInfo>();
        List<Assembly> assemblies = new List<Assembly>(AppDomain.CurrentDomain.GetAssemblies());
        assemblies.Sort((x, y) => x.FullName.CompareTo(y.FullName));
        foreach (Assembly assembly in assemblies) {
            List<Type> types = new List<Type>(assembly.GetTypes());
            types.Sort((x, y) => x.Name.CompareTo(y.Name));
            foreach (Type type in types) {
                if (type.GetCustomAttribute<NetSyncAttribute>() != null) {
                    componentTypeId++;
                    componentTypeToIdMap.Add(type, componentTypeId);
                    idToComponentTypeMap.Add(componentTypeId, type);
                    componentTypes.Add(type);

                    int numberOfMembers = 0;
                    List<NetworkMemberInfo> networkMemberInfos = new List<NetworkMemberInfo>();
                    MemberInfo[] memberInfos = type.GetMembers().OrderBy((x) => x.Name).Where(memberInfo => memberInfo.IsDefined(typeof(NetSyncMemberAttribute), false)).ToArray();
                    for (int i = 0; i < memberInfos.Length; i++) {
                        MemberInfo memberInfo = memberInfos[i];

                        Type networkMemberInfoType = typeof(NetworkMemberInfo<,>);
                        Type mainMemberInfoTypeType = memberInfo.MemberType == MemberTypes.Field ? (memberInfo as FieldInfo).FieldType : (memberInfo as PropertyInfo).PropertyType;
                        Type mainMemberInfoGenericType = networkMemberInfoType.MakeGenericType(type, mainMemberInfoTypeType);
                        NetSyncMemberAttribute netSyncMemberAttribute = memberInfo.GetCustomAttribute<NetSyncMemberAttribute>(false);
                        NetworkMemberInfo mainMemberInfo = (NetworkMemberInfo)Activator.CreateInstance(mainMemberInfoGenericType, memberInfo, netSyncMemberAttribute);
                        NetSyncSubMemberAttribute[] netSyncSubMemberAttributes = memberInfo.GetCustomAttributes<NetSyncSubMemberAttribute>(false).ToArray();

                        numberOfMembers += netSyncSubMemberAttributes.Length;
                        foreach (NetSyncSubMemberAttribute NetSyncSubMemberAttribute in netSyncSubMemberAttributes) {
                            if (!NetSyncSubMemberAttribute.OverriddenValues) {
                                NetSyncSubMemberAttribute.SetValuesFrom(netSyncMemberAttribute);
                            }

                            Type subType = memberInfo.MemberType == MemberTypes.Field ? (memberInfo as FieldInfo).FieldType : (memberInfo as PropertyInfo).PropertyType;

                            bool found = false;
                            IEnumerable<MemberInfo> subMemberInfos = subType.GetMembers().OrderBy(x=>x.Name);
                            foreach (MemberInfo subMemberInfo in subMemberInfos) {
                                if (subMemberInfo.Name.Equals(NetSyncSubMemberAttribute.MemberName)) {
                                    Type networkSubMemberInfoType = typeof(NetworkMemberInfo<,,>);
                                    Type mainSubMemberInfoTypeType = subMemberInfo.MemberType == MemberTypes.Field ? (subMemberInfo as FieldInfo).FieldType : (subMemberInfo as PropertyInfo).PropertyType;
                                    Type subMemberInfoGenericType = networkSubMemberInfoType.MakeGenericType(type, subType, mainSubMemberInfoTypeType);
                                    networkMemberInfos.Add((NetworkMemberInfo)Activator.CreateInstance(subMemberInfoGenericType, subMemberInfo, mainMemberInfo, NetSyncSubMemberAttribute));
                                    found = true;
                                    break;
                                }
                            }

                            if (!found) {
                                throw new MissingMemberException(NetSyncSubMemberAttribute.MemberName);
                            }
                        }

                        if (netSyncSubMemberAttributes.Length == 0) {
                            numberOfMembers++;
                            networkMemberInfos.Add(mainMemberInfo);
                        }
                    }
                    cashedNetworkMemberInfo.Add(type, networkMemberInfos.ToArray());
                    componentTypeMemberCount.Add(type, numberOfMembers);
                }

                if (type.GetCustomAttribute<NetworkEntityFactoryAttribute>() != null) {
                    //networkFactoryMethods.AddRange(type.GetMethods().Where(methodInfo => methodInfo.IsDefined(typeof(NetworkInstantiatorAttribute), false)));
                    MethodInfo[] methodInfos = type.GetMethods().Where(methodInfo => methodInfo.IsDefined(typeof(NetworkEntityFactoryMethodAttribute), false)).ToArray();
                    foreach (MethodInfo methodInfo in methodInfos) {
                        NetworkInstantiationHandlerDelegate networkInstantiationHandlerDelegate = null;
                        try {
                            networkInstantiationHandlerDelegate = (NetworkInstantiationHandlerDelegate)Delegate.CreateDelegate(typeof(NetworkInstantiationHandlerDelegate), methodInfo);
                        } catch(Exception ex) {
                            throw new Exception(string.Format("Wrong signature for {0}. Signature requires static Entity {0}(EntityManager)", methodInfo.Name));
                        }
                        RegisterEntityFactoryMethod(methodInfo.GetCustomAttribute<NetworkEntityFactoryMethodAttribute>().InstanceId, networkInstantiationHandlerDelegate);
                    }
                }
            }
        }

        ComponentTypes = componentTypes.ToArray();
        //NetworkFactoryMethods = networkFactoryMethods.ToArray();
    }


    public ReflectionUtility(Assembly assembly) {

        byte componentTypeId = 0;
        var componentTypes = new List<ComponentType>();
        List<Type> types = new List<Type>(assembly.GetTypes());
        types.Sort((x, y) => x.Name.CompareTo(y.Name));
        foreach (Type type in types) {
            if (type.GetCustomAttribute<NetSyncAttribute>() != null) {
                componentTypeId++;
                componentTypeToIdMap.Add(type, componentTypeId);
                idToComponentTypeMap.Add(componentTypeId, type);
                componentTypes.Add(type);

                int numberOfMembers = 0;
                List<NetworkMemberInfo> networkMemberInfos = new List<NetworkMemberInfo>();
                MemberInfo[] memberInfos = type.GetMembers().OrderBy((x) => x.Name).Where(memberInfo => memberInfo.IsDefined(typeof(NetSyncMemberAttribute), false)).ToArray();
                for (int i = 0; i < memberInfos.Length; i++) {
                    MemberInfo memberInfo = memberInfos[i];

                    Type networkMemberInfoType = typeof(NetworkMemberInfo<,>);
                    Type mainMemberInfoTypeType = memberInfo.MemberType == MemberTypes.Field ? (memberInfo as FieldInfo).FieldType : (memberInfo as PropertyInfo).PropertyType;
                    Type mainMemberInfoGenericType = networkMemberInfoType.MakeGenericType(type, mainMemberInfoTypeType);
                    NetSyncMemberAttribute netSyncMemberAttribute = memberInfo.GetCustomAttribute<NetSyncMemberAttribute>(false);
                    NetworkMemberInfo mainMemberInfo = (NetworkMemberInfo)Activator.CreateInstance(mainMemberInfoGenericType, memberInfo, netSyncMemberAttribute);
                    NetSyncSubMemberAttribute[] netSyncSubMemberAttributes = memberInfo.GetCustomAttributes<NetSyncSubMemberAttribute>(false).ToArray();

                    numberOfMembers += netSyncSubMemberAttributes.Length;
                    foreach (NetSyncSubMemberAttribute NetSyncSubMemberAttribute in netSyncSubMemberAttributes) {
                        if (!NetSyncSubMemberAttribute.OverriddenValues) {
                            NetSyncSubMemberAttribute.SetValuesFrom(netSyncMemberAttribute);
                        }

                        Type subType = memberInfo.MemberType == MemberTypes.Field ? (memberInfo as FieldInfo).FieldType : (memberInfo as PropertyInfo).PropertyType;

                        bool found = false;
                        IEnumerable<MemberInfo> subMemberInfos = subType.GetMembers().OrderBy(x => x.Name);
                        foreach (MemberInfo subMemberInfo in subMemberInfos) {
                            if (subMemberInfo.Name.Equals(NetSyncSubMemberAttribute.MemberName)) {
                                Type networkSubMemberInfoType = typeof(NetworkMemberInfo<,,>);
                                Type mainSubMemberInfoTypeType = subMemberInfo.MemberType == MemberTypes.Field ? (subMemberInfo as FieldInfo).FieldType : (subMemberInfo as PropertyInfo).PropertyType;
                                Type subMemberInfoGenericType = networkSubMemberInfoType.MakeGenericType(type, subType, mainSubMemberInfoTypeType);
                                networkMemberInfos.Add((NetworkMemberInfo)Activator.CreateInstance(subMemberInfoGenericType, subMemberInfo, mainMemberInfo, NetSyncSubMemberAttribute));
                                found = true;
                                break;
                            }
                        }

                        if (!found) {
                            throw new MissingMemberException(NetSyncSubMemberAttribute.MemberName);
                        }
                    }

                    if (netSyncSubMemberAttributes.Length == 0) {
                        numberOfMembers++;
                        networkMemberInfos.Add(mainMemberInfo);
                    }
                }
                cashedNetworkMemberInfo.Add(type, networkMemberInfos.ToArray());
                componentTypeMemberCount.Add(type, numberOfMembers);
            }

            if (type.GetCustomAttribute<NetworkEntityFactoryAttribute>() != null) {
                MethodInfo[] methodInfos = type.GetMethods().Where(methodInfo => methodInfo.IsDefined(typeof(NetworkEntityFactoryMethodAttribute), false)).ToArray();
                foreach (MethodInfo methodInfo in methodInfos) {
                    NetworkInstantiationHandlerDelegate networkInstantiationHandlerDelegate = null;
                    try {
                        networkInstantiationHandlerDelegate = (NetworkInstantiationHandlerDelegate)Delegate.CreateDelegate(typeof(NetworkInstantiationHandlerDelegate), methodInfo);
                    } catch (Exception ex) {
                        throw new Exception(string.Format("Wrong signature for {0}. Signature requires static Entity {0}(EntityManager)", methodInfo.Name));
                    }
                    RegisterEntityFactoryMethod(methodInfo.GetCustomAttribute<NetworkEntityFactoryMethodAttribute>().InstanceId, networkInstantiationHandlerDelegate);
                }
            }
        }

        ComponentTypes = componentTypes.ToArray();
        //NetworkFactoryMethods = networkFactoryMethods.ToArray();
    }

    public void RegisterEntityFactoryMethod(int id, NetworkInstantiationHandlerDelegate networkInstantiationHandler) {
        entityFactoryMethodMap.Add(id, networkInstantiationHandler);
    }

    public NetworkInstantiationHandlerDelegate GetEntityFactoryMethod(int id) {
        return entityFactoryMethodMap[id];
    }

    public NetworkMemberInfo[] GetNetworkMemberInfo(ComponentType componentType) {
        return cashedNetworkMemberInfo[componentType];
    }

    public int GetComponentTypeID(ComponentType componentType) {
        return componentTypeToIdMap[componentType];
    }

    public ComponentType GetComponentType(int id) {
        return idToComponentTypeMap[id];
    }

    public int GetNumberOfMembers(Type componentType) {
        return componentTypeMemberCount[componentType];
    }
}