using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Persistence;
using MixServer.Shared.Interfaces;

namespace MixServer.Domain.Sessions.Repositories;

public interface IFolderSortRepository : ITransientRepository
{
    Task AddAsync(FolderSort folderSort);

    Task<Dictionary<string, IFolderSort>> GetFolderSortsAsync(IReadOnlyCollection<string> usernames, string absoluteFolderPath);
}