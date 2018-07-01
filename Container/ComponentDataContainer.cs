using ProtoBuf;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ProtoContract]
public class ComponentDataContainer {
    [ProtoMember(1, IsPacked = true, DataFormat = DataFormat.ZigZag)]
    public int ComponentTypeId;

    [ProtoMember(2, DataFormat = DataFormat.ZigZag)]
    public List<MemberDataContainer> MemberData = new List<MemberDataContainer>();
}
