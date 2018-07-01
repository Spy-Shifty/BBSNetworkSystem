using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

internal static class NetworkMathOld {
    const int DefaultAccuracy = 10;
    static readonly Type intType = typeof(int);
    static readonly Type floatType = typeof(float);
    static readonly Type boolType = typeof(bool);
    static readonly Type booleanType = typeof(boolean);


    public static int NativeToInteger(Type type, object value, int accuracy = DefaultAccuracy) {
        if (type == intType) {
            return (int)value;
        } else if (type == floatType) {
            return (int)((float)value * accuracy);
        } else if (type == boolType) {
            return ((bool)value) ? 1 : 0;
        } else if (type == booleanType) {
            return ((boolean)value) ? 1 : 0;
        } else {
            throw new NotSupportedException(type.ToString());
        }
    }

    public static object IntegerToNative(Type type, object fieldValue, int value, int accuracy = DefaultAccuracy, float interpolation = 1, float jumpThreshold = 0) {
        if (type == intType) {
            return value;
        } else if (type == floatType) {
            float newValue = (float)value / accuracy;
            float currentValue = (float)fieldValue;
            if (jumpThreshold != 0 && math.greaterThan(math.abs(newValue - currentValue), jumpThreshold)) {
                return newValue;
            }
            return math.lerp(currentValue, newValue, interpolation);
        } else if (type == boolType || type == booleanType) {
            return (value == 1);
        } else {
            throw new NotSupportedException(type.ToString());
        }
    }

    public static object IntegerToNative(Type type, int value, int accuracy = DefaultAccuracy) {
        if (type == intType) {
            return value;
        } else if (type == floatType) {
            return (float)value / accuracy;
        } else if (type == boolType || type == booleanType) {
            return (value == 1);
        } else {
            throw new NotSupportedException(type.ToString());
        }
    }

    //-----------------------------------------------------------
    public static int NativeToInteger(int value, int accuracy = DefaultAccuracy) {
        return value;
    }

    public static int NativeToInteger(float value, int accuracy = DefaultAccuracy) {
        return (int)(value * accuracy);
    }

    public static int NativeToInteger(bool value, int accuracy = DefaultAccuracy) {
        return value ? 1 : 0;
    }

    public static int NativeToInteger(boolean value, int accuracy = DefaultAccuracy) {
        return value ? 1 : 0;
    }
}


internal abstract class NetworkMath {
    public abstract int NativeToInteger(object value);
    public abstract object IntegerToNative(object fieldValue, int oldValue, int newValue, float deltaTimeFrame, float deltaTimeMessage);
}

internal class NetworkMathInteger : NetworkMath {
    public override object IntegerToNative(object fieldValue, int oldValue, int newValue, float deltaTimeFrame, float deltaTimeMessage) {
        return newValue;
    }

    public override int NativeToInteger(object value) {
        return (int)value;
    }
}

internal class NetworkMathBoolean: NetworkMath {
    public override object IntegerToNative(object fieldValue, int oldValue, int newValue, float deltaTimeFrame, float deltaTimeMessage) {
        return (boolean)(newValue == 1);
    }

    public override int NativeToInteger(object value) {
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

    public override object IntegerToNative(object fieldValue, int oldValue, int newValue, float deltaTimeFrame, float deltaTimeMessage) {
        float oldFloatValue = (float)oldValue / accuracy;
        float newFloatValue = (float)newValue / accuracy;
        float currentValue = (float)fieldValue;

        if (jumpThreshold != 0 && math.greaterThan(math.abs(newFloatValue - currentValue), jumpThreshold)) {
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

    public override int NativeToInteger(object value) {
        return (int)((float)value * accuracy);
    }
}



