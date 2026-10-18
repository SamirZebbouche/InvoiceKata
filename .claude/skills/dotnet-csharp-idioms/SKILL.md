---
name: dotnet-csharp-idioms
description: "Use when writing or reviewing C# code — immutability by default, sealed and required, pattern matching, async all the way, nullable reference types, naming and namespace conventions."
---

<!-- ai-core:dotnet-csharp-idioms@v2 -->
## Rule
- **Immutable by default**: `record` for value objects and DTOs, `readonly`, immutable collections.
- `sealed` on anything not designed for inheritance; `required` for mandatory properties.
- **Pattern matching** rather than type checks followed by casts.
- **Async all the way**: never `.Result` or `.Wait()`.
- **Nullable reference types** enabled; validate at the boundaries and fail fast.
- No `static` utility class carrying **business logic**: inject a service instead.
- Naming: interfaces prefixed `I`, async methods suffixed `Async`, private fields `_camelCase`,
  no abbreviations.
- The **root namespace equals the assembly name**, without exception.
- A `switch` over an enum meant to grow: one explicit case per value, a `default` that **throws**, and a
  test that iterates every enum value. Never a `default` that returns a plausible value.

## Why
Immutability and sealed types remove whole classes of bugs and make intent explicit. Blocking on async
code causes deadlocks and thread starvation. Nullable annotations move null errors from runtime to compile
time. Static helpers with business logic cannot be substituted in tests and hide dependencies. A namespace
that differs from its assembly hides a dependency boundary behind a misleading name. A C# enum can hold any
integer, so the compiler cannot prove a switch exhaustive; a plausible `default` turns a forgotten case
into silently wrong behaviour, while a throwing one plus a test over all values fails the day a value is
added.

## When to apply
All C# code. Interop or performance-critical code may need mutable structures: make that an explicit,
local exception.
<!-- /ai-core:dotnet-csharp-idioms -->

## In this project
No project-specific notes.
