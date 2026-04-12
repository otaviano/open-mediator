namespace OpenMediator.Abstractions;

/// <summary>
/// Handles a fire-and-forget command of type <typeparamref name="TCommand"/>.
/// </summary>
/// <typeparam name="TCommand">The command type, must implement <see cref="ICommand"/>.</typeparam>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Handles a command of type <typeparamref name="TCommand"/> and returns a result of type <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="TCommand">The command type, must implement <see cref="ICommand{TResult}"/>.</typeparam>
/// <typeparam name="TResult">The type of the result produced by handling the command.</typeparam>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
