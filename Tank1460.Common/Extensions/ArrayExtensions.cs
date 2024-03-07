using System.Collections.Generic;
using System.Linq;

namespace Tank1460.Common.Extensions;

public static class ArrayExtensions
{
    public static T ElementAtOrDefault<T>(this T[,] array, int x, int y)
    {
        if (x < 0 || x >= array.GetLength(0))
            return default;

        if (y < 0 || y >= array.GetLength(1))
            return default;
        return array[x, y];
    }

    /// <summary>
    /// Вернуть случайный элемент из массива.
    /// </summary>
    public static T GetRandom<T>(this T[] array) => array[Rng.Next(array.Length)];

    /// <summary>
    /// Вернуть <paramref name="maxCount" /> неповторяющихся случайных элемента из массива.
    /// </summary>
    /// <remarks>
    /// Если <paramref name="maxCount"/> ≥ длины массива, вернутся все элементы в случайном порядке.
    /// </remarks>
    public static IEnumerable<T> TakeRandom<T>(this T[] array, int maxCount)
    {
        var bottomIndex = array.Length - maxCount;
        if (bottomIndex < 0)
            bottomIndex = 0;

        for (var i = array.Length - 1; i >= bottomIndex; i--)
        {
            var swapIndex = Rng.Next(i + 1);
            yield return array[swapIndex];
            array[swapIndex] = array[i];
        }
    }

    public static IEnumerable<T> Shuffle<T>(this T[] array)
    {
        for (var i = array.Length - 1; i >= 0; i--)
        {
            var swapIndex = Rng.Next(i + 1);
            yield return array[swapIndex];
            array[swapIndex] = array[i];
        }
    }

    /// <summary>
    /// Дополнить все строки указанными символами до длины максимальной из них.
    /// </summary>
    public static string[] TopAllToMaxLength(this string[] array, char placeholder = ' ')
    {
        var maxLength = array.Select(s => s.Length).Max();

        var resultArray = new string[array.Length];
        for (var i = 0; i < array.Length; i++)
            resultArray[i] = array[i] + new string(placeholder, maxLength - array[i].Length);

        return resultArray;
    }

    public static bool ContainsCoords<T>(this T[,] array, int x, int y)
        => x >= 0 && x < array.GetLength(0) && y >= 0 && y < array.GetLength(1);
}