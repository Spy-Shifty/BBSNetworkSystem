using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public static class NetworkSystemExtension {
    public static void SetNetworkManager(this World world, INetworkManager networkManager) {
        world.GetOrCreateManager<NetworkSyncFullStatedSystem>().SetNetworkManager(networkManager);
        world.GetOrCreateManager<NetworkSendSystem>().SetNetworkManager(networkManager);
        world.GetOrCreateManager<NetworkReceiveSystem>().SetNetworkManager(networkManager);
    }
}
