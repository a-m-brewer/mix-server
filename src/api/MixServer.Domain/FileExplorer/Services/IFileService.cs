using MixServer.Domain.FileExplorer.Models;

namespace MixServer.Domain.FileExplorer.Services;

public interface IFileService
{
    Task<IFileExplorerFolder> GetFolderAsync(string absolutePath);
    Task<IFileExplorerFolder> GetFolderOrRootAsync(string? absolutePath);
    List<IFileExplorerFileNode> GetFiles(IReadOnlyList<string> absoluteFilePaths);
    IFileExplorerFileNode GetFile(string absoluteFolderPath, string fileName);
    IFileExplorerFileNode GetFile(string absoluteFilePath);
    Task SetFolderSortAsync(IFolderSortRequest request);
}