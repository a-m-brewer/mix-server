using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Repositories.DbQueryOptions;
using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Streams.Entities;

namespace MixServer.Infrastructure.EF.Extensions;

public static class EfIncludeExtensions
{
    public static IQueryable<PlaybackSession> IncludeNode(this IQueryable<PlaybackSession> query, GetFileQueryOptions fileQueryOptions)
    {
        return query
            .IncludeGetFileQueryOptions(f => f.Node, fileQueryOptions);
    }
    
    public static IQueryable<T> IncludeGetFileQueryOptions<T>(
        this IQueryable<T> query,
        Expression<Func<T, FileExplorerFileNodeEntity?>> navigationPropertyPath,
        GetFileQueryOptions options) where T : class
    {
        if (options.IncludeMetadata)
        {
            query = query
                .Include(navigationPropertyPath)
                .ThenInclude(t => t!.Metadata);
        }

        if (options.IncludeTranscode)
        {
            query = query
                .Include(navigationPropertyPath)
                .ThenInclude(t => t!.Transcode);
        }

        return query;
    }
    
    public static IQueryable<FileExplorerFileNodeEntity> IncludeGetFileQueryOptions(this IQueryable<FileExplorerFileNodeEntity> query, GetFileQueryOptions options)
    {
        if (options.IncludeMetadata)
        {
            query = query
                .Include(i => i.Metadata);
        }

        if (options.IncludeTranscode)
        {
            query = query
                .Include(i => i.Transcode);
        }

        return query;
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

    public static IQueryable<TEntity> IncludeParents<TEntity>(this IQueryable<TEntity> query)
        where TEntity : FileExplorerNodeEntity
    {
        return query.Include(i => i.RootChild)
            .Include(i => i.Parent);
    }
}