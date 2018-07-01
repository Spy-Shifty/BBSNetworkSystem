using System;
/// <summary>
/// Requires signature of: 
/// static Entity (EntityManager)
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class NetworkEntityFactoryMethodAttribute : Attribute {
    public readonly int InstanceId;
    public NetworkEntityFactoryMethodAttribute(int instanceId) {
        InstanceId = instanceId;
    }
}
