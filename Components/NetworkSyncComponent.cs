using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;


public enum Authority {
    Client,
    Master,
    Scene,
}

[Serializable]
public struct NetworkSync : IComponentData {
    public int instanceId;
    public Authority authority;
}


public class NetworkSyncComponent : ComponentDataWrapper<NetworkSync> { }
