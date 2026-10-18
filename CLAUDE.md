<!-- ai-core:method-deliberation@v2 -->
## Deliberate before proposing

### Rule
Never go straight from "go" to code. Classify the decision, then deliberate in proportion:

- **Classify the door.** Two-way door (rename, small internal fix, anything cheap to undo): just do it.
  One-way door (bounded-context boundary, persistence choice, public contract, data migration):
  deliberate in depth.
- **Proposer.** Draft a few concrete approaches.
- **Refuter.** Attack each approach against the project's rules. Does it apply a pattern uniformly where
  it belonged locally? Does it break or ignore a bounded context? Does it create an unnecessary one-way
  door? Is it the simplest thing that works? Does it violate TDD, SOLID, hexagonal boundaries or the
  domain language? Drop a branch on a proven violation; keep digging into the promising ones.
- **Present**, instead of "here is the code": the recommended approach, and each killed branch with the
  rule it broke.
- **Name structural decisions before building them**: interface placement, a new project or module, a new
  namespace, a new architectural concept or context name, a new layer, a dependency direction. Propose each
  one explicitly; it is ratified before implementation and never buried inside a larger batch.
- **The human decides** design questions. Implementation then runs under a real oracle (tests, build,
  types, lint).

### Why
An agent that jumps to code commits to its first idea and hides its reasoning. Design has no objective
oracle: the refuter judges judgement, not truth. It never certifies an architecture; it shows that the
proposal survived a rigorous critique. The tree of killed branches is the collaboration artifact: it lets
the human steer at design level with full visibility. A structural choice slipped into a large change is
the silent drift between the owner's intent and the code that this loop exists to prevent. Sizing the
effort to the door avoids the opposite failure: deliberating a typo is waste.

### When to apply
Every feature, fix or refactor request. Depth scales with risk × irreversibility. Implementation tasks
have an oracle, so the loop is short and the tests decide. Design decisions have none, so the human gate
is mandatory. For trivial, reversible edits, stop after the classification step.
<!-- /ai-core:method-deliberation -->

### In this project
No project-specific notes.

<!-- ai-core:method-reviewable-batches@v1 -->
## Deliver in reviewable batches

### Rule
Split a large task into coherent, reviewable batches: one batch = one TDD increment = one commit.
Announce the batches up front, stick to them, and pause for human review between batches.

When proposing commits for the human to validate:
- propose **at most two** commits at a time;
- **stage the first one** (`git add` of its files). Staging prepares; it does not cross the action gate.

### Why
A small diff is a reviewable diff, so the gate is real rather than a rubber stamp. Beyond two proposed
commits, the human can no longer review or arbitrate calmly. Staging the first commit spares the human
from sorting files into commits after the fact.

### When to apply
Any task large enough to need several commits. A single small change is one batch: no announcement needed.
<!-- /ai-core:method-reviewable-batches -->

### In this project
No project-specific notes.

<!-- ai-core:method-verify-before-claiming@v1 -->
## Verify before claiming

### Rule
No claim of success without fresh evidence from this session:
- **Name the proof**: the check that would show the claim false (the test suite, the build, a request to
  the running service, the file on disk).
- **Run it now**, after the last change, and **read the whole output**: exit code, failures, skipped tests,
  warnings.
- **Then state the claim with its evidence**: the check and its relevant result. A partial check is
  reported as partial.
- "Should work", "probably", "seems to" signal a missing check: run it, or say that it was not verified.
- A success reported by a subagent, a tool or an earlier message is not evidence: check the artefact itself
  (the diff, the test output, the deployed state).

### Why
An unverified claim is among the most expensive agent errors: the human relies on it, moves on, and meets
the failure later and further away. Running the proof costs seconds; a false "done" costs a review round
and trust.

### When to apply
Every claim that work is complete or correct, including at the end of each batch. A question or an
explanation makes no claim to verify.
<!-- /ai-core:method-verify-before-claiming -->

### In this project
Proofs here: `dotnet test` and `dotnet format --verify-no-changes`.

<!-- ai-core:git-action-gate@v3 -->
## Git and remote action gate

### Rule
The agent acts on git and remote services only within the **autonomy level** the project declares in its
"In this project" section (e.g. `Level: pr`).

At every level:
- never work directly on `main`, unless the project declares a single-developer regime that allows it;
- never force-push or rewrite history that has been pushed;
- **never perform the human's own acknowledgements**, even after an agreement in conversation: no merge,
  no approval, no tag or release (a tag can trigger a deployment), no acknowledgement label on a guarded
  change. Say it is ready and let the human act;
- never trigger any other remote action (deploy, issue or comment, CI re-run) without an explicit GO.

An explicit GO is present and specific. Describing a plan is not a GO; an earlier approval does not cover
the next action.

### Options
| Level | The agent may |
|---|---|
| `none` | edit and stage; never commit |
| `commit` | commit locally after an explicit GO; never push |
| `pr` | after an explicit GO: commit, push its own branch (or `main` under a single-developer regime), open the pull request |
| `autonomous-pr` | commit, push its own branch and open the pull request without asking; the gate is the pull request review |

Choose by how much of the review the human wants to keep, and by how reversible the project's remote
actions are. **Default when nothing is declared: `none`**, and ask the human to choose.

### Why
Commits, pushes and remote actions are outward-facing and hard to undo. The right amount of autonomy
depends on the project and on the person, not on the agent. Git and forge tools run with the human's
credentials: an acknowledgement performed by the agent looks exactly like the human's own and silently
defeats the gate it was meant to be. Declaring the level removes guessing in both directions.

### When to apply
Every project where an agent can run git or a forge CLI. The level is a per-project choice; the
invariants hold at every level.
<!-- /ai-core:git-action-gate -->

### In this project
Level: `autonomous-pr`. No remote or forge yet: until one exists, commits stay local.

<!-- ai-core:test-tdd-cycle@v3 -->
## Test-driven development cycle

### Rule
No production code without a test that has been **seen failing**: **RED → GREEN → REFACTOR**.

- **RED**: write the test first, run it, and confirm it fails for the expected reason. A test written after
  the code is only accepted once it has been seen failing by temporarily breaking that code.
- **GREEN**: write the minimum code that makes it pass, then run the whole suite.
- **REFACTOR**: only on green, in small safe steps.
- A bug is always reproduced by a failing test first, then fixed.
- One behaviour at a time; Arrange-Act-Assert; test names describe behaviour, not implementation
  (e.g. `Should_X_When_Y`).
- No step is skipped because a change looks trivial.

### Why
The test suite is the oracle that makes bold changes safe and gives an agent an objective "done". A test
never seen failing may pass for the wrong reason and guard nothing. Running the whole suite catches what the
new test does not cover. Behaviour-named tests survive refactors; implementation-named ones break with them.

### When to apply
All production code, in every stack. Changes with no production behaviour (documentation, prose,
formatting) have nothing to test. Throwaway zones a project declares under differential rigour follow that
regime: only what has a machine oracle is tested there.
<!-- /ai-core:test-tdd-cycle -->

### In this project
No project-specific notes.

<!-- ai-core:lang-conversation-and-code@v2 -->
## Languages of conversation, code and messages

### Rule
Each project **declares its languages** in its "In this project" section, and the agent follows them:
the conversation, the code (identifiers and comments), technical messages (logs, exceptions,
configuration errors), and text read by end users. In every project:
- name things with the domain's **ubiquitous language**: no home-made synonyms; a missing term is added to
  the project's glossary in the same commit as the code that introduces it;
- **user-facing text stays translatable**: gathered in few places, never scattered through the logic, even
  before any localisation system exists.

### Options
- **Conversation**: the human's language (default: the language they write in).
- **Code and comments**: English (default), or the team's language when the team decides so (for example,
  comments or test names in the team's language).
- **Technical messages**: English (default) — they address developers, even when they end up on screen.
- **User-facing text**: hard-coded in one language while no localisation is needed (YAGNI), or a
  localisation system from the start when translation is certain. Record the trigger for setting it up.

### Why
The conversation is for thinking, so it belongs in the human's language. A mixed codebase is harder to
search and to hand over; technical messages are read by developers, not users. User-facing strings
scattered through the logic make later localisation a rewrite, while gathering them costs nothing. One name
per domain concept keeps code, tests and conversation aligned.

### When to apply
Always. The defaults hold until the project declares otherwise.
<!-- /ai-core:lang-conversation-and-code -->

### In this project
- Conversation: French.
- Code and comments: English.
- Technical messages: English.
- User-facing text: none (kata); no localisation.
