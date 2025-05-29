using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Persistence;

namespace MixServer.Domain.Sessions.Repositories;

public interface IFolderSortRepository : ITransientRepository
{
    Task AddAsync(FolderSort folderSort, CancellationToken cancellationToken);

    Task<Dictionary<string, IFolderSort>> GetFolderSortsAsync(IReadOnlyCollection<string> usernames, NodePath nodePath, CancellationToken cancellationToken);
}