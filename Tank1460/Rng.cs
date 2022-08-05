using System;

namespace Tank1460;

public static class Rng
{
    public static int Next(int maxValue)
    {
        return Next(0, maxValue);
    }

    public static int Next(int minValue, int maxValue)
    {
        return Random.Shared.Next(minValue, maxValue);
    }

    public static int NextEven(int minValue, int maxValue)
    {
        return Next((minValue + 1) / 2, (maxValue + 1) / 2) * 2;
    }
}