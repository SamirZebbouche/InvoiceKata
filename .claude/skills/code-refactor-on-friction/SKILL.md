---
name: code-refactor-on-friction
description: "Use when tempted to refactor or when choosing between a fix and a cleanup — refactor only where friction shows, on green, never mixed with a behaviour change, and fix the bug first unless the refactor makes the fix safe."
---

<!-- ai-core:code-refactor-on-friction@v2 -->
## Rule
- Refactor when the code resists a change ("this should be simple but it isn't"), not for aesthetics.
  Make the change easy, then make the easy change.
- Refactor just enough, on green, in small safe steps (extract, rename, move), then move on.
- **Never refactor and change behaviour in the same commit.**
- Facing a choice, **fix the bug first** and refactor afterwards, unless the refactor de-risks the fix
  (makes it safe or possible).

## Why
Friction is the code telling you where the abstraction is wrong; aesthetic refactors spend effort where
nothing hurts. A commit that both moves code and changes behaviour cannot be reviewed or reverted safely:
the behaviour change hides in the noise. A regression should never wait behind cosmetic cleanup.

## When to apply
Any change to existing code. Stable code that works and is not being changed is best left alone:
"it works, don't touch it" is often the right call.
<!-- /ai-core:code-refactor-on-friction -->

## In this project
No project-specific notes.
