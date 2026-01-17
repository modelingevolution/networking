using Async = System.Linq.AsyncEnumerable;

namespace ModelingEvolution.NetworkManager.Services;

/// <summary>
/// Helper static class to avoid ambiguity between System.Linq.Async and .NET 10's native async LINQ.
/// Use these methods with explicit static calls instead of extension method syntax.
/// </summary>
internal static class Ax
{
    public static IAsyncEnumerable<TResult> OfType<TResult>(IAsyncEnumerable<object?> source)
        => Async.OfType<TResult>(source);

    public static ValueTask<TSource[]> ToArrayAsync<TSource>(IAsyncEnumerable<TSource> source, CancellationToken cancellationToken = default)
        => Async.ToArrayAsync(source, cancellationToken);

    public static IAsyncEnumerable<TSource> Where<TSource>(IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate)
        => Async.Where(source, predicate);

    public static IAsyncEnumerable<TResult> Select<TSource, TResult>(IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector)
        => Async.Select(source, selector);

    public static ValueTask<TSource?> FirstOrDefaultAsync<TSource>(IAsyncEnumerable<TSource> source, CancellationToken cancellationToken = default)
        => Async.FirstOrDefaultAsync(source, cancellationToken);

    public static ValueTask<TSource?> FirstOrDefaultAsync<TSource>(IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate, CancellationToken cancellationToken = default)
        => Async.FirstOrDefaultAsync(source, predicate, cancellationToken);

    public static ValueTask<bool> AnyAsync<TSource>(IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate, CancellationToken cancellationToken = default)
        => Async.AnyAsync(source, predicate, cancellationToken);

    public static IAsyncEnumerable<TSource> OrderByDescending<TSource, TKey>(IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        => Async.OrderByDescending(source, keySelector);

    public static ValueTask<List<TSource>> ToListAsync<TSource>(IAsyncEnumerable<TSource> source, CancellationToken cancellationToken = default)
        => Async.ToListAsync(source, cancellationToken);
}
