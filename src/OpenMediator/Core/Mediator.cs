using Microsoft.Extensions.DependencyInjection;
using OpenMediator.Abstractions;

namespace OpenMediator.Core;

internal sealed class Mediator(IServiceProvider serviceProvider) : IMediator
{
    public async Task SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        var handler = ResolveSingleHandler<TCommand, ICommandHandler<TCommand>>();

        await BuildPipeline<TCommand, Unit>(
            command,
            ct => handler.HandleAsync(command, ct).ContinueWith(_ => Unit.Value, ct),
            cancellationToken);
    }

    public Task<TResult> SendAsync<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResult>
    {
        var handler = ResolveSingleHandler<TCommand, ICommandHandler<TCommand, TResult>>();

        return BuildPipeline<TCommand, TResult>(
            command,
            ct => handler.HandleAsync(command, ct),
            cancellationToken);
    }

    public Task<TResult> QueryAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResult>
    {
        var handler = ResolveSingleHandler<TQuery, IQueryHandler<TQuery, TResult>>();

        return BuildPipeline<TQuery, TResult>(
            query,
            ct => handler.HandleAsync(query, ct),
            cancellationToken);
    }

    public async Task PublishAsync<TEvent>(TEvent evt, CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        var handlers = serviceProvider.GetServices<IEventHandler<TEvent>>();
        List<Exception>? exceptions = null;

        foreach (var handler in handlers)
        {
            try
            {
                await handler.HandleAsync(evt, cancellationToken);
            }
            catch (Exception ex)
            {
                (exceptions ??= []).Add(ex);
            }
        }

        if (exceptions is { Count: > 0 })
            throw new AggregateException(exceptions);
    }

    private THandler ResolveSingleHandler<TRequest, THandler>()
        where THandler : class
    {
        var handlers = serviceProvider.GetServices<THandler>().ToArray();

        return handlers.Length switch
        {
            0 => throw new InvalidOperationException(
                $"No handler registered for '{typeof(TRequest).FullName}'. " +
                "Register it via AddOpenMediator()."),
            > 1 => throw new InvalidOperationException(
                $"Multiple handlers registered for '{typeof(TRequest).FullName}'. " +
                "Only one handler per command or query is allowed."),
            _ => handlers[0]
        };
    }

    private Task<TResponse> BuildPipeline<TRequest, TResponse>(
        TRequest request,
        RequestHandlerDelegate<TResponse> handlerDelegate,
        CancellationToken cancellationToken)
    {
        var behaviors = serviceProvider
            .GetServices<IPipelineBehavior<TRequest, TResponse>>()
            .ToArray();

        RequestHandlerDelegate<TResponse> pipeline = handlerDelegate;

        for (var i = behaviors.Length - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var next = pipeline;
            pipeline = ct => behavior.HandleAsync(request, next, ct);
        }

        return pipeline(cancellationToken);
    }
}
