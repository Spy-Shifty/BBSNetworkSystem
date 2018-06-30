using ProtoBuf;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ProtoContract]
//[ProtoInclude(10, typeof(NetworkSyncDataEntityContainer))]
//[ProtoInclude(11, typeof(NetworkEntityData))]
//[ProtoInclude(12, typeof(NetworkSyncEntity))]
public class NetworkSyncDataContainer {

    [ProtoMember(1, DataFormat = DataFormat.ZigZag)]
    public List<NetworkSyncDataEntityContainer> NetworkSyncDataEntities = new List<NetworkSyncDataEntityContainer>(10);

    [ProtoMember(2, DataFormat = DataFormat.ZigZag)]
    public List<NetworkEntityData> AddedNetworkSyncEntities = new List<NetworkEntityData>(10);

    [ProtoMember(3 ,DataFormat = DataFormat.ZigZag)]
    public List<NetworkSyncEntity> RemovedNetworkSyncEntities = new List<NetworkSyncEntity>(10);    
}

[ProtoContract]
public class NetworkEntityData {
    [ProtoMember(1, IsPacked = true, DataFormat = DataFormat.ZigZag)]
    public int InstanceId;

    [ProtoMember(2, DataFormat = DataFormat.ZigZag)]
    public NetworkSyncEntity NetworkSyncEntity;
    
    [ProtoMember(3, DataFormat = DataFormat.ZigZag)]
    public List<ComponentDataContainer> ComponentData = new List<ComponentDataContainer>(100);
}


[ProtoContract]
public struct NetworkSyncEntity {
    [ProtoMember(1, IsPacked = true, DataFormat = DataFormat.ZigZag)]
    public int NetworkId;

    [ProtoMember(2, IsPacked = true, DataFormat = DataFormat.ZigZag)]
    public int ActorId;
}

[ProtoContract]
public class NetworkSyncDataEntityContainer {
    [ProtoMember(1, DataFormat = DataFormat.ZigZag)]
    public NetworkSyncEntity NetworkSyncEntity;

    [ProtoMember(2, DataFormat = DataFormat.ZigZag)]
    public List<ComponentDataContainer> AddedComponents = new List<ComponentDataContainer>(10);

    [ProtoMember(3, DataFormat = DataFormat.ZigZag)]
    public List<int> RemovedComponents = new List<int>(10);

    [ProtoMember(4, DataFormat = DataFormat.ZigZag)]
    public List<ComponentDataContainer> ComponentData = new List<ComponentDataContainer>(100);
}

[ProtoContract]
public class ComponentDataContainer {
    [ProtoMember(1, IsPacked = true, DataFormat = DataFormat.ZigZag)]
    public int ComponentTypeId;

    [ProtoMember(2, DataFormat = DataFormat.ZigZag)]
    public List<MemberDataContainer> MemberData = new List<MemberDataContainer>();
}

[ProtoContract]
public struct MemberDataContainer {
    [ProtoMember(1, IsPacked = true, DataFormat = DataFormat.ZigZag)]
    public int MemberId;

    [ProtoMember(2, IsPacked = true, DataFormat = DataFormat.ZigZag)]
    public int Data;
}
