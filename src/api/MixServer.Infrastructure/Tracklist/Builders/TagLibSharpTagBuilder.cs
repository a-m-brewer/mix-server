using MixServer.Domain.Tracklists.Builders;
using MixServer.Domain.Tracklists.Models;
using TagLib;
using TagLib.Id3v2;
using Tag = TagLib.Tag;

namespace MixServer.Infrastructure.Tracklist.Builders;

public class TagLibSharpTagBuilder : ITagBuilder
{
    private readonly TagLib.File _file;
    private readonly Tag _id3Tag;

    public TagLibSharpTagBuilder(string filePath)
    { 
        _file = TagLib.File.Create(filePath);
        
        if (_file.TagTypes != TagTypes.Id3v2)
        {
            _file.GetTag(TagTypes.Id3v2, true);
        }
        
        _id3Tag = (TagLib.Id3v2.Tag) _file.GetTag(TagTypes.Id3v2);
    }

    public ITagBuilder AddChapter(
        TimeSpan startTime,
        string title,
        string subtitle,
        string artist,
        ICollection<CustomTag> customTags)
    {
        var chapter = new ChapterFrame(startTime.ToString(), title);
        
        if (!string.IsNullOrEmpty(subtitle))
        {
            chapter.SubFrames.Add(new TextInformationFrame((ByteVector) "TIT3")
            {
                Text = [subtitle]
            });
        }
        
        if (!string.IsNullOrEmpty(artist))
        {
            chapter.SubFrames.Add(new TextInformationFrame((ByteVector) "TPE1")
            {
                Text = [artist]
            });
        }
        
        foreach (var customTag in customTags)
        {
            chapter.SubFrames.Add(new UserTextInformationFrame(customTag.description)
            {
                Text = [customTag.value]
            });
        }
        
        return this;
    }


    public void Save()
    {
        _file.Save();
    }
}