# Dario status

Work items: [`team-plan.md`](team-plan.md#darios-work). Labels: headless, Unity editor, simulator, Quest 2.

## Done

- **D1, Nov 10.** Merged `m1-gameplay` into `main` under the v1.3 requirements. `README.md` updated;
  `tools/check_source.py` now pins the v1.3 requirements and build plan alongside v1.2. Verified headless: 59
  developer-mode and 51 release-mode tests pass with .NET 8 on Windows. `tools/check_source.py` passes locally with Python 3.12 (`py -3.12`)
  after a fix for Windows line endings.

## Next

- D2, valley-local space and planting, once the owner says start.

## Blocked

- D6, D9, D10 need Unity 6000.0.66f2 on this PC (owner installs).

## Questions for the owner

- Unity install: 6000.0.66f2 with Android Build Support, OpenJDK, Android SDK & NDK Tools, and a licence activated,
  so batch-mode compiles can run here.
- The GitHub CLI (`gh`) would let both developers open and merge PRs with CI shown. Optional.
