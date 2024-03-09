using System.Collections.Generic;

namespace Tank1460.Common.Extensions;

public static class ReadOnlyListExtensions
{
    /// <summary>
    /// Вернуть случайный элемент из массива.
    /// </summary>
    public static T GetRandom<T>(this IReadOnlyList<T> list) => list[Rng.Next(list.Count)];
}