using MixServer.Domain.FileExplorer.Models;

namespace MixServer.Domain.FileExplorer.Services;

public interface IFileService
{
    Task<IFileExplorerFolderNode> GetFolderAsync(string absolutePath);
    Task<IFileExplorerFolderNode> GetFolderOrRootAsync(string? absolutePath);
    List<IFileExplorerFileNode> GetFiles(IReadOnlyList<string> absoluteFilePaths);
    IFileExplorerFileNode GetFile(string absoluteFolderPath, string fileName);
    IFileExplorerFileNode GetFile(string absoluteFilePath);
    Task SetFolderSortAsync(IFolderSortRequest request);
}