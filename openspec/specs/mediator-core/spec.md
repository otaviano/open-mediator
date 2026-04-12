## ADDED Requirements

### Requirement: IMediator dispatches commands without result
The system SHALL provide `IMediator.SendAsync<TCommand>(TCommand command, CancellationToken ct)` that resolves and invokes the registered `ICommandHandler<TCommand>`.

#### Scenario: Registered command handler is invoked
- **WHEN** a command implementing `ICommand` is sent via `IMediator.SendAsync`
- **THEN** the corresponding `ICommandHandler<TCommand>` is resolved from DI and `HandleAsync` is called with the command and cancellation token

#### Scenario: No handler registered throws
- **WHEN** a command is sent for which no `ICommandHandler<TCommand>` is registered
- **THEN** the mediator SHALL throw `InvalidOperationException` with a message identifying the missing handler type

---

### Requirement: IMediator dispatches commands with result
The system SHALL provide `IMediator.SendAsync<TCommand, TResult>(TCommand command, CancellationToken ct)` that resolves and invokes `ICommandHandler<TCommand, TResult>` and returns the result.

#### Scenario: Registered command-with-result handler is invoked and result returned
- **WHEN** a command implementing `ICommand<TResult>` is sent via the typed `SendAsync` overload
- **THEN** the corresponding `ICommandHandler<TCommand, TResult>` is resolved, `HandleAsync` is called, and its return value is returned to the caller

---

### Requirement: IMediator dispatches queries
The system SHALL provide `IMediator.QueryAsync<TQuery, TResult>(TQuery query, CancellationToken ct)` that resolves and invokes `IQueryHandler<TQuery, TResult>` and returns the result.

#### Scenario: Registered query handler is invoked and result returned
- **WHEN** a query implementing `IQuery<TResult>` is dispatched via `IMediator.QueryAsync`
- **THEN** the corresponding `IQueryHandler<TQuery, TResult>` is resolved from DI, `HandleAsync` is called, and its return value is returned to the caller

#### Scenario: No query handler registered throws
- **WHEN** a query is dispatched for which no `IQueryHandler` is registered
- **THEN** the mediator SHALL throw `InvalidOperationException` identifying the missing handler type

---

### Requirement: IMediator publishes events to all handlers
The system SHALL provide `IMediator.PublishAsync<TEvent>(TEvent evt, CancellationToken ct)` that resolves all registered `IEventHandler<TEvent>` instances and invokes each one.

#### Scenario: All registered event handlers are invoked
- **WHEN** an event implementing `IEvent` is published via `IMediator.PublishAsync`
- **THEN** every registered `IEventHandler<TEvent>` is resolved and `HandleAsync` is called on each

#### Scenario: No event handlers registered is a no-op
- **WHEN** an event is published and no `IEventHandler<TEvent>` is registered
- **THEN** `PublishAsync` SHALL complete without error (no-op)

#### Scenario: Exceptions from event handlers are aggregated
- **WHEN** one or more event handlers throw during `PublishAsync`
- **THEN** all handlers SHALL still be invoked and a single `AggregateException` containing all thrown exceptions SHALL be thrown after all handlers complete

---

### Requirement: Request marker interfaces are well-typed
The system SHALL define the following marker interfaces:
- `ICommand` — fire-and-forget command
- `ICommand<TResult>` — command returning a result
- `IQuery<TResult>` — read-only query returning a result
- `IEvent` — notification dispatched to multiple handlers

#### Scenario: Marker interfaces are distinct types
- **WHEN** a developer implements `ICommand` on a class
- **THEN** that class SHALL NOT be assignable to `IQuery<TResult>` or `IEvent` without explicit implementation
