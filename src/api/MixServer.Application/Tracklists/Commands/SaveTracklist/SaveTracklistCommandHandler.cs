using Microsoft.Extensions.Logging;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Tracklists.Builders;
using MixServer.Domain.Tracklists.Factories;
using MixServer.Domain.Tracklists.Models;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.Tracklists.Commands.SaveTracklist;

public class SaveTracklistCommandHandler(
    ICurrentUserRepository currentUserRepository,
    ILogger<SaveTracklistCommandHandler> logger,
    ITagBuilderFactory tagBuilderFactory)
    : ICommandHandler<SaveTracklistCommand, SaveTracklistResponse>
{
    public async Task<SaveTracklistResponse> HandleAsync(SaveTracklistCommand request)
    {
        await currentUserRepository.LoadCurrentPlaybackSessionAsync();

        if (currentUserRepository.CurrentUser.CurrentPlaybackSession is null)
        {
            throw new InvalidRequestException("CurrentPlaybackSession", "No playback session is currently active.");
        }

        var currentSessionFilePath = currentUserRepository.CurrentUser.CurrentPlaybackSession.AbsolutePath;

        if (!File.Exists(currentSessionFilePath))
        {
            throw new NotFoundException("File", currentSessionFilePath);
        }

        var tagBuilder = tagBuilderFactory.Create(currentSessionFilePath);

        foreach (var cue in request.Tracklist.Cues)
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

        return new SaveTracklistResponse
        {
            Tracklist = request.Tracklist
        };
    }
}