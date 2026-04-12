using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OpenMediator.Abstractions;
using OpenMediator.Extensions;

namespace OpenMediator.Tests;

public class PipelineBehaviorTests
{
    // ── Fakes ───────────────────────────────────────────────────────────────

    private sealed record NumberCommand(int Value) : ICommand<int>;

    private sealed class NumberHandler : ICommandHandler<NumberCommand, int>
    {
        public Task<int> HandleAsync(NumberCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(command.Value);
    }

    /// <summary>Records the order of entry/exit for verifying pipeline wrapping order.</summary>
    private sealed class OrderTrackingBehavior(string name, List<string> log)
        : IPipelineBehavior<NumberCommand, int>
    {
        public async Task<int> HandleAsync(
            NumberCommand request,
            RequestHandlerDelegate<int> next,
            CancellationToken cancellationToken = default)
        {
            log.Add($"{name}:enter");
            var result = await next(cancellationToken);
            log.Add($"{name}:exit");
            return result;
        }
    }

    private sealed class DoublingBehavior : IPipelineBehavior<NumberCommand, int>
    {
        public async Task<int> HandleAsync(
            NumberCommand request,
            RequestHandlerDelegate<int> next,
            CancellationToken cancellationToken = default)
        {
            var result = await next(cancellationToken);
            return result * 2;
        }
    }

    private sealed class ShortCircuitBehavior : IPipelineBehavior<NumberCommand, int>
    {
        public Task<int> HandleAsync(
            NumberCommand request,
            RequestHandlerDelegate<int> next,
            CancellationToken cancellationToken = default)
            => Task.FromResult(-1); // Does NOT call next
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static IMediator BuildMediator(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        services.AddOpenMediator(); // registers IMediator; no assembly scanning
        configure(services);
        return services.BuildServiceProvider().GetRequiredService<IMediator>();
    }

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SingleBehavior_WrapsHandlerDispatch()
    {
        var log = new List<string>();
        var mediator = BuildMediator(s =>
        {
            s.AddScoped<ICommandHandler<NumberCommand, int>, NumberHandler>();
            s.AddSingleton<IPipelineBehavior<NumberCommand, int>>(_ =>
                new OrderTrackingBehavior("A", log));
        });

        await mediator.SendAsync<NumberCommand, int>(new NumberCommand(5));

        log.Should().Equal("A:enter", "A:exit");
    }

    [Fact]
    public async Task MultipleBehaviors_ExecuteInRegistrationOrder()
    {
        var log = new List<string>();
        var mediator = BuildMediator(s =>
        {
            s.AddScoped<ICommandHandler<NumberCommand, int>, NumberHandler>();
            s.AddSingleton<IPipelineBehavior<NumberCommand, int>>(_ => new OrderTrackingBehavior("A", log));
            s.AddSingleton<IPipelineBehavior<NumberCommand, int>>(_ => new OrderTrackingBehavior("B", log));
            s.AddSingleton<IPipelineBehavior<NumberCommand, int>>(_ => new OrderTrackingBehavior("C", log));
        });

        await mediator.SendAsync<NumberCommand, int>(new NumberCommand(1));

        log.Should().Equal("A:enter", "B:enter", "C:enter", "C:exit", "B:exit", "A:exit");
    }

    [Fact]
    public async Task Behavior_CanShortCircuitAndPreventHandlerInvocation()
    {
        var mediator = BuildMediator(s =>
        {
            s.AddScoped<ICommandHandler<NumberCommand, int>, NumberHandler>();
            s.AddScoped<IPipelineBehavior<NumberCommand, int>, ShortCircuitBehavior>();
        });

        var result = await mediator.SendAsync<NumberCommand, int>(new NumberCommand(99));

        result.Should().Be(-1);
    }

    [Fact]
    public async Task Behavior_CanInspectAndReplaceHandlerResult()
    {
        var mediator = BuildMediator(s =>
        {
            s.AddScoped<ICommandHandler<NumberCommand, int>, NumberHandler>();
            s.AddScoped<IPipelineBehavior<NumberCommand, int>, DoublingBehavior>();
        });

        var result = await mediator.SendAsync<NumberCommand, int>(new NumberCommand(7));

        result.Should().Be(14);
    }

    [Fact]
    public async Task NoBehaviors_HandlerIsInvokedDirectly()
    {
        var mediator = BuildMediator(s =>
            s.AddScoped<ICommandHandler<NumberCommand, int>, NumberHandler>());

        var result = await mediator.SendAsync<NumberCommand, int>(new NumberCommand(42));

        result.Should().Be(42);
    }

    [Fact]
    public async Task Behavior_PropagatesCancellationTokenThroughPipeline()
    {
        var capturedTokens = new List<CancellationToken>();
        var mediator = BuildMediator(s =>
        {
            s.AddScoped<ICommandHandler<NumberCommand, int>, NumberHandler>();
            s.AddSingleton<IPipelineBehavior<NumberCommand, int>>(_ =>
                new CaptureCancellationBehavior(capturedTokens));
        });

        using var cts = new CancellationTokenSource();
        await mediator.SendAsync<NumberCommand, int>(new NumberCommand(1), cts.Token);

        capturedTokens.Should().ContainSingle().Which.Should().Be(cts.Token);
    }

    [Fact]
    public async Task MultipleBehaviors_EachReceivesSameCancellationToken()
    {
        var capturedTokens = new List<CancellationToken>();
        var mediator = BuildMediator(s =>
        {
            s.AddScoped<ICommandHandler<NumberCommand, int>, NumberHandler>();
            s.AddSingleton<IPipelineBehavior<NumberCommand, int>>(_ =>
                new CaptureCancellationBehavior(capturedTokens));
            s.AddSingleton<IPipelineBehavior<NumberCommand, int>>(_ =>
                new CaptureCancellationBehavior(capturedTokens));
        });

        using var cts = new CancellationTokenSource();
        await mediator.SendAsync<NumberCommand, int>(new NumberCommand(1), cts.Token);

        capturedTokens.Should().HaveCount(2).And.AllSatisfy(t => t.Should().Be(cts.Token));
    }

    [Fact]
    public async Task DoublingBehavior_ComposedWithTrackingBehavior_AppliesInOrder()
    {
        var log = new List<string>();
        var mediator = BuildMediator(s =>
        {
            s.AddScoped<ICommandHandler<NumberCommand, int>, NumberHandler>();
            s.AddSingleton<IPipelineBehavior<NumberCommand, int>>(_ => new OrderTrackingBehavior("outer", log));
            s.AddScoped<IPipelineBehavior<NumberCommand, int>, DoublingBehavior>();
        });

        var result = await mediator.SendAsync<NumberCommand, int>(new NumberCommand(5));

        result.Should().Be(10);
        log.Should().Equal("outer:enter", "outer:exit");
    }

    // ── Additional fakes ─────────────────────────────────────────────────────

    private sealed class CaptureCancellationBehavior(List<CancellationToken> captured)
        : IPipelineBehavior<NumberCommand, int>
    {
        public Task<int> HandleAsync(
            NumberCommand request,
            RequestHandlerDelegate<int> next,
            CancellationToken cancellationToken = default)
        {
            captured.Add(cancellationToken);
            return next(cancellationToken);
        }
    }
}
