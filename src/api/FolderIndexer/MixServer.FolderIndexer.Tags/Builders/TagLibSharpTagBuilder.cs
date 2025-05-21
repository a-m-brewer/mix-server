using Microsoft.Extensions.Logging;
using MixServer.FolderIndexer.Tags.Interface.Interfaces;
using MixServer.FolderIndexer.Tags.Models;
using TagLib;
using TagLib.Id3v2;

namespace MixServer.FolderIndexer.Tags.Builders;

internal class TagLibSharpTagBuilder : ITagBuilder
{
    private readonly ILogger<TagLibSharpTagBuilder> _logger;
    private readonly TagLib.File _file;
    private readonly TagLib.Id3v2.Tag? _id3Tag;

    public TagLibSharpTagBuilder(
        string filePath,
        bool create,
        ILogger<TagLibSharpTagBuilder> logger)
    {
        _logger = logger;
        _file = TagLib.File.Create(filePath);
        
        if (_file.TagTypes != TagTypes.Id3v2)
        {
            _file.GetTag(TagTypes.Id3v2, create);
        }
        
        _id3Tag = (TagLib.Id3v2.Tag) _file.GetTag(TagTypes.Id3v2);
    }

    public ITagBuilder AddChapter(
        string id,
        string title,
        string[] subtitles,
        string[] artists,
        ICollection<CustomTag> customTags)
    {
        if (_id3Tag is null)
        {
            return this;
        }

        var existingChapter = _id3Tag.GetFrames<ChapterFrame>().FirstOrDefault(f => f.Id == id);
        if (existingChapter is not null)
        {
            _id3Tag.RemoveFrame(existingChapter);
        }
        
        var chapter = new ChapterFrame(id, title.Replace("/", "-"));
        
        if (subtitles.Length > 0)
        {
            chapter.SubFrames.Add(new TextInformationFrame((ByteVector) "TIT3")
            {
                Text = [
                    string.Join("/", subtitles.Select(s => s.Replace("/", "-")))
                ]
            });
        }
        
        if (artists.Length > 0)
        {
            chapter.SubFrames.Add(new TextInformationFrame((ByteVector) "TPE1")
            {
                Text = artists
            });
        }
        
        foreach (var customTag in customTags)
        {
            if (customTag.values.Length == 0)
            {
                continue;
            }
            
            chapter.SubFrames.Add(new UserTextInformationFrame(customTag.description)
            {
                Text = customTag.values
            });
        }
        
        _id3Tag.AddFrame(chapter);
        
        return this;
    }

    public void ClearChapters(Func<Chapter, bool> selector)
    {
        if (_id3Tag is null)
        {
            return;
        }
        
        foreach (var frame in _id3Tag.GetFrames<ChapterFrame>().Where(f => selector(ToChapter(f))).ToList())
        {
            _id3Tag.RemoveFrame(frame);
        }
    }


    public void Save()
    {
        _file.Save();
    }

    public TimeSpan Duration => _file.Properties.Duration;

    public int Bitrate => _file.Properties.AudioBitrate;

    public ICollection<Chapter> Chapters => GetChapters();

    private ICollection<Chapter> GetChapters()
    {
        if (_id3Tag is not null) return _id3Tag.GetFrames<ChapterFrame>().Select(ToChapter).ToList();
        return new List<Chapter>();

    }

    private Chapter ToChapter(ChapterFrame frame)
    {
        var titleFrame = frame.SubFrames
            .OfType<TextInformationFrame>()
            .SingleOrDefault(f => f.FrameId == "TIT2");
        var title = titleFrame is not null && titleFrame.Text.Length > 0
            ? titleFrame.Text[0] ?? string.Empty
            : string.Empty;
            
        var subtitleFrame = frame.SubFrames
            .OfType<TextInformationFrame>()
            .SingleOrDefault(f => f.FrameId == "TIT3");
        var subtitles = subtitleFrame is not null && subtitleFrame.Text.Length > 0
            ? subtitleFrame.Text[0]?.Split('/') ?? []
            : [];
            
        var artistFrame = frame.SubFrames
            .OfType<TextInformationFrame>()
            .SingleOrDefault(f => f.FrameId == "TPE1");
        var artists = artistFrame is not null && artistFrame.Text.Length > 0
            ? artistFrame.Text ?? []
            : [];
            
        var customTags = frame.SubFrames
            .OfType<UserTextInformationFrame>()
            .Select(f => new CustomTag(f.Description, f.Text))
            .ToList();
            
        return new Chapter(
            frame.Id,
            title,
            subtitles,
            artists,
            customTags
        );
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        
        _file.Dispose();
    }
}