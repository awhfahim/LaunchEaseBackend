using System.Collections.Concurrent;
using System.Reflection;
using Common.Domain.Misc;
using Dapper;

namespace Common.Infrastructure.Extensions;

public static class CommonModuleDapperConfiguration
{
    public static void ConfigureMappings()
    {
    }

    private static void SetTypeMap<T>()
    {
        SqlMapper.SetTypeMap(
            typeof(T),
            new CustomPropertyTypeMap(
                typeof(T),
                GetMappedProperty
            )
        );
    }

    private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> TypePropertyCache =
        new();

    private static PropertyInfo GetMappedProperty(Type type, string columnName)
    {
        var propertyMap = TypePropertyCache.GetOrAdd(type, t =>
        {
            var map = new Dictionary<string, PropertyInfo>();
            foreach (var propertyInfo in t.GetProperties())
            {
                var dbColumnAttr = propertyInfo.GetCustomAttribute<DbColumnAttribute>();
                if (dbColumnAttr != null)
                {
                    map[dbColumnAttr.Name] = propertyInfo;
                }
            }

            return map;
        });

        return propertyMap.TryGetValue(columnName, out var property) ? property
            : throw new InvalidOperationException(
            $"Type {type.Name} does not have a property mapped to column {columnName}.");
    }
}
