---
name: arch-earn-your-architecture
description: "Use when choosing an architecture or a pattern (DDD, CQRS, event sourcing, hexagonal) — start with the simplest thing, invest only where the domain pushes, choose per bounded context."
---

<!-- ai-core:arch-earn-your-architecture@v2 -->
## Rule
- Start with the **simplest thing that works** (often plain CRUD). Move up to tactical DDD, CQRS or event
  sourcing **when the domain pushes**, not before.
- Invest design effort **differentially**: in time, on hotspots (churn × complexity); in space, per
  **bounded context**. Choose one architecture per context, not one per project.
- Treat paradigms as vocabulary and safety nets, not recipes: TDD to be bold safely, DDD for a shared
  language, hexagonal to isolate the volatile (infrastructure) from the stable (domain). Apply each one
  when you feel the pain it cures.
- **Tolerate duplication until a real second consumer exists**, and refuse an extraction that adds a place
  without removing the copies.
- A polymorphic seam (Strategy and the like) is earned only when the choice is **unknown at compile time**;
  one implementation per strategy is a lying abstraction.

## Why
You discover the right design by building the wrong one first: "naive version, mess, refactor" is the
method, not its failure. Over-architecting a simple domain is as harmful as spaghetti. Inheriting an
architecture by team identity makes the simple parts pay for the complex ones.

## When to apply
Any architecture or pattern decision. Stable, working code that is not a hotspot can stay as it is.
<!-- /ai-core:arch-earn-your-architecture -->

## In this project
No project-specific notes.
