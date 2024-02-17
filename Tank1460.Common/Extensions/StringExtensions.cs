using System;

namespace Tank1460.Common.Extensions;

public static class StringExtensions
{
    public static string[] SplitIntoLines(this string s, StringSplitOptions options = StringSplitOptions.None) =>
        s.Split(NewLineSeparators, options);

    public static int CountLines(this string s)
    {
        if (string.IsNullOrEmpty(s))
            return 0;

        var index = -1;
        var count = 0;
        while (-1 != (index = s.IndexOf('\n', index + 1)))
            count++;

        return count + 1;
    }

    private static readonly string[] NewLineSeparators = { "\r\n", "\r", "\n" };
}