namespace SimpleMediator.Abstractions;

/// <summary>
/// Marker interface for a fire-and-forget command that produces no result.
/// </summary>
public interface ICommand;

/// <summary>
/// Marker interface for a command that produces a result of type <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="TResult">The type returned by the command handler.</typeparam>
public interface ICommand<out TResult>;
