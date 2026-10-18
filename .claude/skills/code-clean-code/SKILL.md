---
name: code-clean-code
description: "Use when writing or reviewing any code — meaningful names, one level of abstraction per method, comments that explain why, fail fast, I/O separated from logic, scoped boy-scout cleanup."
---

<!-- ai-core:code-clean-code@v1 -->
## Rule
- **Meaningful names**: no abbreviations, no single-letter variables except loop indices.
- **Small methods, one level of abstraction each.**
- **Comments explain why**; the code explains what. Bad code is refactored, not commented.
- **Fail fast**: validate at the boundaries and throw early on invalid state.
- **Separate I/O from logic**: functions that decide are predictable and free of side effects.
- **Boy-scout rule within the scope of the change**: leave the code you touch cleaner, without drifting
  into unrelated files.

## Why
Code is read far more often than written, increasingly by agents that take names and structure literally.
Mixed levels of abstraction and logic tangled with I/O make both reading and testing expensive. Cleanup
outside the change's scope bloats the diff and hides the real change from the reviewer.

## When to apply
All code. Generated or vendored code is out of scope.
<!-- /ai-core:code-clean-code -->

## In this project
No project-specific notes.
