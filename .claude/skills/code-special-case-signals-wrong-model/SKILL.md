---
name: code-special-case-signals-wrong-model
description: "Use when a rule needs a hand-written special case in the middle of its logic — look for the model in which the case falls out naturally."
---

<!-- ai-core:code-special-case-signals-wrong-model@v1 -->
## Rule
When a rule (geometric, mathematical, business) needs a **hand-written special case** in the middle of its
logic, treat it as a signal that the **model is wrong**. Look for the model in which that case falls out
naturally, ideally as the extreme value of the general behaviour, before adding the branch.

## Why
Special cases accumulate, interact and surprise; each one is a place where the code disagrees with the
idea it implements. A better model usually removes several of them at once and makes the remaining
behaviour predictable.

## When to apply
Rule engines, physics or geometry, pricing or eligibility rules. A genuine business exception decided by
the domain stays an explicit, named case.
<!-- /ai-core:code-special-case-signals-wrong-model -->

## In this project
No project-specific notes.
