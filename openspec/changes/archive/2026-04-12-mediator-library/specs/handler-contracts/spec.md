## ADDED Requirements

### Requirement: ICommandHandler contract for fire-and-forget commands
The system SHALL define `ICommandHandler<TCommand>` where `TCommand : ICommand` with a single method `Task HandleAsync(TCommand command, CancellationToken cancellationToken)`.

#### Scenario: Handler is invoked with the dispatched command
- **WHEN** a class implements `ICommandHandler<TCommand>` and is registered in DI
- **THEN** calling `IMediator.SendAsync(command, ct)` SHALL invoke `HandleAsync` on that class with the same command instance and cancellation token

---

### Requirement: ICommandHandler contract for commands with result
The system SHALL define `ICommandHandler<TCommand, TResult>` where `TCommand : ICommand<TResult>` with a single method `Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken)`.

#### Scenario: Handler returns result to caller
- **WHEN** a class implements `ICommandHandler<TCommand, TResult>` and `HandleAsync` returns a value
- **THEN** `IMediator.SendAsync<TCommand, TResult>` SHALL return that same value to the caller

---

### Requirement: IQueryHandler contract
The system SHALL define `IQueryHandler<TQuery, TResult>` where `TQuery : IQuery<TResult>` with a single method `Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken)`.

#### Scenario: Query handler returns result to caller
- **WHEN** a class implements `IQueryHandler<TQuery, TResult>` and `HandleAsync` returns a value
- **THEN** `IMediator.QueryAsync<TQuery, TResult>` SHALL return that same value to the caller

---

### Requirement: IEventHandler contract
The system SHALL define `IEventHandler<TEvent>` where `TEvent : IEvent` with a single method `Task HandleAsync(TEvent evt, CancellationToken cancellationToken)`.

#### Scenario: All registered event handlers are called
- **WHEN** multiple classes implement `IEventHandler<TEvent>` and are registered in DI
- **THEN** `IMediator.PublishAsync` SHALL invoke `HandleAsync` on every registered instance

---

### Requirement: Exactly one handler per command or query
The system SHALL enforce that only one `ICommandHandler<TCommand>` (or `ICommandHandler<TCommand, TResult>` or `IQueryHandler<TQuery, TResult>`) is registered per request type.

#### Scenario: Duplicate command handler registration throws at dispatch
- **WHEN** two handlers are registered for the same command type
- **THEN** the mediator SHALL throw `InvalidOperationException` at dispatch time identifying the ambiguous registration
