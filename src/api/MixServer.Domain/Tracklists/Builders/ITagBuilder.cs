using MixServer.Domain.Tracklists.Models;

namespace MixServer.Domain.Tracklists.Builders;

public interface ITagBuilder
{
    ITagBuilder AddChapter(
        TimeSpan startTime,
        string title,
        string subtitle,
        string artist,
        ICollection<CustomTag> customTags);
    void Save();
}