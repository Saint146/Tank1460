using System.Drawing;

namespace Tank1460.LevelImport;

public class PngLevelImporter
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
    public string ConvertPngToLvl(Bitmap bitmap)
    {
        var pixel = bitmap.GetPixel(1, 1);

        // см. Tank1460.LevelStructure.TileTypeFromChar

        return "asdasd";
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
    public Bitmap CreateBitmap(byte[] imageData)
    {
        using var stream = new MemoryStream(imageData);

        return new Bitmap(stream);
    }
}