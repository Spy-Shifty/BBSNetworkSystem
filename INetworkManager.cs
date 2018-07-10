using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void EventDataDelegate(byte eventId, int playerId, object data);
public delegate void PlayerJoinedDelegate(int playerId);
public delegate void PlayerLeftDelegate(int playerId);

public interface INetworkManager {
    int LocalPlayerID { get; }
    bool IsMaster { get; }
    bool IsConnectedAndReady { get; }

    event EventDataDelegate OnEventData;
    event PlayerJoinedDelegate OnPlayerJoined;
    event PlayerLeftDelegate OnPlayerLeft;
    event Action OnDisconnected;

    void Update();
    void SendMessage(byte eventId, byte[] data, bool reliable, NetworkEventOptions networkEventOptions);
    int GetNetworkId();
}

public struct NetworkEventOptions {
    public NetworkReceiverGroup Receiver;
    public int[] TargetActors;
}

public enum NetworkReceiverGroup {
    Others,
    MasterClient,
    Target,
}