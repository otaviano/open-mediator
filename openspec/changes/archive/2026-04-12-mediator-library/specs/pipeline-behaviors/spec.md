## ADDED Requirements

### Requirement: IPipelineBehavior wraps handler dispatch
The system SHALL define `IPipelineBehavior<TRequest, TResponse>` with a single method:
`Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)`
where `RequestHandlerDelegate<TResponse>` is `Func<CancellationToken, Task<TResponse>>`.

#### Scenario: Behavior is invoked before and after the handler
- **WHEN** a pipeline behavior is registered and a request is dispatched
- **THEN** the behavior's `HandleAsync` SHALL be called with the request, and calling `next(ct)` SHALL invoke the next behavior or the handler itself

#### Scenario: Behavior can short-circuit the pipeline
- **WHEN** a behavior does not call `next`
- **THEN** the handler SHALL NOT be invoked and the behavior's return value SHALL be returned to the caller

---

### Requirement: Behaviors are applied in registration order
The system SHALL apply registered `IPipelineBehavior<TRequest, TResponse>` instances in the order they were registered in DI, forming a middleware chain from first-registered (outermost) to last-registered (innermost), with the handler at the center.

#### Scenario: First registered behavior wraps all subsequent behaviors
- **WHEN** behaviors A, B, and C are registered in that order for a request type
- **THEN** the execution order SHALL be: A enters → B enters → C enters → handler → C exits → B exits → A exits

---

### Requirement: Behaviors apply to commands-with-result and queries
The system SHALL apply the pipeline behavior chain for `ICommand<TResult>` and `IQuery<TResult>` dispatches, where `TResponse` is the result type.

#### Scenario: Behavior receives and can modify the result
- **WHEN** a behavior wraps a command-with-result dispatch
- **THEN** the behavior MAY inspect or replace the value returned by `next` before returning to the caller

---

### Requirement: Fire-and-forget commands use a Unit-equivalent pipeline
The system SHALL apply pipeline behaviors to `ICommand` (no result) dispatches by using `Task` (or an internal `ValueTuple`/`Unit` representation) so that `IPipelineBehavior` has a uniform generic signature.

#### Scenario: Behaviors are invoked for fire-and-forget commands
- **WHEN** a pipeline behavior is registered and a fire-and-forget command is dispatched
- **THEN** the behavior's `HandleAsync` SHALL be invoked with the command and a `next` delegate that ultimately calls the command handler

---

### Requirement: Behaviors are resolved from DI per dispatch
The system SHALL resolve `IEnumerable<IPipelineBehavior<TRequest, TResponse>>` from the DI container at each dispatch invocation, supporting scoped behavior lifetimes.

#### Scenario: Scoped behavior receives scoped dependencies
- **WHEN** a pipeline behavior has scoped dependencies and is registered as scoped
- **THEN** the mediator SHALL resolve it within the active DI scope at dispatch time
