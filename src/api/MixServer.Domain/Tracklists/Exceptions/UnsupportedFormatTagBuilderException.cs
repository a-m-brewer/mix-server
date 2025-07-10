namespace MixServer.Domain.Tracklists.Exceptions;

public class UnsupportedFormatTagBuilderException(string filePath, Exception? innerException = null)
    : TagBuilderException($"{filePath} does not support tags", innerException)
{
    
}