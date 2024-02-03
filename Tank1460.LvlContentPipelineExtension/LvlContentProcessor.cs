using Microsoft.Xna.Framework.Content.Pipeline;

namespace Tank1460.LvlContentPipelineExtension;

[ContentProcessor(DisplayName = "Tank1460 Level Processor")]
internal class LvlContentProcessor : ContentProcessor<string, LvlContentResult>
{
    public override LvlContentResult Process(string input, ContentProcessorContext context)
    {
        return new LvlContentResult
        {
            Xml = input
        };
    }
}