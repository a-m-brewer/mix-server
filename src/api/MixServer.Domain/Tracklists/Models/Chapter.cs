namespace MixServer.Domain.Tracklists.Models;

public class Chapter(
    string id,
    string title,
    string[] subTitles,
    string[] artists,
    List<CustomTag> customTags)
{
    public string Id { get; } = id;
    
    public string Title { get; } = title;
    
    public string[] SubTitles { get; } = subTitles;
    
    public string[] Artists { get; } = artists;

    public List<CustomTag> CustomTags { get; } = customTags;
}