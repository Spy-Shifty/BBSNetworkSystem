# BBSNetworkSystem
Generic network system for Unity Entity Component System. Primary developed for Photon network, but can be any network solution.

# Basics
To synchronize an entity through the network, it will require the NetworkSync component attached to it. This component holds a instanceId, which is an unique identifier that discripes the overall type of an entity. It is used together with the NetworkEntityFactory and NetworkEntityFactoryMethods to create the remote entity. The NetworkEntityFactory can be used to create entities with "offline" component. for e.g. if you require different components for the remote entity instance.

```
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

```
[NetSync] // sync the component through the network
public struct Health : IComponentData {

    [NetSyncMember] //sync the value through the network
    public float value;
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


# Attributes
[NetSync]
This attribute ensures that the component will be added and removed on the remote instance.
It is also required if you want to share component member values. Attach a NetSync attribute to each component that should be shared with the network...

```
[NetSync] // sync the component through the network
public struct Health : IComponentData {
     ///...
}
```

[NetSyncMember]

[NetSyncSubMember]

[NetworkEntityFactory]
Mark a class as a entity factory. A NetworkEntityFactory class is used to instantiate Entiy through the network.


[NetworkEntityFactoryMethod]



