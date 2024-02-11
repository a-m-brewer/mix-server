namespace MixServer.Domain.Exceptions;

public class UnauthorizedRequestException : MixServerException
{
    public UnauthorizedRequestException()
    {
    }
    
    public UnauthorizedRequestException(string message, Exception innerException) : base(message, innerException)
    {
    }
}