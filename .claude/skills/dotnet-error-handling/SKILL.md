---
name: dotnet-error-handling
description: "Use when handling failures in .NET code — result pattern or domain exceptions, no null returns, validation at the boundaries, catch-all only at the top level."
---

<!-- ai-core:dotnet-error-handling@v1 -->
## Rule
- Use the **result pattern** or **domain exceptions**; never return `null` to signal a failure.
- **Validate at the boundaries** (controllers, handlers); the domain **throws** on invariant violations.
- Never catch `Exception` except in the top-level middleware.

## Why
A `null` return carries no reason and spreads null checks everywhere. Validation at the edges keeps the
domain free of input parsing, and an invariant violation inside the domain is a bug that must surface.
Catching `Exception` locally swallows bugs and leaves the system in an unknown state.

## When to apply
All .NET application code. Libraries exposing a public API may follow the conventions of their consumers.
<!-- /ai-core:dotnet-error-handling -->

## In this project
No project-specific notes.
