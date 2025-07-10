using Microsoft.Extensions.Logging;
using MixServer.Domain.Tracklists.Builders;
using MixServer.Domain.Tracklists.Exceptions;
using MixServer.Domain.Tracklists.Factories;
using MixServer.Infrastructure.Tracklist.Builders;
using TagLib;

namespace MixServer.Infrastructure.Tracklist.Factories;

public class TagLibSharpTagBuilderFactory(ILoggerFactory loggerFactory) : ITagBuilderFactory
{
    public ITagBuilder Create(string filePath)
    {
        try
        {
            return new TagLibSharpTagBuilder(filePath, true, loggerFactory.CreateLogger<TagLibSharpTagBuilder>());
        }
        catch (UnsupportedFormatException e)
        {
            throw new UnsupportedFormatTagBuilderException(filePath, e);
        }
        catch (Exception e)
        {
            throw new TagBuilderException($"Failed to create tag builder for {filePath}", e);
        }
    }

    public IReadOnlyTagBuilder CreateReadOnly(string filePath)
    {
        try
        {
            return new TagLibSharpTagBuilder(filePath, false, loggerFactory.CreateLogger<TagLibSharpTagBuilder>());
        }
        catch (UnsupportedFormatException e)
        {
            throw new UnsupportedFormatTagBuilderException(filePath, e);
        }
        catch (Exception e)
        {
            throw new TagBuilderException($"Failed to create tag builder for {filePath}", e);
        }
    }
}