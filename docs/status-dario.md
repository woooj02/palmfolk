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

- **D8, Nov 10.** Visitors and the living valley. After the finale the first visitor is called at once (E16);
  each launch then brings the next, and the bell calls one at once when nobody is here. On a call, every bridge
  on the visitor's way in and every edge they need is checked against their rules; failing bridges glow and
  are rebuilt (the old one stays until the new one passes, R4), missing ones are built. The visitor walks in
  from the sanctuary, their travelers cross, their window lights and their bridges get their mark (R3). Settled
  visitors and Bram then walk the valley's bridges, and are already walking on a relaunch (R1). Saves already
  held all of it. The tests use the five starting visitors on Sam's G, G1 and G2 layout: Gardener (walker, A to
  G), Merchant (cart, rebuild C to E over the rock), Twins (walkers, one fork hand from G to G1 and G2),
  Stargazer (walker, L1 to L2) and Lantern-maker (cart, A to G again). All five are helped with the left hand
  alone and with the right hand alone. Before each visitor, every saved bridge in turn is removed or swapped for
  another edge's bridge, and the visitor can still be helped (V3). Five deliberately planted bugs (bell while a
  visitor is here, no rebuild check, no visitor on relaunch, residents not walking on relaunch, marks
  overwritten) each made the tests fail. Verified headless: 209 developer-mode and 195 release-mode tests pass.
  Verified in the Unity editor (batch mode): 224 EditMode tests pass, 241 with `PALMFOLK_DEVELOPMENT`. Not
  verified on a headset: needs Sam's visitor views and content, then the owner.

- **D9, Nov 10.** Table mode platform. A launch now decides its mode and where the valley starts. Table mode
  with a saved anchor looks for it for up to 5 s and opens there without planting. If it isn't found, Pip waves and
  the player replants, and no bridge is lost. Each planting in Table mode saves a new local anchor and erases the old
  one. Table mode without passthrough runs as VR at the saved pose, and keeps the player's choice; losing
  passthrough mid-game switches to VR in place. The waystone can switch modes, and a save made in one mode opens in
  the other. The Meta side (local `OVRSpatialAnchor`s, an `OVRPassthroughLayer` underlay) is its own assembly
  behind two small interfaces, so Core and the runner stay SDK-free. A setup menu item turns on passthrough
  (Supported) and anchors. Verified headless: 216 developer-mode and 202 release-mode tests pass (the mode decisions).
  Verified in the Unity editor (batch mode) with scripted anchors and passthrough: 238 EditMode tests pass, 255
  with `PALMFOLK_DEVELOPMENT`. The Meta adapters compile against SDK 207 but have not run: that needs the headset
  (below).

- **D10, Nov 10.** Gaze. Looking at whoever waits for about 300 ms raises their need sign (rope, plank,
  branches, arch), and looking away puts it down. The need comes from the puzzle or the visitor, so every puzzle
  and all five visitors show the right one (tested over the whole valley). Look and pinch chooses waystone
  buttons with either hand resting anywhere; a pinch with the palm toward the headset is left to the system menu
  (E9). The game reports whether gaze is eye-tracked or head-synthesized (the developer probe logs it). Gaze never
  reaches the simulation: a valley played with gaze on and off ends on the same tick, and building hands made no
  waystone choice while gaze rested on a button for a whole valley. Verified headless: 223 developer-mode and 209
  release-mode tests pass. Verified in the Unity editor (batch mode) with a scripted head gaze: 247 EditMode tests
  pass, 264 with `PALMFOLK_DEVELOPMENT`. The eye-tracked path has not run: Meta XR Simulator isn't installed and
  Quest 2, 3 and 3S have no eye tracking. Not verified on a headset (below).

- **D11, Nov 10.** Performance and release safety. Profiled the valley tick, the bake and a whole rendered frame
  (live source, 60 Hz clock, game, need signs, look and pinch, presentation). Times are small, so the work went
  into garbage: hand samples, valley-local frames, routes and the presentation's dictionaries are now reused or
  copied only when they change. Measured headless (.NET 8 on this PC, chapter one plus all five visitors):
  - Ticks with no hand on Pip (walking, crossing, widening, residents, Bram) allocate 0 bytes, mean under 2 us.
  - A tick with a hand on Pip makes only the live hand route the views draw: about 0.6 to 1.3 KB (it was about
    7 KB). A tracked hand that hasn't reached Pip makes nothing on most ticks.
  - Worst tick 0.15 ms, worst bake 0.15 ms (the 72 Hz frame is 13.9 ms). In the rendered-frame run the worst
    tick was 4.75 ms, on the first pass (that run isn't warmed up).
  - Gaze and need signs on a still target allocate 0 bytes. The presentation object is 568 bytes a frame.
  Verified in the Unity editor (batch mode) with Unity's GC.Alloc recorder: 720 resting runner frames (hand
  tracked, need sign up, look and pinch running) allocate nothing. Release safety: a release build has no
  recording or developer assembly at all, and no shipped assembly references one (checked by Unity's own compile
  and by `tools/check_source.py` from the sources, which fails on a planted leak). The 28 manifest checks pass.
  Verified headless: 226 developer-mode and 212 release-mode tests pass. Unity editor: 253 EditMode tests pass,
  270 with `PALMFOLK_DEVELOPMENT`. The 72 fps headroom on the Quest 2 is not measured yet (below).

- **View contracts for Sam, Nov 12.** The four things Sam's status asked of Core and the recorder:
  - E17: a `ConnectionLocked` event and flag. It turns on when the hand holds every anchor (the Board: when the
    fingertip holds the next island), once per hold.
  - S6: each traveler's bridge and cursor (`Support`, `SupportCursor`), so feet stay on the deck. Bram, residents
    and the arriving visitor too, and Pip's cursor on a carrying hand.
  - T5 and E8: the whole hand behind the recovery ghost (`GhostHand`), and a working shape for the hint hand
    (`Hint`: which shape, and a bridge that passes the rules).
  - Recording format 3 keeps the waystone's settings and mode changes, so those sessions replay identically.
    Format 2 still loads.
  Verified headless over chapter one and all five visitors: every build starts on a locked connection, travelers
  stand on their decks, and every interaction's hint passes validation (only the Width ledge before widening has
  none). A run with settings and mode changed mid-session replays with identical decisions and final save.
  Headless: 234 developer-mode and 218 release-mode tests pass. Unity editor (batch mode): 268 EditMode tests
  pass, 289 with `PALMFOLK_DEVELOPMENT`. Not seen in Sam's views or on a headset yet.

- **First Quest 2 run, Nov 12-13.** The Developer APK (Table mode) builds, installs and runs on the owner's
  Quest 2. Fixes on the way: Meta's dev-agent screen capture service and operator OpenXR layer are kept out of the
  APK, the manifest stays hands-only, and the Platform SDK's `hzplatformclientcore` supplement requirement is
  dropped (the headset refused the install; the game makes no Platform SDK calls). The headset needed Horizon OS
  v74: on v65 the runtime only spoke OpenXR 1.0 and the display never started. Quest 2: 72 FPS, no stale frames,
  GPU about a third busy, hands-only mode and the hand tracking service connected, planting works (`Planted`).
- **Owner's verdict, Nov 13 (Quest 2): "very very hard", "didn't feel intuitive at all".** Nothing on screen said
  what to do; carrying Pip came first; the 2 cm connection and 4 cm break were too tight to find by feel (the logs
  show two connections lost within a second); Pip was a tan figure 3 cm tall; Slice 0's two grey boxes were still
  in M1 and read as the islands. The owner then changed the concept to hand-sculpting and handed Sam's paths to me
  while Sam is away: [`concept-sculpting.md`](concept-sculpting.md).
- **Opening and guidance, Nov 13.** Live play skips Board (`GameSettings.SkipBoard`), connects within 4 cm and
  breaks past 8 cm (`PalmfolkRunner.LiveSettings`); Core defaults are unchanged. The valley view adds glowing
  anchor rings, a one-line instruction (including why a bridge was refused) and a larger gold Pip; the greybox
  islands are removed from M1. Headless: 236 tests pass. Unity editor (batch mode): compiles.
- **Walls, Nov 13.** `GustField` sends a gust at crossing travelers; an upright hand across its path shields them,
  otherwise they brace for 75 ticks. Live play only (`GameSettings.Gusts`). Headless: 240 tests pass, including
  shielding geometry and a full chapter with gusts. Not seen on a headset yet.

- **Bake moment and wind sounds, Nov 13.** Sparks run along a bridge as it solidifies; wind, shield and gust cues
  (`Audio/AudioCues.cs`); the instruction line counts shielded gusts. Not seen or heard on a headset yet.

## Next

- Quest 2 pass on the new opening, walls, bake moment and sounds, then a tidier waystone
  ([`concept-sculpting.md`](concept-sculpting.md)).
- For Sam, when back: I merged `sam/hand-guidance` into my branch and edited `Views/ValleyView.cs` (rings, prompt,
  Pip, gusts) and the M1 scene (greybox removed). Delete untracked generated files before rebasing onto `main`.

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
- For Sam (D8): `ChapterOne` needs its visitors: pass a `VisitorSpec[]` as the last `ValleySpec` argument (see
  `core-api.md`, Visitors). The tests' version is `TestValley.Visitors()` in `ValleyDirectorTests.cs`. Views to
  draw: `VisitorFigure`, `Residents`, the windows (`Windows`, `LitWindows`), `BridgeMarks`, and the bell on the
  waystone sending `RingBell`.
- For Sam and H1 (D8 designs):
  - The Lantern-maker's cart needs a plank road from A to G. If the player built the Gardener's bridge wide
    already, her need is met and she just crosses. The tests cover both. If she must always ask for something,
    she needs another edge.
  - The Merchant's cart over the rock: the arch must clear the rock's headroom within the cart's 25 degree
    slope. The tests' hand rises 2.5 cm over the first two fifths of the gap and comes down the same way. On the
    headset that is a careful hand; H1 should check whether a rock that size is fair for a cart.
  - The Stargazer's "clearance above the Fork": bridges are not obstacles for other bridges, so a Fork branch
    under L1 to L2 is not checked. If the design needs it, it is a new rule (tell me).
- D9, not done here: MR2's snap to a scene-model table plane (only if one already exists; never triggering Space
  Setup), and MR4's room dimming (passthrough brightness, with Sam's views). The valley is placed once from the
  anchor; if the headset recentres mid-session the valley doesn't follow the anchor yet. Tell me if the relaunch
  test shows drift.
- For Sam (D10): bind need signs to `runner.NeedSignChanged` (null puts the sign down) and draw the sign by shape
  and motion (A7). For look and pinch, give the waystone's buttons through `runner.GazeButtons` (their centres in
  world space, and their width in degrees from the head) and act on `runner.GazeSelected`; draw `runner.Pointer.Hovered`.
  A settings toggle for gaze can set `runner.GazeEnabled`. The Interaction SDK's gaze building block adds an
  `EyeGaze` to the scene; without one, head direction is used.
- For Sam (D11): a hand sample from `IHandReader.Read`, `LiveFrameSource.Left`/`Right` or the frame it builds
  may be refilled next frame; keep one with `HandSample.Copy()` (or `HandFrame.Copy()`). `ValleyPresentation`'s
  `Bridges` and `BridgeMarks` are shared read-only copies: they change only when a bridge does, so comparing the
  reference tells you when to rebuild. For need signs each frame, `NeedSigns.Targets(game, pose, list)` fills your
  list without allocating.
- For Sam (view contracts): play the connection chime and dust on `ConnectionLocked` (event, or the
  presentation flag). Plant feet with `TravelerView.Support`/`SupportCursor` (and `BramWalk`, `PipCursor`) instead of
  projecting. Draw the carved ghost from `GhostHand` and the moving hint hand along `Hint.Tree` in `Hint.Shape`
  after `HintDue`. `SessionRecording` now writes `.pfr3` and picks up `PalmfolkRunner.SettingsApplied` and
  `ModeChanged` itself; `docs/testing/waystone.md`'s recording note can go.
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

- D9's headset check (slice 12, build plan default 6) needs the owner (Quest 2):
  1. In Unity, open the scene with the runner and run **Palmfolk > Platform > 3. Enable Table Mode**, then save the
     scene.
  2. Build and install the APK as for D6.
  3. Plant on a table and build a bridge or two. Quit from the headset menu, relaunch: the valley should come back
     on the same spot (within 2 cm) with its bridges, without planting.
  4. Quit, move the table about a metre, relaunch: within 5 s it should ask to replant (Pip waves); after
     replanting, every bridge is still there.
  5. Airplane mode (default 6): turn on airplane mode on the headset, relaunch: the valley should still come back.
     If it doesn't, tell me; Table mode then keeps planting but asks to replant each launch, and I9 isn't claimed.
  6. Switch to VR mode on the waystone (once Sam's waystone sends `ChooseMode`), relaunch: the night valley, same
     bridges. Switch back: the room shows again.
  The views for "looking for your valley" and Pip's wave are Sam's: `runner.WaitingForAnchor`,
  `runner.Flow.FindProgress` and `runner.Flow.Search == Lost`.

- D10's headset check (slice 13) needs Sam's need-sign views and waystone gaze buttons, then the owner (Quest 2):
  look at the waiting traveler for a moment (the sign comes up), look away (it goes down); choose each waystone
  button by looking and pinching with the hand resting on the table, left then right; look down at Pip on a palm-up
  hand and pinch (the system menu may open, but the game must not choose anything). `adb logcat -s Unity` shows
  `Palmfolk gaze source: Head`.

- D11's headset check needs the owner (Quest 2), on the Release APK (no `PALMFOLK_DEVELOPMENT`):
  1. Build with `tools/Build-OwnerApk.ps1` and check its manifest with
     `tools/Test-Slice0Apk.ps1 -Apk Builds/Palmfolk-Release.apk -TableMode`.
  2. Install it, run OVR Metrics Tool with the performance HUD on, and play the first bridges and a visitor.
     Note the frame rate at 72 Hz and the GPU and CPU levels, and say "Quest 2" with the numbers.
  3. `adb logcat -s Unity` for a few minutes of building: no `GC` lines that line up with a dropped frame.

## Questions for the owner

- The PC's clock starts about 7 hours fast after each reboot until Windows syncs, which can break Unity
  licensing. The hardware clock keeps UTC and Windows reads it as local time. The lasting fix (the owner's to make,
  in an administrator terminal, then reboot): `reg add "HKLM\SYSTEM\CurrentControlSet\Control\TimeZoneInformation"
  /v RealTimeIsUniversal /t REG_DWORD /d 1 /f`, and `sc config w32time start= delayed-auto` so Windows syncs on
  its own. Or set the BIOS clock to local time.
