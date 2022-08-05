using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Tank1460.Extensions;

namespace Tank1460;

public class ContentManagerEx : ContentManager
{
    private readonly Dictionary<string, Texture2D> _dynamicTextures = new(StringComparer.OrdinalIgnoreCase);

    public ContentManagerEx(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public ContentManagerEx(IServiceProvider serviceProvider, string rootDirectory) : base(serviceProvider, rootDirectory)
    {
    }

    public override void Unload()
    {
        _dynamicTextures.Clear();
        base.Unload();
    }

    public Texture2D LoadRecoloredTexture(string textureName, string recolorTextureName)
    {
        if (string.IsNullOrEmpty(textureName))
            throw new ArgumentNullException(nameof(textureName));

        string key = $"{textureName.Replace('\\', '/')}|{recolorTextureName.Replace('\\', '/')}";
        if (_dynamicTextures.TryGetValue(key, out var texture))
            return texture;

        texture = Load<Texture2D>(textureName);
        var recolorTexture = Load<Texture2D>(recolorTextureName);

        var recoloredTexture = texture.RecolorAsCopy(recolorTexture);
        _dynamicTextures[key] = recoloredTexture;
        return recoloredTexture;
    }

    public Dictionary<string, T> MassLoadContent<T>(string contentFolder)
    {
        var dir = new DirectoryInfo(Path.Combine(RootDirectory, contentFolder));
        if (!dir.Exists)
            throw new DirectoryNotFoundException();

        var result = new Dictionary<string, T>();

        var files = dir.GetFiles("*.*");
        foreach (var file in files)
        {
            var key = Path.GetFileNameWithoutExtension(file.Name);

            result[key] = Load<T>(Path.Combine(contentFolder, key));
        }
        return result;
    }
}