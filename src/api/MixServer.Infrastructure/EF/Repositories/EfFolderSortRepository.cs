using Microsoft.EntityFrameworkCore;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Sessions.Repositories;

namespace MixServer.Infrastructure.EF.Repositories;

public class EfFolderSortRepository(MixServerDbContext context) : IFolderSortRepository
{
    public async Task AddAsync(FolderSort folderSort)
    {
        await context.FolderSorts.AddAsync(folderSort);
    }
    
    public async Task<Dictionary<string, IFolderSort>> GetFolderSortsAsync(IReadOnlyCollection<string> usernames, string absoluteFolderPath)
    {
        var query = from folderSort in context.FolderSorts
            join dbUser in context.Users on folderSort.UserId equals dbUser.Id
            where usernames.Contains(dbUser.UserName) &&
                  folderSort.AbsoluteFolderPath == absoluteFolderPath
            select new {dbUser.UserName, folderSort};
        
        return await query.ToDictionaryAsync(k => k.UserName, IFolderSort (v) => v.folderSort);
    }
}