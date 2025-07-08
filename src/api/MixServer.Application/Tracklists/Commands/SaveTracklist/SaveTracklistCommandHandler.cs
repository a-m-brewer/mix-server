using MixServer.Domain.Exceptions;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Tracklists.Services;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.Tracklists.Commands.SaveTracklist;

public class SaveTracklistCommandHandler(
    ICurrentUserRepository currentUserRepository,
    ITracklistPersistenceService tracklistPersistenceService,
    ITracklistFileTaggingService tracklistFileTaggingService,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SaveTracklistCommand, SaveTracklistResponse>
{
    public async Task<SaveTracklistResponse> HandleAsync(SaveTracklistCommand request, CancellationToken cancellationToken = default)
    {
        var user = await currentUserRepository.GetCurrentUserAsync();
        await currentUserRepository.LoadCurrentPlaybackSessionAsync(cancellationToken);

        if (user.CurrentPlaybackSession is null)
        {
            throw new InvalidRequestException("CurrentPlaybackSession", "No playback session is currently active.");
        }

        var currentSessionFilePath = user.CurrentPlaybackSession.NodeEntity.Path.AbsolutePath;

        if (!File.Exists(currentSessionFilePath))
        {
            throw new NotFoundException("File", currentSessionFilePath);
        }

        await tracklistPersistenceService.AddOrUpdateTracklistAsync(user.CurrentPlaybackSession.NodeEntity, 
            request.Tracklist, cancellationToken);
        tracklistFileTaggingService.SaveTags(currentSessionFilePath, request.Tracklist);
        
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SaveTracklistResponse
        {
            Tracklist = request.Tracklist
        };
    }
}