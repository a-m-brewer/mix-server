using MixServer.Domain.Tracklists.Builders;
using MixServer.Domain.Tracklists.Models;
using TagLib;
using TagLib.Id3v2;

namespace MixServer.Infrastructure.Tracklist.Builders;

public class TagLibSharpTagBuilder : ITagBuilder
{
    private readonly TagLib.File _file;
    private readonly TagLib.Id3v2.Tag _id3Tag;

    public TagLibSharpTagBuilder(
        string filePath,
        bool create)
    { 
        _file = TagLib.File.Create(filePath);
        
        if (_file.TagTypes != TagTypes.Id3v2)
        {
            _file.GetTag(TagTypes.Id3v2, create);
        }
        
        _id3Tag = (TagLib.Id3v2.Tag) _file.GetTag(TagTypes.Id3v2);
    }

    public ITagBuilder AddChapter(
        TimeSpan startTime,
        string title,
        string[] subtitles,
        string[] artists,
        ICollection<CustomTag> customTags)
    {
        var existingChapter = _id3Tag.GetFrames<ChapterFrame>().FirstOrDefault(f => f.Id == startTime.ToString());
        if (existingChapter is not null)
        {
            _id3Tag.RemoveFrame(existingChapter);
        }
        
        var chapter = new ChapterFrame(startTime.ToString(), title);
        
        if (subtitles.Length > 0)
        {
            chapter.SubFrames.Add(new TextInformationFrame((ByteVector) "TIT3")
            {
                Text = [
                    string.Join("/", subtitles)
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

    public void ClearChapters()
    {
        foreach (var frame in _id3Tag.GetFrames<ChapterFrame>().ToList())
        {
            _id3Tag.RemoveFrame(frame);
        }
    }


    public void Save()
    {
        _file.Save();
    }

    public ICollection<Chapter> Chapters => GetChapters();

    private ICollection<Chapter> GetChapters()
    {
        var chapters = new List<Chapter>();

        foreach (var frame in _id3Tag.GetFrames<ChapterFrame>())
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
            
            var chapter = new Chapter(
                frame.Id,
                title,
                subtitles,
                artists,
                customTags
            );

            chapters.Add(chapter);
        }

        return chapters;
    }
}