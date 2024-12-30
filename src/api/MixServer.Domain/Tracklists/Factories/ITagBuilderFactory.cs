using MixServer.Domain.Tracklists.Builders;

namespace MixServer.Domain.Tracklists.Factories;

public interface ITagBuilderFactory
{
    ITagBuilder Create(string filePath);

    IReadOnlyTagBuilder CreateReadOnly(string filePath);
}