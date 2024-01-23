using System;
using System.Collections.Generic;
using System.Linq;

namespace Tank1460.Extensions;

public static class EnumerableExtensions
{
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var element in source)
            action(element);
    }

    public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> source) => source ?? Enumerable.Empty<T>();

    public static bool TryGetFirst<T>(this IEnumerable<T> source, out T found, Func<T, bool> predicate)
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
}