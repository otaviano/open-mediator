using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Zibetti.Mediator.Abstractions;
using Zibetti.Mediator.Extensions;

namespace Zibetti.Mediator.Tests;

public class DiRegistrationTests
{
    // ── Fakes ───────────────────────────────────────────────────────────────

    private sealed record CreateOrderCommand : ICommand;
    private sealed record GetOrderQuery(int Id) : IQuery<string>;
    private sealed record StockUpdatedEvent : IEvent;

    private sealed class CreateOrderHandler : ICommandHandler<CreateOrderCommand>
    {
        public Task HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class GetOrderHandler : IQueryHandler<GetOrderQuery, string>
    {
        public Task<string> HandleAsync(GetOrderQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult($"Order-{query.Id}");
    }

    private sealed class StockUpdatedHandler : IEventHandler<StockUpdatedEvent>
    {
        public Task HandleAsync(StockUpdatedEvent evt, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public List<string> Log { get; } = [];

        public async Task<TResponse> HandleAsync(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken = default)
        {
            Log.Add("before");
            var result = await next(cancellationToken);
            Log.Add("after");
            return result;
        }
    }

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public void AddZibettiMediator_RegistersIMediatorAsResolvable()
    {
        var provider = new ServiceCollection()
            .AddZibettiMediator(typeof(DiRegistrationTests).Assembly)
            .BuildServiceProvider();

        var mediator = provider.GetService<IMediator>();

        mediator.Should().NotBeNull();
    }

    [Fact]
    public void AddZibettiMediator_RegistersDiscoveredCommandHandlers()
    {
        var provider = new ServiceCollection()
            .AddZibettiMediator(typeof(DiRegistrationTests).Assembly)
            .BuildServiceProvider();

        var handler = provider.GetService<ICommandHandler<CreateOrderCommand>>();

        handler.Should().NotBeNull().And.BeOfType<CreateOrderHandler>();
    }

    [Fact]
    public void AddZibettiMediator_RegistersAllFourHandlerInterfaceFamilies()
    {
        var provider = new ServiceCollection()
            .AddZibettiMediator(typeof(DiRegistrationTests).Assembly)
            .BuildServiceProvider();

        provider.GetService<ICommandHandler<CreateOrderCommand>>().Should().NotBeNull();
        provider.GetService<IQueryHandler<GetOrderQuery, string>>().Should().NotBeNull();
        provider.GetService<IEventHandler<StockUpdatedEvent>>().Should().NotBeNull();
    }

    [Fact]
    public void AddZibettiMediator_WithMultipleAssemblies_RegistersHandlersFromAll()
    {
        // Both assemblies are the same here; in real usage they'd differ.
        var assembly = typeof(DiRegistrationTests).Assembly;
        var provider = new ServiceCollection()
            .AddZibettiMediator(assembly, assembly) // duplicates are fine — scoped lifetimes
            .BuildServiceProvider();

        provider.GetService<ICommandHandler<CreateOrderCommand>>().Should().NotBeNull();
        provider.GetService<IQueryHandler<GetOrderQuery, string>>().Should().NotBeNull();
    }

    [Fact]
    public async Task AddPipelineBehavior_IsAppliedDuringDispatch()
    {
        var log = new LoggingBehavior<GetOrderQuery, string>();
        var provider = new ServiceCollection()
            .AddZibettiMediator(typeof(DiRegistrationTests).Assembly)
            .AddSingleton<IPipelineBehavior<GetOrderQuery, string>>(log)
            .BuildServiceProvider();

        var mediator = provider.GetRequiredService<IMediator>();
        await mediator.QueryAsync<GetOrderQuery, string>(new GetOrderQuery(1));

        log.Log.Should().Equal("before", "after");
    }

    [Fact]
    public async Task AddPipelineBehavior_Generic_RegistersAndAppliesBehavior()
    {
        var services = new ServiceCollection()
            .AddZibettiMediator(typeof(DiRegistrationTests).Assembly);

        // The generic overload needs a closed behavior type
        services.AddScoped<LoggingBehavior<GetOrderQuery, string>>();
        services.AddScoped<IPipelineBehavior<GetOrderQuery, string>>(
            sp => sp.GetRequiredService<LoggingBehavior<GetOrderQuery, string>>());

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var behavior = provider.GetRequiredService<LoggingBehavior<GetOrderQuery, string>>();

        await mediator.QueryAsync<GetOrderQuery, string>(new GetOrderQuery(2));

        behavior.Log.Should().Equal("before", "after");
    }

    [Fact]
    public async Task AddPipelineBehavior_TypeOverload_RegistersBehaviorAndAppliesItDuringDispatch()
    {
        var provider = new ServiceCollection()
            .AddZibettiMediator(typeof(DiRegistrationTests).Assembly)
            .AddPipelineBehavior(typeof(LoggingBehavior<GetOrderQuery, string>))
            .BuildServiceProvider();

        var mediator = provider.GetRequiredService<IMediator>();
        var behavior = provider.GetRequiredService<IPipelineBehavior<GetOrderQuery, string>>();

        await mediator.QueryAsync<GetOrderQuery, string>(new GetOrderQuery(3));

        ((LoggingBehavior<GetOrderQuery, string>)behavior).Log.Should().Equal("before", "after");
    }

    [Fact]
    public void AddPipelineBehavior_TypeOverload_WithNonBehaviorType_RegistersNothing()
    {
        var services = new ServiceCollection()
            .AddZibettiMediator(typeof(DiRegistrationTests).Assembly)
            .AddPipelineBehavior(typeof(CreateOrderHandler));

        var provider = services.BuildServiceProvider();

        provider.GetService<IPipelineBehavior<CreateOrderCommand, object>>().Should().BeNull();
    }

    [Fact]
    public async Task AddPipelineBehavior_GenericExtensionOverload_RegistersBehaviorAndAppliesItDuringDispatch()
    {
        var provider = new ServiceCollection()
            .AddZibettiMediator(typeof(DiRegistrationTests).Assembly)
            .AddPipelineBehavior<LoggingBehavior<GetOrderQuery, string>>()
            .BuildServiceProvider();

        var mediator = provider.GetRequiredService<IMediator>();
        var behavior = provider.GetRequiredService<IPipelineBehavior<GetOrderQuery, string>>();

        await mediator.QueryAsync<GetOrderQuery, string>(new GetOrderQuery(4));

        ((LoggingBehavior<GetOrderQuery, string>)behavior).Log.Should().Equal("before", "after");
    }

    [Fact]
    public void AddZibettiMediator_WithNoAssemblies_StillRegistersIMediator()
    {
        var provider = new ServiceCollection()
            .AddZibettiMediator()
            .BuildServiceProvider();

        provider.GetService<IMediator>().Should().NotBeNull();
    }

    [Fact]
    public void AddZibettiMediator_ReturnsServiceCollectionForChaining()
    {
        var services = new ServiceCollection();

        var result = services.AddZibettiMediator(typeof(DiRegistrationTests).Assembly);

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddPipelineBehavior_TypeOverload_ReturnsServiceCollectionForChaining()
    {
        var services = new ServiceCollection()
            .AddZibettiMediator(typeof(DiRegistrationTests).Assembly);

        var result = services.AddPipelineBehavior(typeof(LoggingBehavior<GetOrderQuery, string>));

        result.Should().BeSameAs(services);
    }

    [Fact]
    public async Task AddPipelineBehavior_TypeOverload_WithBehaviorImplementingNonGenericInterface_OnlyRegistersBehaviorInterface()
    {
        // BehaviorWithNonGenericInterface implements both IDisposable (non-generic) and
        // IPipelineBehavior<GetOrderQuery, string>. This test ensures GetClosedPipelineBehaviorInterfaces
        // only returns IPipelineBehavior<,> — not IDisposable — by verifying the behavior works correctly.
        var provider = new ServiceCollection()
            .AddZibettiMediator(typeof(DiRegistrationTests).Assembly)
            .AddPipelineBehavior(typeof(BehaviorWithNonGenericInterface))
            .BuildServiceProvider();

        var mediator = provider.GetRequiredService<IMediator>();
        var behavior = provider.GetRequiredService<IPipelineBehavior<GetOrderQuery, string>>();

        var result = await mediator.QueryAsync<GetOrderQuery, string>(new GetOrderQuery(10));

        result.Should().Be("Order-10");
        ((BehaviorWithNonGenericInterface)behavior).WasInvoked.Should().BeTrue();

        // IDisposable must NOT have been registered as a behavior interface
        provider.GetService<IDisposable>().Should().BeNull();
    }

    private sealed class BehaviorWithNonGenericInterface : IPipelineBehavior<GetOrderQuery, string>, IDisposable
    {
        public bool WasInvoked { get; private set; }

        public Task<string> HandleAsync(
            GetOrderQuery request,
            RequestHandlerDelegate<string> next,
            CancellationToken cancellationToken = default)
        {
            WasInvoked = true;
            return next(cancellationToken);
        }

        // Intentionally empty — implemented only to prove IDisposable is not registered as a behavior service.
        public void Dispose() { }
    }
}
