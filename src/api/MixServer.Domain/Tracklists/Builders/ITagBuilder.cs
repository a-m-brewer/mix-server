using MixServer.Domain.Tracklists.Models;

namespace MixServer.Domain.Tracklists.Builders;

public interface IReadOnlyTagBuilder
{
}

public interface ITagBuilder : IReadOnlyTagBuilder
{
    ITagBuilder AddChapter(
        TimeSpan startTime,
        string title,
        string[] subtitles,
        string[] artists,
        ICollection<CustomTag> customTags);
    void Save();
}