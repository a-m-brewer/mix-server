namespace MixServer.Domain.Interfaces;

public interface IHandler;

public interface ICommandHandler : IHandler;

public interface ICommandHandler<in TRequest, TResponse> : ICommandHandler
{
    Task<TResponse> HandleAsync(TRequest request);
}

public interface ICommandHandler<in TRequest> : IHandler
{
    Task HandleAsync(TRequest request);
}

public interface IQueryHandler<in TRequest, TResponse> : IHandler
{
    Task<TResponse> HandleAsync(TRequest request);
}

public interface IQueryHandler<TResponse> : IHandler
{
    Task<TResponse> HandleAsync();
}