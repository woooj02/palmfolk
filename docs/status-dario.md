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

- **D3, Nov 10.** Valley model and puzzle director. `ValleySpec` (islands, anchor zones, edges, ledge, six
  puzzles) is published for Sam's content; `ValleySimulation` runs Board to Finale with walker, group and cart
  travelers, widens the ledge before the finale (V2), checks Bram's three-crossing route and asks only for the
  failing bridges (V1, V3); `ValleyPresentation` is the view contract. The M1 slice is unchanged. Details:
  [`core-api.md`](core-api.md). Verified headless: 136 developer-mode and 128 release-mode tests pass,
  including Board to Finale with Sam's S1 fixtures on his candidate layout (either hand, and planted through
  `PalmfolkGame`), the First Bridge deck capped by the ledge whatever the hand, a rope refused for Width, a
  straight deck refused over the rock and an arch accepted, the old bridge kept until its replacement passes, and
  20 V3 cases (each side and route bridge missing, narrowed, steepened or moved in a save) all finishing with three
  crossings. Verified in the Unity editor (batch mode): compiles, 135 EditMode tests pass (after rebasing on Sam's latest). Not verified on a headset.

- **D4, Nov 10.** Fork and Arch geometry. A spread hand becomes a branching curve tree (fingers less than 1 cm
  apart merge into one surface); the Fork puzzle builds both lookouts with one hand, judges each branch with the
  trunk on its own, bakes the branches that pass and leaves a refused branch's lookout open. The capacity table and
  end ramps are settings for H1. Verified headless after rebasing on Sam's latest: 153 developer-mode and 145 release-mode tests pass, including
  Sam's flat and spread fixtures grouping into one and four surfaces, a fork from either hand, one steep branch
  refused while the other is baked and then rebuilt alone, fingers closing mid-build pausing the fork, and 3000
  random decks across the Arch gap where every deck that clears the rock (checked by dense sampling, near misses
  under 2 mm skipped) passes and every blocked one is refused. Verified in the Unity editor (batch mode): compiles,
  160 EditMode tests pass. Not verified on a headset.

## Next

- D5, Save v2.

## Notes for later items

- For Sam: the `Curved` fixture's metacarpal to proximal segment is about 36 degrees, just over the walker's
  35 degree limit, so a curved hand is refused as a walker bridge. The D3 tests use their own sloped and arched
  hands for the Fork and the Arch. If Curved is meant to pass, its first bend needs to be a little flatter.
- The rock's size in the D3 tests (1.6 cm square, 3.5 cm tall, centred in the C to E gap) is a test choice;
  H1 and Sam's content set the real one.
- For Sam and H1: from C, the two lookouts are about 68 degrees apart. That is a wide spread for one hand;
  the D4 tests use synthetic hands. Fork edges need one shared source anchor on C (see `core-api.md`); the
  candidate layout's rim anchors facing each lookout are 2 cm apart, which leaves a shared zone only about
  1 cm across for the wrist.

- The M1 slice still sits at the palm (valley origin). A valley game uses Sam's layout around the palm.
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
