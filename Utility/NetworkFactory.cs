using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

internal class NetworkFactory:IDisposable {

    internal readonly EntityArchetype NetworkReceiveMessageArchetype;


    //internal readonly EntityArchetype NetworkPlayerJoinedArchetype;
    //internal readonly EntityArchetype NetworkPlayerLeftArchetype;
    //internal readonly EntityArchetype NetworkRoomJoinedArchetype;
    //internal readonly EntityArchetype NetworkRoomLeftArchetype;
    //internal readonly EntityArchetype NetworkJoinedLobbyArchetype;
    //internal readonly EntityArchetype NetworkJoinedGameArchetype;
    //internal readonly EntityArchetype NetworkConnetedToGameServerArchetype;
    //internal readonly EntityArchetype NetworkConnetedToMasterServerArchetype;
    //internal readonly EntityArchetype NetworkDisconnetedFromMasterServerArchetype;
    //internal readonly EntityArchetype NetworkDisconnetedArchetype;

    

    internal readonly World NetworkWorld;
    internal readonly EntityManager NetworkEntityManager;
    internal readonly EntityManager EntityManager;

    public NetworkFactory(EntityManager entityManager) {
        NetworkWorld = new World("NetworkWorld");
        NetworkEntityManager = NetworkWorld.GetOrCreateManager<EntityManager>();
        EntityManager = entityManager;

        //NetworkReceiveMessageArchetype = EntityManager.CreateArchetype(typeof(NetworkMessageEvent), typeof(NetworkReceiveEvent));
        //NetworkPlayerJoinedArchetype = EntityManager.CreateArchetype(typeof(NetworkPlayerJoined));
        //NetworkPlayerLeftArchetype = EntityManager.CreateArchetype(typeof(NetworkPlayerLeft));
        //NetworkRoomJoinedArchetype = EntityManager.CreateArchetype(typeof(NetworkRoomJoined));
        //NetworkRoomLeftArchetype = EntityManager.CreateArchetype(typeof(NetworkRoomLeft));
        //NetworkJoinedLobbyArchetype = EntityManager.CreateArchetype(typeof(NetworkJoinedLobby));
        //NetworkJoinedGameArchetype = EntityManager.CreateArchetype(typeof(NetworkJoinedGame));
        //NetworkConnetedToGameServerArchetype = EntityManager.CreateArchetype(typeof(NetworkConnetedToGameServer));
        //NetworkConnetedToMasterServerArchetype = EntityManager.CreateArchetype(typeof(NetworkConnetedToMasterServer));
        //NetworkDisconnetedFromMasterServerArchetype = EntityManager.CreateArchetype(typeof(NetworkDisconnetedFromMasterServer));
        //NetworkDisconnetedArchetype = EntityManager.CreateArchetype(typeof(NetworkDisconneted));

    }

    public void Dispose() {
        NetworkWorld.Dispose();
    }

    internal Entity CreateNetworkComponentData<T>(Entity entityReference, int numberOfMembers) {
        Entity entity = NetworkEntityManager.CreateEntity(
            ComponentType.Create<NetworkComponentData<T>>(),
            ComponentType.Create<NetworkComponentEntityReference>()
            //ComponentType.FixedArray(typeof(int), numberOfMembers*2)); // 2x because of history
            );
        NetworkEntityManager.AddBuffer<NetworkValue>(entity);
        NetworkEntityManager.GetBuffer<NetworkValue>(entity).ResizeUninitialized(numberOfMembers * 2); // 2x because of history
        NetworkEntityManager.SetComponentData(entity, new NetworkComponentEntityReference { Index = entityReference.Index, Version = entityReference.Version });

        return entity;
    }


    internal void FlushNetworkManager() {
        EntityManager.MoveEntitiesFrom(NetworkEntityManager);
    }
}
