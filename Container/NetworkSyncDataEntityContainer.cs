using ProtoBuf;
using System.Collections.Generic;

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
