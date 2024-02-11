namespace MixServer.Domain.Exceptions;

public class MixServerException : Exception
{
    public MixServerException()
    {
    }
    
    public MixServerException(string message) : base(message)
    {
    }
    
    public MixServerException(string message, Exception? innerException) : base(message, innerException)
    {
    }
}