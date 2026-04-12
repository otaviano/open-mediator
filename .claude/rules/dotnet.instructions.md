---
name: dotnet-clean-architecture-rules
description: Architecture and coding rules for .NET Clean Architecture projects. Clean Architecture with CQRS (Brighter/Darker), Chain of Responsibility for domain workflows, and Minimal API conventions.
applyTo: "**/*.{cs,csproj,slnx}"
---

# .NET Clean Architecture Rules

## Architecture Overview
This project follows Clean Architecture with CQRS pattern using Brighter (commands) and Darker (queries). The structure is:
- **Api**: Minimal API endpoints, middlewares, filters, view models
- **Application**: Use cases (commands, queries, handlers, validators, results)
- **Domain**: Business rules, enums, services, handlers (Chain of Responsibility)
- **Infra**: IoC, persistence, core concerns

## Coding Conventions

### Libraries
- FluentValidation for input validation
- Brighter (`Paramore.Brighter`) for command dispatching
- Darker (`Paramore.Darker`) for query dispatching
- Chain of Responsibility pattern for domain workflows

### Commands
- Inherit from `Command` base class: `public class MyCommand : Command { }`
- Handlers extend `RequestHandler<T>` and override `Handle(T command)`
- For async handlers, extend `RequestHandlerAsync<T>` and override `HandleAsync`
- Register handlers via `SubscriberRegistry` or assembly scanning
- Dispatch with `IAmACommandProcessor.Send(command)`

### Queries
- Implement `IQuery<TResult>`: `public class MyQuery : IQuery<Result> { }`
- Handlers implement `IQueryHandler<TQuery, TResult>` with `ExecuteAsync`
- Dispatch with `IQueryProcessor.ExecuteAsync(query)`

### General
- Use primary constructors for dependency injection
- Keep handlers focused on a single responsibility
- Validate with FluentValidation before processing

### Domain Logic
- Domain workflows use Chain of Responsibility with an abstract base handler
- Each handler checks if it can process the request, otherwise passes to next

### API Layer
- Minimal API with endpoint filters for validation
- Use Scalar for OpenAPI documentation
- Custom middleware for exception handling
- CORS enabled for frontend integration

### Validation
- Request validators in Api.Validators
- Use `ValidateAndThrowAsync` in handlers
- Endpoint filters apply validation before handler

### Persistence
- Repository pattern for data access
