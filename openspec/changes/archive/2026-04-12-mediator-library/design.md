## Context

The project currently uses Brighter and Darker as its mediator infrastructure for dispatching commands and queries. These are capable libraries but carry significant conceptual overhead (subscriber registries, decorator pipelines, request context, etc.) that isn't needed here. The goal is a minimal, idiomatic C# mediator that prioritizes DI-native handler registration and clean request/handler contracts — no global state, no magic, no opaque decorator chains.

The library targets .NET 10 and leverages the standard `Microsoft.Extensions.DependencyInjection` abstractions for handler resolution, making it portable across any host (ASP.NET Core, Worker Service, etc.).

## Goals / Non-Goals

**Goals:**
- DI-first handler resolution: handlers are registered and resolved via `IServiceProvider`
- Typed request contracts: `ICommand`, `ICommand<TResult>`, `IQuery<TResult>`, `IEvent` as the primary extension points
- Typed handler contracts: one interface per request type; DI wires them by type
- Pipeline behaviors: ordered cross-cutting middleware around dispatch (`IPipelineBehavior<TRequest, TResponse>`)
- Assembly scanning registration via `IServiceCollection` extensions
- Zero external dependencies beyond `Microsoft.Extensions.DependencyInjection.Abstractions`
- Support for `CancellationToken` throughout

**Non-Goals:**
- Distributed messaging / message bus (no queues, no Kafka, no RabbitMQ)
- Saga or process manager orchestration
- Request batching or streaming
- Request deduplication or idempotency handling
- Retry / circuit breaker (use Polly as a pipeline behavior if needed)

## Decisions

### 1. DI-based handler resolution over manual registration

**Decision**: Handlers are registered in `IServiceCollection` and resolved via `IServiceProvider` at dispatch time.

**Rationale**: Brighter uses a `SubscriberRegistry` (manual mapping of request type → handler type). This is redundant when DI already tracks types. Resolving `ICommandHandler<TCommand>` directly from the container removes the registry indirection, leverages scoped lifetimes naturally, and is immediately familiar to .NET developers.

**Alternative considered**: A static `HandlerFactory` delegate — rejected because it breaks scoped lifetime support and makes testing harder.

---

### 2. Separate marker interfaces per request kind

**Decision**: `ICommand` (fire-and-forget), `ICommand<TResult>` (command with result), `IQuery<TResult>` (read), `IEvent` (fan-out notification) are distinct marker interfaces, not a single `IRequest<TResponse>`.

**Rationale**: Semantic clarity. A query and a command with a result may look identical structurally (`IRequest<T>`) but have different dispatch semantics (single handler vs. fan-out, write vs. read intent). Separate interfaces make the intent explicit in the type system and allow the mediator to enforce dispatch rules (e.g., `Publish` only accepts `IEvent`).

**Alternative considered**: Single `IRequest<TResponse>` à la MediatR — rejected because it conflates commands, queries, and events.

---

### 3. `void` commands use `Task` return, not `Unit`

**Decision**: `ICommandHandler<TCommand>` has `Task HandleAsync(TCommand command, CancellationToken ct)`. No `Unit` sentinel type.

**Rationale**: `Unit` is a functional-language workaround for generic return types. In C#, `Task` is the idiomatic void-async return. Avoiding `Unit` keeps the API simpler and avoids confusion.

**Alternative considered**: `IRequest<Unit>` + `Task<Unit>` — rejected for the above reason.

---

### 4. Pipeline behaviors wrap dispatch, not handler internals

**Decision**: `IPipelineBehavior<TRequest, TResponse>` is a middleware-style delegate chain, resolved from DI and applied in registration order before the handler is invoked.

**Rationale**: Cross-cutting concerns (validation, logging, caching) should not be embedded in handlers. A behavior pipeline (similar to ASP.NET Core middleware) keeps handlers pure. Resolution from DI means behaviors can have their own dependencies (validators, loggers, etc.).

**Implementation**: `Mediator` resolves `IEnumerable<IPipelineBehavior<TRequest, TResponse>>` from the container and composes a delegate chain. The innermost delegate calls the actual handler.

---

### 5. `Publish` for events is fire-and-fan-out; exceptions are aggregated

**Decision**: `IMediator.Publish<TEvent>` resolves all registered `IEventHandler<TEvent>` and invokes them. Exceptions from individual handlers are collected and re-thrown as `AggregateException`.

**Rationale**: Events are notifications — multiple handlers may react. Failing silently on one handler and continuing is dangerous; aggregating exceptions preserves visibility while ensuring all handlers run.

---

### 6. Assembly scanning via extension methods

**Decision**: Provide `services.AddOpenMediator(assemblies)` that scans for all handler interfaces and registers them, plus `services.AddPipelineBehavior<TBehavior>()` for explicit behavior registration.

**Rationale**: Convention-based registration reduces boilerplate. Explicit behavior registration (not scanned) keeps pipeline order intentional and deterministic.

## Risks / Trade-offs

- **Scoped handler resolution**: If the mediator is registered as singleton but handlers are scoped, `IServiceProvider` must create a scope per dispatch. → Mitigation: document the pattern; provide an `IScopedMediator` wrapper or use `IServiceScopeFactory` in dispatch.
- **No built-in retry/timeout**: Unlike Brighter, there's no decorator-based retry. → Mitigation: implement as a pipeline behavior using Polly; document the pattern.
- **Event handler ordering**: Fan-out order is DI registration order — not guaranteed across assemblies. → Mitigation: document that event handlers must not depend on execution order.
- **Breaking change**: Replacing Brighter/Darker requires updating all handler registrations, `Send`/`ExecuteAsync` call sites, and endpoint filters. → Mitigation: migrate incrementally; keep Brighter/Darker in place until all handlers are ported, then remove.

## Migration Plan

1. Add `OpenMediator` project to the solution; no existing code touched yet
2. Register `IMediator` alongside existing Brighter/Darker registrations (parallel run)
3. Port handlers one capability at a time: implement new handler interface, update DI registration, update call site
4. Run tests after each capability migration
5. Remove Brighter/Darker registrations once all handlers are ported
6. Remove Brighter/Darker package references

**Rollback**: Keep Brighter/Darker packages until final cleanup step; reverting a capability migration is a one-file change.

## Open Questions

- Should `IMediator.Send<TCommand>` support fire-and-forget without awaiting (i.e., background dispatch)? Initial answer: no — keep it synchronous-async only; background work is a concern for `IHostedService` or a queue.
- Should pipeline behaviors be applied to event handlers individually or to the `Publish` call as a whole? Initial answer: per-event-handler, so each handler gets its own behavior chain.
