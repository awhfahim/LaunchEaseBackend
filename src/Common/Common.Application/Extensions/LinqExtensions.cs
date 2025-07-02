namespace Common.Application.Extensions;

public static class LinqExtensions
{
    public static IEnumerable<T> Paginate<T>(this IEnumerable<T> enumerable, int page, int limit)
    {
        return page > 0 && limit > 0
            ? enumerable.Skip((page - 1) * limit).Take(limit)
            : enumerable;
    }

    public static void WhenExists<T>(this IEnumerable<T> enumerable,
        Func<T, bool> condition, Action<T> logic)
    {
        var entity = enumerable.FirstOrDefault(condition);

        if (entity is not null)
        {
            logic(entity);
        }
    }
    public static async Task WhenExistsAsync<T>(
        this IEnumerable<T> enumerable,
        Func<T, bool> condition,
        Func<T, Task> logic)
    {
        var entity = enumerable.FirstOrDefault(condition);

        if (entity is not null)
        {
            await logic(entity);
        }
    }
}
