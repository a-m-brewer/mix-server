using Microsoft.EntityFrameworkCore;
using MixServer.FolderIndexer.Domain.Entities;

namespace MixServer.FolderIndexer.Data.EF.Extensions;

internal static class FileSystemInfoEntityExtensions
{
    public static IQueryable<TEntity> IncludeChildren<TEntity>(this IQueryable<TEntity> query)
        where TEntity : FileSystemInfoEntity
    {
        return typeof(TEntity).IsAssignableTo(typeof(DirectoryInfoEntity))
            ? query.Include(i => (i as DirectoryInfoEntity)!.Children)
            : query;
    }
}