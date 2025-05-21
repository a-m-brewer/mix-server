using MixServer.FolderIndexer.Tags.Models;

namespace MixServer.FolderIndexer.Tags.Interface.Interfaces;

public interface IReadOnlyTagBuilder : IDisposable
{
    TimeSpan Duration { get; }
    
    int Bitrate { get; }
    
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