---
name: code-object-design
description: "Use when designing or reviewing classes and their collaborations — SOLID, tell don't ask, the law of Demeter, command-query separation, immutability."
---

<!-- ai-core:code-object-design@v1 -->
## Rule
- **SOLID**: one reason to change per class; extend with new implementations rather than editing existing
  ones; subtypes substitutable without surprise; small, focused interfaces; depend on abstractions where a
  seam is needed.
- **Tell, don't ask**: send a command to the object that owns the data instead of reading its state to
  decide in its place.
- **Law of Demeter**: talk to direct collaborators; no chains such as `a.B.C.Do()`.
- **Command-query separation**: a method either answers a question or changes state, not both.
- **Immutability by default**; mutation is a deliberate, local choice.

## Why
These principles keep each decision next to the data it depends on, so that a change touches one place.
Chains and ask-then-decide code spread knowledge of internal structure across callers, which then break
together. Methods that both query and mutate surprise their callers.

## When to apply
Object-oriented code with real behaviour. They are weighed, not applied mechanically: when one would add a
speculative abstraction, simpler and more fundamental principles (make it work, YAGNI, KISS) win.
<!-- /ai-core:code-object-design -->

## In this project
No project-specific notes.
