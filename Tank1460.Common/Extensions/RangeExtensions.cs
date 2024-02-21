using MonoGame.Extended;
using System;
using System.Collections.Generic;

namespace Tank1460.Common.Extensions;

public static class RangeExtensions
{
    public static bool Contains<T>(this Range<T> range, T value) where T : IComparable<T>
    {
        return range.Min.CompareTo(value) <= 0 && value.CompareTo(range.Max) <= 0;
    }

    public static int NextLooping(this Range<int> range, int value)
    {
        if (value == range.Max)
            return range.Min;

        return value + 1;
    }

    public static int PrevLooping(this Range<int> range, int value)
    {
        if (value == range.Min)
            return range.Max;

        return value - 1;
    }

    public static IEnumerator<int> GetEnumerator(this Range<int> range)
        => range.AsEnumerable().GetEnumerator();

    public static IEnumerable<int> AsEnumerable(this Range<int> range)
    {
        for (var i = range.Min; i <= range.Max; i++)
            yield return i;
    }

    public static int Length(this Range<int> range) => range.Max - range.Min + 1;
}