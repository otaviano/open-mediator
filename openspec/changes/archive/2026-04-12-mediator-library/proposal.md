## Why

The project needs its own Mediator implementation to decouple request senders from handlers, replacing external dependencies (Brighter/Darker) with a lightweight, DI-first library tailored to this codebase's conventions. This gives full control over the dispatch pipeline, extensibility via pipeline behaviors, and a clean contract model based on request interfaces (commands, queries, events).

## What Changes

- Introduce a new `OpenMediator` library project with core mediator abstractions and runtime
- Define typed request interfaces: `ICommand`, `ICommand<TResult>`, `IQuery<TResult>`, `IEvent`
- Define handler interfaces: `ICommandHandler<T>`, `ICommandHandler<T, TResult>`, `IQueryHandler<T, TResult>`, `IEventHandler<T>`
- Implement `IMediator` with `Send` (commands), `Query` (queries), and `Publish` (events) dispatch methods
- Implement `IRequestHandler` resolution via DI container — handlers registered by their request type interface
- Support pipeline behaviors (`IPipelineBehavior<TRequest, TResponse>`) for cross-cutting concerns (validation, logging, etc.)
- Provide `IServiceCollection` extension methods for handler auto-registration via assembly scanning
- **BREAKING**: Remove Brighter (`Paramore.Brighter`) and Darker (`Paramore.Darker`) dependencies
- Ship a comprehensive `README.md` covering all interfaces, behaviors, and DI setup
- Add GitHub Actions CI: build, xUnit tests with Dorny report, Stryker mutation testing with PR comment
- Scope: implementation is entirely within the `open-mediator` repository — no changes to consumer projects

## Capabilities

### New Capabilities

- `mediator-core`: Core mediator abstraction — `IMediator` interface, `IRequest`, `ICommand`, `IQuery<TResult>`, `IEvent` marker interfaces, and the `Mediator` concrete implementation with DI-based handler resolution
- `handler-contracts`: Typed handler interfaces (`ICommandHandler<T>`, `ICommandHandler<T, TResult>`, `IQueryHandler<T, TResult>`, `IEventHandler<T>`) and their base contracts
- `pipeline-behaviors`: `IPipelineBehavior<TRequest, TResponse>` pipeline support for cross-cutting concerns, executed in registration order around handler dispatch
- `di-registration`: `IServiceCollection` extension for auto-registering handlers and behaviors from assemblies via assembly scanning, matching handler interface to request type

### Modified Capabilities

<!-- No existing specs to modify — this is a greenfield library -->

## Impact

- **Scope**: `open-mediator` repository only — consumer project migration is out of scope
- **New project**: `src/OpenMediator/` (class library, no external mediator deps)
- **New project**: `tests/OpenMediator.Tests/` (xUnit, NSubstitute, FluentAssertions, Stryker)
- **New file**: `README.md` — full usage documentation with code examples
- **New file**: `.github/workflows/ci.yml` — build, test (Dorny report), mutation test (Stryker PR comment)
- **Dependencies removed**: `Paramore.Brighter`, `Paramore.Darker`
- **Dependencies added**: `Microsoft.Extensions.DependencyInjection.Abstractions` only (targets `net10.0`)
