using Microsoft.EntityFrameworkCore;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Repositories;
using MixServer.Infrastructure.EF.Extensions;

namespace MixServer.Infrastructure.EF.Repositories;

public class EfFolderSortRepository(MixServerDbContext context) : IFolderSortRepository
{
    public async Task AddAsync(FolderSort folderSort, CancellationToken cancellationToken)
    {
        await context.FolderSorts.AddAsync(folderSort, cancellationToken);
    }
    
    public async Task<Dictionary<string, IFolderSort>> GetFolderSortsAsync(IReadOnlyCollection<string> usernames, NodePath nodePath, CancellationToken cancellationToken)
    {
        var query = from folderSort in context.FolderSorts
                .IncludeNode()
            join dbUser in context.Users on folderSort.UserId equals dbUser.Id
            where usernames.Contains(dbUser.UserName) &&
                  folderSort.Node != null &&
                  folderSort.Node.RootChild.RelativePath == nodePath.RootPath &&
                  folderSort.Node.RelativePath == nodePath.RelativePath
            select new {dbUser.UserName, folderSort};
        
        return await query.ToDictionaryAsync(k => k.UserName, IFolderSort (v) => v.folderSort, cancellationToken: cancellationToken);
    }
}