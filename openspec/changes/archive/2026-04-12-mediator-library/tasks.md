## 0. Workflow Convention

- [x] 0.1 After completing each numbered section, stage only related files and create an atomic commit with a conventional message (e.g., `feat: define ICommand marker interfaces`)
- [x] 0.2 Never batch unrelated changes into a single commit — one logical unit per commit

## 1. Project Setup

- [x] 1.1 Create `src/OpenMediator/` class library project targeting `net10.0`
- [x] 1.2 Add `OpenMediator` project reference to `OpenMediator.sln`
- [x] 1.3 Add `Microsoft.Extensions.DependencyInjection.Abstractions` package to `OpenMediator`
- [x] 1.4 Create `tests/OpenMediator.Tests/` xUnit test project and add project reference to `OpenMediator`
- [x] 1.5 Add xUnit, NSubstitute, FluentAssertions, and Stryker.NET packages to test project

## 2. Request Marker Interfaces

- [x] 2.1 Define `ICommand` marker interface in `OpenMediator/Abstractions/`
- [x] 2.2 Define `ICommand<TResult>` marker interface in `OpenMediator/Abstractions/`
- [x] 2.3 Define `IQuery<TResult>` marker interface in `OpenMediator/Abstractions/`
- [x] 2.4 Define `IEvent` marker interface in `OpenMediator/Abstractions/`

## 3. Handler Contracts

- [x] 3.1 Define `ICommandHandler<TCommand>` interface with `Task HandleAsync(TCommand, CancellationToken)`
- [x] 3.2 Define `ICommandHandler<TCommand, TResult>` interface with `Task<TResult> HandleAsync(TCommand, CancellationToken)`
- [x] 3.3 Define `IQueryHandler<TQuery, TResult>` interface with `Task<TResult> HandleAsync(TQuery, CancellationToken)`
- [x] 3.4 Define `IEventHandler<TEvent>` interface with `Task HandleAsync(TEvent, CancellationToken)`

## 4. Pipeline Behavior Contract

- [x] 4.1 Define `RequestHandlerDelegate<TResponse>` delegate type (`Func<CancellationToken, Task<TResponse>>`)
- [x] 4.2 Define `IPipelineBehavior<TRequest, TResponse>` interface with `Task<TResponse> HandleAsync(TRequest, RequestHandlerDelegate<TResponse>, CancellationToken)`

## 5. IMediator Interface

- [x] 5.1 Define `IMediator` interface with `SendAsync<TCommand>(TCommand, CancellationToken)` for fire-and-forget commands
- [x] 5.2 Add `SendAsync<TCommand, TResult>(TCommand, CancellationToken)` overload to `IMediator` for commands with result
- [x] 5.3 Add `QueryAsync<TQuery, TResult>(TQuery, CancellationToken)` to `IMediator`
- [x] 5.4 Add `PublishAsync<TEvent>(TEvent, CancellationToken)` to `IMediator`

## 6. Mediator Implementation

- [x] 6.1 Create `Mediator` class implementing `IMediator` with `IServiceProvider` primary constructor
- [x] 6.2 Implement `SendAsync<TCommand>` — resolve `ICommandHandler<TCommand>`, throw if missing, invoke `HandleAsync`
- [x] 6.3 Implement `SendAsync<TCommand, TResult>` — resolve `ICommandHandler<TCommand, TResult>`, build behavior pipeline, invoke
- [x] 6.4 Implement `QueryAsync<TQuery, TResult>` — resolve `IQueryHandler<TQuery, TResult>`, build behavior pipeline, invoke
- [x] 6.5 Implement `PublishAsync<TEvent>` — resolve `IEnumerable<IEventHandler<TEvent>>`, invoke all, aggregate exceptions
- [x] 6.6 Extract pipeline composition logic into a private `BuildPipeline<TRequest, TResponse>` helper method
- [x] 6.7 Implement duplicate handler detection — throw `InvalidOperationException` when more than one command/query handler is registered

## 7. DI Registration

- [x] 7.1 Create `OpenMediatorServiceCollectionExtensions` static class in `OpenMediator/Extensions/`
- [x] 7.2 Implement `AddOpenMediator(params Assembly[])` — registers `IMediator` as scoped and triggers assembly scanning
- [x] 7.3 Implement assembly scanner — discover and register `ICommandHandler<>`, `ICommandHandler<,>`, `IQueryHandler<,>`, `IEventHandler<>` implementations as scoped
- [x] 7.4 Implement `AddPipelineBehavior<TBehavior>()` extension method (scoped lifetime)
- [x] 7.5 Implement non-generic `AddPipelineBehavior(Type behaviorType)` overload

## 8. Tests — Core Dispatch

- [x] 8.1 Test `SendAsync<TCommand>` invokes registered handler
- [x] 8.2 Test `SendAsync<TCommand>` throws `InvalidOperationException` when no handler registered
- [x] 8.3 Test `SendAsync<TCommand, TResult>` invokes handler and returns result
- [x] 8.4 Test `QueryAsync<TQuery, TResult>` invokes handler and returns result
- [x] 8.5 Test `QueryAsync` throws when no handler registered
- [x] 8.6 Test `PublishAsync` invokes all registered event handlers
- [x] 8.7 Test `PublishAsync` is no-op when no handlers registered
- [x] 8.8 Test `PublishAsync` aggregates exceptions from multiple failing handlers

## 9. Tests — Pipeline Behaviors

- [x] 9.1 Test single behavior wraps handler dispatch (entry before, exit after)
- [x] 9.2 Test multiple behaviors execute in registration order (outermost first)
- [x] 9.3 Test behavior can short-circuit and prevent handler invocation
- [x] 9.4 Test behavior can inspect and replace handler result

## 10. Tests — DI Registration

- [x] 10.1 Test `AddOpenMediator` registers `IMediator` as resolvable from container
- [x] 10.2 Test assembly scanning registers `ICommandHandler<TCommand>` for all discovered implementations
- [x] 10.3 Test assembly scanning registers all four handler interface families
- [x] 10.4 Test `AddOpenMediator` with multiple assemblies registers handlers from all of them
- [x] 10.5 Test `AddPipelineBehavior` registers behavior and it is applied during dispatch

## 11. Mutation Testing — Stryker

- [x] 11.1 Add `stryker-config.json` at repo root — configure reporters (`json`, `html`, `progress`), thresholds (`break: 80`, `low: 85`), and project files
- [x] 11.2 Run `dotnet stryker` after core dispatch implementation (section 6) and fix surviving mutants
- [x] 11.3 Run `dotnet stryker` after pipeline behaviors implementation and fix surviving mutants
- [x] 11.4 Run `dotnet stryker` after DI registration implementation and fix surviving mutants
- [x] 11.5 Confirm mutation score meets threshold before marking implementation complete

## 12. README

- [x] 12.1 Create `README.md` at repo root with project overview, motivation, and comparison to MediatR/Brighter
- [x] 12.2 Document all request marker interfaces (`ICommand`, `ICommand<TResult>`, `IQuery<TResult>`, `IEvent`) with usage examples
- [x] 12.3 Document all handler interfaces with code snippets showing implementation patterns
- [x] 12.4 Document `IPipelineBehavior<TRequest, TResponse>` with a full validation behavior example
- [x] 12.5 Document `AddOpenMediator` and `AddPipelineBehavior` DI registration with a minimal `Program.cs` example
- [x] 12.6 Add section on dispatch methods (`SendAsync`, `QueryAsync`, `PublishAsync`) with before/after examples
- [x] 12.7 Add contributing guide, license, and badges (build status, mutation score)

## 13. CI Pipeline

- [x] 13.1 Create `.github/workflows/ci.yml` with triggers on `push` to `main` and `pull_request`
- [x] 13.2 Add build step: `dotnet build --configuration Release`
- [x] 13.3 Add test step: `dotnet test --logger trx --results-directory TestResults/`
- [x] 13.4 Add Dorny test reporter step using `dorny/test-reporter` action to publish TRX results as PR check
- [x] 13.5 Add Stryker mutation testing step: `dotnet stryker --reporter json --reporter html`
- [x] 13.6 Upload Stryker HTML report as GitHub Actions artifact
- [x] 13.7 Add Stryker PR comment step — post mutation score summary as a PR comment using the JSON report
- [x] 13.8 Fail the CI build if Stryker mutation score falls below configured threshold
