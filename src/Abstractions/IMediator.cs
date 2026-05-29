namespace Zibetti.Mediator.Abstractions;

/// <summary>
/// Central mediator for dispatching commands, queries, and events.
/// </summary>
public interface IMediator
{
    /// <summary>
    /// Dispatches a fire-and-forget command to its single registered handler.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <exception cref="InvalidOperationException">Thrown when no handler is registered for <typeparamref name="TCommand"/>.</exception>
    Task SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand;

    /// <summary>
    /// Dispatches a command to its single registered handler and returns the result.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <exception cref="InvalidOperationException">Thrown when no handler is registered for <typeparamref name="TCommand"/>.</exception>
    Task<TResult> SendAsync<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResult>;

    /// <summary>
    /// Dispatches a read-only query to its single registered handler and returns the result.
    /// </summary>
    /// <typeparam name="TQuery">The query type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <exception cref="InvalidOperationException">Thrown when no handler is registered for <typeparamref name="TQuery"/>.</exception>
    Task<TResult> QueryAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResult>;

    /// <summary>
    /// Publishes an event to all registered handlers. All handlers are invoked even if some throw.
    /// Exceptions from individual handlers are aggregated and re-thrown as <see cref="AggregateException"/>.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    Task PublishAsync<TEvent>(TEvent evt, CancellationToken cancellationToken = default)
        where TEvent : IEvent;
}
