---
name: arch-rich-domain-model
description: "Use before adding a method to a service class, or when designing entities — behaviour that changes an entity or applies its rules belongs in the entity (tell, don't ask)."
---

<!-- ai-core:arch-rich-domain-model@v1 -->
## Rule
- The domain has **no reference to infrastructure** (ORM, HTTP, file system).
- Entities **protect their invariants**: private setters, factory methods.
- Aggregates communicate through **domain events**.

"Anemic" has no oracle, so make it a procedure. **Before adding a method to a service, stop and answer:**
1. Does it change an entity's state, or apply a business rule to that entity? If yes, the behaviour
   belongs **in the entity**; the service only orchestrates (load, call, persist).
2. Am I reading an entity's state to decide in its place? That is *ask*. Switch to *tell*: expose the
   decision on the entity, which then owns its invariant.
3. Would this entity have at least one unit test of its own behaviour? If not, it smells anemic: either
   the behaviour is misplaced, or it is a genuine pure-data object. Decide explicitly; do not drift.

**Richness test:** can the rule be violated from outside the entity? If yes, the model is anemic. In a rich
model, the invariant has exactly one home.

## Why
In an anemic model, rules are scattered across services, duplicated, and easy to bypass; nothing guarantees
the invariant. Putting behaviour next to the data gives each rule a single home and makes it testable in
isolation.

## When to apply
Domains with real business rules. A plain CRUD context with no invariants does not need rich entities:
data objects are fine there.
<!-- /ai-core:arch-rich-domain-model -->

## In this project
No project-specific notes.
