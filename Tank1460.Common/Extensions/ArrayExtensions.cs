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