using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using OfficeSystem.Domain.Common;

namespace OfficeSystem.Application.Abstractions.Messaging;

/// <summary>
/// Resolves the handler for a request and runs the validation stage in front of it.
/// Handler lookup is reflective but cached, so each request type pays for it once.
/// </summary>
internal sealed class Dispatcher(IServiceProvider serviceProvider, RequestValidator validator) : IDispatcher
{
    private static readonly ConcurrentDictionary<(Type Request, Type? Response, MessageKind Kind), HandlerInvoker> Invokers = new();

    public async Task<Result> SendAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        Result validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);

        if (validation.IsFailure)
        {
            return validation;
        }

        HandlerInvoker invoker = GetInvoker(command.GetType(), null, MessageKind.Command);

        return await invoker.InvokeAsync<Result>(serviceProvider, command, cancellationToken).ConfigureAwait(false);
    }

    public Task<Result<TResponse>> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        return ExecuteAsync<TResponse>(command, MessageKind.CommandWithResponse, cancellationToken);
    }

    public Task<Result<TResponse>> QueryAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return ExecuteAsync<TResponse>(query, MessageKind.Query, cancellationToken);
    }

    private async Task<Result<TResponse>> ExecuteAsync<TResponse>(object request, MessageKind kind, CancellationToken cancellationToken)
    {
        Result validation = await validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);

        if (validation.IsFailure)
        {
            return Result.Failure<TResponse>(validation.Error);
        }

        HandlerInvoker invoker = GetInvoker(request.GetType(), typeof(TResponse), kind);

        return await invoker.InvokeAsync<Result<TResponse>>(serviceProvider, request, cancellationToken).ConfigureAwait(false);
    }

    private static HandlerInvoker GetInvoker(Type requestType, Type? responseType, MessageKind kind)
        => Invokers.GetOrAdd((requestType, responseType, kind), static key =>
        {
            Type handlerType = key.Kind switch
            {
                MessageKind.Command => typeof(ICommandHandler<>).MakeGenericType(key.Request),
                MessageKind.CommandWithResponse => typeof(ICommandHandler<,>).MakeGenericType(key.Request, key.Response!),
                MessageKind.Query => typeof(IQueryHandler<,>).MakeGenericType(key.Request, key.Response!),
                _ => throw new ArgumentOutOfRangeException(nameof(key))
            };

            MethodInfo method = handlerType.GetMethods().Single(m => m.Name is "HandleAsync");

            return new HandlerInvoker(handlerType, method);
        });

    private enum MessageKind
    {
        Command,
        CommandWithResponse,
        Query
    }

    private sealed class HandlerInvoker(Type handlerType, MethodInfo method)
    {
        public Task<TResult> InvokeAsync<TResult>(IServiceProvider serviceProvider, object request, CancellationToken cancellationToken)
        {
            object handler = serviceProvider.GetRequiredService(handlerType);

            return (Task<TResult>)method.Invoke(handler, [request, cancellationToken])!;
        }
    }
}
