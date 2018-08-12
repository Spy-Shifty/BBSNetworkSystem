using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[AlwaysUpdateSystem]
[UpdateInGroup(typeof(NetworkUpdateGroup))]
[UpdateAfter(typeof(NetworkReceiveSystem))]
public class NetworkSendSystem : ComponentSystem {

    struct AddedEntityData {
        public ComponentDataArray<NetworkSync> networkSyncComponents;
        public SubtractiveComponent<NetworkSyncState> networkSyncStateComponents;
        public EntityArray entities;
        public readonly int Length;
    }

    struct RemovedEntityData {
        public SubtractiveComponent<NetworkSync> networkSyncComponents;
        public ComponentDataArray<NetworkSyncState> networkSyncStateComponents;
        public EntityArray entities;
        public readonly int Length;
    }

    [Inject] AddedEntityData addedSyncEntities;
    [Inject] RemovedEntityData removedSyncEntities;

    public static bool LogSendMessages;

    private NetworkFactory networkFactory;

    //private readonly NetworkSyncDataContainer ownNetworkSyncDataContainer = new NetworkSyncDataContainer();
    //private readonly Dictionary<Entity, NetworkSyncDataEntityContainer> ownEntityContainerMap = new Dictionary<Entity, NetworkSyncDataEntityContainer>();
    private readonly NetworkSendMessageUtility ownNetworkSendMessageUtility = new NetworkSendMessageUtility();
    internal static readonly NetworkSendMessageUtility AllNetworkSendMessageUtility = new NetworkSendMessageUtility();


    private readonly List<NetworkMethodInfo<NetworkSendSystem>> AddedComponentsMethods = new List<NetworkMethodInfo<NetworkSendSystem>>();
    private readonly List<NetworkMethodInfo<NetworkSendSystem>> RemovedComponentsMethods = new List<NetworkMethodInfo<NetworkSendSystem>>();
    private readonly List<NetworkMethodInfo<NetworkSendSystem>> UpdateComponentsMethods = new List<NetworkMethodInfo<NetworkSendSystem>>();
    private readonly List<NetworkInOutMethodInfo<NetworkSendSystem, Entity, ComponentDataContainer>> AddComponentDataOnEntityAddedMethods = new List<NetworkInOutMethodInfo<NetworkSendSystem, Entity, ComponentDataContainer>>();
    private readonly List<NetworkMethodInfo<NetworkSendSystem, Entity>> RemoveComponentOnDestroyEntityMethods = new List<NetworkMethodInfo<NetworkSendSystem, Entity>>();

    private NetworkMessageSerializer<NetworkSyncDataContainer> messageSerializer;
    private int lastSend = (Environment.TickCount - SendInterval) & Int32.MaxValue;
    private INetworkManager networkManager;
    internal const int SendInterval = 100;
    private readonly ReflectionUtility reflectionUtility = new ReflectionUtility();

    internal static string LastSendMessage { get; private set; }


    protected override void OnCreateManager(int capacity) {
        messageSerializer = new NetworkMessageSerializer<NetworkSyncDataContainer>();
        ComponentType[] componentTypes = reflectionUtility.ComponentTypes.ToArray();
        networkFactory = new NetworkFactory(EntityManager);
        Type networkSystemType = typeof(NetworkSendSystem);
        for (int i = 0; i < componentTypes.Length; i++) {
            AddedComponentsMethods.Add(
                new NetworkMethodInfo<NetworkSendSystem>(networkSystemType
                    .GetMethod("AddedComponents", BindingFlags.Instance | BindingFlags.NonPublic)
                    .MakeGenericMethod(componentTypes[i].GetManagedType())));

            RemovedComponentsMethods.Add(
                new NetworkMethodInfo<NetworkSendSystem>(networkSystemType
                    .GetMethod("RemovedComponents", BindingFlags.Instance | BindingFlags.NonPublic)
                    .MakeGenericMethod(componentTypes[i].GetManagedType())));

            UpdateComponentsMethods.Add(
                new NetworkMethodInfo<NetworkSendSystem>(networkSystemType
                    .GetMethod("UpdateDataState", BindingFlags.Instance | BindingFlags.NonPublic)
                    .MakeGenericMethod(componentTypes[i].GetManagedType())));

            AddComponentDataOnEntityAddedMethods.Add(
               new NetworkInOutMethodInfo<NetworkSendSystem, Entity, ComponentDataContainer>(networkSystemType
                   .GetMethod("AddComponentDataOnEntityAdded", BindingFlags.Instance | BindingFlags.NonPublic)
                   .MakeGenericMethod(componentTypes[i].GetManagedType())));

            RemoveComponentOnDestroyEntityMethods.Add(
                new NetworkMethodInfo<NetworkSendSystem, Entity>(networkSystemType
                    .GetMethod("RemoveComponentOnDestroyEntity", BindingFlags.Instance | BindingFlags.NonPublic)
                    .MakeGenericMethod(componentTypes[i].GetManagedType())));
        }

        Enabled = networkManager != null;
    }

    protected override void OnDestroyManager() {
        messageSerializer.Dispose();
        networkFactory.Dispose();
    }

    protected override void OnStartRunning() {
        base.OnStartRunning();
        Enabled = networkManager != null;
    }

    protected override void OnUpdate() {
        if (networkManager == null || !networkManager.IsConnectedAndReady) {
            return;
        }

        AddedEntities();
        RemovedEntities();

        for (int i = 0; i < AddedComponentsMethods.Count; i++) {
            AddedComponentsMethods[i].Invoke(this);
        }

        for (int i = 0; i < RemovedComponentsMethods.Count; i++) {
            RemovedComponentsMethods[i].Invoke(this);
        }

        int currentTime = Environment.TickCount & Int32.MaxValue;
        if (math.abs(currentTime - lastSend) > SendInterval) {

            lastSend = currentTime;
            for (int i = 0; i < UpdateComponentsMethods.Count; i++) {
                UpdateComponentsMethods[i].Invoke(this);
            }

            SendData();
        }

        networkFactory.FlushNetworkManager();

    }
    
    private void AddedEntities() {
        EntityArray entities = addedSyncEntities.entities;
        ComponentDataArray<NetworkSync> networkSyncs = addedSyncEntities.networkSyncComponents;

        for (int i = 0; i < entities.Length; i++) {
            NetworkSync networkSync = networkSyncs[i];
            int instanceId = networkSync.instanceId;
            NetworkSyncState component = new NetworkSyncState() {
                actorId = networkManager.LocalPlayerID,
                networkId = networkManager.GetNetworkId(),
            };
            Entity entity = entities[i];
            PostUpdateCommands.AddComponent(entity, component);

            if (NetworkUtility.CanAssignAuthority(networkManager, networkSync.authority)) {
                PostUpdateCommands.AddComponent(entity, new NetworktAuthority());
            }

            NetworkEntityData networkEntityData = new NetworkEntityData {
                InstanceId = networkSync.instanceId,

                NetworkSyncEntity = new NetworkSyncEntity {
                    ActorId = component.actorId,
                    NetworkId = component.networkId,
                }
            };

            for (int j = 0; j < AddComponentDataOnEntityAddedMethods.Count; j++) {
                if (AddComponentDataOnEntityAddedMethods[j].Invoke(this, ref entity, out ComponentDataContainer componentData)) {
                    networkEntityData.ComponentData.Add(componentData);
                }
            }

            ownNetworkSendMessageUtility.AddEntity(networkEntityData);
            AllNetworkSendMessageUtility.AddEntity(networkEntityData);
        }
    }

   

    private void RemovedEntities() {
        EntityArray entities = removedSyncEntities.entities;
        ComponentDataArray<NetworkSyncState> networkSyncs = removedSyncEntities.networkSyncStateComponents;

        for (int i = 0; i < entities.Length; i++) {
            NetworkSyncState component = new NetworkSyncState() {
                actorId = networkManager.LocalPlayerID,
                networkId = networkManager.GetNetworkId(),
            };
            PostUpdateCommands.RemoveComponent<NetworkSyncState>(entities[i]);
            for (int j = 0; j < RemoveComponentOnDestroyEntityMethods.Count; j++) {
                RemoveComponentOnDestroyEntityMethods[j].Invoke(this, entities[i]);
            }

            NetworkSyncEntity networkSyncEntity = new NetworkSyncEntity {
                ActorId = component.actorId,
                NetworkId = component.networkId,
            };
            ownNetworkSendMessageUtility.RemoveEntity(networkSyncEntity);
            AllNetworkSendMessageUtility.RemoveEntity(networkSyncEntity);
        }
    }

    void RemoveComponentOnDestroyEntity<T>(Entity entity) where T : struct, IComponentData {
        if (EntityManager.HasComponent<NetworkComponentState<T>>(entity)) {
            PostUpdateCommands.RemoveComponent<NetworkComponentState<T>>(entity);
            PostUpdateCommands.DestroyEntity(EntityManager.GetComponentData<NetworkComponentState<T>>(entity).dataEntity);
        }
    }

    private bool AddComponentDataOnEntityAdded<T>(ref Entity entity, out ComponentDataContainer componentDataContainer) where T : struct, IComponentData {
        componentDataContainer = null;
        if (EntityManager.HasComponent<T>(entity)) {
            ComponentType componentType = ComponentType.Create<T>();
            int numberOfMembers = reflectionUtility.GetNumberOfMembers(componentType.GetManagedType());
            Entity networkDataEntity = networkFactory.CreateNetworkComponentData<T>(entity, numberOfMembers);
            NativeArray<int> values = networkFactory.NetworkEntityManager.GetFixedArray<int>(networkDataEntity);
            PostUpdateCommands.AddComponent(entity, new NetworkComponentState<T>());

            T component = EntityManager.GetComponentData<T>(entity);
            NetworkMemberInfo[] networkMemberInfos = reflectionUtility.GetNetworkMemberInfo(componentType);
            List<MemberDataContainer> memberDataContainers = new List<MemberDataContainer>();
            for (int i = 0; i < numberOfMembers; i++) {
                int value = (networkMemberInfos[i] as NetworkMemberInfo<T>).GetValue(component);
                memberDataContainers.Add(new MemberDataContainer() {
                    MemberId = i,
                    Data = value
                });
                values[i] = value;
            }

            componentDataContainer = new ComponentDataContainer() {
                ComponentTypeId = reflectionUtility.GetComponentTypeID(componentType),
                MemberData = memberDataContainers
            };
            return true;
        }
        return false;
    }
     
    private void AddedComponents<T>() where T : struct, IComponentData {
        ComponentType componentType = ComponentType.Create<T>();
        ComponentGroup group = GetComponentGroup(ComponentType.Create<NetworkSyncState>(), componentType, ComponentType.Subtractive<NetworkComponentState<T>>(), ComponentType.Create<NetworktAuthority>());
        ComponentDataArray<T> components = group.GetComponentDataArray<T>();
        ComponentDataArray<NetworkSyncState> networkSyncStateComponents = group.GetComponentDataArray<NetworkSyncState>();
        EntityArray entities = group.GetEntityArray();

        NetworkMemberInfo[] networkMemberInfos = reflectionUtility.GetNetworkMemberInfo(componentType);

        for (int i = 0; i < entities.Length; i++) {

            NetworkSyncState networkSyncState = networkSyncStateComponents[i];
            ComponentDataContainer componentData = new ComponentDataContainer {
                ComponentTypeId = reflectionUtility.GetComponentTypeID(componentType)
            };

            T component = components[i];
            for (int j = 0; j < networkMemberInfos.Length; j++) {
                componentData.MemberData.Add(new MemberDataContainer {
                    MemberId = j,
                    Data = (networkMemberInfos[j] as NetworkMemberInfo<T>).GetValue(component),
                });
            }


            ownNetworkSendMessageUtility.AddComponent(entities[i], networkSyncState.actorId, networkSyncState.networkId, componentData);
            AllNetworkSendMessageUtility.AddComponent(entities[i], networkSyncState.actorId, networkSyncState.networkId, componentData);

            int numberOfMembers = reflectionUtility.GetNumberOfMembers(componentType.GetManagedType());
            networkFactory.CreateNetworkComponentData<T>(entities[i], numberOfMembers);
            PostUpdateCommands.AddComponent(entities[i], new NetworkComponentState<T>());
        }
    }

    private void RemovedComponents<T>() where T : IComponentData {
        ComponentType componentType = ComponentType.Create<T>();
        ComponentGroup group = GetComponentGroup(ComponentType.Create<NetworkSyncState>(), ComponentType.Subtractive<T>(), ComponentType.Create<NetworkComponentState<T>>(), ComponentType.Create<NetworktAuthority>());
        ComponentDataArray<NetworkSyncState> networkSyncStateComponents = group.GetComponentDataArray<NetworkSyncState>();
        ComponentDataArray<NetworkComponentState<T>> networkComponentStates = group.GetComponentDataArray<NetworkComponentState<T>>();
        EntityArray entities = group.GetEntityArray();

        for (int i = 0; i < entities.Length; i++) {
            NetworkSyncState networkSyncState = networkSyncStateComponents[i];
            ownNetworkSendMessageUtility.RemoveComponent(entities[i], networkSyncState.actorId, networkSyncState.networkId, reflectionUtility.GetComponentTypeID(componentType));
            AllNetworkSendMessageUtility.RemoveComponent(entities[i], networkSyncState.actorId, networkSyncState.networkId, reflectionUtility.GetComponentTypeID(componentType));

            PostUpdateCommands.DestroyEntity(networkComponentStates[i].dataEntity);
            PostUpdateCommands.RemoveComponent<NetworkComponentState<T>>(entities[i]);
        }
    }

    private void UpdateDataState<T>() where T : struct, IComponentData {
        ComponentType componentType = ComponentType.Create<T>();
        ComponentGroup group = GetComponentGroup(ComponentType.Create<NetworkSyncState>(), componentType, ComponentType.Create<NetworkComponentState<T>>(), ComponentType.Create<NetworktAuthority>());
        ComponentDataArray<NetworkSyncState> networkSyncStateComponents = group.GetComponentDataArray<NetworkSyncState>();
        ComponentDataArray<T> networkComponents = group.GetComponentDataArray<T>();
        ComponentDataArray<NetworkComponentState<T>> networkComponentStates = group.GetComponentDataArray<NetworkComponentState<T>>();
        EntityArray entities = group.GetEntityArray();

        NetworkMemberInfo[] networkMemberInfos = reflectionUtility.GetNetworkMemberInfo(componentType);
        for (int i = 0; i < entities.Length; i++) {            
            NativeArray<int> values = EntityManager.GetFixedArray<int>(networkComponentStates[i].dataEntity);
            ComponentDataContainer componentDataContainer = new ComponentDataContainer {
                ComponentTypeId = reflectionUtility.GetComponentTypeID(componentType),
            };
            for (int j = 0; j < networkMemberInfos.Length; j++) {
                NetworkMemberInfo<T> networkMemberInfo = (networkMemberInfos[j] as NetworkMemberInfo<T>);
                if (networkMemberInfo.netSyncOptions.InitOnly) {
                    continue;
                }

                int newValue = networkMemberInfo.GetValue(networkComponents[i]);  
                if(newValue != values[j]) {
                    componentDataContainer.MemberData.Add(new MemberDataContainer {
                        MemberId = j,
                        Data = newValue,
                    });
                }
                values[j] = newValue;
            }

            if (componentDataContainer.MemberData.Count != 0) {
                NetworkSyncState networkSyncState = networkSyncStateComponents[i];
                ownNetworkSendMessageUtility.SetComponentData(entities[i], networkSyncState.actorId, networkSyncState.networkId, componentDataContainer);
                AllNetworkSendMessageUtility.SetComponentData(entities[i], networkSyncState.actorId, networkSyncState.networkId, componentDataContainer);
            }
        }
    }

    private void SendData() {
        NetworkEventOptions networkEventOptions = new NetworkEventOptions();
        byte[] data;
        if (networkManager.IsMaster) {
            if (!AllNetworkSendMessageUtility.DataContainer.AddedNetworkSyncEntities.Any()
                && !AllNetworkSendMessageUtility.DataContainer.RemovedNetworkSyncEntities.Any()
                && !AllNetworkSendMessageUtility.DataContainer.NetworkSyncDataEntities.Any()) {
                return;
            }

            networkEventOptions.Receiver = NetworkReceiverGroup.Others;
            if (LogSendMessages) {
                LastSendMessage = NetworkMessageUtility.ToString(AllNetworkSendMessageUtility.DataContainer);
            }
            data = messageSerializer.Serialize(AllNetworkSendMessageUtility.DataContainer);
        } else {
            if (!ownNetworkSendMessageUtility.DataContainer.AddedNetworkSyncEntities.Any()
                && !ownNetworkSendMessageUtility.DataContainer.RemovedNetworkSyncEntities.Any()
                && !ownNetworkSendMessageUtility.DataContainer.NetworkSyncDataEntities.Any()) {
                return;
            }

            networkEventOptions.Receiver = NetworkReceiverGroup.MasterClient;
            if (LogSendMessages) {
                LastSendMessage = NetworkMessageUtility.ToString(ownNetworkSendMessageUtility.DataContainer);
            }
            data = messageSerializer.Serialize(ownNetworkSendMessageUtility.DataContainer);

        }
        //Debug.Log("NetworkSendSystem:\n" + LastSendMessage);
        networkManager.SendMessage(NetworkEvents.DataSync, data, true, networkEventOptions);

        ownNetworkSendMessageUtility.Reset();
        AllNetworkSendMessageUtility.Reset();
    }

    internal void SetNetworkManager(INetworkManager networkManager) {
        this.networkManager = networkManager;
        Enabled = networkManager != null;
    }
}
