# BBSNetworkSystem
Generic network system for Unity Entity Component System. Primary developed for the Photon Realtime Network Engine. It can can also be used by any other network solution.

This is an easy to use Network System. No need to handle how to sync your member. Just tell the system through attributes, what kind of entities, components and members needs to be synchronized. 


# Basics
To synchronize an entity through the network, it will require the NetworkSync component attached to it. This component holds a instanceId, which is an unique identifier that discripes the overall type of an entity. It is used together with the NetworkEntityFactory and NetworkEntityFactoryMethods to create the remote entity. The NetworkEntityFactory can be used to create entities with "offline" component. for e.g. if you require different components for the remote entity instance.


```csharp
[NetworkEntityFactory] // this is a NetworkEntityFactory
public static class EntityFactory {

    [NetworkEntityFactoryMethod(1)] //this is a NetworkEntityFactoryMethod for entities with the NetworkSync.instanceId = 1
    public static Entity CreateNetPlayer(EntityManager entityManager) {
        GameObject gameObject = GameObject.Instantiate(GameSettings.Instance.NetworkPlayerPrefab); // instantiate the prefab
        return gameObject.GetComponent<GameObjectEntity>().Entity;
    }
}
```

To synchonize an component through the network attach the NetSyc attribute to the component. This attribute handles Adding and removing of an Component through the network. To synchonize component values, you have to add NetSyncMember to it. 

```csharp
[NetSync] // sync the component through the network
public struct Health : IComponentData {

    [NetSyncMember] //sync the value through the network
    public int value;
}


[NetSync] // sync the component through the network
public struct Position : IComponentData {

    [NetSyncMember(lerpSpeed: 0.9f, jumpThreshold: 0)] //sync the value through the network 
    [NetSyncSubMember("x")] // used to synchronize the x values of the vector3 
    [NetSyncSubMember("y")] // used to synchronize the y values of the vector3 
    [NetSyncSubMember("z")] // used to synchronize the z values of the vector3 
    public Vector3 Value;
}
```

# Components
## NetworktOwner
```csharp
public struct NetworktOwner : IComponentData { }
```

This component is used to identify if the current entity is owned by my self.
It will be added and removed automatically by the NetworkSystem. 
Don't add this component manually!!!

## NetworkSync
```csharp
public struct NetworkSync : IComponentData {
    public int instanceId;
}
```

This component requires each entity which should be synchronized with the network.
The instanceId member is an unique identifier and represents an specific type of entity. It is used to identify which method of the NetworkEntityFactory is used to create this entity on the remote client.

Take a look into the [NetworkEntityFactory] and [NetworkEntityFactoryMethod] section to get more details.


# Attributes
## [NetSync]
This attribute ensures that the component will be added and removed on the remote instance.
It is also required if you want to share component member values. Attach a NetSync attribute to each component that should be shared with the network...

```csharp
[NetSync] // sync the component through the network
public struct Health : IComponentData {
    [NetSyncMember] //sync the value through the network
    public int value;
}
```

## [NetSyncMember]
This attribute ensures that the component member will be synchronized through the network. Supported types are boolean, integer and float. Structs like Quaternion and Vecor3 can be synchronized with the NetSyncSubMember attribute.

### Parameter (used floatingpoint only):

#### LerpDamp: 
this is used to damp the interpolation time between the old received value and the new received value. This happens in a fixed intervall of 100ms which is the sendrate of the system. To counteract latency you can use this value to archive a smoother damp. E.g. a value of 0.9 will stretch the time till the new recveived value will be reached. Because reduce the deltatime of the current frame.
The default value is 1f;
```csharp
math.lerp(oldFloatValue, newFloatValue, lerpTime * LerpDamp);
```

#### Accuray: 
Each synchronized value will be serialized as Integer value. Accuracy will define the number of decimal in the integer format by multiplying the Floatingpoint value with 10^Accuray. So a accuray of 2 means that only the 2 digits after the comma will be transmitted. The default value is 2.
e.g. 2.4567f => 245 => 2.45f


#### JumpThreshold: 
If the difference between the real value and the latest received value is greater than the value of the JumpThreshold, than the latest received value will be instantly assigned to the real value. A value of 0 means no jumpThreshold just interpolation. The default value is 0

#### InitOnly:
Synchronizeation only happens on adding component. Later changes won't be synchronized anymore. The default value is False

```csharp
[NetSync] // sync the component through the network
public struct Health : IComponentData {     
    [NetSyncMember]
    public int value;
}

[NetSync]
public struct Position : IComponentData {
    [NetSyncMember(lerpDamp: 0.9f, jumpThreshold: 0)]
    [NetSyncSubMember("x")]
    [NetSyncSubMember("y")]
    [NetSyncSubMember("z")]
    public Vector3 Value;
}
```

## NetSyncSubMember]
To synchronize structur values like Vector3 and Quaternion or any other custom type, use NetSyncSubMember attribute.
The NetSyncSubMember only works in combination with the NetSyncMember attribute and has the same parameter. Additionally it has a MemberName attribute which defines what inner member of the structur should be synchronized.

You can define LerpDamp, JumpThreshold and Accuracy globaly for all NetSyncSubMember by the NetSyncMember attribute.
You can also define each NetSyncSubMember independently from each other.

Hint: if you override one value of an NetSyncSubMember you have to assign the other values to or the defaults will be assigned to that NetSyncSubMember

```csharp
[NetSync]
public struct Position : IComponentData {
    [NetSyncMember(lerpDamp: 0.9f, jumpThreshold: 0)]
    [NetSyncSubMember("x")]
    [NetSyncSubMember("z")]
    public Vector3 Value;
}

[NetSync]
public struct Position : IComponentData {
    [NetSyncMember(lerpDamp: 0.9f, jumpThreshold: 0)]
    [NetSyncSubMember("x")]
    [NetSyncSubMember("y")]
    [NetSyncSubMember("z")]
    public Vector3 Value;
}

[NetSync]
public struct Position : IComponentData {
    [NetSyncMember(lerpDamp: 0.9f, jumpThreshold: 3, accuray: 2)]
    [NetSyncSubMember("x")]
    [NetSyncSubMember("y", lerpDamp: 0.9f, jumpThreshold: 3, accuray: 1)]
    [NetSyncSubMember("z")]
    public Vector3 Value;
}
```


## [NetworkEntityFactory]
The NetworkEntityFactory attribute mark a class as a EntityFactory. An EntityFactory is used to create entities which will be synchronized through the network. The EntityFactory enables you to add additional Components to the synchronized entity which will not be synchronized through the network.   

## [NetworkEntityFactoryMethod]
In additionally to the NetworkEntityFactory the NetworkEntityFactoryMethod defines the instantation method for a specific synchronized entity. It will called on the remote client each time an entity with a NetSync component was created.

#### InstanceId:
The InstanceId parameter is used to identify which method should be used for the specific NetSync component of the created entity.

```csharp
[NetworkEntityFactory] // this is a NetworkEntityFactory
public static class EntityFactory {

    [NetworkEntityFactoryMethod(1)] //this is a NetworkEntityFactoryMethod for entities with the NetworkSync.instanceId = 1
    public static Entity CreateNetPlayer(EntityManager entityManager) {
        GameObject gameObject = GameObject.Instantiate(GameSettings.Instance.NetworkPlayerPrefab); // instantiate the prefab
        return gameObject.GetComponent<GameObjectEntity>().Entity;
    }
}
```


# NetworkManager
The NetworkManager is used as interface to communicate with the Network. 

```csharp
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
```
This is the main interface to the specific network solution. This was mainly designed for Photon Realtime Network Engine

LocalPlayerID: unique identifier of the local player
IsMaster: Is the client a master client. Only one client per room can be a master client.
IsConnectedAndReady: Is true if we are connected and a LocalPlayerId is assigned and the network is ready for send and receive data
OnEventData: will be called if a message has received.
OnPlayerJoined: will be called if a player has joined the game
OnPlayerLeft: will be called if a player has left the game
OnDisconnected: will be calld if we have been disconnected from the game
Update: Read all data from the message buffer. Should fire OnEventData
SendMessage: sends a message with the given options
GetNetworkId(): should generate a localy unique identifier, is used to identify instances of entities through the network.  



```csharp
public enum NetworkReceiverGroup {
    Others,
    MasterClient,
    Target,
}

public struct NetworkEventOptions {
    public NetworkReceiverGroup Receiver;
    public int[] TargetActors;
}
```
Eventoption to filter packages for specific receivers
NetworkReceiverGroup: send only to Other clients, Master client or to a specific clients
TargetActors: array of clientIds default is null

To assign the Networkmanager to the Systems just call
```csharp
World.Active.SetNetworkManager(networkManager);
```
this method will automatically setup all systems with the network manager



# Networking in detail

## Authority and anti cheat mechanism
Only the client that owns the entity (NetworkOwner component attached to it) has the authority to change the state of the entity. Therefore interaction with entities, that you not own, will always be handled on the remote part, by the owner of that entitie.
E.g. if you hit an entity, the hit information and threfore the damage applied to the hitten entity will only be set by the owner of that entity.
More clearly: 
 * Client A's entity shoots on the entity owned by client B. 
 * Client B receive the shoot information and applies the damage to it's entity. (which he owns)
 * Client B will send the new health or dead state to client A.

We don't offer a mechanism against cheating at the moment. This will may be a part of a future release.

## Hostmigration
Hostmigration should be simple. Each client knows the full state of all clients. So if the current master client leaves the game, a new masterclient should be assigned and propageted.
Well there may a issue of package lost especially for rarely changed members or components. This won't handled at the moment.
 

## Handling joining players in running game
Joining a player in a running game is no problem. The master client will send the full world state to that client. 
 
 
## Message Size and the Number of Packages
The NetworkSystem was created to reduce the size and the number of packages send through the network. Basically because of the use of Photon Network Engine and to support mobile devices and lots of players.

Therfore we don't send any message if there is nothing to send. We only send changes to the last transmitted state of an component or entity (delta compression). We collect all changes to one big update message,so that we only send one message per intervall (100ms).
We use googles Protobuf protocol to also reduce the size of the message. So that only those bytes of an integer will be send, which are relevant to its value (leading zeros won't be send). 

To also reduce the number of messages send through the network, we use the concept of Masterclient. All packages will send to the Masterclient and propagate all changes of each client in one message to all other clients. This will reduce the number of packages from N*N to 2*(N-1)+N where N is the number of clients.

## Representation of synchronized components and their values
The system adds to each synchronized component a specific component state component and an separate entity. This entity yields the type of the synchronized component and an array of integer values, which represents the network view of that component. This integer array holds, in case of the remote entity, the last received values and the latest received value of the component members. In the other case, it holds the last send state, so that we can safely recognize changed of that component member since the last send intervall.

## Performance
The overall performance wasn't tested in a real project jet. It's still in an early alpha.
We use reflection methods to achive an simple and easy to use framework. Reflection is generaly 10 - 40 times slower than directl call. To counteract this performance issue, we create delegates for each getter and setter and cashing them. A chashed delegate isn't fast as a direct call, but it is much faster as using the reflection method. Therefore calling a delegate is only ~2 times slower than a direct call.

This values may depends on the computer and the compiler.

## Donate
If you want to support us and the development of the NetworkSystem! 

[![paypal](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=GTWT5GJSL7CEQ)

Thank you very much! 







