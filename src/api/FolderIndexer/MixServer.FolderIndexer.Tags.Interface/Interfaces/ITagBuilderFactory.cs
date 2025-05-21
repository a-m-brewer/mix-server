namespace MixServer.FolderIndexer.Tags.Interface.Interfaces;

public interface ITagBuilderFactory
{
    ITagBuilder Create(string filePath);

    IReadOnlyTagBuilder CreateReadOnly(string filePath);
}