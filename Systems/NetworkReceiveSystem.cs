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
public class NetworkReceiveSystem : ComponentSystem {
    private const float DeltaTimeMessage = NetworkSendSystem.SendInterval / 1000f;
    public static bool LogReceivedMessages;

    private NetworkMessageSerializer<NetworkSyncDataContainer> messageSerializer;
    private INetworkManager networkManager;
    private readonly Dictionary<ComponentType, NetworkMethodInfo<NetworkReceiveSystem, Entity, List<MemberDataContainer>>> AddComponentsMethods = new Dictionary<ComponentType, NetworkMethodInfo<NetworkReceiveSystem, Entity, List<MemberDataContainer>>>();
    private readonly Dictionary<ComponentType, NetworkMethodInfo<NetworkReceiveSystem, Entity>> RemoveComponentsMethods = new Dictionary<ComponentType, NetworkMethodInfo<NetworkReceiveSystem, Entity>>();
    private readonly Dictionary<ComponentType, NetworkMethodInfo<NetworkReceiveSystem, Entity, List<MemberDataContainer>>> SetComponentsMethods = new Dictionary<ComponentType, NetworkMethodInfo<NetworkReceiveSystem, Entity, List<MemberDataContainer>>>();
    private readonly List<NetworkMethodInfo<NetworkReceiveSystem, Entity>> RemoveComponentOnDestroyEntityMethods = new List<NetworkMethodInfo<NetworkReceiveSystem, Entity>>();
    private readonly List<NetworkMethodInfo<NetworkReceiveSystem>> UpdateComponentsMethods = new List<NetworkMethodInfo<NetworkReceiveSystem>>();

    private NetworkFactory networkFactory;
    private readonly ReflectionUtility reflectionUtility = new ReflectionUtility();
    private readonly List<GameObject> gameObjectsToDestroy = new List<GameObject>();

    protected override void OnCreateManager(int capacity) {
        networkFactory = new NetworkFactory(EntityManager);
        ComponentType[] componentTypes = reflectionUtility.ComponentTypes;
        Type networkSystemType = typeof(NetworkReceiveSystem);
        for (int i = 0; i < componentTypes.Length; i++) {
            AddComponentsMethods.Add(componentTypes[i],
                new NetworkMethodInfo<NetworkReceiveSystem, Entity, List<MemberDataContainer>>(networkSystemType
                    .GetMethod("AddComponent", BindingFlags.Instance | BindingFlags.NonPublic)
                    .MakeGenericMethod(componentTypes[i].GetManagedType())));

            RemoveComponentsMethods.Add(componentTypes[i],
              new NetworkMethodInfo<NetworkReceiveSystem, Entity>(networkSystemType
                  .GetMethod("RemoveComponent", BindingFlags.Instance | BindingFlags.NonPublic)
                  .MakeGenericMethod(componentTypes[i].GetManagedType())));

            SetComponentsMethods.Add(componentTypes[i],
              new NetworkMethodInfo<NetworkReceiveSystem, Entity, List<MemberDataContainer>>(networkSystemType
                  .GetMethod("SetComponent", BindingFlags.Instance | BindingFlags.NonPublic)
                  .MakeGenericMethod(componentTypes[i].GetManagedType())));

            UpdateComponentsMethods.Add(
                new NetworkMethodInfo<NetworkReceiveSystem>(networkSystemType
                    .GetMethod("UpdateComponent", BindingFlags.Instance | BindingFlags.NonPublic)
                    .MakeGenericMethod(componentTypes[i].GetManagedType())));

            RemoveComponentOnDestroyEntityMethods.Add(
              new NetworkMethodInfo<NetworkReceiveSystem, Entity>(networkSystemType
                  .GetMethod("RemoveComponentOnDestroyEntity", BindingFlags.Instance | BindingFlags.NonPublic)
                  .MakeGenericMethod(componentTypes[i].GetManagedType())));            
        }

        messageSerializer = new NetworkMessageSerializer<NetworkSyncDataContainer>();
    }

    protected override void OnDestroyManager() {
        messageSerializer.Dispose();
        networkFactory.Dispose();
    }

    protected override void OnUpdate() {
        //return;
        networkManager.Update();
        for (int i = 0; i < UpdateComponentsMethods.Count; i++) {
            UpdateComponentsMethods[i].Invoke(this);
        }
        for (int i = 0; i < gameObjectsToDestroy.Count; i++) {
            UnityEngine.Object.Destroy(gameObjectsToDestroy[i]);
        }
        gameObjectsToDestroy.Clear();
        networkFactory.FlushNetworkManager();
    }

    private void NetworkManager_OnEventData(byte eventId, int playerId, object data) {
        switch (eventId) {
            case NetworkEvents.DataSync:
                ReceiveNetworkUpdate((byte[])data);
                break;
        }
    }


    private void NetworkManager_OnPlayerLeft(int actorId) {
        ComponentGroup group = GetComponentGroup(ComponentType.Create<NetworkSyncState>());
        ComponentDataArray<NetworkSyncState> networkSyncStateComponents = group.GetComponentDataArray<NetworkSyncState>();
        EntityArray entities = group.GetEntityArray();
        for (int i = 0; i < entities.Length; i++) {
            if (networkSyncStateComponents[i].actorId == actorId) {
                Entity entity = entities[i];
                PostUpdateCommands.RemoveComponent<NetworkSyncState>(entity);
                PostUpdateCommands.DestroyEntity(entity);
                if (EntityManager.HasComponent<Transform>(entity)) {
                    gameObjectsToDestroy.Add(EntityManager.GetComponentObject<Transform>(entity).gameObject);
                }
                for (int j = 0; j < RemoveComponentOnDestroyEntityMethods.Count; j++) {
                    RemoveComponentOnDestroyEntityMethods[j].Invoke(this, entity);
                }
            }
        }
    }

    private void NetworkManager_OnDisconnect() {
        ComponentGroup group = GetComponentGroup(ComponentType.Create<NetworkSyncState>());
        ComponentDataArray<NetworkSyncState> networkSyncStateComponents = group.GetComponentDataArray<NetworkSyncState>();
        EntityArray entities = group.GetEntityArray();
        for (int i = 0; i < entities.Length; i++) {
            Entity entity = entities[i];
            PostUpdateCommands.RemoveComponent<NetworkSyncState>(entity);

            if (networkSyncStateComponents[i].actorId != networkManager.LocalPlayerID) {
                PostUpdateCommands.DestroyEntity(entity);
                if (EntityManager.HasComponent<Transform>(entity)) {
                    gameObjectsToDestroy.Add(EntityManager.GetComponentObject<Transform>(entity).gameObject);
                }
            } else {
                PostUpdateCommands.RemoveComponent<NetworktOwner>(entity);
            }

            for (int j = 0; j < RemoveComponentOnDestroyEntityMethods.Count; j++) {
                RemoveComponentOnDestroyEntityMethods[j].Invoke(this, entity);
            }

        }
    }

    private void ReceiveNetworkUpdate(byte[] data) {
        
        NetworkSyncDataContainer networkSyncDataContainer = messageSerializer.Deserialize(data);
        if (LogReceivedMessages && (networkSyncDataContainer.AddedNetworkSyncEntities.Any() 
            || networkSyncDataContainer.RemovedNetworkSyncEntities.Any() 
            || networkSyncDataContainer.NetworkSyncDataEntities.Any())) {
            Debug.Log("ReceiveNetworkUpdate: " + NetworkMessageUtility.ToString(networkSyncDataContainer));
        }

        ComponentGroup group = GetComponentGroup(ComponentType.Create<NetworkSyncState>());
        ComponentDataArray<NetworkSyncState> networkSyncStateComponents = group.GetComponentDataArray<NetworkSyncState>();
        EntityArray entities = group.GetEntityArray();

        NativeHashMap<int, int> entityIndexMap = new NativeHashMap<int, int>(entities.Length, Allocator.Temp);
        for (int i = 0; i < entities.Length; i++) {
            NetworkSyncState networkSyncState = networkSyncStateComponents[i];
            int hash = (int)math.pow(networkSyncState.actorId,  networkSyncState.networkId);
            entityIndexMap.TryAdd(hash, i);
        }

        // added Entities
        List<NetworkEntityData> addedNetworkSyncEntities = networkSyncDataContainer.AddedNetworkSyncEntities;
        for (int i = 0; i < addedNetworkSyncEntities.Count; i++) {
            if (addedNetworkSyncEntities[i].NetworkSyncEntity.ActorId == networkManager.LocalPlayerID) {
                continue;
            }
            Entity entity = reflectionUtility.GetEntityFactoryMethod(addedNetworkSyncEntities[i].InstanceId).Invoke(EntityManager);
            PostUpdateCommands.AddComponent(entity, new NetworkSyncState {
                actorId = addedNetworkSyncEntities[i].NetworkSyncEntity.ActorId,
                networkId = addedNetworkSyncEntities[i].NetworkSyncEntity.NetworkId,
            });

            var componentData = addedNetworkSyncEntities[i].ComponentData;
            for (int j = 0; j < componentData.Count; j++) {
                ComponentType componentType = reflectionUtility.GetComponentType(componentData[j].ComponentTypeId);
                AddComponentsMethods[componentType].Invoke(this, entity, componentData[j].MemberData);
            }

            if (addedNetworkSyncEntities[i].NetworkSyncEntity.ActorId != networkManager.LocalPlayerID) {
                NetworkSendSystem.AllNetworkSendMessageUtility.AddEntity(addedNetworkSyncEntities[i]);
            }
        }

        // removed Entities
        List<NetworkSyncEntity> removedNetworkSyncEntities = networkSyncDataContainer.RemovedNetworkSyncEntities;
        for (int i = 0; i < removedNetworkSyncEntities.Count; i++) {
            if (removedNetworkSyncEntities[i].ActorId == networkManager.LocalPlayerID) {
                continue;
            }
            NetworkSyncEntity networkSyncEntity = removedNetworkSyncEntities[i];
            int hash = (int)math.pow(networkSyncEntity.ActorId, networkSyncEntity.NetworkId);
            if (entityIndexMap.TryGetValue(hash, out int index)) {
                Entity entity = entities[index];
                PostUpdateCommands.RemoveComponent<NetworkSyncState>(entity);
                PostUpdateCommands.DestroyEntity(entity);

                if (EntityManager.HasComponent<Transform>(entity)) {
                    gameObjectsToDestroy.Add(EntityManager.GetComponentObject<Transform>(entity).gameObject);
                }
                for (int j = 0; j < RemoveComponentOnDestroyEntityMethods.Count; j++) {
                    RemoveComponentOnDestroyEntityMethods[j].Invoke(this, entity);
                }
            }

            if (removedNetworkSyncEntities[i].ActorId != networkManager.LocalPlayerID) {
                NetworkSendSystem.AllNetworkSendMessageUtility.RemoveEntity(removedNetworkSyncEntities[i]);
            }
        }

        // update components
        List<NetworkSyncDataEntityContainer> networkSyncDataEntities = networkSyncDataContainer.NetworkSyncDataEntities;
        for (int i = 0; i < networkSyncDataEntities.Count; i++) {
            NetworkSyncEntity networkSyncEntity = networkSyncDataEntities[i].NetworkSyncEntity;
            if (networkSyncEntity.ActorId == networkManager.LocalPlayerID) {
                continue;
            }

            int hash = (int)math.pow(networkSyncEntity.ActorId, networkSyncEntity.NetworkId);
            if (!entityIndexMap.TryGetValue(hash, out int index)) {
                continue;
            }
            Entity entity = entities[index];

            List<ComponentDataContainer> addedComponents = networkSyncDataEntities[i].AddedComponents;
            List<int> removedComponents = networkSyncDataEntities[i].RemovedComponents;
            List<ComponentDataContainer> componentData = networkSyncDataEntities[i].ComponentData;

            for (int j = 0; j < addedComponents.Count; j++) {
                ComponentType componentType = reflectionUtility.GetComponentType(addedComponents[j].ComponentTypeId);
                AddComponentsMethods[componentType].Invoke(this, entity, addedComponents[j].MemberData);
            }

            for (int j = 0; j < componentData.Count; j++) {
                ComponentType componentType = reflectionUtility.GetComponentType(componentData[j].ComponentTypeId);
                SetComponentsMethods[componentType].Invoke(this, entity, componentData[j].MemberData);
            }

            for (int j = 0; j < removedComponents.Count; j++) {
                ComponentType componentType = reflectionUtility.GetComponentType(removedComponents[j]);
                RemoveComponentsMethods[componentType].Invoke(this, entity);
            }


            if (networkSyncEntity.ActorId == networkManager.LocalPlayerID) {
                continue;
            }

            NetworkSendSystem.AllNetworkSendMessageUtility.AddComponents(entity, networkSyncEntity.ActorId, networkSyncEntity.NetworkId, addedComponents);
            NetworkSendSystem.AllNetworkSendMessageUtility.RemoveComponents(entity, networkSyncEntity.ActorId, networkSyncEntity.NetworkId, removedComponents);
            NetworkSendSystem.AllNetworkSendMessageUtility.SetComponentData(entity, networkSyncEntity.ActorId, networkSyncEntity.NetworkId, componentData);
        }        

        entityIndexMap.Dispose();
    }

    void AddComponent<T>(Entity entity, List<MemberDataContainer> memberDataContainers) where T : struct, IComponentData {
        Debug.Log(typeof(T));
        int numberOfMembers = reflectionUtility.GetNumberOfMembers(typeof(T));
        NetworkMemberInfo[] networkMemberInfos = reflectionUtility.GetNetworkMemberInfo(ComponentType.Create<T>());
        if (!EntityManager.HasComponent<T>(entity)) {
            T component = new T();
            for (int i = 0; i < memberDataContainers.Count; i++) {
                int value = memberDataContainers[i].Data;
                (networkMemberInfos[i] as NetworkMemberInfo<T>).SetValue(ref component, value, value, Time.deltaTime, NetworkSendSystem.SendInterval);
            }
            PostUpdateCommands.AddComponent(entity, component);
        }

        if (!EntityManager.HasComponent<NetworkComponentState<T>>(entity)) {
            Entity syncEntity = networkFactory.CreateNetworkComponentData<T>(entity, numberOfMembers);
            NativeArray<int> values = networkFactory.NetworkEntityManager.GetFixedArray<int>(syncEntity);
            for (int i = 0; i < memberDataContainers.Count; i++) {
                int index = i * 2;
                values[index] = memberDataContainers[i].Data;
                values[index + 1] = memberDataContainers[i].Data;
            }
            PostUpdateCommands.AddComponent(entity, new NetworkComponentState<T>());
        }
    }

    void RemoveComponent<T>(Entity entity) where T : struct, IComponentData {
        if (EntityManager.HasComponent<T>(entity)) {
            PostUpdateCommands.RemoveComponent<T>(entity);
        }

        if (EntityManager.HasComponent<NetworkComponentState<T>>(entity)) {
            PostUpdateCommands.RemoveComponent<NetworkComponentState<T>>(entity);
            PostUpdateCommands.DestroyEntity(EntityManager.GetComponentData<NetworkComponentState<T>>(entity).dataEntity);
        }
    }

    void RemoveComponentOnDestroyEntity<T>(Entity entity) where T : struct, IComponentData {
        if (EntityManager.HasComponent<NetworkComponentState<T>>(entity)) {
            PostUpdateCommands.RemoveComponent<NetworkComponentState<T>>(entity);
            PostUpdateCommands.DestroyEntity(EntityManager.GetComponentData<NetworkComponentState<T>>(entity).dataEntity);
        }
    }

    void SetComponent<T>(Entity entity, List<MemberDataContainer> memberDataContainers) {
        if (!EntityManager.HasComponent<NetworkComponentState<T>>(entity)) {
            return;
        }

        NativeArray<int> values = EntityManager.GetFixedArray<int>(EntityManager.GetComponentData<NetworkComponentState<T>>(entity).dataEntity);
        for (int i = 0; i < memberDataContainers.Count; i++) {
            int index = memberDataContainers[i].MemberId * 2;
            values[index] = values[index + 1];
            values[index+1] = memberDataContainers[i].Data;
        }
    }

    void UpdateComponent<T>() where T: struct, IComponentData {
        var group = GetComponentGroup(ComponentType.ReadOnly<NetworkSync>(), ComponentType.Create<T>(), ComponentType.ReadOnly<NetworkComponentState<T>>(), ComponentType.Subtractive<NetworktOwner>());

        EntityArray entities = group.GetEntityArray();
        ComponentDataArray<T> components = group.GetComponentDataArray<T>();
        ComponentDataArray<NetworkComponentState<T>> componentStates = group.GetComponentDataArray<NetworkComponentState<T>>();

        NetworkMemberInfo[] networkMemberInfos = reflectionUtility.GetNetworkMemberInfo(ComponentType.Create<T>());

        for (int i = 0; i < entities.Length; i++) {
            T component = components[i];
            //Debug.Log(componentStates[i].dataEntity);
            NativeArray<int> values = EntityManager.GetFixedArray<int>(componentStates[i].dataEntity);
            for (int j = 0; j < values.Length; j+=2) {
                (networkMemberInfos[j/2] as NetworkMemberInfo<T>).SetValue(ref component, values[j], values[j+1], Time.deltaTime, DeltaTimeMessage);
            }
            components[i] = component;
        }        
    }


    internal void SetNetworkManager(INetworkManager networkManager) {
        if (this.networkManager != null) {
            this.networkManager.OnEventData -= NetworkManager_OnEventData;
            this.networkManager.OnPlayerLeft -= NetworkManager_OnPlayerLeft;
            this.networkManager.OnDisconnected -= NetworkManager_OnDisconnect;
        }
        this.networkManager = networkManager;

        if (networkManager != null) {
            networkManager.OnEventData += NetworkManager_OnEventData;
            this.networkManager.OnPlayerLeft += NetworkManager_OnPlayerLeft;
            this.networkManager.OnDisconnected += NetworkManager_OnDisconnect;

        }
    }
}