---
name: test-strategy
description: "Use when deciding what to test, at which level, with which doubles, and where test files go."
---

<!-- ai-core:test-strategy@v1 -->
## Rule
- **Levels**: unit tests for domain logic and application services (fast, isolated, exhaustive on
  invariants and edge cases); integration tests for adapters and HTTP endpoints; end-to-end tests sparingly,
  for critical user journeys.
- **Doubles only at architecture boundaries** (ports): never mock domain objects, use real instances.
  Prefer hand-written fakes (in-memory implementations) over mocking libraries; verify behaviour, not
  implementation details.
- **One concept per test**, a single act.
- **Layout mirrors production**: unit tests in a folder tree matching the production code, endpoint tests
  apart, shared helpers apart, test namespaces matching their folder. When production code moves, its tests
  move with it. Never organise tests by chronology (step 1, step 2…). Front-end tests sit next to the
  component they test.
- **Do not test**: private methods (go through the public API), framework or third-party behaviour, trivial
  accessors.

## Why
The right level keeps the suite fast and the failures precise. Mocks of domain objects test the mock setup
and break on every refactor; fakes at the ports test real behaviour. A layout that mirrors production makes
the test for a given piece of code findable without searching, and keeps it moving with that code.

## When to apply
Every project with an automated test suite. Stack-specific conventions (framework, naming) complement it.
<!-- /ai-core:test-strategy -->

## In this project
No project-specific notes.
