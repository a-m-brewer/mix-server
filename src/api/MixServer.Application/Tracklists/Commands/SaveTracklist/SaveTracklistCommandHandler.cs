using Microsoft.Extensions.Logging;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Tracklists.Builders;
using MixServer.Domain.Tracklists.Factories;
using MixServer.Domain.Tracklists.Models;
using MixServer.Domain.Tracklists.Services;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.Tracklists.Commands.SaveTracklist;

public class SaveTracklistCommandHandler(
    ICurrentUserRepository currentUserRepository,
    ITracklistTagService tracklistTagService)
    : ICommandHandler<SaveTracklistCommand, SaveTracklistResponse>
{
    public async Task<SaveTracklistResponse> HandleAsync(SaveTracklistCommand request)
    {
        var user = await currentUserRepository.GetCurrentUserAsync();
        await currentUserRepository.LoadCurrentPlaybackSessionAsync();

        if (user.CurrentPlaybackSession is null)
        {
            throw new InvalidRequestException("CurrentPlaybackSession", "No playback session is currently active.");
        }

        var currentSessionFilePath = user.CurrentPlaybackSession.NodeEntity.Path.AbsolutePath;

        if (!File.Exists(currentSessionFilePath))
        {
            throw new NotFoundException("File", currentSessionFilePath);
        }

        tracklistTagService.SaveTags(currentSessionFilePath, request.Tracklist);

        return new SaveTracklistResponse
        {
            Tracklist = request.Tracklist
        };
    }
}