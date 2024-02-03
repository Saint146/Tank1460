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

    public static T GetRandom<T>(this T[] array) => array[Rng.Next(array.Length)];
}