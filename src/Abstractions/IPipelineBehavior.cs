namespace SimpleMediator.Abstractions;

/// <summary>
/// Delegate representing the next step in the pipeline — either the next behavior or the handler itself.
/// </summary>
/// <typeparam name="TResponse">The response type produced by the pipeline.</typeparam>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken);

/// <summary>
/// Cross-cutting behavior that wraps handler dispatch.
/// Behaviors are applied in registration order (first registered = outermost wrapper).
/// </summary>
/// <typeparam name="TRequest">The request type flowing through the pipeline.</typeparam>
/// <typeparam name="TResponse">The response type produced by the pipeline.</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse>
{
    Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken = default);
}
