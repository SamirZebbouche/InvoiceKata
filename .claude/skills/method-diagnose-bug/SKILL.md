---
name: method-diagnose-bug
description: "Use when a bug, a failing test or an unexpected behaviour has no obvious cause — build a fast reproduction loop first, test ranked hypotheses one at a time, fix the root cause under a regression test, and stop after three failed fixes."
---

<!-- ai-core:method-diagnose-bug@v1 -->
## Rule
No fix without a cause, and no hypothesis without a loop that shows the bug. Diagnose with evidence, one
change at a time, then fix the root cause under a failing test.

## Steps
1. **Build the loop first**: the fastest deterministic way to make the bug show (a test, a script, a
   command), red while the bug is there, seconds per run. Flaky or slow → make it deterministic and fast
   before going further (fixed seed, fake clock, narrowed input).
2. **Minimise the reproduction**: remove input, steps and components until removing anything more makes
   the bug disappear.
3. **Rank three to five falsifiable hypotheses**, each with the observation that would refute it. Start
   with the cheapest to test, not the most likely-sounding.
4. **Test one hypothesis at a time.** Instrument with tagged logs (`[DEBUG-<short id>]`) so that one search
   finds them all, and change one thing per run.
5. **Trace to the root cause**: follow the bad value back to where it first goes wrong, not to where it
   crashes. Check whether the same cause can strike elsewhere.
6. **Fix under a regression test**: the loop becomes a failing test at the right seam, then the minimal fix
   turns it green, following the project's TDD cycle. No seam fits the test → report it as a design
   finding.
7. **Three fixes have failed → stop.** The model of the problem is wrong: question the design with the
   human instead of trying a fourth fix.
8. **Clean up and report**: remove the tagged logs (one search), then state the root cause, the fix and the
   evidence (the test, red then green).

## Why
Fixes guessed without a loop may hide the symptom and prove nothing. A fast loop turns each hypothesis into
an experiment of a few seconds; ranking and changing one thing at a time keep the search honest. The
three-fix limit catches the moment when the problem is the design, not the line.

## When to apply
Any defect whose cause is not obvious at first reading. A typo whose fix is evident only needs its
regression test.
<!-- /ai-core:method-diagnose-bug -->

## In this project
No project-specific notes.
