---
name: dotnet-clean-architecture
description: Workflow skill for implementing features in a .NET Clean Architecture project. Use when adding new use cases, commands, handlers, or domain logic following CQRS and Chain of Responsibility patterns.
---

# .NET Clean Architecture Implementation Skill

This skill provides workflows for implementing new features in a .NET Clean Architecture project following Clean Architecture principles.

## Invocation
Use this skill by asking Claude to implement a new use case, command, or feature. Example: _"add a use case to process an order"_ or _"create a query to list pending items"_.

## When to Use
- Adding new business logic
- Creating new commands or queries
- Implementing domain workflows
- Adding validation rules
- Extending the API endpoints

## Workflow: Add New Use Case

1. **Define the Command or Query** in `Application/UseCases/`
   - Command: `NewCommand.cs` inheriting `Command`
   - Query: `NewQuery.cs` implementing `IQuery<Result>`
   - Include all necessary parameters

2. **Create Validator** in `Application/UseCases/`
   - `NewCommandValidator.cs` extending `AbstractValidator<NewCommand>`
   - Add validation rules

3. **Implement Handler** in `Application/UseCases/`
   - Command handler: `NewCommandHandler.cs` extending `RequestHandler<NewCommand>`, override `Handle`
   - Query handler: `NewQueryHandler.cs` implementing `IQueryHandler<NewQuery, Result>`, implement `ExecuteAsync`
   - Use primary constructor for dependencies
   - Validate, execute business logic, persist if needed

4. **Update Domain** if needed
   - Add services or handlers in `Domain/`
   - Extend Chain of Responsibility if workflow-related

5. **Add API Endpoint** in `Api/Endpoints/`
   - Map new route in endpoint group
   - Add validation filter
   - Create request/response view models

6. **Register Dependencies** in `Infra.IoC/`
   - Add to appropriate setup extensions

7. **Add Tests** in `tests/`
   - Unit tests for validator, handler, domain logic

## Validation
After implementation:
- Run unit tests
- Run integration tests if applicable
- Run mutation tests to ensure code quality
- Verify API documentation updates
- Test endpoint with sample data
- Ensure code adheres to Clean Code principles (small functions, meaningful names, etc.) /shared/rules/clean-code-uncle-bob.md
- Follow .NET conventions for async patterns and language features /dotnet/rules/dotnet
