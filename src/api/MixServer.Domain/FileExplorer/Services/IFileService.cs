using MixServer.Domain.FileExplorer.Models;

namespace MixServer.Domain.FileExplorer.Services;

public interface IFileService
{
    Task<IFileExplorerFolderNode> GetFolderAsync(string absolutePath);
    IFileExplorerFolderNode GetUnpopulatedFolder(string absolutePath);
    Task<IFileExplorerFolderNode> GetFolderOrRootAsync(string? absolutePath);
    Task<IFileExplorerFolderNode> GetFilesInFolderAsync(string absolutePath);
    List<IFileExplorerFileNode> GetFiles(IReadOnlyList<string> absoluteFilePaths);
    IFileExplorerFileNode GetFile(string absoluteFolderPath, string fileName);
    IFileExplorerFileNode GetFile(string fileAbsolutePath, IFileExplorerFolderNode parent);
    Task SetFolderSortAsync(IFolderSortRequest request);
}