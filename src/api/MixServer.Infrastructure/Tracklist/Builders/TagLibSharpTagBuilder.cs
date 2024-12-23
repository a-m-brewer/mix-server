using MixServer.Domain.Tracklists.Builders;
using MixServer.Domain.Tracklists.Models;
using TagLib;
using TagLib.Id3v2;
using Tag = TagLib.Tag;

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


    public void Save()
    {
        _file.Save();
    }
}