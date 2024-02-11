using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.Persistence;

namespace MixServer.Domain.Sessions.Repositories;

public interface IFolderSortRepository : ITransientRepository
{
    Task AddAsync(FolderSort folderSort);
}