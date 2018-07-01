using ProtoBuf;

[ProtoContract]
public struct NetworkSyncEntity {
    [ProtoMember(1, IsPacked = true, DataFormat = DataFormat.ZigZag)]
    public int NetworkId;

    [ProtoMember(2, IsPacked = true, DataFormat = DataFormat.ZigZag)]
    public int ActorId;
}
