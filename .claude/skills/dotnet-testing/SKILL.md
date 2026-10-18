---
name: dotnet-testing
description: "Use when writing .NET tests — xUnit, Arrange-Act-Assert, no mocks of domain objects."
---

<!-- ai-core:dotnet-testing@v1 -->
## Rule
- **xUnit**, structured as **Arrange-Act-Assert**.
- **Never mock domain objects**: build real ones. Mock only ports to the outside world.

## Why
A uniform structure makes tests readable at a glance. Mocking domain objects tests the mock setup instead
of the behaviour, and couples tests to implementation details, so they break on every refactor.

## When to apply
All .NET test projects. An existing suite on another framework keeps its framework; the structure and the
no-domain-mock rule still apply.
<!-- /ai-core:dotnet-testing -->

## In this project
No project-specific notes.
