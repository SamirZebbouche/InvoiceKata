---
name: git-commit
description: "Use when committing — review the staged diff, keep one coherent increment per commit, write a tight Conventional Commits message whose body explains why."
---

<!-- ai-core:git-commit@v2 -->
## Rule
One commit = one coherent increment, with a tight Conventional Commits message that explains **why**.

## Steps
1. **Autonomy level.** Read the project's declared autonomy level for git actions (`none`, `commit`, `pr`,
   `autonomous-pr`).
   - `none`, or no level declared: prepare the message and stage, but do not commit; hand over to the human.
   - `commit` or `pr`: commit only after an explicit GO on this commit.
   - `autonomous-pr`: commit without asking.
   - Never commit on `main`, unless the project declares a single-developer branch regime that allows it.
2. **Inspect the staged diff** (`git diff --cached`). If nothing is staged, show `git status` and ask what to
   include. Never stage blindly (`git add -A` on a tree you have not reviewed).
3. **One coherent increment** (one TDD batch). If the diff mixes topics, propose a split. Never one commit
   per file.
4. **Write the message.**
   - Subject `<type>(<scope>): <summary>`, **72 characters at most**, imperative mood. Types: `feat`, `fix`,
     `refactor`, `test`, `docs`, `chore`, `ci`; also `perf`, `build`, `style`, `revert` if the project uses
     them. `!` after the type (or a `BREAKING CHANGE:` footer) marks a breaking change.
   - Body: **2 to 5 lines**, the why, not the what. A longer explanation belongs in the pull request
     description.
   - Trailers the project requires (co-author, issue reference) go last.
5. **Pass multi-line messages safely**: `git commit -F <file>` or `git commit -F -` with a heredoc on POSIX
   shells; in PowerShell, a single-quoted here-string (`@' … '@`, closing marker at column 0).
6. **Present** the message and the file list, then commit as step 1 allows. Report the resulting hash.
7. **Never** `--no-verify`, never amend or rebase a commit that has been pushed, never skip signing the
   project requires. If a hook fails, fix the cause and create a new commit.

## Why
Small, single-purpose commits make review, bisect and revert possible. A subject line that fits in every
tool's display, and a body that records the why, keep the history useful long after the diff stops being
self-explanatory. The autonomy level keeps the commit act where the human wants it.

## When to apply
Every commit made by an agent. Throwaway commits on a private scratch branch can be looser, but they never
reach a shared branch as such.
<!-- /ai-core:git-commit -->

## In this project
No project-specific notes.
