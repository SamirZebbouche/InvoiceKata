---
name: test-check
description: "Use before committing or when asked whether a change is safe — run every objective gate CI would require (lint, tests, types, build) for each active stack and give one verdict."
---

<!-- ai-core:test-check@v2 -->
## Rule
Produce **one verdict**, "is it green?", over every objective gate CI would require — lint, tests, types,
build — for **every active stack**. Green only if everything passes; if red, say what breaks.

## Steps
1. **Resolved commands first.** If "In this project" lists resolved check commands
   (`- <stack>: <command>`), run them **verbatim, in order**. Do not rediscover, do not ask again. Go to step 5.
2. **Otherwise, discover** the real commands, preferring what the project already runs:
   - CI definitions (`.github/workflows/`, `.gitlab-ci.yml`, `azure-pipelines.yml`, `Jenkinsfile`…),
     then task runners (`Makefile`, `justfile`, `Taskfile.yml`), then package scripts;
   - otherwise the stack hints below.
3. **Propose** the commands per stack and get confirmation before running them.
4. After a confirmed run, **propose** adding the lines to "In this project" (show the diff, wait for a GO).
   Never rewrite that section on your own.
5. **Mirror CI exactly**: the same commands **on the same scope** (lint the whole folder CI lints, not only
   `src`). Run non-interactively: never a watch mode here; set `CI=true` where a tool would otherwise
   prompt or watch. In a monorepo, run each affected project.
6. **Run every step, even after a failure**, then aggregate one verdict: green only if every command
   passed. For each failure, name the gate and quote the first relevant error; report skipped tests and
   why they were skipped.
7. **Fix nothing on your own authority**: report, then propose the fixes.

Stack hints:
- **.NET**: `dotnet test` (solution or `*Tests.csproj`); `dotnet format --verify-no-changes`;
  `dotnet build -warnaserror` (stands in for a type check).
  A running application or `dotnet watch` locks its build output (on Windows: `MSB3027`, file in use).
  Build the tests outside the repository: `dotnet test --artifacts-path <stable folder outside the repo>`
  (.NET 8 SDK or later); a stable folder keeps the build incremental. Older SDKs:
  `--property:BaseOutputPath=<dir>/bin/ --property:BaseIntermediateOutputPath=<dir>/obj/`.
- **JavaScript / TypeScript**: package manager from the lockfile (`package-lock.json` → npm,
  `pnpm-lock.yaml` → pnpm, `yarn.lock` → yarn, `bun.lock`/`bun.lockb` → bun). Prefer the `test`, `lint`,
  `typecheck` scripts; otherwise `vitest run` or `jest`, `tsc --noEmit`, `eslint .`. Plain `vitest` and some
  `npm test` setups start a watcher: use `vitest run` or `CI=true`.
- **Python**: `pytest`; `ruff check` and `ruff format --check`; `mypy` or `pyright` if configured; prefix
  with `uv run` or `poetry run` when the project uses them.
- **Java / Kotlin**: `./mvnw verify` or `./gradlew check` (prefer the wrapper).
- **Go**: `go test ./...`, `go vet ./...`, `golangci-lint run` if configured.
- **Rust**: `cargo test`, `cargo clippy -- -D warnings`, `cargo fmt --check`.

## Why
A single, CI-equivalent verdict before committing catches what a partial run misses (lint or types after
green tests). Freezing the confirmed commands makes the check deterministic: the same gates every time,
without the agent reinterpreting the project. A narrower scope than CI's turns "green here" into "red
there".

## When to apply
Before every commit and whenever a change must be declared safe. For the fast inner loop while coding, a
continuous test watcher is the better tool.
<!-- /ai-core:test-check -->

## In this project
- dotnet: dotnet test && dotnet format --verify-no-changes
