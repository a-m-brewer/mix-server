using Microsoft.Extensions.Logging;
using MixServer.FolderIndexer.Tags.Builders;
using MixServer.FolderIndexer.Tags.Interface.Interfaces;

namespace MixServer.FolderIndexer.Tags.Factories;

internal class TagLibSharpTagBuilderFactory(ILoggerFactory loggerFactory) : ITagBuilderFactory
{
    public ITagBuilder Create(string filePath)
    {
        return new TagLibSharpTagBuilder(filePath, true, loggerFactory.CreateLogger<TagLibSharpTagBuilder>());
    }

    public IReadOnlyTagBuilder CreateReadOnly(string filePath)
    {
        return new TagLibSharpTagBuilder(filePath, false, loggerFactory.CreateLogger<TagLibSharpTagBuilder>());
    }
}