using MixServer.Domain.Exceptions;

namespace MixServer.Domain.Tracklists.Exceptions;

public class TagBuilderException(string message, Exception? innerException = null) : MixServerException(message, innerException);