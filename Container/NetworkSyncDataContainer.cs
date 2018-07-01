using ProtoBuf;
using System.Collections.Generic;

[ProtoContract]
public class NetworkSyncDataContainer {

    [ProtoMember(1, DataFormat = DataFormat.ZigZag)]
    public List<NetworkSyncDataEntityContainer> NetworkSyncDataEntities = new List<NetworkSyncDataEntityContainer>(10);

    [ProtoMember(2, DataFormat = DataFormat.ZigZag)]
    public List<NetworkEntityData> AddedNetworkSyncEntities = new List<NetworkEntityData>(10);

    [ProtoMember(3 ,DataFormat = DataFormat.ZigZag)]
    public List<NetworkSyncEntity> RemovedNetworkSyncEntities = new List<NetworkSyncEntity>(10);    
}
