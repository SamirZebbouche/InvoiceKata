---
name: git-branch-discipline
description: "Use when starting work on a topic or preparing a pull request — follow the project's declared branch regime, check the branch first, create branches that do not track main."
---

<!-- ai-core:git-branch-discipline@v2 -->
## Rule
Follow the **branch regime** the project declares in its "In this project" section, and in every regime:
- **check the current branch before editing**;
- one topic = one branch with a conventional prefix (`feat/`, `fix/`, `chore/`, `ci/`, `docs/`,
  `refactor/`, `test/`) whenever the regime asks for a branch; when the conversation moves to an unrelated
  subject, propose a fresh branch rather than piling changes onto the current one;
- **create a branch without tracking the base**: `git switch --no-track -c <topic> origin/main`, then
  `git push -u origin <topic>` straight away; before handing a branch over, check its upstream
  (`git rev-parse --abbrev-ref @{u}` must name the topic branch, not `main`);
- no stacked branches.

## Options
- **`pr-only`** (default): `main` only moves through a reviewed pull request with green CI. On `main`, stop
  and propose a branch first. One pull request = one deliverable package, holding all its commits.
- **`solo-direct`**: a single-developer repository may commit directly to `main` — a pull request reviewed
  by its own author is a ceremony, not a review. A branch is still required as soon as a topic outlasts a
  day or leaves the build broken. The declaration names its **return trigger**: the day a second developer
  (or any reinforcement) clones the repository, the project switches back to `pr-only`.

Choose with the human by team size and by how much traceability the phase needs (a prototype iterates
faster solo). Never infer the regime from the repository.

## Why
In a team, the pull request is the unit of ratification: reviewable and shippable on its own; stacked
branches couple reviews and blur what is ratified. Alone, the same ceremony costs speed and buys nothing,
which is why the choice belongs to the project, with a written trigger so it is revisited at the right
moment rather than at the first merge conflict. In both regimes, a branch created from `origin/main` tracks
`main` by default, so a plain push or an editor's "sync" sends its commits straight to `main`.

## When to apply
Every git repository. Throwaway local experiments need no prefix.
<!-- /ai-core:git-branch-discipline -->

## In this project
Regime: `pr-only`.
