using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(NetworkUpdateGroup))]
[UpdateAfter(typeof(NetworkReceiveSystem))]
public class NetworkSyncFullStatedSystem : ComponentSystem {

    struct AddedEntityData {
        public ComponentDataArray<NetworkSync> networkSyncComponents;
        public ComponentDataArray<NetworkSyncState> networkSyncStateComponents;
        public EntityArray entities;
        public int Length;
    }
    
    [Inject] AddedEntityData addedSyncEntities;

    private static bool LogSendMessages;

    //private readonly NetworkSyncDataContainer ownNetworkSyncDataContainer = new NetworkSyncDataContainer();
    //private readonly Dictionary<Entity, NetworkSyncDataEntityContainer> ownEntityContainerMap = new Dictionary<Entity, NetworkSyncDataEntityContainer>();
    private readonly NetworkSendMessageUtility networkSendMessageUtility = new NetworkSendMessageUtility();


    //private readonly List<NetworkMethodInfo<NetworkSyncFullStatedSystem>> ComponentDataMethods = new List<NetworkMethodInfo<NetworkSyncFullStatedSystem>>();
    private readonly List<NetworkInOutMethodInfo<NetworkSyncFullStatedSystem, Entity, ComponentDataContainer>> GetComponentDataMethods = new List<NetworkInOutMethodInfo<NetworkSyncFullStatedSystem, Entity, ComponentDataContainer>>();

    private NetworkMessageSerializer<NetworkSyncDataContainer> messageSerializer;
    private int lastSend = Environment.TickCount & Int32.MaxValue;
    private INetworkManager networkManager;
    private readonly List<int> jonedPlayer = new List<int>();
    private readonly ReflectionUtility reflectionUtility = new ReflectionUtility();

    protected override void OnCreateManager(int capacity) {
        messageSerializer = new NetworkMessageSerializer<NetworkSyncDataContainer>();
        ComponentType[] componentTypes = reflectionUtility.ComponentTypes;
        GetComponentGroup(typeof(NetworkSync));

        Type networkSystemType = typeof(NetworkSyncFullStatedSystem);
        for (int i = 0; i < componentTypes.Length; i++) {
            GetComponentDataMethods.Add(
                new NetworkInOutMethodInfo<NetworkSyncFullStatedSystem, Entity, ComponentDataContainer>(networkSystemType
                    .GetMethod("GetComponentData", BindingFlags.Instance | BindingFlags.NonPublic)
                    .MakeGenericMethod(componentTypes[i].GetManagedType())));
        }
    }

    protected override void OnDestroyManager() {
        messageSerializer.Dispose();
    }

    private void NetworkManager_OnPlayerJoined(int player) {
        if(player != networkManager.LocalPlayerID) {
            jonedPlayer.Add(player);
        }
    }

    private void NetworkManager_OnPlayerLeft(int player) {
        jonedPlayer.Remove(player);
    }


    protected override void OnUpdate() {
        if (!networkManager.IsConnectedAndReady) {
            return;
        }
        if (jonedPlayer.Count == 0 || !networkManager.IsMaster) {
            jonedPlayer.Clear();
            return;
        }

        Entities();
        
        SendData();
        jonedPlayer.Clear();
    }

    private void Entities() {
        EntityArray entities = addedSyncEntities.entities;
        ComponentDataArray<NetworkSync> networkSyncs = addedSyncEntities.networkSyncComponents;

        for (int i = 0; i < entities.Length; i++) {
            int instanceId = networkSyncs[i].instanceId;
            NetworkSyncState component = new NetworkSyncState() {
                actorId = networkManager.LocalPlayerID,
                networkId = networkManager.GetNetworkId(),
            };
            Entity entity = entities[i];
            NetworkEntityData networkEntityData = new NetworkEntityData {
                InstanceId = networkSyncs[i].instanceId,

                NetworkSyncEntity = new NetworkSyncEntity {
                    ActorId = component.actorId,
                    NetworkId = component.networkId,
                }
            };

            for (int j = 0; j < GetComponentDataMethods.Count; j++) {
                if (GetComponentDataMethods[j].Invoke(this, ref entity, out ComponentDataContainer componentData)) {
                    networkEntityData.ComponentData.Add(componentData);
                }
            }

            networkSendMessageUtility.AddEntity(networkEntityData);
        }
    }

    private bool GetComponentData<T>(ref Entity entity, out ComponentDataContainer componentDataContainer) where T : struct, IComponentData {
        componentDataContainer = null;
        if (EntityManager.HasComponent<T>(entity)) {
            ComponentType componentType = ComponentType.Create<T>();
            int numberOfMembers = reflectionUtility.GetNumberOfMembers(componentType.GetManagedType());

            T component = EntityManager.GetComponentData<T>(entity);
            NetworkMemberInfo[] networkMemberInfos = reflectionUtility.GetNetworkMemberInfo(componentType);
            List<MemberDataContainer> memberDataContainers = new List<MemberDataContainer>();
            for (int i = 0; i < numberOfMembers; i++) {
                memberDataContainers.Add(new MemberDataContainer() {
                    MemberId = i,
                    Data = (networkMemberInfos[i] as NetworkMemberInfo<T>).GetValue(component)
                });
            }


            componentDataContainer = new ComponentDataContainer() {
                ComponentTypeId = reflectionUtility.GetComponentTypeID(componentType),
                MemberData = memberDataContainers
            };
            return true;
        }
        return false;
    }

    private void SendData() {
        NetworkEventOptions networkEventOptions = new NetworkEventOptions {
            TargetActors = jonedPlayer.ToArray(),
            Receiver = NetworkReceiverGroup.Target,
        };
        if (LogSendMessages) {
            Debug.Log("SendFullState:\n" + NetworkMessageUtility.ToString(networkSendMessageUtility.DataContainer));
        }
        networkManager.SendMessage(NetworkEvents.DataSync, messageSerializer.Serialize(networkSendMessageUtility.DataContainer), true, networkEventOptions);
        networkSendMessageUtility.Reset();
    }

    internal void SetNetworkManager(INetworkManager networkManager) {
        if (this.networkManager != null) {
            this.networkManager.OnPlayerJoined -= NetworkManager_OnPlayerJoined;
            this.networkManager.OnPlayerLeft -= NetworkManager_OnPlayerLeft;
        }
        this.networkManager = networkManager;

        if (this.networkManager != null) {
            networkManager.OnPlayerJoined += NetworkManager_OnPlayerJoined;
            networkManager.OnPlayerLeft += NetworkManager_OnPlayerLeft;
        }
    }
}
