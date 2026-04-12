using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OpenMediator.Abstractions;
using OpenMediator.Extensions;

namespace OpenMediator.Tests;

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
    public void AddOpenMediator_RegistersIMediatorAsResolvable()
    {
        var provider = new ServiceCollection()
            .AddOpenMediator(typeof(DiRegistrationTests).Assembly)
            .BuildServiceProvider();

        var mediator = provider.GetService<IMediator>();

        mediator.Should().NotBeNull();
    }

    [Fact]
    public void AddOpenMediator_RegistersDiscoveredCommandHandlers()
    {
        var provider = new ServiceCollection()
            .AddOpenMediator(typeof(DiRegistrationTests).Assembly)
            .BuildServiceProvider();

        var handler = provider.GetService<ICommandHandler<CreateOrderCommand>>();

        handler.Should().NotBeNull().And.BeOfType<CreateOrderHandler>();
    }

    [Fact]
    public void AddOpenMediator_RegistersAllFourHandlerInterfaceFamilies()
    {
        var provider = new ServiceCollection()
            .AddOpenMediator(typeof(DiRegistrationTests).Assembly)
            .BuildServiceProvider();

        provider.GetService<ICommandHandler<CreateOrderCommand>>().Should().NotBeNull();
        provider.GetService<IQueryHandler<GetOrderQuery, string>>().Should().NotBeNull();
        provider.GetService<IEventHandler<StockUpdatedEvent>>().Should().NotBeNull();
    }

    [Fact]
    public void AddOpenMediator_WithMultipleAssemblies_RegistersHandlersFromAll()
    {
        // Both assemblies are the same here; in real usage they'd differ.
        var assembly = typeof(DiRegistrationTests).Assembly;
        var provider = new ServiceCollection()
            .AddOpenMediator(assembly, assembly) // duplicates are fine — scoped lifetimes
            .BuildServiceProvider();

        provider.GetService<ICommandHandler<CreateOrderCommand>>().Should().NotBeNull();
        provider.GetService<IQueryHandler<GetOrderQuery, string>>().Should().NotBeNull();
    }

    [Fact]
    public async Task AddPipelineBehavior_IsAppliedDuringDispatch()
    {
        var log = new LoggingBehavior<GetOrderQuery, string>();
        var provider = new ServiceCollection()
            .AddOpenMediator(typeof(DiRegistrationTests).Assembly)
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
            .AddOpenMediator(typeof(DiRegistrationTests).Assembly);

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
}
