namespace MixServer.Domain.FileExplorer.Models;

public interface IFolderSortRequest : IFolderSort
{
    NodePath Path { get; }
}