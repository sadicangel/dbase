using System.Collections.Immutable;
using System.Reflection;

namespace DBase;

internal static class DbfTypeProperties
{
    public static ImmutableArray<PropertyInfo> GetMappedProperties(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var builder = ImmutableArray.CreateBuilder<PropertyInfo>(properties.Length);

        foreach (var property in properties)
        {
            var ignored = property.IsDefined(typeof(DbfIgnoreAttribute), inherit: true);
            var hasFieldAttribute = property.GetCustomAttributes<DbfFieldAttribute>(inherit: true).Any();

            if (ignored)
            {
                if (hasFieldAttribute)
                {
                    throw new ArgumentException(
                        $"Property '{property.DeclaringType?.FullName}.{property.Name}' cannot have both {nameof(DbfIgnoreAttribute)} and a DBF field attribute.",
                        nameof(type));
                }

                continue;
            }

            builder.Add(property);
        }

        return builder.ToImmutable();
    }
}
