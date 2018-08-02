using System;
using System.Reflection;

internal abstract class NetworkMemberInfo { }

internal abstract class NetworkMemberInfo<OBJ> : NetworkMemberInfo {
    public readonly NetSyncBaseAttribute netSyncOptions;
    public NetworkMemberInfo(NetSyncBaseAttribute netSyncOptions) {
        this.netSyncOptions = netSyncOptions;
    }

    public abstract void SetValue(ref OBJ obj, int oldValue, int newValue, float deltaTimeFrame, float deltaTimeMessage);
    public abstract int GetValue(OBJ obj);
}

internal sealed class NetworkMemberInfo<OBJ, TYPE> : NetworkMemberInfo<OBJ> {
    //private readonly MemberInfo memberInfo;
    private readonly NetworkMemberInfo parent;

    public readonly RefFunc<OBJ, TYPE> GetValueDelegate;
    public readonly RefAction<OBJ, TYPE> SetValueDelegate;

    private NetworkMath networkMath;

    public NetworkMemberInfo(MemberInfo memberInfo, NetSyncBaseAttribute netSyncOptions) : base(netSyncOptions) {
        //Debug.Log(typeof(OBJ) + " --- " + typeof(TYPE));
        switch (memberInfo.MemberType) {
            case MemberTypes.Field:
                FieldInfo fieldInfo = (FieldInfo)memberInfo;
                GetValueDelegate = NetworkMemberInfoUtility.CreateGetter<OBJ, TYPE>(fieldInfo);
                SetValueDelegate = NetworkMemberInfoUtility.CreateSetter<OBJ, TYPE>(fieldInfo);
                break;

            case MemberTypes.Property:
                PropertyInfo propertyInfo = (PropertyInfo)memberInfo;
                GetValueDelegate = (RefFunc<OBJ, TYPE>)Delegate.CreateDelegate(typeof(Func<OBJ, TYPE>), propertyInfo.GetGetMethod());
                SetValueDelegate = (RefAction<OBJ, TYPE>)Delegate.CreateDelegate(typeof(Action<OBJ, TYPE>), propertyInfo.GetSetMethod());
                break;

            default:
                throw new NotImplementedException(memberInfo.MemberType.ToString());
        }

        Type typeType = typeof(TYPE);
        if (typeType == typeof(int)) {
            networkMath = new NetworkMathInteger();
        } else if (typeType == typeof(float)) {
            networkMath = new NetworkMathFloat(netSyncOptions.Accuracy, netSyncOptions.LerpSpeed, netSyncOptions.JumpThreshold);
        } else if (typeType == typeof(boolean)) {
            networkMath = new NetworkMathBoolean();
        }
    }

    public NetworkMemberInfo(MemberInfo memberInfo, NetworkMemberInfo parent, NetSyncBaseAttribute netSyncOptions) : base(netSyncOptions) {
        this.parent = parent;
    }

    public override void SetValue(ref OBJ obj, int oldValue, int newValue, float deltaTimeFrame, float deltaTimeMessage) {
        SetValueDelegate(ref obj, (TYPE)networkMath.IntegerToNative(GetValueDelegate(ref obj), oldValue, newValue, deltaTimeFrame, deltaTimeMessage));
    }

    public override int GetValue(OBJ obj) {
        return networkMath.NativeToInteger(GetValueDelegate(ref obj));
    }
}

internal sealed class NetworkMemberInfo<Parent_OBJ, OBJ, TYPE> : NetworkMemberInfo<Parent_OBJ> {
    //private readonly MemberInfo memberInfo;
    private readonly NetworkMemberInfo<Parent_OBJ, OBJ> parent;


    public readonly RefFunc<OBJ, TYPE> GetValueDelegate;
    public readonly RefAction<OBJ, TYPE> SetValueDelegate;
    private NetworkMath networkMath;

    public NetworkMemberInfo(MemberInfo memberInfo, NetworkMemberInfo parent, NetSyncBaseAttribute netSyncOptions) : base(netSyncOptions) {
        this.parent = (NetworkMemberInfo<Parent_OBJ, OBJ>)parent;
        //Debug.Log(typeof(Parent_OBJ) + " --- " + typeof(OBJ) + " --- " + typeof(TYPE));

        switch (memberInfo.MemberType) {
            case MemberTypes.Field:
                FieldInfo fieldInfo = (FieldInfo)memberInfo;
                GetValueDelegate = NetworkMemberInfoUtility.CreateGetter<OBJ, TYPE>(fieldInfo);
                SetValueDelegate = NetworkMemberInfoUtility.CreateSetter<OBJ, TYPE>(fieldInfo);
                break;

            case MemberTypes.Property:
                PropertyInfo propertyInfo = (PropertyInfo)memberInfo;
                GetValueDelegate = (RefFunc<OBJ, TYPE>)Delegate.CreateDelegate(typeof(RefFunc<OBJ, TYPE>), propertyInfo.GetGetMethod());
                SetValueDelegate = (RefAction<OBJ, TYPE>)Delegate.CreateDelegate(typeof(RefAction<OBJ, TYPE>), propertyInfo.GetSetMethod());
                break;

            default:
                throw new NotImplementedException(memberInfo.MemberType.ToString());
        }

        Type typeType = typeof(TYPE);
        if (typeType == typeof(int)) {
            networkMath = new NetworkMathInteger();
        } else if (typeType == typeof(float)) {
            networkMath = new NetworkMathFloat(netSyncOptions.Accuracy, netSyncOptions.LerpSpeed, netSyncOptions.JumpThreshold);
        } else if (typeType == typeof(boolean)) {
            networkMath = new NetworkMathBoolean();
        }
    }

    public override void SetValue(ref Parent_OBJ parentObj, int oldValue, int newValue, float deltaTimeFrame, float deltaTimeMessage) {
        OBJ obj = parent.GetValueDelegate(ref parentObj);
        SetValueDelegate(ref obj, (TYPE)networkMath.IntegerToNative(GetValueDelegate(ref obj), oldValue, newValue, deltaTimeFrame, deltaTimeMessage));
        parent.SetValueDelegate(ref parentObj, obj);
    }

    public override int GetValue(Parent_OBJ parentObj) {
        OBJ obj = parent.GetValueDelegate(ref parentObj);
        return networkMath.NativeToInteger(GetValueDelegate(ref obj));
    }
}




internal sealed class NetworkMethodInfo<T> {
    public NetworkMethodInfo(MethodInfo methodInfo) {
        actionDelegate = (Action<T>)Delegate.CreateDelegate(typeof(Action<T>), methodInfo);
    }
    private readonly Action<T> actionDelegate;

    public void Invoke(T obj) {
        actionDelegate(obj);
    }
}


internal sealed class NetworkMethodInfo<T, Param> {
    public NetworkMethodInfo(MethodInfo methodInfo) {
        actionDelegate = (Action<T, Param>)Delegate.CreateDelegate(typeof(Action<T, Param>), methodInfo);
    }
    private readonly Action<T, Param> actionDelegate;

    public void Invoke(T obj, Param arg) {
        actionDelegate(obj, arg);
    }
}

internal sealed class NetworkMethodInfo<T, Param1, Param2> {
    public NetworkMethodInfo(MethodInfo methodInfo) {
        actionDelegate = (Action<T, Param1, Param2>)Delegate.CreateDelegate(typeof(Action<T, Param1, Param2>), methodInfo);
    }
    private readonly Action<T, Param1, Param2> actionDelegate;

    public void Invoke(T obj, Param1 arg1, Param2 arg2) {
        actionDelegate(obj, arg1, arg2);
    }
}

internal sealed class NetworkInOutMethodInfo<T, RefParam, OutParam> {
    internal delegate bool NetworkInOutDelegate(T obj, ref RefParam refArg, out OutParam outArg);

    public NetworkInOutMethodInfo(MethodInfo methodInfo) {
        functionDelegate = (NetworkInOutDelegate)Delegate.CreateDelegate(typeof(NetworkInOutDelegate), methodInfo);
    }
    private readonly NetworkInOutDelegate functionDelegate;

    public bool Invoke(T obj, ref RefParam refArg, out OutParam outArg) {
        return functionDelegate(obj, ref refArg, out outArg);
    }

}
