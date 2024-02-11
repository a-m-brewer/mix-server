using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.Sessions.Repositories;

namespace MixServer.Infrastructure.EF.Repositories;

public class EfFolderSortRepository : IFolderSortRepository
{
    private readonly MixServerDbContext _context;

    public EfFolderSortRepository(MixServerDbContext context)
    {
        _context = context;
    }
    
    public async Task AddAsync(FolderSort folderSort)
    {
        await _context.FolderSorts.AddAsync(folderSort);
    }
}