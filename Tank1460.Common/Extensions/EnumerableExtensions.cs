using System;
using System.Collections.Generic;
using System.Linq;

namespace Tank1460.Common.Extensions;

public static class EnumerableExtensions
{
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var element in source)
            action(element);
    }

    public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> source) => source ?? Enumerable.Empty<T>();

    public static bool TryGetFirst<T>(this IEnumerable<T> source, out T found, Predicate<T> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        foreach (var element in source)
        {
            if (!predicate(element)) continue;

            found = element;
            return true;
        }

        found = default;
        return false;
    }

    public static (IEnumerable<T> thoseWithTrue, IEnumerable<T> thoseWithFalse) SplitByCondition<T>(this IEnumerable<T> source, Predicate<T> predicate)
    {
        var lookup = source.ToLookup(element => predicate(element));
        return (lookup[true], lookup[false]);
    }

    public static IEnumerable<IEnumerable<T>> GetCombinations<T>(this IEnumerable<T> source, int length) where T : IComparable
    {
        if (length == 1) return source.Select(t => new[] { t });
        return GetCombinations(source, length - 1).SelectMany(t => source.Where(o => o.CompareTo(t.Last()) > 0),
                                                              (t1, t2) => t1.Concat(new[] { t2 }));
    }

    /// <summary>
    /// Вернуть <paramref name="maxCount" /> неповторяющихся случайных элемента.
    /// </summary>
    /// <remarks>
    /// Если <paramref name="maxCount"/> ≥ длины перечисления, вернутся все элементы в случайном порядке.
    /// </remarks>
    public static IEnumerable<T> TakeRandom<T>(this IEnumerable<T> source, int maxCount) => source.ToArray().TakeRandom(maxCount);

    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source) => source.ToArray().Shuffle();

    public static bool IsNullOrEmpty<T>(this IEnumerable<T> source) => source is null || !source.Any();
}