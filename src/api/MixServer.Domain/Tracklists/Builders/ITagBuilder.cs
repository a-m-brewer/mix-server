using MixServer.Domain.Tracklists.Models;

namespace MixServer.Domain.Tracklists.Builders;

public interface IReadOnlyTagBuilder
{
    ICollection<Chapter> Chapters { get; }
}

public interface ITagBuilder : IReadOnlyTagBuilder
{
    ITagBuilder AddChapter(
        string id,
        string title,
        string[] subtitles,
        string[] artists,
        ICollection<CustomTag> customTags);
    void ClearChapters(Func<Chapter, bool> selector);
    void Save();
}