using Microsoft.EntityFrameworkCore;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Sessions.Repositories;

namespace MixServer.Infrastructure.EF.Repositories;

public class EfPlaybackSessionRepository : IPlaybackSessionRepository
{
    private readonly MixServerDbContext _context;

    public EfPlaybackSessionRepository(MixServerDbContext context)
    {
        _context = context;
    }
    
    public async Task<PlaybackSession> GetAsync(Guid id)
    {
        return await _context.PlaybackSessions.SingleOrDefaultAsync(s => s.Id == id)
               ?? throw new NotFoundException(nameof(_context.PlaybackSessions), id);
    }

    public async Task AddAsync(PlaybackSession session)
    {
        await _context.PlaybackSessions.AddAsync(session);
    }
}