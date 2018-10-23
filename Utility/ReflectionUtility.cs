using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Entities;
using UnityEngine;

public delegate Entity NetworkInstantiationHandlerDelegate(EntityManager entityManager);

internal class ReflectionUtility {

    private Dictionary<ComponentType, NetworkMemberInfo[]> cashedNetworkMemberInfo = new Dictionary<ComponentType, NetworkMemberInfo[]>();
    private byte componentTypeId = 0;

    private readonly Dictionary<ComponentType, int> componentTypeToIdMap = new Dictionary<ComponentType, int>();
    private readonly Dictionary<int, ComponentType> idToComponentTypeMap = new Dictionary<int, ComponentType>();
    private readonly Dictionary<ComponentType, int> componentTypeMemberCount = new Dictionary<ComponentType, int>();

    private readonly List<ComponentType> componentTypes;
    public IEnumerable<ComponentType> ComponentTypes => componentTypes;
    //public static readonly MethodInfo[] NetworkFactoryMethods;

    private Dictionary<int, NetworkInstantiationHandlerDelegate> entityFactoryMethodMap = new Dictionary<int, NetworkInstantiationHandlerDelegate>();

    public ReflectionUtility() {
        var componentTypes = new List<ComponentType>();
        List<Assembly> assemblies = new List<Assembly>(AppDomain.CurrentDomain.GetAssemblies());
        assemblies.Sort((x, y) => x.FullName.CompareTo(y.FullName));
        foreach (Assembly assembly in assemblies) {
            componentTypes.AddRange(LoadFromAssembly(assembly));                    
        }

        this.componentTypes = componentTypes;
    }

    public ReflectionUtility(Assembly assembly) {
        componentTypes = LoadFromAssembly(assembly);
    }

    private List<ComponentType> LoadFromAssembly(Assembly assembly) {        
        var componentTypes = new List<ComponentType>();
        List<Type> types = new List<Type>(assembly.GetTypes());
        types.Sort((x, y) => x.Name.CompareTo(y.Name));
        foreach (Type type in types) {
            if (type.GetCustomAttribute<NetSyncAttribute>() != null) {
                componentTypes.Add(type);
                ExtractNetworkMemberInfos(type);
            }else {
                ProxyNetSyncAttribute proxyNetSyncAttribute = type.GetCustomAttribute<ProxyNetSyncAttribute>();
                if(proxyNetSyncAttribute != null) {
                    componentTypes.Add(proxyNetSyncAttribute.Type);
                    ExtractNetworkMemberInfosFromProxy(type, proxyNetSyncAttribute.Type);
                }
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

        return componentTypes;
        //NetworkFactoryMethods = networkFactoryMethods.ToArray();
    }

    private  void ExtractNetworkMemberInfos(Type type) {
        componentTypeId++;
        componentTypeToIdMap.Add(type, componentTypeId);
        idToComponentTypeMap.Add(componentTypeId, type);

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
    
    private void ExtractNetworkMemberInfosFromProxy(Type proxyType, Type type) {
        componentTypeId++;
        componentTypeToIdMap.Add(type, componentTypeId);
        idToComponentTypeMap.Add(componentTypeId, type);

        int numberOfMembers = 0;
        List<NetworkMemberInfo> networkMemberInfos = new List<NetworkMemberInfo>();
        MemberInfo[] proxyMemberInfos = proxyType.GetMembers().Where(memberInfo => memberInfo.IsDefined(typeof(NetSyncMemberAttribute), false)).OrderBy((x) => x.Name).ToArray();
        List<MemberInfo> memberInfos = new List<MemberInfo>();

        // type.GetMembers().Where(memberInfo => proxyMemberInfoNames.Contains(memberInfo.Name)).OrderBy((x) => x.Name).ToArray();
        foreach (MemberInfo proxyMemberInfo in proxyMemberInfos) {
            MemberInfo memberInfo = type.GetMembers().FirstOrDefault(x => x.Name == proxyMemberInfo.Name);
            if(memberInfo == null) {
                throw new NullReferenceException(string.Format("Member \"{0}\" couldn't be found in type {1}", proxyMemberInfo.Name, type));
            }
            memberInfos.Add(memberInfo);
        }

        for (int i = 0; i < proxyMemberInfos.Length; i++) {
            MemberInfo proxyMemberInfo = proxyMemberInfos[i];
            MemberInfo memberInfo = memberInfos[i];

            Type networkMemberInfoType = typeof(NetworkMemberInfo<,>);
            Type mainMemberInfoTypeType = memberInfo.MemberType == MemberTypes.Field ? (memberInfo as FieldInfo).FieldType : (memberInfo as PropertyInfo).PropertyType;
            Type mainMemberInfoGenericType = networkMemberInfoType.MakeGenericType(type, mainMemberInfoTypeType);
            NetSyncMemberAttribute netSyncMemberAttribute = proxyMemberInfo.GetCustomAttribute<NetSyncMemberAttribute>(false);
            NetworkMemberInfo mainMemberInfo = (NetworkMemberInfo)Activator.CreateInstance(mainMemberInfoGenericType, memberInfo, netSyncMemberAttribute);
            NetSyncSubMemberAttribute[] netSyncSubMemberAttributes = proxyMemberInfo.GetCustomAttributes<NetSyncSubMemberAttribute>(false).ToArray();

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

    public void RegisterEntityFactoryMethod(int id, NetworkInstantiationHandlerDelegate networkInstantiationHandler) {
        entityFactoryMethodMap.Add(id, networkInstantiationHandler);
    }

    public NetworkInstantiationHandlerDelegate GetEntityFactoryMethod(int id) {
        try {
            return entityFactoryMethodMap[id];
        } catch {
            throw new NetworkEntityFactoryMethodNotFoundException(id);
        }
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