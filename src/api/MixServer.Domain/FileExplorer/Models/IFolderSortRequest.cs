namespace MixServer.Domain.FileExplorer.Models;

public interface IFolderSortRequest : IFolderSort
{
    public string AbsoluteFolderPath { get; }
}