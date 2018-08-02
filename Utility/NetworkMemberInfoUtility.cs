using System.Linq.Expressions;
using System.Reflection;

internal delegate void RefAction<S, T>(ref S instance, T value);
internal delegate T RefFunc<S, T>(ref S instance);

internal static class NetworkMemberInfoUtility {

    public static RefFunc<S, T> CreateGetter<S, T>(FieldInfo field) {
        ParameterExpression instance = Expression.Parameter(typeof(S).MakeByRefType(), "instance");
        MemberExpression memberAccess = Expression.MakeMemberAccess(instance, field);
        Expression<RefFunc<S, T>> expr =
            Expression.Lambda<RefFunc<S, T>>(memberAccess, instance);
        return expr.Compile();
    }

    public static RefAction<S, T> CreateSetter<S, T>(FieldInfo field) {        
        ParameterExpression instance = Expression.Parameter(typeof(S).MakeByRefType(), "instance");
        ParameterExpression value = Expression.Parameter(typeof(T), "value");
        Expression<RefAction<S, T>> expr =
            Expression.Lambda<RefAction<S, T>>(
                Expression.Assign(
                    Expression.Field(instance, field),
                    Expression.Convert(value, field.FieldType)),
                instance,
                value);

        return expr.Compile();
    }
}
