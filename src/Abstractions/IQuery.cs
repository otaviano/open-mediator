namespace OpenMediator.Abstractions;

/// <summary>
/// Marker interface for a read-only query that returns a result of type <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="TResult">The type returned by the query handler.</typeparam>
public interface IQuery<out TResult>;
