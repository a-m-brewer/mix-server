using MixServer.Domain.FileExplorer.Enums;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Models.Caching;

namespace MixServer.Infrastructure.Extensions;

public static class FileSortExtensions
{
    public static IOrderedEnumerable<T> OrderNodes<T>(this IEnumerable<T> values, IFolderSort sort)
        where T : ICacheFileSystemInfo
    {
        Func<T, object> func = sort.SortMode switch
        {
            FolderSortMode.Name => info => info.Name,
            FolderSortMode.Created => info => info.CreationTimeUtc,
            _ => info => info.Name
        };

        return sort.Descending
            ? values.OrderByDescending(func)
            : values.OrderBy(func);
    }
}