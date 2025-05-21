using MixServer.Domain.Exceptions;
using MixServer.Domain.Tracklists.Services;
using MixServer.Infrastructure.Users.Repository;
using MixServer.Shared.Interfaces;

namespace MixServer.Application.Tracklists.Commands.SaveTracklist;

public class SaveTracklistCommandHandler(
    ICurrentUserRepository currentUserRepository,
    ITracklistTagService tracklistTagService)
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

        tracklistTagService.SaveTags(currentSessionFilePath, request.Tracklist);

        return new SaveTracklistResponse
        {
            Tracklist = request.Tracklist
        };
    }
}