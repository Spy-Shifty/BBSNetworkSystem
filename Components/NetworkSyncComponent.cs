using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct NetworkSync : IComponentData {
    public int instanceId;
}


public class NetworkSyncComponent : ComponentDataWrapper<NetworkSync> { }
