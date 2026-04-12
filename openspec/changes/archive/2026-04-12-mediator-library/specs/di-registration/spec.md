## ADDED Requirements

### Requirement: AddOpenMediator registers IMediator and scans assemblies
The system SHALL provide `IServiceCollection.AddOpenMediator(params Assembly[] assemblies)` that:
1. Registers `IMediator` → `Mediator` as a scoped service
2. Scans the given assemblies for all handler interface implementations and registers them

#### Scenario: IMediator is resolvable after AddOpenMediator
- **WHEN** `services.AddOpenMediator(Assembly.GetExecutingAssembly())` is called
- **THEN** `IMediator` SHALL be resolvable from the built `IServiceProvider`

#### Scenario: Handlers discovered by assembly scanning are registered
- **WHEN** an assembly contains a class implementing `ICommandHandler<TCommand>`
- **THEN** after `AddOpenMediator`, `ICommandHandler<TCommand>` SHALL be resolvable from the container

---

### Requirement: Assembly scanning registers all handler interface variants
The system SHALL detect and register implementations of all four handler interface families:
- `ICommandHandler<TCommand>`
- `ICommandHandler<TCommand, TResult>`
- `IQueryHandler<TQuery, TResult>`
- `IEventHandler<TEvent>`

#### Scenario: All handler types in assembly are registered
- **WHEN** an assembly contains one class per handler interface type
- **THEN** all four interface bindings SHALL be registered after scanning

---

### Requirement: Handler lifetime defaults to scoped
The system SHALL register discovered handlers with a scoped lifetime unless overridden.

#### Scenario: Handler is resolved within the current scope
- **WHEN** a handler is resolved from a DI scope created for a request
- **THEN** the same handler instance SHALL be returned for multiple resolutions within the same scope

---

### Requirement: Pipeline behaviors are registered explicitly
The system SHALL provide `IServiceCollection.AddPipelineBehavior<TBehavior>()` (and a non-generic overload accepting `Type`) for explicit behavior registration, with scoped lifetime by default.

#### Scenario: Registered behavior is applied during dispatch
- **WHEN** `services.AddPipelineBehavior<ValidationBehavior<MyCommand, MyResult>>()` is called before building the host
- **THEN** `ValidationBehavior` SHALL be invoked during dispatch of `MyCommand`

#### Scenario: Behaviors are applied in registration order
- **WHEN** behaviors are registered in order B1, B2 via `AddPipelineBehavior`
- **THEN** B1 SHALL wrap B2 in the pipeline chain

---

### Requirement: Multiple assemblies can be scanned
The system SHALL accept multiple assemblies in `AddOpenMediator` and scan all of them.

#### Scenario: Handlers from multiple assemblies are all registered
- **WHEN** `AddOpenMediator(assemblyA, assemblyB)` is called and each assembly contains handlers
- **THEN** handlers from both assemblies SHALL be resolvable from the container
