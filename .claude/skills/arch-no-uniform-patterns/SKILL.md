---
name: arch-no-uniform-patterns
description: "Use when reviewing or proposing a design that spreads a pattern across a whole codebase — event sourcing, CQRS or value objects everywhere are over-engineering signals."
---

<!-- ai-core:arch-no-uniform-patterns@v2 -->
## Rule
Over-engineering is a **right pattern applied uniformly** where it belonged **locally**, erasing the
boundaries that should have contained it. Flag these signals:

- **Event sourcing or CQRS across the whole project**: the CRUD majority pays the tax for the few contexts
  that need it.
- **Value objects everywhere**, down to SQL queries and view models: primitive obsession cured so hard it
  becomes value-object obsession, and it mixes domain, persistence and presentation. Introduce a value
  object only when primitives could be **swapped by mistake** in a call, or when it **carries behaviour**.

Event sourcing / CQRS is **earned** when history is the value (audit, finance, regulation), when
invariants are complex over time, or when reads and writes are strongly asymmetric. Otherwise: assumed CRUD.

## Why
A uniform pattern often substitutes for the real work of drawing boundaries ("we are a CQRS shop" replaces
"where does history create value?"). Every context then pays the full cost while only a few get the
benefit.

## When to apply
Design reviews and architecture proposals, especially at project start or when a pattern starts spreading
beyond the context that justified it.
<!-- /ai-core:arch-no-uniform-patterns -->

## In this project
Event sourcing is rejected (ADR-0006); domain events serve reactions only (ADR-0003), never as a persistence model. Typed ids (`IssuerId`, `CustomerId`) meet the swap criterion; API requests and persistence contracts stay primitive. `Siren` and `VatNumber` are plain strings side by side in commands: value-object candidates once validation or a mix-up shows up.
