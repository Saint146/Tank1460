using System.Collections.Generic;

namespace Tank1460.Common.Extensions;

public static class ListExtensions
{
    /// <summary>
    /// Вернуть <paramref name="maxCount" /> неповторяющихся случайных элемента из массива.
    /// </summary>
    /// <remarks>
    /// Если <paramref name="maxCount"/> ≥ длины массива, вернутся все элементы в случайном порядке.
    /// </remarks>
    public static IEnumerable<T> TakeRandom<T>(this IList<T> list, int maxCount)
    {
        var bottomIndex = list.Count - maxCount;
        if (bottomIndex < 0)
            bottomIndex = 0;

        for (var i = list.Count - 1; i >= bottomIndex; i--)
        {
            var swapIndex = Rng.Next(i + 1);
            yield return list[swapIndex];
            list[swapIndex] = list[i];
        }
    }

    public static IEnumerable<T> Shuffle<T>(this IList<T> list)
    {
        for (var i = list.Count - 1; i >= 0; i--)
        {
            var swapIndex = Rng.Next(i + 1);
            yield return list[swapIndex];
            list[swapIndex] = list[i];
        }
    }
}