---
name: dotnet-dependency-injection
description: "Use when registering or consuming services in a .NET dependency injection container — an interface where a test needs a double, constructor injection only."
---

<!-- ai-core:dotnet-dependency-injection@v2 -->
## Rule
- Introduce an **interface where a test needs a double**: ports to the outside world, I/O, the clock,
  external or AI services. Register those through their interface.
- A plain internal service with no such need may be registered and injected as its concrete type.
- One implementation per interface, except with keyed services.
- **Constructor injection only**: no service locator, no property injection.

## Why
An interface earns its place by enabling substitution in tests; "always an interface" adds a file and an
indirection per class for nothing. Constructor injection makes every dependency visible in the signature
and guarantees a fully built object; a service locator hides dependencies and moves wiring errors to
runtime.

## When to apply
Any .NET application using a dependency injection container. Simple value types and DTOs are not services
and are created directly.
<!-- /ai-core:dotnet-dependency-injection -->

## In this project
No project-specific notes.
