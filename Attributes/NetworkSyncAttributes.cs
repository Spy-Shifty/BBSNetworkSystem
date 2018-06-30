using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class NetworkEntityFactoryAttribute : Attribute { }

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


[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public abstract class NetSyncBaseAttribute : Attribute {
    public float LerpSpeed { get; private set; }
    //public bool Reliable { get; private set; }
    public int Accuracy { get; private set; }
    public float JumpThreshold { get; private set; }
    /// <summary>
    /// This attribute signs that the field will be synchronized through the network. 
    /// The containing class also requires the <see cref="NetSyncAttribute"/>
    /// </summary>
    /// <param name="lerpSpeed">use to smoothly interpolate the current value and the latest network value</param>
    /// <param name="reliable">send this field value reliable</param>
    /// <param name="accuracy">will only be applied to float fields</param>

    protected NetSyncBaseAttribute(float lerpSpeed, /*bool reliable,*/ int accuracy, float jumpThreshold) {
        LerpSpeed = lerpSpeed;
        //Reliable = reliable;
        Accuracy = (int)math.pow(10, accuracy);
        JumpThreshold = jumpThreshold;
    }



    internal void SetValuesFrom(NetSyncBaseAttribute other) {
        LerpSpeed = other.LerpSpeed;
        //Reliable = other.Reliable;
        Accuracy = other.Accuracy;
        JumpThreshold = other.JumpThreshold;
    }
}

/// <summary>
/// This attribute signs that the field will be synchronized through the network. 
/// The containing class also requires the <see cref="NetSyncAttribute"/>
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class NetSyncMemberAttribute : NetSyncBaseAttribute {
    /// <summary>
    /// This attribute signs that the field will be synchronized through the network. 
    /// The containing class also requires the <see cref="NetSyncAttribute"/>
    /// </summary>
    /// <param name="lerpDamp">use to smoothly interpolate the current value and the latest network value</param>
    //// <param name="reliable">send this field value reliable</param>
    /// <param name="accuracy">will only be applied to float fields</param>    
    public NetSyncMemberAttribute(float lerpDamp = 1, /*bool reliable = false,*/ int accuracy = 2, float jumpThreshold = 0) 
        : base(lerpDamp, /*reliable*/, accuracy, jumpThreshold) { }
}

/// <summary>
/// This attribute signs that a subfield of the current field will be synchronized through the network. 
/// The containing class also requires the <see cref="NetSyncAttribute"/> and <see cref="NetSyncMemberAttribute"/>
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
public sealed class NetSyncSubMemberAttribute : NetSyncBaseAttribute {
    public readonly string MemberName;
    public readonly bool OverriddenValues;
    /// <summary>
    /// This attribute signs that the subfield of the field will be synchronized through the network. 
    /// The containing class also requires the <see cref="NetSyncAttribute"/> and <see cref="NetSyncMemberAttribute"/>
    /// </summary>
    /// <param name="memberName">the name of the subfield</param>
    /// <param name="lerpSpeed">use to smoothly interpolate the current value and the latest network value</param>
    /// <param name="reliable">send this field value reliable</param>
    /// <param name="accuracy">will only be applied to float fields</param>
    public NetSyncSubMemberAttribute(string memberName, float lerpSpeed = 1, /*bool reliable = true,*/ int accuracy = 2, float jumpThreshold = 0) 
        : base(lerpSpeed, /*reliable,*/ accuracy, jumpThreshold) {
        MemberName = memberName;
        OverriddenValues = true;
    }

    public NetSyncSubMemberAttribute(string memberName)
        : base(0, /*false,*/ 0, 0) {
        MemberName = memberName;
        OverriddenValues = false;
    }
}


/// <summary>
/// This attribte enables you to synchronize data through the network. 
/// It's required to all structs that will contains  <see cref="NetSyncMemberAttribute"/>
/// </summary>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class NetSyncAttribute : Attribute { }