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

- **D5, Nov 10.** Save v2. The save holds every bridge's curve tree in valley space, the ledge, the visitor
  fields D8 will fill, mode, anchor id, last pose and the player's settings; version 1 saves load as the M1 slice
  and are rewritten as version 2. `PalmfolkGame` opens a save with or without a pose, captures one, and applies
  settings. `Restart` takes the scaffold down and keeps the bridges. Verified headless: 163 developer-mode and 155
  release-mode tests pass, including a full valley with fork trees round-tripping bit for bit, a v1 slot file
  migrating through the repository, 400 random bit flips and six truncations all rejected, quitting during the
  Fork (one branch baked) and finishing after reloading from disk, a save made in Table mode opening in VR, and a
  save from other content dropping edges it doesn't know. Verified in the Unity editor (batch mode): compiles,
  170 EditMode tests pass. Not verified on a headset.

- **D6, Nov 10.** Live input and the clock. `PalmfolkRunner` reads XR Hands each rendered frame through an
  explicit joint map (validity, confidence, radii), runs Core at 60 Hz, places the valley root at the planted pose,
  and saves. Developer builds record the whole game (format 2) with consent and log joint radii for H3; a
  recording replays in the editor with identical decisions and final save. Replay found a real bug: a saved pose's
  facing was re-normalised on load, so a resumed VR session read hands one bit differently; fixed.
  Verified headless: 175 developer-mode and 162 release-mode tests pass, including a whole valley played from
  live samples at 72 Hz, the clock giving 60 ticks a second at 50, 72 and 90 Hz, and full live sessions
  (planted on a table, and resumed in VR mode) replaying with identical decisions. Verified in the Unity editor
  (batch mode): compiles in both configurations; 187 EditMode tests pass (190 after rebasing on Sam's latest), 203 with `PALMFOLK_DEVELOPMENT`,
  including the joint map against XR Hands' names, tracking space to world, the runner planting and placing the
  valley root where Core says, playing the whole valley, coming back after a restart in Table and VR mode, a
  5 s frame not fast-forwarding, and a session recorded through the runner replaying. Not verified on a
  headset: needs the owner (below).

- **D7, Nov 10.** Recovery across the whole valley. A headless sweep plays Chapter 1 and forces tracking loss
  every 3 ticks (50 ms) along every hand route: Board's carry, First Bridge, Width, the Fork, the Arch and both
  finale rebuilds. It uses five kinds of loss: 10 ticks gone, 30 ticks low-confidence, gone then back 20 cm away
  for 2.5 s, gone for over 9 s, and the other hand shown where the owner was. It also drops tracking while Pip
  walks and while travelers and Bram cross. In every one of about 2,300 runs, Pip, Bram and the travelers never move
  more than 5 mm in a tick, Pip never stands on an island he hadn't reached, nothing is carried, baked or
  completed while recovering, a long loss always returns Pip to the island he left, and the valley still
  finishes. A deliberately planted skip bug made the sweep fail, so the test is known to catch one. The sweep
  found one real bug: after boarding, Pip stood still until the build began, so a hand that shifted while
  settling made him jump to the build front (5 mm in one tick); he now rides the fingertip. T18: the
  lighting line and the loosened-stillness icon are now presentation flags, shown once, with connection distances
  unchanged. T19: developer recordings log dropouts and disruptive dropouts per puzzle. Verified headless after
  rebasing on Sam's latest: 189 developer-mode and 175 release-mode tests pass. Verified in the Unity editor
  (batch mode): 204 EditMode tests pass, 221 with `PALMFOLK_DEVELOPMENT`. The editor runs the sweep every 15 ticks, because it is about eight times
  slower. Not verified on a headset: owner-checks Slice 9 needs the owner and Sam's views.

## Next

- D8, visitors and the living valley.

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
- For Sam (S4): the settings, restart and bell commands are in `core-api.md` (Saves). Mode switching, quit and
  finding the anchor are platform work (D9); the save keeps `Mode` and `AnchorId` for it.
- For Sam (D6): bind your views to `PalmfolkRunner.Ticked` (events) and `Presented` (frame), and the waystone's
  requests to `Request` and `Apply` (see `core-api.md`). Your scene setup could call
  `Palmfolk.Platform.Editor.LiveInputSetup.EnableHands()` from Configure Android, so nobody misses that step.
  `PlayMode` is now `ValleyMode`, because `UnityEngine.PlayMode` made it ambiguous in any file that uses both
  namespaces.
- For Sam (D7): T5 and T18 now have presentation flags to draw: `Recovering` and `RecoveryPhase` (the still
  ghost from `GhostRoute`, Bram's gesture), `QualityPrompt` (the one line of text, P8's exception) and
  `StillnessLoosened` (the icon). See `core-api.md`.
- Defaults to check on the headset (H2): reference hand length 18 cm, scale clamp 0.75 to 1.3, palm half
  thickness 1.5 cm, and whether a 20 degree tilt limit is right on a lap.

## Blocked

- D6's headset check needs the owner (Quest 2):
  1. In Unity, open the M1 scene (Palmfolk > Slice 0 > 2. Create Scene if there is none), then run
     **Palmfolk > Platform > 1. Enable XR Hands** and **2. Add Runner To Open Scene**, and save the scene.
  2. To record, select **Palmfolk Runner** and tick **Tester Consented** on Session Recording (only with the
     tester's verbal OK).
  3. Build the Developer APK with `tools/Build-OwnerApk.ps1 -Configuration Developer` (Android scripting defines
     must include `PALMFOLK_DEVELOPMENT` for that build) and install it.
  4. On the headset: rest one flat palm, fingers together, for a second. A green disc should appear where the
     palm was, and an orange dot (Pip) on it. `adb logcat -s Unity` shows a line per Core event
     (`Palmfolk event: Planted ...`).
  5. Quit, then copy `/sdcard/Android/data/com.keepsrunning.palmfolk/files/recordings/` into `playtest-logs/`
     in the repo folder (Git ignores it) and tell me; I'll replay it in the editor and read the radius log (H3).

- D7's headset check (owner-checks Slice 9) waits for Sam's views of the ghost, Bram's gesture, the lighting line
  and the icon. Then, on the Quest 2: hide the hand mid-build, bring it back 20 cm away (Pip must stay put), then
  back in place (he blends back on); hide it for 8 s (he walks back, the scaffold comes down); keep the hand half
  out of view for 20 s (one line of text and the icon). Copy the session's `-dropouts.txt` into `playtest-logs/`.

## Questions for the owner

- The PC's clock starts about 7 hours fast after each reboot until Windows syncs, which can break Unity
  licensing. Turning on **Set time automatically** and clicking **Sync now** fixes it for that session.
