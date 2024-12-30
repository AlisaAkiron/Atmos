namespace Atmos.Common.Extensions;

public static class EnumerableExtensions
{
    public static async Task<List<T>> ToListAsync<T>(this Task<IEnumerable<T>> enumerableTask)
    {
        var enumerable = await enumerableTask;
        return enumerable.ToList();
    }
}
