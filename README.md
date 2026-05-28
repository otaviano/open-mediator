# SimpleMediator

![Mutation Score](https://img.shields.io/badge/mutation%20score-88%25-brightgreen)

A minimal, DI-first .NET 10 mediator library. No magic, no global state — just clean request/handler contracts wired through the standard `IServiceCollection`.

## Why SimpleMediator?

Libraries like MediatR and Brighter are capable but carry significant overhead: custom registries, opaque decorator chains, and `Unit` sentinels. SimpleMediator is built around one principle: **the DI container is your handler registry**. Register a handler, dispatch a request — that's it.

| Feature | SimpleMediator | MediatR | Brighter |
|---|---|---|---|
| DI-native resolution | ✓ | Partial | ✗ (SubscriberRegistry) |
| Distinct command/query/event types | ✓ | ✗ (all `IRequest<T>`) | ✓ |
| Pipeline behaviors | ✓ | ✓ | ✓ (decorators) |
| Zero extra dependencies | ✓ | ✓ | ✗ |
| Targets .NET 10 | ✓ | ✓ | ✓ |

---

## Installation

```bash
dotnet add package SimpleMediator
```

---

## Concepts

### Request Marker Interfaces

Requests are plain types that implement one of four markers. The marker determines the dispatch semantics.

```csharp
// Fire-and-forget — no result
public record DeleteUserCommand(Guid UserId) : ICommand;

// Command that returns a value
public record CreateUserCommand(string Email) : ICommand<Guid>;

// Read-only query
public record GetUserQuery(Guid UserId) : IQuery<UserDto>;

// Notification dispatched to all handlers (fan-out)
public record UserDeletedEvent(Guid UserId) : IEvent;
```

---

### Handler Interfaces

Each marker interface has a corresponding handler interface. Register exactly **one** handler per command or query; events support multiple handlers.

#### `ICommandHandler<TCommand>` — fire-and-forget

```csharp
public class DeleteUserHandler(IUserRepository repo) : ICommandHandler<DeleteUserCommand>
{
    public async Task HandleAsync(DeleteUserCommand command, CancellationToken cancellationToken = default)
    {
        await repo.DeleteAsync(command.UserId, cancellationToken);
    }
}
```

#### `ICommandHandler<TCommand, TResult>` — command with result

```csharp
public class CreateUserHandler(IUserRepository repo) : ICommandHandler<CreateUserCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        var user = new User(command.Email);
        await repo.AddAsync(user, cancellationToken);
        return user.Id;
    }
}
```

#### `IQueryHandler<TQuery, TResult>` — read-only query

```csharp
public class GetUserHandler(IUserRepository repo) : IQueryHandler<GetUserQuery, UserDto>
{
    public async Task<UserDto> HandleAsync(GetUserQuery query, CancellationToken cancellationToken = default)
    {
        var user = await repo.FindAsync(query.UserId, cancellationToken);
        return new UserDto(user.Id, user.Email);
    }
}
```

#### `IEventHandler<TEvent>` — event notification (fan-out)

```csharp
public class AuditOnUserDeleted : IEventHandler<UserDeletedEvent>
{
    public Task HandleAsync(UserDeletedEvent evt, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Audit: user {evt.UserId} deleted");
        return Task.CompletedTask;
    }
}

public class CleanupOnUserDeleted(IFileService files) : IEventHandler<UserDeletedEvent>
{
    public async Task HandleAsync(UserDeletedEvent evt, CancellationToken cancellationToken = default)
    {
        await files.DeleteUserFilesAsync(evt.UserId, cancellationToken);
    }
}
```

---

### Pipeline Behaviors

`IPipelineBehavior<TRequest, TResponse>` is middleware-style cross-cutting logic that wraps handler dispatch. Behaviors execute in registration order (first registered = outermost wrapper).

#### Validation behavior example

```csharp
public class ValidationBehavior<TRequest, TResponse>(IValidator<TRequest> validator)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return await next(cancellationToken);
    }
}
```

Behaviors can also short-circuit (not call `next`) or modify the result returned by the handler.

---

### Dispatch Methods

All dispatch is through `IMediator`:

```csharp
// Fire-and-forget command
await mediator.SendAsync(new DeleteUserCommand(userId), ct);

// Command with result
var newId = await mediator.SendAsync<CreateUserCommand, Guid>(new CreateUserCommand(email), ct);

// Query
var user = await mediator.QueryAsync<GetUserQuery, UserDto>(new GetUserQuery(userId), ct);

// Event — all handlers run; exceptions are aggregated
await mediator.PublishAsync(new UserDeletedEvent(userId), ct);
```

**Error semantics:**
- `SendAsync` / `QueryAsync`: throws `InvalidOperationException` if no handler or multiple handlers are registered.
- `PublishAsync`: all handlers run even if some throw; exceptions are collected and re-thrown as `AggregateException`.

---

### DI Registration

Minimal `Program.cs` setup:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddSimpleMediator(typeof(Program).Assembly)           // scan for handlers
    .AddPipelineBehavior<ValidationBehavior<CreateUserCommand, Guid>>() // explicit behaviors
    .AddPipelineBehavior<LoggingBehavior<CreateUserCommand, Guid>>();   // in registration order

var app = builder.Build();
```

`AddSimpleMediator(params Assembly[])`:
- Registers `IMediator` as **scoped**
- Scans all provided assemblies for `ICommandHandler<>`, `ICommandHandler<,>`, `IQueryHandler<,>`, `IEventHandler<>` implementations and registers them as **scoped**

`AddPipelineBehavior<TBehavior>()`:
- Registers a behavior as **scoped**
- Applied in registration order (first = outermost wrapper)

---

## Contributing

1. Fork the repo and create a feature branch
2. Write tests first (xUnit + FluentAssertions)
3. Run `dotnet test` — all tests must pass
4. Run `dotnet stryker` — mutation score must meet the threshold (≥ 85%)
5. Open a pull request — CI will run build, tests, and Stryker with results posted as a PR comment

---

## License

This project is licensed under the GNU General Public License v3.0 (GPL-3.0). You are free to use, modify, and distribute this software under the terms of the GPL-3.0. See the [LICENSE](https://github.com/otaviano/braza-sso/blob/main/LICENSE) file for details.
