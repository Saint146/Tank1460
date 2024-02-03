using System;

namespace Tank1460.Common.Extensions;

public static class IntExtensions
{
    public static int CeilingByBase(this int number, int baseNumber)
    {
        return (int)Math.Ceiling(number / (double)baseNumber) * baseNumber;
    }
}