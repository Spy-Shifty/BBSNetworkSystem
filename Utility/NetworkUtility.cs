using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;

internal class NetworkUtility {
    public static bool CanAssignAuthority(INetworkManager networkManager, Authority authority) {
        return authority == Authority.Client || (networkManager.IsMaster && (authority == Authority.Scene || authority == Authority.Master));
    }


    public static int GetNetworkEntityHash(int actorId, int networkId) {
        return (int)unchecked(math.pow(actorId, networkId));
    }
}

