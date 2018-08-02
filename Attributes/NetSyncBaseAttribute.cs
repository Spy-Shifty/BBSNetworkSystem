using System;
using Unity.Mathematics;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public abstract class NetSyncBaseAttribute : Attribute {
    public float LerpSpeed { get; private set; }
    public int Accuracy { get; private set; }
    public float JumpThreshold { get; private set; }
    public bool InitOnly { get; private set; }

    /// <summary>
    /// This attribute signs that the field will be synchronized through the network. 
    /// The containing class also requires the <see cref="NetSyncAttribute"/>
    /// </summary>
    /// <param name="lerpSpeed">use to smoothly interpolate the current value and the latest network value</param>
    /// <param name="reliable">send this field value reliable</param>
    /// <param name="accuracy">will only be applied to float fields</param>
    protected NetSyncBaseAttribute(float lerpSpeed, int accuracy, float jumpThreshold, bool initOnly) {
        LerpSpeed = lerpSpeed;
        Accuracy = (int)math.pow(10, accuracy);
        JumpThreshold = jumpThreshold;
        InitOnly = initOnly;
    }
    
    internal void SetValuesFrom(NetSyncBaseAttribute other) {
        LerpSpeed = other.LerpSpeed;
        Accuracy = other.Accuracy;
        JumpThreshold = other.JumpThreshold;
        InitOnly = other.InitOnly;
    }
}
