using MixServer.Domain.FileExplorer.Models;

namespace MixServer.Domain.FileExplorer.Services;

public interface IFileService
{
    Task<IFileExplorerFolder> GetFolderAsync(string absolutePath);
    Task<IFileExplorerFolder> GetFolderOrRootAsync(string? absolutePath);
    Task<List<IFileExplorerFileNode>> GetFilesAsync(IReadOnlyList<string> absoluteFilePaths);
    Task<IFileExplorerFileNode> GetFileAsync(string absoluteFolderPath, string fileName);
    Task<IFileExplorerFileNode> GetFileAsync(string absoluteFilePath);
    void CopyNode(
        string sourcePath,
        string destinationFolder,
        string destinationName,
        bool move,
        bool overwrite);
    void DeleteNode(string absolutePath);
    Task SetFolderSortAsync(IFolderSortRequest request);
}