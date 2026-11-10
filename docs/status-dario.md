# Dario status

Work items: [`team-plan.md`](team-plan.md#darios-work). Labels: headless, Unity editor, simulator, Quest 2.

## Done

- **D1, Nov 10.** Merged `m1-gameplay` into `main` under the v1.3 requirements. `README.md` updated;
  `tools/check_source.py` now pins the v1.3 requirements and build plan alongside v1.2. Verified headless: 59
  developer-mode and 51 release-mode tests pass with .NET 8 on Windows. `tools/check_source.py` passes locally with Python 3.12 (`py -3.12`)
  after a fix for Windows line endings.
- **Unity setup, Nov 10.** Unity 6000.0.66f2 with Android support is installed and licensed on the owner's PC.
  Added the five built-in modules the Meta SDK needs (Animation, AssetBundle, ParticleSystem, Physics2D,
  Terrain) and committed `packages-lock.json`. Verified in the Unity editor (batch mode): `E:\palmfolk\Dario`
  imports and compiles with no errors (exit code 0), and a clean checkout of this commit compiles with no
  errors. How to use it: [`unity-setup.md`](unity-setup.md).

## Next

- D2, valley-local space and planting, once the owner says start.

## Blocked

- Nothing.

## Questions for the owner

- The PC's clock starts about 7 hours fast after each reboot until Windows syncs, which can break Unity
  licensing. Turning on **Set time automatically** and clicking **Sync now** fixes it for that session.
