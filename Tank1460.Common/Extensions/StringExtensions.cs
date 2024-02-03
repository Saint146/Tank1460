using System;

namespace Tank1460.Common.Extensions;

public static class StringExtensions
{
    public static string[] SplitIntoLines(this string s, StringSplitOptions options = StringSplitOptions.None) =>
        s.Split(NewLineSeparators, options);

    private static readonly string[] NewLineSeparators = { "\r\n", "\r", "\n" };
}