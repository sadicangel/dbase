using System.Linq.Expressions;

namespace DBase.Serialization;

// Bidirectional projection between T and object?[]
internal readonly record struct TypeProjection<T>
{
    private readonly Func<DbfRecordStatus, object?[], T> _create = GetCreateFunction();
    private readonly Func<T, object?[]> _values = GetValuesFunction();

    public TypeProjection() { }

    public T Create(DbfRecordStatus status, object?[] arguments) => _create(status, arguments);

    public object?[] Values(T instance) => _values(instance);

    private static Func<DbfRecordStatus, object?[], T> GetCreateFunction()
    {
        var type = typeof(T);
        var properties = DbfTypeProperties.GetMappedProperties(type);
        var constructor = type.GetConstructor([.. properties.Select(x => x.PropertyType)])
            ?? type.GetConstructor(Type.EmptyTypes)
            ?? throw new InvalidOperationException($"Type {type} does not have a parameterless constructor or a constructor with the arguments {string.Join(", ", properties.Select(a => a.PropertyType.ToString()))}");

        var statusParameter = Expression.Parameter(typeof(DbfRecordStatus), "status");
        var argumentsParameter = Expression.Parameter(typeof(object[]), "args");

        var arguments = new Expression[properties.Length];
        for (var i = 0; i < properties.Length; ++i)
        {
            arguments[i] = Expression.Convert(Expression.ArrayIndex(argumentsParameter, Expression.Constant(i)), properties[i].PropertyType);
        }

        Expression body;
        if (constructor.GetParameters().Length != 0)
        {
            body = Expression.New(constructor, arguments);
        }
        else
        {
            var readOnlyProperty = properties.FirstOrDefault(static property => property.SetMethod?.IsPublic is not true);
            if (readOnlyProperty is not null)
            {
                throw new InvalidOperationException($"Property {type}.{readOnlyProperty.Name} must have a public setter for parameterless construction.");
            }

            var instance = Expression.Variable(type, "instance");
            var expressions = new List<Expression>(properties.Length + 2)
            {
                Expression.Assign(instance, Expression.New(constructor))
            };

            for (var i = 0; i < properties.Length; ++i)
            {
                expressions.Add(Expression.Assign(Expression.Property(instance, properties[i]), arguments[i]));
            }

            expressions.Add(instance);

            body = Expression.Block([instance], expressions);
        }

        return Expression.Lambda<Func<DbfRecordStatus, object?[], T>>(body, statusParameter, argumentsParameter).Compile();
    }

    private static Func<T, object?[]> GetValuesFunction()
    {
        var type = typeof(T);
        var properties = DbfTypeProperties.GetMappedProperties(type);
        var instance = Expression.Parameter(type, "instance");
        var array = Expression.NewArrayInit(
            typeof(object),
            properties.Select(p => Expression.Convert(Expression.Property(instance, p), typeof(object))));
        return Expression.Lambda<Func<T, object?[]>>(array, instance).Compile();
    }
}
