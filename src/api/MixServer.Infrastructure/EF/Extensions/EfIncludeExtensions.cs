using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Enums;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Repositories.DbQueryOptions;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Streams.Entities;
using MixServer.Domain.Users.Models;
using Range = MixServer.Domain.FileExplorer.Models.Range;

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
        query = query
            .Include(navigationPropertyPath)
            .ThenInclude(t => t!.RootChild)
            .Include(navigationPropertyPath)
            .ThenInclude(t => t!.Parent);
        
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
        
        if (options.IncludeTracklist)
        {
            query = query
                .Include(navigationPropertyPath)
                .ThenInclude(t => t!.Tracklist)
                .ThenInclude(t => t!.Cues.OrderBy(o => o.Cue.ToString()))
                .ThenInclude(t => t.Tracks.OrderBy(o => o.Name).ThenBy(tb => tb.Artist))
                .ThenInclude(t => t.Players.OrderBy(b => b.Type));
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
        
        if (options.IncludeTracklist)
        {
            query = query
                .Include(i => i.Tracklist)
                .ThenInclude(t => t!.Cues.OrderBy(o => o.Cue.ToString()))
                .ThenInclude(t => t.Tracks.OrderBy(o => o.Name).ThenBy(tb => tb.Artist))
                .ThenInclude(t => t.Players.OrderBy(b => b.Type));
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
    
    public static IQueryable<FileExplorerFileNodeEntity> IncludeTracklist(this IQueryable<FileExplorerFileNodeEntity> query) 
    {
        return query
            .Include(t => t!.Tracklist)
            .ThenInclude(t => t!.Cues.OrderBy(o => o.Cue.ToString()))
            .ThenInclude(t => t.Tracks.OrderBy(o => o.Name).ThenBy(tb => tb.Artist))
            .ThenInclude(t => t.Players.OrderBy(b => b.Type));
    }

    public static IQueryable<FileExplorerNodeEntity> ApplySort(this IQueryable<FileExplorerNodeEntity> query, IFolderSort sort, Page? page, Range? range)
    {
        if (page is not null && range is not null)
        {
            throw new ArgumentException("Cannot specify both page and range");
        }
        
        Expression<Func<FileExplorerNodeEntity, object>> func = sort.SortMode switch
        {
            FolderSortMode.Name => i => i.RelativePath,
            FolderSortMode.Created => i => i.CreationTimeUtc,
            _ => i => i.RelativePath
        };

        var (directoryIndex, fileIndex) = sort.GetDirectoryFileSort();

        var orderedQuery = query.OrderBy(o => o.NodeType == FileExplorerEntityNodeType.File ? fileIndex : directoryIndex);
        
        orderedQuery = sort.Descending
            ? orderedQuery.ThenByDescending(func)
            : orderedQuery.ThenBy(func);

        orderedQuery = orderedQuery.ThenBy(t => t.Id);

        if (page is not null)
        {
            return orderedQuery.Skip(page.PageIndex * page.PageSize)
                .Take(page.PageSize);
        }
        
        if (range is not null)
        {
            return orderedQuery
                .Skip(range.Start)
                .Take(range.End);
        }

        return orderedQuery;
    }
    
    private static (int DirectoryIndex, int FileIndex) GetDirectoryFileSort(this IFolderSort sort)
    {
        return sort.SortMode == FolderSortMode.Name
            ? (0, 1)
            : (1, 0);
    }
}