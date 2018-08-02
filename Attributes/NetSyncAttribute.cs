using System;
/// <summary>
/// This attribute enables you to synchronize components through the network. 
/// It's required to all structs that will contains  <see cref="NetSyncMemberAttribute"/>
/// </summary>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class NetSyncAttribute : Attribute { }

/// <summary>
/// This attribute enables you to synchronize components through the network.
/// It's required to all structs that will contains <see cref="NetSyncMemberAttribute"/>
/// Use this attribute if you want to synchronize components thats comes from 3rd party.
/// </summary>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class ProxyNetSyncAttribute : Attribute {
    public readonly Type Type;
    /// <summary>
    /// Initializes a new instance of the <see cref="ProxyNetSyncAttribute"/> class.
    /// </summary>
    /// <param name="type">The type of the 3rd party class to synchronize</param>
    public ProxyNetSyncAttribute(Type type) {
        this.Type = type;
    }
}