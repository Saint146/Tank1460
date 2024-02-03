using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Content;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Tank1460.Common.Extensions;

namespace Tank1460;

public class ContentManagerEx : ContentManager
{
    private readonly Dictionary<string, Texture2D> _dynamicTextures = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Font> _fonts = new(StringComparer.OrdinalIgnoreCase);

    private const char MixSeparator = ',';
    private const char RecolorSeparator = ':';

    public ContentManagerEx(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public ContentManagerEx(IServiceProvider serviceProvider, string rootDirectory) : base(serviceProvider, rootDirectory)
    {
    }

    public override void Unload()
    {
        _dynamicTextures.Clear();
        _fonts.Clear();
        base.Unload();
    }

    /// <summary>
    /// Загрузить текстуру, перекрасив её с помощью другой текстуры.
    /// </summary>
    /// <remarks>
    /// Можно смешать цвета, указав в <paramref name="recolorTextureName"/> две текстуры из одной с помощью символа запятой, например ../Green,Gray
    /// </remarks>
    public Texture2D LoadRecoloredTexture(string textureName, string recolorTextureName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(textureName);
        ArgumentException.ThrowIfNullOrWhiteSpace(recolorTextureName);

        var key = $"{textureName.Replace('\\', '/')}{RecolorSeparator}{recolorTextureName.Replace('\\', '/')}";
        if (_dynamicTextures.TryGetValue(key, out var texture))
            return texture;

        var recolorTexture = !recolorTextureName.Contains(MixSeparator) ? Load<Texture2D>(recolorTextureName) : LoadMixedTexture(recolorTextureName);

        texture = Load<Texture2D>(textureName);
        var recoloredTexture = texture.RecolorAsCopy(recolorTexture);
        _dynamicTextures[key] = recoloredTexture;
        return recoloredTexture;
    }

    /// <summary>
    /// Загрузить текстуру, перекрасив её с помощью двух других текстур, смешанных вместе.
    /// </summary>
    public Texture2D LoadRecoloredMixedTexture(string textureName, string recolorTexture1Name, string recolorTexture2Name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(textureName);
        ArgumentException.ThrowIfNullOrWhiteSpace(recolorTexture1Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(recolorTexture2Name);

        var key = $"{textureName.Replace('\\', '/')}{RecolorSeparator}{recolorTexture1Name.Replace('\\', '/')}{MixSeparator}{recolorTexture2Name.Replace('\\', '/')}";
        if (_dynamicTextures.TryGetValue(key, out var texture))
            return texture;

        texture = Load<Texture2D>(textureName);
        var recolorMixedTexture = MixTextures(recolorTexture1Name, recolorTexture2Name);

        var recoloredTexture = texture.RecolorAsCopy(recolorMixedTexture);
        _dynamicTextures[key] = recoloredTexture;
        return recoloredTexture;
    }

    /// <summary>
    /// Загрузить смесь двух текстур из одной папки, разделенные символом запятой.
    /// Например, textures/one/green,yellow
    /// </summary>
    public Texture2D LoadMixedTexture(string textureNames)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(textureNames);

        var key = textureNames.Replace('\\', '/');
        if (_dynamicTextures.TryGetValue(key, out var texture))
            return texture;

        var split = textureNames.Split(MixSeparator, StringSplitOptions.TrimEntries);
        if (split.Length != 2)
            throw new Exception($"LoadMixedTexture: more than one {MixSeparator} separator is not allowed.");

        var recolorTexture1Name = split[0];
        var recolorTexture2Name = recolorTexture1Name.Remove(recolorTexture1Name.LastIndexOf('/') + 1) + split[1];

        texture = MixTextures(recolorTexture1Name, recolorTexture2Name);
        _dynamicTextures[key] = texture;

        return texture;
    }

    public Texture2D CreateColoredTexture(Color color, int width, int height)
    {
        Debug.Assert(width > 0 && height > 0);

        // Во всех обычных текстурах не может быть обратного слэша.
        var key = $"\\{color.PackedValue}.{width}*{height}";
        if (_dynamicTextures.TryGetValue(key, out var texture))
            return texture;

        _dynamicTextures[key] = texture = TextureExtensions.CreateColoredTexture(this.GetGraphicsDevice(), color, width, height);
        return texture;
    }

    public Dictionary<string, T> MassLoadContent<T>(string contentFolder, string filePattern = "*.*", bool recurse = false)
    {
        var dir = new DirectoryInfo(Path.Combine(RootDirectory, contentFolder));
        if (!dir.Exists)
            throw new DirectoryNotFoundException();

        var result = new Dictionary<string, T>();

        var files = dir.GetFiles(filePattern, new EnumerationOptions { RecurseSubdirectories = recurse });
        foreach (var file in files)
        {
            var key = Path.ChangeExtension(Path.GetRelativePath(RootDirectory, file.FullName), null);
            result[key] = Load<T>(key);
        }
        return result;
    }

    public Font LoadFont(string fontName, Color? fontColor = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fontName);

        var key = $"{fontName.Replace('\\', '/')}{RecolorSeparator}{fontColor?.PackedValue.ToString() ?? string.Empty}";
        if (_fonts.TryGetValue(key, out var font))
            return font;

        var texture = Load<Texture2D>(fontName);
        if (fontColor.HasValue)
            texture = texture.RecolorAsCopy(Color.Black, fontColor.Value);

        _fonts[key] = font = new Font(texture);

        return font;
    }

    /// <summary>
    /// Не кэширует.
    /// </summary>
    private Texture2D MixTextures(string texture1Name, string texture2Name)
    {
        var texture1 = Load<Texture2D>(texture1Name);
        var texture2 = Load<Texture2D>(texture2Name);

        return texture1.MixAverage(texture2);
    }
}