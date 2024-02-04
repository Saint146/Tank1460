using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tank1460.Common.Extensions;

public static class Texture2DExtensions
{
    public static Color[] ToColorData(this Texture2D texture)
    {
        var data = new Color[texture.Width * texture.Height];
        texture.GetData(data);
        return data;
    }

    public static Texture2D Copy(this Texture2D texture)
    {
        var newTexture = new Texture2D(texture.GraphicsDevice, texture.Width, texture.Height);

        newTexture.SetData(texture.ToColorData());

        return newTexture;
    }

    public static Texture2D RecolorAsCopy(this Texture2D texture, Color originalColor, Color newColor)
    {
        var data = texture.ToColorData();

        var newTexture = new Texture2D(texture.GraphicsDevice, texture.Width, texture.Height);

        for (var i = 0; i < data.Length; i++)
        {
            if (data[i] == originalColor)
                data[i] = newColor;
        }

        newTexture.SetData(data);
        return newTexture;
    }

    public static Texture2D RecolorAsCopy(this Texture2D texture, Dictionary<Color, Color> colorsMap)
    {
        var data = texture.ToColorData();

        var newTexture = new Texture2D(texture.GraphicsDevice, texture.Width, texture.Height);

        for (var i = 0; i < data.Length; i++)
        {
            if (colorsMap.TryGetValue(data[i], out var newColor))
                data[i] = newColor;
        }

        newTexture.SetData(data);

        return newTexture;
    }

    public static Texture2D RecolorAsCopy(this Texture2D texture, Texture2D recolorTexture)
    {
        return RecolorAsCopy(texture, RecolorTextureToMap(recolorTexture));
    }

    /// <summary>
    /// "Смешать" две текстуры по цветам, найдя средние цвета между ними.
    /// Используется для смешения текстур для перекрашивания, при этом 
    /// </summary>
    /// <remarks>
    /// В оригинальной игре для имитации этого использовалось переключение цвета танков каждый кадр.
    /// </remarks>
    public static Texture2D AverageWith(this Texture2D texture, Texture2D otherTexture)
    {
        if (texture.Bounds != otherTexture.Bounds)
            throw new Exception($"Textures cannot be mixed due to different sizes ({texture.Bounds} vs {otherTexture.Bounds}");

        var data = texture.ToColorData();
        var otherData = otherTexture.ToColorData();

        var newTexture = new Texture2D(texture.GraphicsDevice, texture.Width, texture.Height);

        for (var i = 0; i < data.Length; i++)
            data[i] = data[i].Average(otherData[i]);

        newTexture.SetData(data);

        return newTexture;
    }

    public static Texture2D CreateColoredTexture(GraphicsDevice graphicsDevice, Color color, Point size) =>
        CreateColoredTexture(graphicsDevice, color, size.X, size.Y);

    public static Texture2D CreateColoredTexture(GraphicsDevice graphicsDevice, Color color, int width, int height)
    {
        var texture = new Texture2D(graphicsDevice, width, height);
        var data = new Color[width * height];

        for (var i = 0; i < data.Length; i++)
            data[i] = color;

        texture.SetData(data);

        return texture;
    }

    /// <summary>
    /// Нарисовать текстурой на текстуре.
    /// </summary>
    /// <param name="canvasTexture">Исходная текстура-холст, которая изменяется после рисования.</param>
    /// <param name="overlayTexture">Текстура, которая накладывается.</param>
    /// <param name="position">Точка в рамках <paramref name="canvasTexture"/>, от которой рисуется <paramref name="overlayTexture"/>.</param>
    public static void Draw(this Texture2D canvasTexture, Texture2D overlayTexture, Point position)
    {
        var drawingBounds = new Rectangle(position, overlayTexture.Bounds.Size);
        Debug.Assert(canvasTexture.Bounds.Contains(drawingBounds));

        var canvasData = canvasTexture.ToColorData();
        var overlayData = overlayTexture.ToColorData();

        foreach (var point in drawingBounds.GetAllPoints())
        {
            var overlayColor = overlayData[(point.Y - drawingBounds.Y) * overlayTexture.Width + point.X - drawingBounds.X];
            canvasData[point.Y * canvasTexture.Width + point.X] = overlayColor.DrawUpon(canvasData[point.Y * canvasTexture.Width + point.X]);
        }

        canvasTexture.SetData(canvasData);
    }

    private static Dictionary<Color, Color> RecolorTextureToMap(Texture2D recolorTexture)
    {
        var recolorMap = new Dictionary<Color, Color>();
        var recolorData = new Color[recolorTexture.Width * recolorTexture.Height];
        recolorTexture.GetData(recolorData);

        var offset = recolorData.Length / 2;

        for (var i = 0; i < offset; i++)
            recolorMap[recolorData[i]] = recolorData[i + offset];

        return recolorMap;
    }
}