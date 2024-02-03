using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;

namespace Tank1460.LvlContentPipelineExtension;

[ContentTypeWriter]
internal class LvlContentWriter : ContentTypeWriter<LvlContentResult>
{
    protected override void Write(ContentWriter output, LvlContentResult value)
    {
        output.Write(value.Xml);
    }

    public override string GetRuntimeReader(TargetPlatform targetPlatform)
    {
        return "Tank1460.Common.ContentPipeline.LvlContentTypeReader, Tank1460.Common";
    }
}