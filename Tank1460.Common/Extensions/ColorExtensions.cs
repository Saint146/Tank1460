using System;
using Microsoft.Xna.Framework;

namespace Tank1460.Common.Extensions;

public static class ColorExtensions
{
    /// <summary>
    /// Найти средний цвет между двумя.
    /// </summary>
    /// <remarks>
    /// В оригинальной игре для имитации этого использовалось переключение цвета танков каждый кадр.
    /// </remarks>
    public static Color Average(this Color color, Color otherColor)
    {
        // TODO: Сделать через операции с uint, будет гораздо быстрее. С первого наскока не получилось.
        return new((color.R + otherColor.R) / 2, (color.G + otherColor.G) / 2, (color.B + otherColor.B) / 2, (color.A + otherColor.A) / 2);
    }

    public static Color DrawUpon(this Color foregroundColor, Color backgroundColor)
    {
        const float divisor = byte.MaxValue;

        var fr = foregroundColor.R / divisor;
        var fg = foregroundColor.G / divisor;
        var fb = foregroundColor.B / divisor;
        var fa = foregroundColor.A / divisor;
        var fm = fa;

        var br = backgroundColor.R / divisor;
        var bg = backgroundColor.G / divisor;
        var bb = backgroundColor.B / divisor;
        var ba = backgroundColor.A / divisor;
        var bm = Math.Max(0, 1 - fm) * ba;

        var r = Math.Min(1, fm * fr + bm * br);
        var g = Math.Min(1, fm * fg + bm * bg);
        var b = Math.Min(1, fm * fb + bm * bb);
        var a = Math.Min(1, fm * fa + bm * ba);

        return new Color(
            (int)(r * byte.MaxValue),
            (int)(g * byte.MaxValue),
            (int)(b * byte.MaxValue),
            (int)(a * byte.MaxValue));
    }

    /// <summary>
    /// Смешать два цвета с учетом прозрачности.
    /// </summary>
    /// <param name="foregroundColor">Цвет, накладывающийся поверх другого.</param>
    /// <param name="backgroundColor">Цвет, на который накладывается другой.</param>
    public static Color BlendUpon(this Color foregroundColor, Color backgroundColor)
    {
        const float divisor = byte.MaxValue;

        var fa = foregroundColor.A / divisor;
        var fr = foregroundColor.R / divisor;
        var fg = foregroundColor.G / divisor;
        var fb = foregroundColor.B / divisor;

        var ba = backgroundColor.A / divisor;
        var br = backgroundColor.R / divisor;
        var bg = backgroundColor.G / divisor;
        var bb = backgroundColor.B / divisor;

        var a = fa + ba - fa * ba;

        if (a <= 0)
            return Color.Transparent;

        var r = (fa * (1 - ba) * fr + fa * ba * fa + (1 - fa) * ba * br) / a;
        var g = (fa * (1 - ba) * fg + fa * ba * fa + (1 - fa) * ba * bg) / a;
        var b = (fa * (1 - ba) * fb + fa * ba * fa + (1 - fa) * ba * bb) / a;

        return new Color(
            (int)(r * byte.MaxValue),
            (int)(g * byte.MaxValue),
            (int)(b * byte.MaxValue),
            (int)(a * byte.MaxValue));
    }
}