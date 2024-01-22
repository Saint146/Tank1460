using System;

namespace Tank1460;

/// <summary>
/// Обёртка вокруг рандома.
/// </summary>
public static class Rng
{
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
}