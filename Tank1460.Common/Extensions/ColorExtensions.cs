using Microsoft.Xna.Framework;

namespace Tank1460.Common.Extensions;

public static class ColorExtensions
{
    /// <summary>
    /// "Смешать" два цвета. На самом деле, найти средний цвет между ними.
    /// </summary>
    /// <remarks>
    /// В оригинальной игре для имитации этого использовалось переключение цвета танков каждый кадр.
    /// </remarks>
    public static Color Mix(this Color color, Color otherColor)
    {
        // TODO: Сделать через операции с uint, будет гораздо быстрее. С первого наскока не получилось.
        return new((color.R + otherColor.R)/2, (color.G + otherColor.G) / 2, (color.B + otherColor.B) / 2);
    }
}