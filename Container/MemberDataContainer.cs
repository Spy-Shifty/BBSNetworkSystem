using ProtoBuf;

[ProtoContract]
public struct MemberDataContainer {
    [ProtoMember(1, IsPacked = true, DataFormat = DataFormat.ZigZag)]
    public int MemberId;

    [ProtoMember(2, IsPacked = true, DataFormat = DataFormat.ZigZag)]
    public int Data;
}
