namespace MixServer.Domain.Interfaces;

public interface IHandler;

public interface ICommandHandler : IHandler;

public interface ICommandHandler<in TRequest, TResponse> : ICommandHandler
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}

public interface ICommandHandler<in TRequest> : IHandler
{
    Task HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}

public interface IQueryHandler<in TRequest, TResponse> : IHandler
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}

public interface IQueryHandler<TResponse> : IHandler
{
    Task<TResponse> HandleAsync(CancellationToken cancellationToken = default);
}