using Microsoft.Extensions.Logging;
using MixServer.Domain.Tracklists.Dtos.Import;
using MixServer.Domain.Tracklists.Factories;
using MixServer.Domain.Tracklists.Models;

namespace MixServer.Domain.Tracklists.Services;

public interface ITracklistTagService
{
    void SaveTags(string absoluteFilePath, ImportTracklistDto tracklist);
    ImportTracklistDto GetTracklistForFile(string absolutePath);
}

public class TracklistTagService(
    ITagBuilderFactory factory,
    ILogger<TracklistTagService> logger) : ITracklistTagService
{
    public void SaveTags(string absoluteFilePath, ImportTracklistDto tracklist)
    {
        var tagBuilder = factory.Create(absoluteFilePath);
        
        // TODO: actually compare tags
        tagBuilder.ClearChapters();

        foreach (var cue in tracklist.Cues)
        {
            if (cue.Tracks.Count == 0)
            {
                logger.LogWarning("Skipping cue: {Cue} with no tracks", cue.Cue);
                continue;
            }

            var primaryTrack = cue.Tracks.First();
            var additionalTracks = cue.Tracks.Skip(1).ToList();

            var customTags = (from track in cue.Tracks
                let lines =
                    (from player in track.Players
                        let urls = string.Join(",", player.Urls)
                        where urls.Length > 0
                        select $"{player.Type};{urls}").ToArray()
                where lines.Length > 0
                select new CustomTag($"{track.Name};{track.Artist};Players", lines)).ToList();

            tagBuilder.AddChapter(
                cue.Cue,
                primaryTrack.Name,
                additionalTracks.Select(t => t.Name).ToArray(),
                cue.Tracks.Select(t => t.Artist).ToArray(),
                customTags);
        }

        tagBuilder.Save();
    }
    
    public ImportTracklistDto GetTracklistForFile(string absolutePath)
    {
        var tagBuilder = factory.CreateReadOnly(absolutePath);

        var cues = new List<ImportCueDto>();

        foreach (var chapter in tagBuilder.Chapters)
        {
            var startTime = TimeSpan.Parse(chapter.Id);
            
            var titles = new List<string> { chapter.Title };
            titles.AddRange(chapter.SubTitles);
            var artists = chapter.Artists;

            var cue = new ImportCueDto
            {
                Cue = startTime,
                Tracks = []
            };
            
            for (var i = 0; i < titles.Count; i++)
            {
                var title = titles[i];
                
                if (string.IsNullOrWhiteSpace(title))
                {
                    logger.LogWarning("Skipping track with no title");
                    continue;
                }
                
                var track = new ImportTrackDto
                {
                    Name = title,
                    Artist = artists[i],
                    Players = []
                };
                
                cue.Tracks.Add(track);
            }
            
            cues.Add(cue);
        }
        
        return new ImportTracklistDto
        {
            Cues = cues
        };
    }
}