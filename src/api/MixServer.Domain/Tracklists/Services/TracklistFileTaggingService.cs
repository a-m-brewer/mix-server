using Microsoft.Extensions.Logging;
using MixServer.Domain.Tracklists.Builders;
using MixServer.Domain.Tracklists.Dtos.Import;
using MixServer.Domain.Tracklists.Enums;
using MixServer.Domain.Tracklists.Factories;
using MixServer.Domain.Tracklists.Models;

namespace MixServer.Domain.Tracklists.Services;

public interface ITracklistFileTaggingService
{
    void SaveTags(string absoluteFilePath, ImportTracklistDto tracklist);
    ImportTracklistDto GetTracklist(IReadOnlyTagBuilder tagBuilder);
}

public class TracklistFileTaggingService(
    ITagBuilderFactory factory,
    ILogger<TracklistFileTaggingService> logger) : ITracklistFileTaggingService
{
    private const string IdPrefix = "ms-ch-";
    
    public void SaveTags(string absoluteFilePath, ImportTracklistDto tracklist)
    {
        using var tagBuilder = factory.Create(absoluteFilePath);
        
        // TODO: actually compare tags
        tagBuilder.ClearChapters(c => c.Id.StartsWith(IdPrefix));

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
                $"{IdPrefix}{cue.Cue}",
                primaryTrack.Name,
                additionalTracks.Select(t => t.Name).ToArray(),
                cue.Tracks.Select(t => t.Artist).ToArray(),
                customTags);
        }

        tagBuilder.Save();
    }
    
    public ImportTracklistDto GetTracklist(IReadOnlyTagBuilder tagBuilder)
    {
        var cues = new List<ImportCueDto>();

        foreach (var chapter in tagBuilder.Chapters.Where(c => c.Id.StartsWith(IdPrefix)))
        {
            if (!TimeSpan.TryParse(chapter.Id.Replace(IdPrefix, ""), out var startTime))
            {
                logger.LogWarning("Skipping chapter with no timestamp: {Id}", chapter.Id);
                continue;
            }

            var titles = new List<string> { chapter.Title };
            titles.AddRange(chapter.SubTitles);
            var artists = chapter.Artists;
            
            var players = new List<(string trackName, string trackArtist, ImportPlayerDto player)>();
            foreach (var tag in chapter.CustomTags)
            {
                var descriptionSplit = tag.description.Split(";");
                if (descriptionSplit.Length != 3)
                {
                    logger.LogWarning("Skipping custom player tag with unexpected description format (invalid split length): {Description}", tag.description);
                    continue;
                }
                
                var trackName = descriptionSplit[0];
                var trackArtist = descriptionSplit[1];
                
                foreach (var tagValue in tag.values)
                {
                    var playerTypeSplit = tagValue.Split(";");
                        
                    if (playerTypeSplit.Length != 2)
                    {
                        logger.LogWarning("Skipping custom player tag with unexpected value format (invalid split length): {Value}", tagValue);
                        continue;
                    }
                        
                    var playerType = playerTypeSplit[0];
                        
                    if (!Enum.TryParse<TracklistPlayerType>(playerType, true, out var playerTypeEnum))
                    {
                        logger.LogWarning("Skipping custom player tag with unexpected player type: {PlayerType}", playerType);
                        continue;
                    }
                        
                    var urls = playerTypeSplit[1].Split(",").ToList();

                    var player = new ImportPlayerDto
                    {
                        Type = playerTypeEnum,
                        Urls = urls
                    };
                        
                    players.Add((trackName, trackArtist, player));
                }
            }

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
                    continue;
                }

                var artist = i < artists.Length ? artists[i] : "Unknown Artist";
                
                var track = new ImportTrackDto
                {
                    Name = title,
                    Artist = artist,
                    Players = players
                        .Where(w => w.trackName == title && w.trackArtist == artist)
                        .Select(s => s.player)
                        .ToList()
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