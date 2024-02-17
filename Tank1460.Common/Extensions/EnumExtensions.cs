using System;
using System.Collections.Generic;
using System.Linq;

namespace Tank1460.Common.Extensions;

public static class EnumExtensions
{
    public static IEnumerable<T> GetCombinedFlagValues<T>(int elementNumber) where T : struct, Enum

    {
        return Enum.GetValues<T>().AsEnumerable().GetCombinations(elementNumber).Select(CombineFlagValues);
    }

    public static T CombineFlagValues<T>(IEnumerable<T> source) where T : struct, Enum
    {
        int? result = null;

        foreach (var enumVal in source)
        {
            // convert enum to int
            var intVal = Convert.ToInt32(enumVal);

            if (result.HasValue == false)
                result = intVal;

            result |= intVal;
        }

        // convert int to enum
        var val = (T)Enum.ToObject(typeof(T), result ?? 0);

        return val;
    }

    public static bool HasOneOfFlags<T>(this T flag, params T[] flagValues) where T : struct, Enum =>
        flagValues.Any(value => flag.HasFlag(value));
}