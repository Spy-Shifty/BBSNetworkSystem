using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

internal abstract class NetworkMath {
    public abstract int NativeToInteger(object value, ComponentDataFromEntity<NetworkSyncState> networkSyncStateEntities);
    public abstract object IntegerToNative(object fieldValue, int oldValue, int newValue, float deltaTimeFrame, float deltaTimeMessage, NativeHashMap<int, Entity> entityHashMap);
}

internal class NetworkMathInteger : NetworkMath {
    public override object IntegerToNative(object fieldValue, int oldValue, int newValue, float deltaTimeFrame, float deltaTimeMessage, NativeHashMap<int, Entity> entityHashMap) {
        return newValue;
    }

    public override int NativeToInteger(object value, ComponentDataFromEntity<NetworkSyncState> networkSyncStateEntities) {
        return (int)value;
    }
}

internal class NetworkMathBoolean: NetworkMath {
    public override object IntegerToNative(object fieldValue, int oldValue, int newValue, float deltaTimeFrame, float deltaTimeMessage, NativeHashMap<int, Entity> entityHashMap) {
        return (boolean)(newValue == 1);
    }

    public override int NativeToInteger(object value, ComponentDataFromEntity<NetworkSyncState> networkSyncStateEntities) {
        return ((boolean)value) ? 1 : 0;
    }
}

internal class NetworkMathFloat: NetworkMath {
    private const int DefaultAccuracy = 10;
    private const int DefaultInterpolationSpeed = 1;
    private const int DefaultJumpTheshold = 0;

    private readonly int accuracy;
    private readonly float interpolationSpeed;
    private readonly float jumpThreshold;

    public NetworkMathFloat(int accuracy = DefaultAccuracy, float interpolationSpeed = DefaultInterpolationSpeed, float jumpThreshold = DefaultJumpTheshold) {
        this.accuracy = accuracy;
        this.interpolationSpeed = interpolationSpeed;
        this.jumpThreshold = jumpThreshold;
    }

    public override object IntegerToNative(object fieldValue, int oldValue, int newValue, float deltaTimeFrame, float deltaTimeMessage, NativeHashMap<int, Entity> entityHashMap) {
        float oldFloatValue = (float)oldValue / accuracy;
        float newFloatValue = (float)newValue / accuracy;
        float currentValue = (float)fieldValue;

        if (jumpThreshold != 0 && math.abs(newFloatValue - currentValue) > jumpThreshold) {
            return newFloatValue;
        }

        if (Mathf.Approximately(oldFloatValue, newFloatValue)) {
            return newFloatValue;
        }

        float totalDeltaTime = (currentValue - oldFloatValue) / (newFloatValue - oldFloatValue) * deltaTimeMessage;


        float lerpTime = ((totalDeltaTime + deltaTimeFrame) / deltaTimeMessage) * interpolationSpeed;
        if (lerpTime > 1) {
            lerpTime = 1;
        }
        return math.lerp(oldFloatValue, newFloatValue, lerpTime);

    }

    public override int NativeToInteger(object value, ComponentDataFromEntity<NetworkSyncState> networkSyncStateEntities) {
        return (int)((float)value * accuracy);
    }
}

internal class NetworkMathEntity : NetworkMath {
    public override object IntegerToNative(object fieldValue, int oldValue, int newValue, float deltaTimeFrame, float deltaTimeMessage, NativeHashMap<int, Entity> entityHashMap) {
        if(entityHashMap.TryGetValue(newValue, out Entity entity)) {
            return entity;
        }
        return Entity.Null;
    }

    public override int NativeToInteger(object value, ComponentDataFromEntity<NetworkSyncState> networkSyncStateEntities) {
        Entity entity = (Entity)value;
        NetworkSyncState networkSynchState = networkSyncStateEntities[entity];
        return NetworkUtility.GetNetworkEntityHash(networkSynchState.actorId, networkSynchState.networkId);
    }
}


