namespace MixServer.FolderIndexer.Domain.Exceptions;

public class FolderIndexerEntityNotFoundException : FolderIndexerException
{
    public FolderIndexerEntityNotFoundException(string table, object id) : base($"Entity '{table}' with id '{id}' not found.")
    {
    }
}