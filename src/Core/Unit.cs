namespace Zibetti.Mediator.Core;

/// <summary>
/// Represents a void return value in a generic pipeline context.
/// Used internally to give fire-and-forget commands a uniform pipeline signature.
/// </summary>
internal readonly struct Unit
{
    public static readonly Unit Value = default;
}
