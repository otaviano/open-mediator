using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using OpenMediator.Abstractions;
using OpenMediator.Core;

namespace OpenMediator.Extensions;

public static class OpenMediatorServiceCollectionExtensions
{
    private static readonly Type[] HandlerInterfaces =
    [
        typeof(ICommandHandler<>),
        typeof(ICommandHandler<,>),
        typeof(IQueryHandler<,>),
        typeof(IEventHandler<>),
    ];

    /// <summary>
    /// Registers <see cref="IMediator"/> and scans <paramref name="assemblies"/> for handler implementations.
    /// </summary>
    public static IServiceCollection AddOpenMediator(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        services.AddScoped<IMediator, Mediator>();

        foreach (var assembly in assemblies)
            RegisterHandlersFromAssembly(services, assembly);

        return services;
    }

    /// <summary>
    /// Registers a pipeline behavior with scoped lifetime.
    /// Behaviors are applied in the order they are registered (first = outermost).
    /// </summary>
    public static IServiceCollection AddPipelineBehavior<TBehavior>(this IServiceCollection services)
        where TBehavior : class
        => services.AddPipelineBehavior(typeof(TBehavior));

    /// <summary>
    /// Registers a pipeline behavior by type with scoped lifetime.
    /// </summary>
    public static IServiceCollection AddPipelineBehavior(this IServiceCollection services, Type behaviorType)
    {
        foreach (var iface in GetClosedPipelineBehaviorInterfaces(behaviorType))
            services.AddScoped(iface, behaviorType);

        return services;
    }

    private static void RegisterHandlersFromAssembly(IServiceCollection services, Assembly assembly)
    {
        var concreteTypes = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false });

        foreach (var type in concreteTypes)
            RegisterHandlerInterfaces(services, type);
    }

    private static void RegisterHandlerInterfaces(IServiceCollection services, Type type)
    {
        foreach (var iface in type.GetInterfaces())
        {
            if (!iface.IsGenericType) continue;

            var definition = iface.GetGenericTypeDefinition();

            if (Array.IndexOf(HandlerInterfaces, definition) >= 0)
                services.AddScoped(iface, type);
        }
    }

    private static IEnumerable<Type> GetClosedPipelineBehaviorInterfaces(Type behaviorType)
        => behaviorType
            .GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));
}
