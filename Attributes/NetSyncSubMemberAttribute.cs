using System;
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
