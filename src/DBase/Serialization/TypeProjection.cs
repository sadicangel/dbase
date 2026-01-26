using System.Linq.Expressions;

namespace DBase.Serialization;

// Bidirectional projection between T and object?[]
internal readonly record struct TypeProjection<T>
{
    private readonly Func<object?[], T> _create = GetCreateFunction();
    private readonly Func<T, object?[]> _values = GetValuesFunction();

    public TypeProjection() { }

    public T Create(object?[] arguments) => _create(arguments);

    public object?[] Values(T instance) => _values(instance);

    private static Func<object?[], T> GetCreateFunction()
    {
        var type = typeof(T);
        var properties = type.GetProperties();
        var constructor = type.GetConstructor([.. properties.Select(x => x.PropertyType)])
            ?? type.GetConstructor(Type.EmptyTypes)
            ?? throw new InvalidOperationException($"Type {type} does not have a parameterless constructor or a constructor with the arguments {string.Join(", ", properties.Select(a => a.PropertyType.ToString()))}");

        var parameter = Expression.Parameter(typeof(object[]), "args");

        var arguments = new Expression[properties.Length];
        for (var i = 0; i < properties.Length; ++i)
        {
            arguments[i] = Expression.Convert(Expression.ArrayIndex(parameter, Expression.Constant(i)), properties[i].PropertyType);
        }

        Expression body;
        if (constructor.GetParameters().Length != 0)
        {
            body = Expression.New(constructor, arguments);
        }
        else
        {
            var instance = Expression.Parameter(typeof(T), "instance");
            var ctor = Expression.New(constructor);
            var setters = Enumerable.Range(0, properties.Length)
                .Select(i => Expression.Assign(Expression.Property(instance, properties[i]), arguments[i]));

            body = Expression.Block([ctor, .. setters]);
        }

        return Expression.Lambda<Func<object?[], T>>(body, parameter).Compile();
    }

    private static Func<T, object?[]> GetValuesFunction()
    {
        var type = typeof(T);
        var properties = type.GetProperties();
        var instance = Expression.Parameter(type, "instance");
        var array = Expression.NewArrayInit(
            typeof(object),
            properties.Select(p => Expression.Convert(Expression.Property(instance, p), typeof(object))));
        return Expression.Lambda<Func<T, object?[]>>(array, instance).Compile();
    }
}
