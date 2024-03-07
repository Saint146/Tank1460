using MonoGame.Extended;
using System;

namespace Tank1460.Common;

/// <summary>
/// Обёртка вокруг рандома.
/// </summary>
public static class Rng
{
    /// <summary>
    /// Случайное неотрицательное число, т.е. из [0; <see cref="int.MaxValue"/>).
    /// </summary>
    public static int Next() => Random.Shared.Next();

    /// <summary>
    /// Случайное число из [0; maxValue).
    /// </summary>
    /// <remarks>
    /// Верхняя граница не входит в интервал.
    /// </remarks>
    public static int Next(int maxValue) => Next(0, maxValue);

    /// <summary>
    /// Случайное число из [minValue; maxValue).
    /// </summary>
    /// <remarks>
    /// Верхняя граница не входит в интервал.
    /// </remarks>
    public static int Next(int minValue, int maxValue) => Random.Shared.Next(minValue, maxValue);

    /// <summary>
    /// Случайное чётное число из [minValue; maxValue).
    /// </summary>
    /// <remarks>
    /// Верхняя граница не входит в интервал.
    /// </remarks>
    public static int NextEven(int minValue, int maxValue) => Next((minValue + 1) / 2, (maxValue + 1) / 2) * 2;

    /// <summary>
    /// Возвращает true с вероятностью 1/<paramref name="n"/>.
    /// </summary>
    public static bool OneIn(int n) => Next(n) == 0;

    /// <summary>
    /// Случайное число из промежутка.
    /// </summary>
    /// <remarks>
    /// Верхняя граница входит в интервал.
    /// </remarks>
    public static int FromRange(Range<int> range) => Next(range.Min, range.Max + 1);
}