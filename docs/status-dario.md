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
  imports and compiles with no errors (exit code 0). A clean checkout also compiles with no errors; its first
  import crashed once in Unity's asset importer on a Meta sample prefab, and the second run finished with exit
  code 0. How to use it: [`unity-setup.md`](unity-setup.md).

- **D2, Nov 10.** Valley-local space and planting. `ValleyPose` converts between world and valley-local space;
  `PlantingDetector` plants on a flat, still palm (build plan default 5) and sets place, height, facing and scale;
  `HandMeasure` sets the scale from bone lengths (T6) with the world-scale setting (A4); `PalmfolkGame` runs
  planting, then the M1 puzzle in valley-local space, and replants on `InputCommand.Replant`. The view contract is
  in [`core-api.md`](core-api.md). Verified headless after rebasing on Sam's S1 fixtures: 99 developer-mode and 91 release-mode tests pass, including
  planting on table, lap (tilted 15 degrees) and air (palm up) poses with tracking noise, refusals (tilted,
  moving, curled fingers, controllers in hand), small to large hands, a full M1 run in a turned and scaled valley
  matching the plain simulation, and replanting after the bridge is baked keeping its points bit-for-bit and Bram
  still crossing. Verified in the Unity editor (batch mode): compiles, 90 EditMode tests pass. Not verified on a
  headset.

## Next

- D3, valley model and puzzle director.

## Notes for later items

- The M1 level still sits at the palm (valley origin). Island layout around the planted palm (V5, E6) comes with
  D3 and Sam's S7 numbers.
- `RecordedRun` still records valley-local M1 frames. Recording world frames through `PalmfolkGame` needs a
  recording format version bump; planned with the replay work.
- Saving the pose (and the Table mode anchor, MR2/MR3) is not done; bridges are already valley-local, so a save
  only needs to add the pose.
- Defaults to check on the headset (H2): reference hand length 18 cm, scale clamp 0.75 to 1.3, palm half
  thickness 1.5 cm, and whether a 20 degree tilt limit is right on a lap.

## Blocked

- Nothing.

## Questions for the owner

- The PC's clock starts about 7 hours fast after each reboot until Windows syncs, which can break Unity
  licensing. Turning on **Set time automatically** and clicking **Sync now** fixes it for that session.
