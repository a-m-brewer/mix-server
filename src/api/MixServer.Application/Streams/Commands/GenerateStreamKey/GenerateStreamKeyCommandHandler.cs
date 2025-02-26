using System.Web;
using FluentValidation;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Interfaces;
using MixServer.Infrastructure.Users.Repository;
using MixServer.Infrastructure.Users.Services;

namespace MixServer.Application.Streams.Commands.GenerateStreamKey;

public class GenerateStreamKeyCommandHandler(
    ICurrentUserRepository currentUserRepository,
    IJwtService jwtService,
    IValidator<GenerateStreamKeyCommand> validator) 
    : ICommandHandler<GenerateStreamKeyCommand, GenerateStreamKeyResponse>
{
    public async Task<GenerateStreamKeyResponse> HandleAsync(GenerateStreamKeyCommand request)
    {
        await validator.ValidateAndThrowAsync(request);

        await currentUserRepository.LoadPlaybackSessionAsync(request.PlaybackSessionId);
        
        if (currentUserRepository.CurrentUser.PlaybackSessions.All(s => s.Id != request.PlaybackSessionId))
        {
            throw new NotFoundException(nameof(currentUserRepository.CurrentUser.PlaybackSessions), request.PlaybackSessionId);
        }

        var (key, expires) = jwtService.GenerateKey(request.PlaybackSessionId.ToString());
        
        return new GenerateStreamKeyResponse
        {
            Key = HttpUtility.UrlEncode(key),
            Expires = expires
        };
    }
}