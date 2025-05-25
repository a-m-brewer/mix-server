using Microsoft.EntityFrameworkCore;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Streams.Entities;

namespace MixServer.Infrastructure.EF.Extensions;

public static class EfIncludeExtensions
{
    public static IQueryable<PlaybackSession> IncludeNode(this IQueryable<PlaybackSession> query)
    {
        return query.Include(session => session.Node)
            .ThenInclude(t => t!.RootChild)
            .Include(i => i.Node)
            .ThenInclude(t => t!.Transcode);
    }
    
    public static IQueryable<FolderSort> IncludeNode(this IQueryable<FolderSort> query)
    {
        return query.Include(session => session.Node)
            .ThenInclude(t => t!.RootChild);
    }
    
    public static IQueryable<Transcode> IncludeNode(this IQueryable<Transcode> query)
    {
        return query
            .Include(session => session.Node)
            .ThenInclude(t => t!.RootChild);
    }
}