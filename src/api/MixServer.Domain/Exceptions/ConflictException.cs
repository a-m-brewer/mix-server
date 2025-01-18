namespace MixServer.Domain.Exceptions;

public class ConflictException : MixServerException
{
    public ConflictException(string table, string id) : base($"Item with id {id} already exists in {table}")
    {
    }
}