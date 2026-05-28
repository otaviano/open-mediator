using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OpenMediator.Abstractions;
using OpenMediator.Extensions;

namespace OpenMediator.Tests;

public class MediatorDispatchTests
{
    // ── Fakes ───────────────────────────────────────────────────────────────

    private sealed record PingCommand : ICommand;
    private sealed record EchoCommand(string Message) : ICommand<string>;
    private sealed record SumQuery(int A, int B) : IQuery<int>;
    private sealed record OrderPlacedEvent(int OrderId) : IEvent;

    private sealed class PingHandler : ICommandHandler<PingCommand>
    {
        public bool WasInvoked { get; private set; }
        public Task HandleAsync(PingCommand command, CancellationToken cancellationToken = default)
        {
            WasInvoked = true;
            return Task.CompletedTask;
        }
    }

    private sealed class EchoHandler : ICommandHandler<EchoCommand, string>
    {
        public Task<string> HandleAsync(EchoCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(command.Message);
    }

    private sealed class SumHandler : IQueryHandler<SumQuery, int>
    {
        public Task<int> HandleAsync(SumQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult(query.A + query.B);
    }

    private sealed class OrderPlacedHandlerA : IEventHandler<OrderPlacedEvent>
    {
        public List<int> Received { get; } = [];
        public Task HandleAsync(OrderPlacedEvent evt, CancellationToken cancellationToken = default)
        {
            Received.Add(evt.OrderId);
            return Task.CompletedTask;
        }
    }

    private sealed class OrderPlacedHandlerB : IEventHandler<OrderPlacedEvent>
    {
        public List<int> Received { get; } = [];
        public Task HandleAsync(OrderPlacedEvent evt, CancellationToken cancellationToken = default)
        {
            Received.Add(evt.OrderId);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingEventHandler : IEventHandler<OrderPlacedEvent>
    {
        public Task HandleAsync(OrderPlacedEvent evt, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("boom");
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static IMediator BuildMediator(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        services.AddOpenMediator(); // registers IMediator; no assembly scanning
        configure(services);
        return services.BuildServiceProvider().GetRequiredService<IMediator>();
    }

    // ── SendAsync (fire-and-forget) ──────────────────────────────────────────

    [Fact]
    public async Task SendAsync_FireAndForget_InvokesRegisteredHandler()
    {
        var handler = new PingHandler();
        var mediator = BuildMediator(s =>
            s.AddSingleton<ICommandHandler<PingCommand>>(handler));

        await mediator.SendAsync(new PingCommand());

        handler.WasInvoked.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_FireAndForget_ThrowsWhenNoHandlerRegistered()
    {
        var mediator = BuildMediator(_ => { });

        var act = () => mediator.SendAsync(new PingCommand());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*PingCommand*");
    }

    // ── SendAsync (with result) ──────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_WithResult_InvokesHandlerAndReturnsResult()
    {
        var mediator = BuildMediator(s =>
            s.AddScoped<ICommandHandler<EchoCommand, string>, EchoHandler>());

        var result = await mediator.SendAsync<EchoCommand, string>(new EchoCommand("hello"));

        result.Should().Be("hello");
    }

    [Fact]
    public async Task SendAsync_WithResult_ThrowsWhenNoHandlerRegistered()
    {
        var mediator = BuildMediator(_ => { });

        var act = () => mediator.SendAsync<EchoCommand, string>(new EchoCommand("x"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*EchoCommand*");
    }

    // ── QueryAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task QueryAsync_InvokesHandlerAndReturnsResult()
    {
        var mediator = BuildMediator(s =>
            s.AddScoped<IQueryHandler<SumQuery, int>, SumHandler>());

        var result = await mediator.QueryAsync<SumQuery, int>(new SumQuery(3, 4));

        result.Should().Be(7);
    }

    [Fact]
    public async Task QueryAsync_ThrowsWhenNoHandlerRegistered()
    {
        var mediator = BuildMediator(_ => { });

        var act = () => mediator.QueryAsync<SumQuery, int>(new SumQuery(1, 2));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*SumQuery*");
    }

    // ── PublishAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task PublishAsync_InvokesAllRegisteredEventHandlers()
    {
        var handlerA = new OrderPlacedHandlerA();
        var handlerB = new OrderPlacedHandlerB();
        var mediator = BuildMediator(s =>
        {
            s.AddSingleton<IEventHandler<OrderPlacedEvent>>(handlerA);
            s.AddSingleton<IEventHandler<OrderPlacedEvent>>(handlerB);
        });

        await mediator.PublishAsync(new OrderPlacedEvent(42));

        handlerA.Received.Should().ContainSingle().Which.Should().Be(42);
        handlerB.Received.Should().ContainSingle().Which.Should().Be(42);
    }

    [Fact]
    public async Task PublishAsync_IsNoOpWhenNoHandlersRegistered()
    {
        var mediator = BuildMediator(_ => { });

        var act = () => mediator.PublishAsync(new OrderPlacedEvent(1));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_AggregatesExceptionsFromMultipleHandlers()
    {
        var mediator = BuildMediator(s =>
        {
            s.AddScoped<IEventHandler<OrderPlacedEvent>, ThrowingEventHandler>();
            s.AddScoped<IEventHandler<OrderPlacedEvent>, ThrowingEventHandler>();
        });

        var act = () => mediator.PublishAsync(new OrderPlacedEvent(1));

        var ex = await act.Should().ThrowAsync<AggregateException>();
        ex.Which.InnerExceptions.Should().HaveCount(2);
    }

    // ── Single failing event handler ────────────────────────────────────────

    [Fact]
    public async Task PublishAsync_ThrowsAggregateExceptionWhenSingleHandlerThrows()
    {
        var mediator = BuildMediator(s =>
            s.AddScoped<IEventHandler<OrderPlacedEvent>, ThrowingEventHandler>());

        var act = () => mediator.PublishAsync(new OrderPlacedEvent(1));

        var ex = await act.Should().ThrowAsync<AggregateException>();
        ex.Which.InnerExceptions.Should().HaveCount(1);
    }

    // ── Error message content ────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_ErrorMessage_ContainsAddOpenMediatorHint()
    {
        var mediator = BuildMediator(_ => { });

        var act = () => mediator.SendAsync(new PingCommand());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*AddOpenMediator*");
    }

    [Fact]
    public async Task SendAsync_DuplicateHandlerErrorMessage_MentionsOnlyOne()
    {
        var mediator = BuildMediator(s =>
        {
            s.AddScoped<ICommandHandler<PingCommand>, PingHandler>();
            s.AddScoped<ICommandHandler<PingCommand>, PingHandler>();
        });

        var act = () => mediator.SendAsync(new PingCommand());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Only one handler*");
    }

    // ── Duplicate handler detection ──────────────────────────────────────────

    [Fact]
    public async Task SendAsync_ThrowsWhenMultipleCommandHandlersRegistered()
    {
        var mediator = BuildMediator(s =>
        {
            s.AddScoped<ICommandHandler<PingCommand>, PingHandler>();
            s.AddScoped<ICommandHandler<PingCommand>, PingHandler>();
        });

        var act = () => mediator.SendAsync(new PingCommand());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Multiple handlers*");
    }

    [Fact]
    public async Task QueryAsync_ThrowsWhenMultipleQueryHandlersRegistered()
    {
        var mediator = BuildMediator(s =>
        {
            s.AddScoped<IQueryHandler<SumQuery, int>, SumHandler>();
            s.AddScoped<IQueryHandler<SumQuery, int>, SumHandler>();
        });

        var act = () => mediator.QueryAsync<SumQuery, int>(new SumQuery(1, 2));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Multiple handlers*");
    }

    [Fact]
    public async Task SendAsync_WithResult_ThrowsWhenMultipleHandlersRegistered()
    {
        var mediator = BuildMediator(s =>
        {
            s.AddScoped<ICommandHandler<EchoCommand, string>, EchoHandler>();
            s.AddScoped<ICommandHandler<EchoCommand, string>, EchoHandler>();
        });

        var act = () => mediator.SendAsync<EchoCommand, string>(new EchoCommand("x"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Multiple handlers*");
    }

    // ── CancellationToken propagation ──────────────────────────────────────

    [Fact]
    public async Task SendAsync_FireAndForget_PropagatesCancellationTokenToHandler()
    {
        var receivedToken = default(CancellationToken);
        var handler = new CaptureCancellationHandler(ct => { receivedToken = ct; return Task.CompletedTask; });
        var mediator = BuildMediator(s =>
            s.AddSingleton<ICommandHandler<PingCommand>>(handler));

        using var cts = new CancellationTokenSource();
        await mediator.SendAsync(new PingCommand(), cts.Token);

        receivedToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task SendAsync_WithResult_PropagatesCancellationTokenToHandler()
    {
        var receivedToken = default(CancellationToken);
        var handler = new CaptureEchoCancellationHandler(ct => { receivedToken = ct; return Task.FromResult("done"); });
        var mediator = BuildMediator(s =>
            s.AddSingleton<ICommandHandler<EchoCommand, string>>(handler));

        using var cts = new CancellationTokenSource();
        await mediator.SendAsync<EchoCommand, string>(new EchoCommand("test"), cts.Token);

        receivedToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task QueryAsync_PropagatesCancellationTokenToHandler()
    {
        var receivedToken = default(CancellationToken);
        var handler = new CaptureSumCancellationHandler(ct => { receivedToken = ct; return Task.FromResult(0); });
        var mediator = BuildMediator(s =>
            s.AddSingleton<IQueryHandler<SumQuery, int>>(handler));

        using var cts = new CancellationTokenSource();
        await mediator.QueryAsync<SumQuery, int>(new SumQuery(1, 2), cts.Token);

        receivedToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task PublishAsync_PropagatesCancellationTokenToHandlers()
    {
        var receivedTokens = new List<CancellationToken>();
        var handler = new CaptureEventCancellationHandler(ct => { receivedTokens.Add(ct); return Task.CompletedTask; });
        var mediator = BuildMediator(s =>
            s.AddSingleton<IEventHandler<OrderPlacedEvent>>(handler));

        using var cts = new CancellationTokenSource();
        await mediator.PublishAsync(new OrderPlacedEvent(1), cts.Token);

        receivedTokens.Should().ContainSingle().Which.Should().Be(cts.Token);
    }

    // ── PublishAsync mixed success/failure ─────────────────────────────────

    [Fact]
    public async Task PublishAsync_InvokesRemainingHandlersAfterOneFails()
    {
        var successHandler = new OrderPlacedHandlerA();
        var mediator = BuildMediator(s =>
        {
            s.AddScoped<IEventHandler<OrderPlacedEvent>, ThrowingEventHandler>();
            s.AddSingleton<IEventHandler<OrderPlacedEvent>>(successHandler);
        });

        var act = () => mediator.PublishAsync(new OrderPlacedEvent(7));

        var ex = await act.Should().ThrowAsync<AggregateException>();
        ex.Which.InnerExceptions.Should().HaveCount(1);
        successHandler.Received.Should().ContainSingle().Which.Should().Be(7);
    }

    // ── Cancellation token handler fakes ────────────────────────────────────

    private sealed class CaptureCancellationHandler(Func<CancellationToken, Task> callback)
        : ICommandHandler<PingCommand>
    {
        public Task HandleAsync(PingCommand command, CancellationToken cancellationToken = default)
            => callback(cancellationToken);
    }

    private sealed class CaptureEchoCancellationHandler(Func<CancellationToken, Task<string>> callback)
        : ICommandHandler<EchoCommand, string>
    {
        public Task<string> HandleAsync(EchoCommand command, CancellationToken cancellationToken = default)
            => callback(cancellationToken);
    }

    private sealed class CaptureSumCancellationHandler(Func<CancellationToken, Task<int>> callback)
        : IQueryHandler<SumQuery, int>
    {
        public Task<int> HandleAsync(SumQuery query, CancellationToken cancellationToken = default)
            => callback(cancellationToken);
    }

    private sealed class CaptureEventCancellationHandler(Func<CancellationToken, Task> callback)
        : IEventHandler<OrderPlacedEvent>
    {
        public Task HandleAsync(OrderPlacedEvent evt, CancellationToken cancellationToken = default)
            => callback(cancellationToken);
    }
}
