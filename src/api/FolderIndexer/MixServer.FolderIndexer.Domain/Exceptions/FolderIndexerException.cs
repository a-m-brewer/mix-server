namespace MixServer.FolderIndexer.Domain.Exceptions;

public abstract class FolderIndexerException : Exception
{
    public FolderIndexerException(string message) : base(message)
    {
    }
}