---
name: dotnet-conventions
description: General C# and .NET coding conventions applicable to all .NET projects. Language features, async patterns, and testing stack.
applyTo: "**/*.{cs,csproj,slnx}"
---

# General .NET Coding Conventions

## Language Features
- Use C# 12+ features: records, primary constructors, collection expressions
- Async/await for all I/O operations; suffix async methods with `Async`
- Prefer `record` for immutable data transfer objects
- Use primary constructors to reduce boilerplate

## Testing
- xUnit as test framework
- NSubstitute for mocking
- FluentAssertions for readable assertions
- Mirror source structure in the `tests/` directory
- Test naming convention: `MethodName_Scenario_ExpectedResult`
