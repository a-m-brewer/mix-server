using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.Sessions.Repositories;

namespace MixServer.Infrastructure.EF.Repositories;

public class EfFolderSortRepository(MixServerDbContext context) : IFolderSortRepository
{
    public async Task AddAsync(FolderSort folderSort)
    {
        await context.FolderSorts.AddAsync(folderSort);
    }
}