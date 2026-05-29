namespace Zibetti.Mediator.Abstractions;

/// <summary>
/// Handles an event notification of type <typeparamref name="TEvent"/>.
/// Multiple handlers may be registered for the same event type (fan-out).
/// </summary>
/// <typeparam name="TEvent">The event type, must implement <see cref="IEvent"/>.</typeparam>
public interface IEventHandler<in TEvent>
    where TEvent : IEvent
{
    Task HandleAsync(TEvent evt, CancellationToken cancellationToken = default);
}
