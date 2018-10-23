using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

//public struct NetworkMessageEvent : ISharedComponentData {
//    public byte id;
//}

//public struct NetworkSendEvent : IComponentData {
//    public boolean reliable;
//    public EventCaching eventCaching;
//    public byte group;
//    public byte channel;
//}

//public struct NetworkReceiveEvent : IComponentData {
//    public int senderId;
//}

//public struct NetworkPlayerJoined : IComponentData { public int id; }
//public struct NetworkPlayerLeft : IComponentData { public int id; }
//public struct NetworkRoomJoined : IComponentData { }
//public struct NetworkRoomLeft : IComponentData { }
//public struct NetworkJoinedLobby : IComponentData { }
//public struct NetworkJoinedGame : IComponentData { }
//public struct NetworkConnetedToGameServer : IComponentData { }
//public struct NetworkConnetedToMasterServer : IComponentData { }
//public struct NetworkDisconnetedFromMasterServer : IComponentData { }
//public struct NetworkDisconneted : IComponentData { }



//internal struct NetworkSyncState :ISystemStateComponentData {
//internal int networkId;
//internal int actorId;
//}

//internal struct NetworkMemberState<T> : ISystemStateComponentData { }


#region Public

public struct NetworktAuthority : IComponentData { }

#endregion

#region Internal

public struct NetworkValue : IBufferElementData {
    public int Value;
}

public struct NetworkSyncState : ISystemStateComponentData {
    public int networkId;
    public int actorId;
}

public struct NetworkComponentState<T> : ISystemStateComponentData {
    public Entity dataEntity;
}

public struct NetworkComponentData<T> : IComponentData { }

public struct NetworkComponentEntityReference : IComponentData {
    public int Index;
    public int Version;
}

#endregion