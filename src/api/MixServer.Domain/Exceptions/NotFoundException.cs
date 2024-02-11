namespace MixServer.Domain.Exceptions;

public class NotFoundException : MixServerException
{
    public NotFoundException(string table, Guid id) : this(table, id.ToString())
    {
    }
    
    public NotFoundException(string table, string id) : base($"Item={id} does not exist in Table={table}")
    {
    }

    public NotFoundException(string table, IEnumerable<string> ids) : base(
        $"Some or all items do not exist in {table} Items={string.Join(", ", ids)}")
    {
    }
}
