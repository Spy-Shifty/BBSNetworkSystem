using System.Collections.Generic;
using Unity.Entities;

public class NetworkSendMessageUtility {
    public readonly NetworkSyncDataContainer DataContainer = new NetworkSyncDataContainer();
    private readonly Dictionary<Entity, NetworkSyncDataEntityContainer> EntityContainerMap = new Dictionary<Entity, NetworkSyncDataEntityContainer>();

    public void AddEntity(NetworkEntityData networkEntityData) {
        DataContainer.AddedNetworkSyncEntities.Add(networkEntityData);
    }

    public void RemoveEntity(NetworkSyncEntity networkSyncEntity) {
        DataContainer.RemovedNetworkSyncEntities.Add(networkSyncEntity);
    }

    public void AddComponent(Entity entity, int actorId, int networkId, ComponentDataContainer componentData) {
        if (!EntityContainerMap.TryGetValue(entity, out NetworkSyncDataEntityContainer dataContainer)) {
            dataContainer = new NetworkSyncDataEntityContainer() {
                NetworkSyncEntity = new NetworkSyncEntity() {
                    ActorId = actorId,
                    NetworkId = networkId,
                }
            };
            DataContainer.NetworkSyncDataEntities.Add(dataContainer);
            EntityContainerMap.Add(entity, dataContainer);
        }
        dataContainer.AddedComponents.Add(componentData);
    }

    public void RemoveComponent(Entity entity, int actorId, int networkId, int componentId) {
        if (!EntityContainerMap.TryGetValue(entity, out NetworkSyncDataEntityContainer dataContainer)) {
            dataContainer = new NetworkSyncDataEntityContainer() {
                NetworkSyncEntity = new NetworkSyncEntity() {
                    ActorId = actorId,
                    NetworkId = networkId,
                }
            };
            DataContainer.NetworkSyncDataEntities.Add(dataContainer);
            EntityContainerMap.Add(entity, dataContainer);
        }
        dataContainer.RemovedComponents.Add(componentId);
    }

    public void SetComponentData(Entity entity, int actorId, int networkId, ComponentDataContainer componentDataContainer) {
        if (!EntityContainerMap.TryGetValue(entity, out NetworkSyncDataEntityContainer dataContainer)) {
            dataContainer = new NetworkSyncDataEntityContainer() {
                NetworkSyncEntity = new NetworkSyncEntity() {
                    ActorId = actorId,
                    NetworkId = networkId,
                }
            };
            DataContainer.NetworkSyncDataEntities.Add(dataContainer);
            EntityContainerMap.Add(entity, dataContainer);
        }
        dataContainer.ComponentData.Add(componentDataContainer);
    }

    public void AddComponents(Entity entity, int actorId, int networkId, List<ComponentDataContainer> componentIds) {
        if (!EntityContainerMap.TryGetValue(entity, out NetworkSyncDataEntityContainer dataContainer)) {
            dataContainer = new NetworkSyncDataEntityContainer() {
                NetworkSyncEntity = new NetworkSyncEntity() {
                    ActorId = actorId,
                    NetworkId = networkId,
                }
            };
            DataContainer.NetworkSyncDataEntities.Add(dataContainer);
            EntityContainerMap.Add(entity, dataContainer);
        }
        dataContainer.AddedComponents.AddRange(componentIds);
    }

    public void RemoveComponents(Entity entity, int actorId, int networkId, List<int> componentIds) {
        if (!EntityContainerMap.TryGetValue(entity, out NetworkSyncDataEntityContainer dataContainer)) {
            dataContainer = new NetworkSyncDataEntityContainer() {
                NetworkSyncEntity = new NetworkSyncEntity() {
                    ActorId = actorId,
                    NetworkId = networkId,
                }
            };
            DataContainer.NetworkSyncDataEntities.Add(dataContainer);
            EntityContainerMap.Add(entity, dataContainer);
        }
        dataContainer.RemovedComponents.AddRange(componentIds);
    }

    public void SetComponentData(Entity entity, int actorId, int networkId, List<ComponentDataContainer> componentDataContainers) {
        if (!EntityContainerMap.TryGetValue(entity, out NetworkSyncDataEntityContainer dataContainer)) {
            dataContainer = new NetworkSyncDataEntityContainer() {
                NetworkSyncEntity = new NetworkSyncEntity() {
                    ActorId = actorId,
                    NetworkId = networkId,
                }
            };
            DataContainer.NetworkSyncDataEntities.Add(dataContainer);
            EntityContainerMap.Add(entity, dataContainer);
        }
        dataContainer.ComponentData.AddRange(componentDataContainers);
    }

    public void Reset() {
        DataContainer.AddedNetworkSyncEntities.Clear();
        DataContainer.RemovedNetworkSyncEntities.Clear();
        DataContainer.NetworkSyncDataEntities.Clear();
        EntityContainerMap.Clear();
    }
}
