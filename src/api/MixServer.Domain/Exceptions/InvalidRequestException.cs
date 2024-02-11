namespace MixServer.Domain.Exceptions;

public class InvalidRequestException : MixServerException
{
    public Dictionary<string, string[]> Errors { get; set; } = new ();

    public InvalidRequestException(string message) :
        this(message, new Dictionary<string, string[]>
        {
            {"Error", [message] }
        })
    {
    }

    public InvalidRequestException(string property, string message)
        : this(message, new Dictionary<string, string[]>
        {
            {property, [message] }
        })
    {
    }
    
    public InvalidRequestException(string message, Dictionary<string, string[]> errors)
        : base(message)
    {
        Errors = errors;
    }
}
