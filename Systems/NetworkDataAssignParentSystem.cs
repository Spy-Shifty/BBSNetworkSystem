using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Entities;

[UpdateInGroup(typeof(NetworkUpdateGroup))]
[UpdateAfter(typeof(NetworkSendSystem))]
public class NetworkDataAssignParentSystem : ComponentSystem {

    private readonly List<NetworkMethodInfo<NetworkDataAssignParentSystem>> SetComponentParentMethods = new List<NetworkMethodInfo<NetworkDataAssignParentSystem>>();

    private readonly ReflectionUtility reflectionUtility = new ReflectionUtility();

    protected override void OnCreateManager(int capacity) {
        ComponentType[] componentTypes = reflectionUtility.ComponentTypes;

        Type systemType = typeof(NetworkDataAssignParentSystem);
        for (int i = 0; i < componentTypes.Length; i++) {
            SetComponentParentMethods.Add(
                new NetworkMethodInfo<NetworkDataAssignParentSystem>(systemType
                    .GetMethod("SetComponentParent", BindingFlags.Instance | BindingFlags.NonPublic)
                    .MakeGenericMethod(componentTypes[i].GetManagedType())));

        }
    }

    private void SetComponentParent<T>() {
        var group = GetComponentGroup(ComponentType.ReadOnly<NetworkComponentData<T>>(), ComponentType.Create<NetworkComponentEntityReference>());
        EntityArray entities = group.GetEntityArray();
        ComponentDataArray<NetworkComponentEntityReference>  componentEntityReference = group.GetComponentDataArray<NetworkComponentEntityReference>();
        for (int i = 0; i < entities.Length; i++) {
            Entity entity = entities[i];
            NetworkComponentEntityReference networkComponentEntityReference = componentEntityReference[i];
            Entity referencedEntity = new Entity { Index = networkComponentEntityReference.Index, Version = networkComponentEntityReference.Version };
            PostUpdateCommands.SetComponent(referencedEntity, new NetworkComponentState<T> { dataEntity = entity });
            PostUpdateCommands.RemoveComponent<NetworkComponentEntityReference>(entity);
        }
    }

    protected override void OnUpdate() {
        for (int i = 0; i < SetComponentParentMethods.Count; i++) {
            SetComponentParentMethods[i].Invoke(this);
        }
    }
}
