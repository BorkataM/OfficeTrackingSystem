using OfficeSystem.Domain.Common;

namespace OfficeSystem.Application.Abstractions.Messaging;

/// <summary>A request that changes state and returns nothing but success or failure.</summary>
public interface ICommand;

/// <summary>A request that changes state and returns a value.</summary>
public interface ICommand<TResponse>;

/// <summary>A request that reads state. Never mutates anything.</summary>
public interface IQuery<TResponse>;

public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken);
}

/// <summary>
/// The single entry point the API layer uses to reach a use case. Keeps endpoints
/// free of any knowledge about which class handles what.
/// </summary>
public interface IDispatcher
{
    Task<Result> SendAsync(ICommand command, CancellationToken cancellationToken = default);

    Task<Result<TResponse>> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);

    Task<Result<TResponse>> QueryAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);
}
