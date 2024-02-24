using System;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;
using Microsoft.Xna.Framework.Content.Pipeline;

namespace Tank1460.LvlContentPipelineExtension;

[ContentImporter(".lvl", DisplayName = "Tank1460 Level Importer", DefaultProcessor = nameof(LvlContentProcessor))]
public class LvlContentImporter : ContentImporter<string>
{
    public override string Import(string filename, ContentImporterContext context)
    {
        var xml = File.ReadAllText(filename);

        var shortName = Path.GetFileNameWithoutExtension(filename);
        xml = ValidateAndAddInfo(xml, shortName, filename);

        Debugger.Log(9, "A", xml);

        return xml;
    }

    private static string ValidateAndAddInfo(string xml, string shortName, string fullPath)
    {
        if (string.IsNullOrEmpty(xml))
            throw new InvalidContentException("The lvl file is empty.");

        XDocument document;
        try
        {
            document = XDocument.Parse(xml);
        }
        catch (Exception ex)
        {
            throw new InvalidContentException("This does not appear to be a valid xml. See inner exception for details.", ex);
        }

        var levelElement = document.Element("level") ?? throw new Exception("Invalid level: cannot find root element 'level'.");
        _ = levelElement.Element("tiles")?.Value ?? throw new Exception("Invalid level: cannot find element 'level/tiles'.");

        levelElement.AddFirst(new XElement("info",
                                           new XAttribute("shortName", shortName),
                                           new XAttribute("fullPath", fullPath)));

        return document.ToString();
    }
}