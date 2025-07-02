using System.Collections;
using System.Globalization;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using Common.Application.Misc;
using Common.Domain.CoreProviders;
using Common.Domain.DataTransferObjects.Request;
using Humanizer;

namespace Common.Infrastructure.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<T> PaginateQueryable<T>(this IQueryable<T> queryable, int page, int limit)
    {
        if (page <= 0)
        {
            page = 1;
        }

        if (limit <= 0)
        {
            limit = 1;
        }

        return queryable.Skip((page - 1) * limit).Take(limit);
    }

    public static IQueryable<TSource> WhereDynamic<TSource>(this IQueryable<TSource> query,
        IReadOnlyList<DynamicFilterDto> filters, IReflectionCacheProvider reflectionCacheProvider)
        where TSource : class
    {
        if (filters.Count == 0)
        {
            return query;
        }

        var props = GetPropertyData(typeof(TSource), reflectionCacheProvider);

        foreach (var filter in filters)
        {
            if (filter.Field.Contains('.'))
            {
                continue;
            }

            var (name, type) = props.Select(x => (x.PropertyName, x.DataType)).FirstOrDefault(x =>
                x.PropertyName.Equals(filter.Field, StringComparison.InvariantCultureIgnoreCase)
            );

            if (name is null || type is null)
            {
                continue;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = Nullable.GetUnderlyingType(type);

                if (type is null)
                {
                    continue;
                }
            }

            object? value;

            try
            {
                if (type == typeof(string))
                {
                    value = filter.Value;
                }

                else if (type == typeof(DateOnly))
                {
                    value = DateOnly.Parse(filter.Value);
                }

                else if (type == typeof(TimeOnly))
                {
                    value = TimeOnly.Parse(filter.Value);
                }

                else if (type == typeof(DateTime) &&
                         name.Contains("utc", StringComparison.CurrentCultureIgnoreCase))
                {
                    value = DateTime.SpecifyKind(Convert.ToDateTime(filter.Value).ToUniversalTime(),
                        DateTimeKind.Utc);
                }

                else
                {
                    // if (type.IsGenericType)
                    // {
                    //     value = JsonSerializer.Deserialize(filter.Value, type);
                    // }

                    if (type.IsEnum)
                    {
                        value = Enum.Parse(type, filter.Value);
                    }
                    else
                    {
                        value = type == typeof(Guid)
                            ? Guid.Parse(filter.Value)
                            : Convert.ChangeType(filter.Value, type, CultureInfo.InvariantCulture);
                    }
                }
            }
            catch (Exception)
            {
                value = null;
            }

            if (value is null)
            {
                continue;
            }

            var predicate = string.Empty;


            if (type == typeof(string))
            {
                var strOperator = filter.Type switch
                {
                    DynamicFilters.Like => "Contains",
                    DynamicFilters.NotLike => "NotContains",
                    DynamicFilters.Starts => "StartsWith",
                    DynamicFilters.NotStarts => "NotStartsWith",
                    DynamicFilters.Ends => "EndsWith",
                    DynamicFilters.NotEnds => "NotEndsWith",
                    _ => "Contains"
                };

                predicate = strOperator switch
                {
                    "NotContains" => "x => !x." + name + ".ToLower().Contains(@0.ToLower())",
                    "NotStartsWith" => "x => !x." + name + ".ToLower().StartsWith(@0.ToLower())",
                    "NotEndsWith" => "x => !x." + name + ".ToLower().EndsWith(@0.ToLower())",
                    _ => "x => x." + name + ".ToLower()." + strOperator + "(@0.ToLower())"
                };
            }

            else if (type == typeof(bool) || type.IsEnum)
            {
                var boolOperator = filter.Type switch
                {
                    DynamicFilters.Equal => DynamicFilters.Equal,
                    DynamicFilters.NotEquals => DynamicFilters.NotEquals,
                    _ => DynamicFilters.Equal
                };
                predicate = "x => x." + name + " != null && x." + name + " " + boolOperator + " @0";
            }


            else if (type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type) is false &&
                     filter.Type is DynamicFilters.GreaterThan
                         or DynamicFilters.LessThan
                         or DynamicFilters.GreaterThanEqual
                         or DynamicFilters.LessThanEqual
                         or DynamicFilters.Equal
                         or DynamicFilters.NotEquals)
            {
                var nonStrOperator = filter.Type switch
                {
                    DynamicFilters.GreaterThan => DynamicFilters.GreaterThan,
                    DynamicFilters.LessThan => DynamicFilters.LessThan,
                    DynamicFilters.Equal => DynamicFilters.Equal,
                    DynamicFilters.NotEquals => DynamicFilters.NotEquals,
                    DynamicFilters.GreaterThanEqual => DynamicFilters.GreaterThanEqual,
                    DynamicFilters.LessThanEqual => DynamicFilters.LessThanEqual,
                    _ => DynamicFilters.Equal
                };

                predicate = "x => x." + name + " != null && x." + name + " " + nonStrOperator + " @0";
            }

            if (predicate != string.Empty)
            {
                query = query.Where(predicate, value);
            }
        }

        return query;
    }

    public static IQueryable<TSource> OrderByDynamic<TSource, TSorter>(this IQueryable<TSource> query,
        IReadOnlyList<DynamicSortingDto> dto,
        (Expression<Func<TSource, TSorter>> orderBy, bool desc) defaultSorter,
        IReflectionCacheProvider reflectionCacheProvider)
        where TSource : class
        where TSorter : IComparable<TSorter>
    {
        if (dto.Count == 0)
        {
            var dir = "ASC";
            if (defaultSorter.desc)
            {
                dir = "DESC";
            }

            return query.OrderBy($"{GetPropertyName(defaultSorter.orderBy)} {dir}");
        }

        (List<(string Field, string Dir)>converted, List<(string Field, string Dir)> whitelisted)
            storage = ([], []);

        foreach (var (field, dir) in dto)
        {
            storage.converted.Add((Field: field.Pascalize(), Dir: dir.ToUpper()));
        }

        var props = GetPropertyData(typeof(TSource), reflectionCacheProvider);

        foreach (var elem in props)
        {
            var data = storage.converted.FirstOrDefault(x => x.Field == elem.PropertyName);
            if (data != default)
            {
                storage.whitelisted.Add((data.Field, data.Dir));
            }
        }

        if (storage.whitelisted.Count == 0)
        {
            var dir = "ASC";
            if (defaultSorter.desc)
            {
                dir = "DESC";
            }

            return query.OrderBy($"{GetPropertyName(defaultSorter.orderBy)} {dir}");
        }

        var firstColumn = storage.whitelisted[0];
        var source = query.OrderBy($"{firstColumn.Field} {firstColumn.Dir}");

        foreach (var (field, dir) in storage.whitelisted.Skip(1))
        {
            source = source.ThenBy($"{field} {dir}");
        }

        return source;
    }

    private static IReadOnlyCollection<(string PropertyName, Type DataType)> GetPropertyData(Type targetType,
        IReflectionCacheProvider reflectionCacheProvider)
    {
        var key = targetType.FullName ?? targetType.Name;

        if (reflectionCacheProvider.DynamicLinqCache.TryGetValue(key, out var existingData))
        {
            return existingData;
        }

        var retrievedFreshData = targetType.GetProperties()
            .Select(p => (PropertyName: p.Name, DataType: p.PropertyType))
            .ToList();

        reflectionCacheProvider.DynamicLinqCache.TryAdd(key, retrievedFreshData);
        return retrievedFreshData;
    }

    private static string GetPropertyName<TSource, TSorter>(Expression<Func<TSource, TSorter>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        throw new ArgumentException("Expression is not a valid member expression.");
    }
}
