using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tank1460.Extensions;

public static class TextureExtensions
{
    public static Texture2D RecolorAsCopy(this Texture2D texture, Color originalColor, Color newColor)
    {
        var data = new Color[texture.Width * texture.Height];
        texture.GetData(data);

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
        var data = new Color[texture.Width * texture.Height];
        texture.GetData(data);

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