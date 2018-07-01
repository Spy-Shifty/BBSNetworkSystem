using System;
using Unity.Mathematics;

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
