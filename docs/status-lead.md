# Lead status

Work items: [`team-plan.md`](team-plan.md#lead-work). Labels: headless, Unity editor, simulator, Quest 2.

## Done

- **L1, Nov 10.** Merged `m1-gameplay` into `main` under the v1.3 requirements. `README.md` updated;
  `tools/check_source.py` now pins the v1.3 requirements and build plan alongside v1.2. Verified headless: 59
  developer-mode and 51 release-mode tests pass with .NET 8 on Windows. `check_source.py` not run locally (no Python
  on this PC); CI runs it.

## In progress

- **L2:** valley-local space and planting.

## Blocked

- L6, L9, L10 need Unity 6000.0.66f2 on this PC (owner installs).

## Questions for the owner

- Unity install: 6000.0.66f2 with Android Build Support, OpenJDK, Android SDK & NDK Tools, and a licence activated,
  so batch-mode compiles can run here.
- The GitHub CLI (`gh`) would let both developers open and merge PRs with CI shown. Optional.
