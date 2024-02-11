namespace MixServer.Domain.Exceptions;

public class ForbiddenRequestException : MixServerException
{
    public string? Property { get; }

    public ForbiddenRequestException()
    {
    }
    
    public ForbiddenRequestException(string message) : base(message)
    {
    }

    public ForbiddenRequestException(string property, string message) : base(message)
    {
        Property = property;
    }
}