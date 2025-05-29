using Microsoft.EntityFrameworkCore;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Sessions.Repositories;
using MixServer.Infrastructure.EF.Extensions;

namespace MixServer.Infrastructure.EF.Repositories;

public class EfPlaybackSessionRepository(MixServerDbContext context) : IPlaybackSessionRepository
{
    public async Task<PlaybackSession> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.PlaybackSessions
                   .IncludeNode()
                   .SingleOrDefaultAsync(s => s.Id == id, cancellationToken)
               ?? throw new NotFoundException(nameof(context.PlaybackSessions), id);
    }

    public async Task AddAsync(PlaybackSession session, CancellationToken cancellationToken)
    {
        await context.PlaybackSessions.AddAsync(session, cancellationToken);
    }
}