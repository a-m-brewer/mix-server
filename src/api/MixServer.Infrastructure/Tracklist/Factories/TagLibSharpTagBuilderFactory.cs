using MixServer.Domain.Tracklists.Builders;
using MixServer.Domain.Tracklists.Factories;
using MixServer.Infrastructure.Tracklist.Builders;

namespace MixServer.Infrastructure.Tracklist.Factories;

public class TagLibSharpTagBuilderFactory : ITagBuilderFactory
{
    public ITagBuilder Create(string filePath)
    {
        return new TagLibSharpTagBuilder(filePath, true);
    }

    public IReadOnlyTagBuilder CreateReadOnly(string filePath)
    {
        return new TagLibSharpTagBuilder(filePath, false);
    }
}