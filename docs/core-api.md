# Core API for views

The contract between Core (Dario) and the Unity views (Sam). Core never references Unity. Views read Core state
and never change it. Each change to these types is listed under **Changes** before it is merged.

## Coordinate spaces

| Space | Axes | Units | Who uses it |
|---|---|---|---|
| World | Unity world: x right, y up, z forward | metres | Tracking input (`HandFrame` from the hand source), `ValleyPose.Origin` |
| Valley-local | x valley right, y up, z valley forward | valley units = world metres / `ValleyPose.Scale` | Everything the puzzle owns: Pip, Bram, routes, scaffold, bridges, level anchors |

The valley only turns about world up, so "up" is the same in both spaces. Rotations from Core use Unity's
convention (`Rotation` is x, y, z, w; `a * b` applies `b` first, like `Quaternion`).

To draw anything from `WorldPresentation`, convert it with the pose:

```csharp
// Either put the valley's content under one Transform per pose ...
valleyRoot.SetPositionAndRotation(ToUnity(pose.Origin), ToUnity(pose.Rotation));
valleyRoot.localScale = Vector3.one * (float)pose.Scale;
// ... then place children at valley-local positions: pip.localPosition = ToUnity(frame.Pip).
// Or convert points one by one: ToUnity(pose.ToWorld(frame.Pip)), widths with pose.ToWorldLength(w).
```

Traveler size (E11) and every gap scale with the valley, so the single parent Transform is the simplest view.

## Types

### `PalmfolkGame` (`Assets/Palmfolk/Core/PalmfolkGame.cs`)

The whole game as one `ITickSimulation`. Feed it world-space `HandFrame`s at 60 Hz, as with `M1Simulation`.

| Member | Meaning |
|---|---|
| `Stage` | `ValleyStage.Planting` or `Playing`. Starts in `Planting`. |
| `Pose` | `ValleyPose`, `null` until the first planting. While replanting it keeps the old pose, so the old valley can stay drawn until the new one grows. |
| `Simulation` | The puzzle being played, in valley-local space: an `M1Simulation` or a `ValleySimulation` (D3). |
| `Valley` | The `ValleySimulation`, or `null` for the M1 slice. Built with `new PalmfolkGame(valleySpec, settings, resume)`. |
| `Puzzle` | The `M1Simulation`, or `null` for a valley game. During planting it is held as if focus were lost: a hand that carried Pip goes into recovery, and the player realigns with the ghost afterwards. |
| `Planting` | `PlantingDetector`: `Progress` (0 to 1 over the resting second), `Hand`, `WaitingForHands`, `ShowStartupText`. |
| `LastPlanting` | `PlantingResult`: `Pose`, `Hand`, `HandLength` (metres, wrist to middle fingertip along the bones). |
| `Plantings` | How many times the valley has been planted. |
| `IsPaused` | The player's pause. |

Commands: `InputCommand.Replant` (new) starts replanting from `Playing`; it is ignored while already planting.
`Pause` and `Resume` work in both stages. `Restart` takes the current scaffold down (like `CancelBuild`) and keeps
every baked bridge. `RingBell` is reserved for the visitors (D8) and does nothing yet.

Saves (D5): `PalmfolkGame.Open(valley, save, pose, settings)` opens a `SaveData`. With a pose (a found anchor, or
VR mode's saved pose) play starts in `Playing`; without one the player plants first and the bridges reappear on
the new valley. `Capture(playerSettings, previous)` returns what to save now; mode and anchor id are the
platform's and are carried over from `previous`. `Apply(playerSettings)` applies stillness tolerance at once and
world scale by rescaling `Pose` about the valley origin.

### Saves (`Assets/Palmfolk/Core/SaveData.cs`, `Persistence.cs`)

| Type | Meaning |
|---|---|
| `SaveData` | `Valley` (`ValleyProgress`, `null` before first play), `Mode` (`ValleyMode.Table` or `VR`), `AnchorId` (Table mode's spatial anchor, at most 128 characters), `Pose` (last planted pose, world space), `Settings`, `M1` (the M1 slice's state), `LoadedVersion` (1 or 2). |
| `PlayerSettings` | `StillnessToleranceStep` (0 to 2, default 1), `WorldScale` (0.5 to 2), `ReducedMotion`, `Captions` (default on), `HighContrast`, `XRay`. Core applies the first two; views read the rest. |
| `SaveCodec.EncodeSave` / `DecodeSave` | Version 2 bytes. Decoding a version 1 save (the M1 slice) gives a `SaveData` with only `M1` set and `LoadedVersion` 1. Anything corrupt, truncated or out of range throws `InvalidDataException`. |
| `SaveRepository.Save(SaveData)` / `Load(out SaveData)` | Two slots, newest valid one wins; a corrupt newest slot falls back to the other. `Load(out M1Snapshot)` and `Save(M1Snapshot)` still work and keep the rest of the save. |

`ValleyProgress` also holds `CurrentVisitor` (-1: none), `VisitorsHelped` and `LitWindows` (bit i is visitor i)
and `BridgeMarks` (edge id to the visitors who crossed it), for D8. Resuming with a save from other content drops
bridges on edges the valley doesn't have; if Pip's island is gone he starts at the current puzzle's island.

### Live input and the runner (D6, `Assets/Palmfolk/Platform`)

`PalmfolkRunner` (MonoBehaviour) is the live game. Each rendered frame it reads both hands, runs Core at 60 Hz
with `FixedTickRunner`, moves `ValleyRoot` to the planted pose, and saves after planting, every bake, every solved
puzzle, the ledge widening, a settings change, pause and quit. It decides nothing itself.

| Member | Meaning |
|---|---|
| `Game` | The `PalmfolkGame`. Opened on `Start` from the newest save in the app's `saves/` folder with `ChapterOne.Greybox()` (`Content` changes it). VR mode resumes at the saved pose; Table mode plants first. |
| `Ticked` | Event: the events of this frame's ticks (only when there were some). Bind `Consume` here. |
| `Presented` | Event: once per rendered frame after ticking. Read `Game` and `InterpolationAlpha` and call `Present`. |
| `Request(command)` | Queues an `InputCommand` for the next tick (at most 8 waiting). The waystone's Pause, Resume, Restart, Replant and Bell go here. |
| `Apply(playerSettings)` | Applies and saves the player's settings; world scale rescales the valley root. |
| `ValleyRoot` | Placed at the pose: position `Origin`, rotation `Euler(0, yaw, 0)`, uniform scale `Scale`. Its local space is valley space, so a view parented under it can draw valley-local points directly. Untouched until the first planting. Its parent must be unscaled. |
| `Capture()`, `SaveNow()` | The save as it stands; write it now. |
| `Source` | The `LiveFrameSource` (Core): the latest hand sample, held for any tick until the next sample. |
| `Hands` | `IHandReader`: `XRHandsReader` on the headset; tests supply their own. |

`HandJointMap` maps XR Hands joints to Core's explicitly (Core's order is Palm, Wrist, ...; XR Hands puts Wrist
first). A joint is valid when XR Hands reports its pose; it is confident when the runtime marks it high-fidelity,
or, until a hand has reported that flag once, whenever its pose is valid (`ConfidencePolicy`). Radii are passed
on when reported, scaled with the tracking space. Non-finite poses become invalid joints.

A rendered frame longer than 0.1 s counts as 0.1 s: after a hitch or a suspend the game does not fast-forward.

Developer builds also have `SessionRecording` (records from the moment a game opens, only with the tester's consent
ticked; writes `recordings/session-*.pfr3` and the H3 radius log on pause or quit) and `CoreProbe` (markers for the
valley origin, planting progress and Pip; every event to the device log). `GameRecording` (Developer) is the
recording format 3 for the whole game, with the settings and mode changes made along the way (`Changes`; format 2
still loads); `GameReplay.Run(recording, valley)` replays it and reports `SameDecisions` and `SameFinalState`.
`GameRecorder.NoteSettings(player)` and `NoteMode(mode)` record a change before the next frame; `SessionRecording`
calls them from `PalmfolkRunner.SettingsApplied` and `ModeChanged`.

Setup: **Palmfolk > Platform > 1. Enable XR Hands** (turns on the XR Hands OpenXR feature; Android builds fail
without it) and **2. Add Runner To Open Scene** (wires the camera rig's TrackingSpace and a new Valley Origin).

### Table mode and VR mode (D9: MR1 to MR3, MR5)

`ModeFlow` (Core) decides which mode a launch runs in and where the valley starts; `PalmfolkRunner.Flow` is the
current one.

- `Chosen` is the player's mode (saved as `SaveData.Mode`); `Running` is what runs. Table mode without passthrough
  (not supported, not permitted, failed) runs as VR (`FellBack`), and the choice is kept for the next launch.
  Passthrough failing mid-game switches to VR with the valley where it is.
- VR (or the fallback) opens at the saved pose, or plants if there is none.
- Table mode with a saved anchor (`SaveData.AnchorId`): `Search` is `Finding` and `runner.Game` is null while the
  platform looks for it (`WaitingForAnchor`, `FindProgress`), for up to `FindSeconds` (5 s). `Found`: the game opens
  on the anchor at the saved scale, without planting. `Lost`: the game opens in planting and Pip waves to replant.
  Bridges are valley-local, so none are lost either way. An anchor found later is ignored.
- Every planting in Table mode saves a new anchor where the valley now stands and erases the old one; the save
  takes the new id once the anchor is stored. If storing fails, the next launch asks to replant.
- `runner.ChooseMode(mode)` is the waystone's switch: passthrough follows, switching into Table mode anchors the
  valley where it stands, and the save is written. `runner.ModeChanged` fires when the running mode changes.

The runner takes `ISpatialAnchors` (`Save`, `Find`, `Erase`) and `IPassthrough` (`Available`, `Visible`,
`SetVisible`) before `Start`. `Palmfolk.Platform.Meta.MetaTableMode` (on the runner; **Palmfolk > Platform > 3.
Enable Table Mode**) supplies them from the Meta XR Core SDK: local `OVRSpatialAnchor`s, and an
`OVRPassthroughLayer` underlay on the camera rig with the eye camera clearing to transparent while passthrough
shows. The menu also sets passthrough to Supported (not Required, so VR mode still runs) and turns anchor
support on. Without passthrough the runner runs Table saves as VR; without anchors, Table mode plants every launch.

### Gaze (D10: GZ1 to GZ3, E9, `Assets/Palmfolk/Core/Gaze.cs`)

Gaze is never an input to the valley: it raises need signs and chooses waystone buttons, nothing else. With gaze
off every puzzle plays tick for tick the same, and recordings don't carry it.

- **GZ3.** `GazeSample(origin, direction, source)`, world space; `GazeSource` is `Head` (synthesized from the head
  pose: Quest 2, 3 and 3S) or `Eyes`. The runner takes an `IGazeReader` (`Read()`, null for no gaze);
  `runner.GazeSource` is what the stats panel shows. `MetaGaze` (set by `MetaTableMode`) reads the Interaction
  SDK's `EyeGaze` when the scene has one (its `IsSyntheticPose` decides Head or Eyes), otherwise the eye camera's
  forward as Head. The developer `CoreProbe` logs the source when it changes.
- **GZ1.** `NeedSigns.Targets(valley, pose)`: who waits for the current bridges (the puzzle's travelers, Bram at
  the camp in the finale, or the visitor once they are walking in) and their `NeedSign`: `Rope` (First Bridge,
  walking visitors), `Plank` (Width, the finale, carts), `Branches` (the Fork, visitors needing two bridges from
  one island), `Arch` (the Arch puzzle's edge, and carts that need it). None on the Board, once nothing is left to
  build, and while travelers cross. `LookWatcher`: 300 ms of looking raises the sign, 200 ms away puts it down,
  and a target that goes away puts it down at once. Head gaze hits within 3 degrees even for small targets.
  `runner.NeedSignChanged` gets the target (null: put down); `runner.Looks.Shown` is the current one.
- **GZ2 and E9.** `GazeButton(id, centre, widthDegrees)`: the waystone sets `runner.GazeButtons` (at most four in
  a row, each at least 6 degrees with 1 degree between). `LookAndPinch`: the button under the gaze is
  `Hovered`; a pinch of either hand (thumb and index tips under 1.5 cm, open again past 3 cm) chooses it once
  (`runner.GazeSelected`). A pinch with the palm facing the head (within 60 degrees) is the system gesture's and
  is never taken. `HandRoutes.PalmNormal` is the direction out of the palm.

### `ValleyPose` (`Assets/Palmfolk/Core/Valley.cs`)

`Origin` (world), `Forward` (unit, level), `Right`, `Scale`, `YawRadians`, `Rotation`. Conversions both ways for
points, lengths, rotations, joints, hands and whole frames: `ToValley(...)`, `ToWorld(...)`,
`ToValleyLength`, `ToWorldLength`. `ValleyPose.Identity` maps valley-local to world unchanged.

How planting sets it (E21, T6, build plan default 5):

- **Place and height:** the valley's origin is under the resting palm: palm joint minus
  `GameSettings.PalmHalfThickness` (1.5 cm) straight down. This works on a table, a lap and in the air.
- **Facing:** wrist to middle knuckle, flattened onto the floor plane. The valley's forward is the way the fingers
  pointed.
- **Scale:** the hand's bone length from wrist to middle fingertip, divided by `ReferenceHandLength` (18 cm),
  clamped to 0.75 to 1.3, then times the player's world-scale setting `WorldScaleAdjustment` (0.5 to 2, A4).

### `WorldPresentation`

New constructor `WorldPresentation(PalmfolkGame game, double alpha)`. The M1 constructor and every existing field
stay as they were. New fields:

| Field | Meaning |
|---|---|
| `Stage` | `Planting` or `Playing`. `Playing` when built from an `M1Simulation`. |
| `Pose` | The pose to draw with. `Identity` when built from an `M1Simulation`; `null` before the first planting. |
| `PlantProgress` | 0 to 1 while the palm rests (for the valley growing from under it). 0 when not planting. |
| `PlantingHand` | The hand being read for planting, or `null`. |

While `Stage == Planting`, `WaitingForHands`, `PutControllersDown` and `ShowStartupText` come from planting
(E19), and `Paused` is the player's pause only.

### Valley content (D3, `Assets/Palmfolk/Core/ValleyModel.cs`)

Sam's content (`Core/Content`) builds a `ValleySpec`; the director runs it. All positions are valley-local.

| Type | Meaning |
|---|---|
| `IslandSpec(id, top, radius)` | An island: centre of its deck top and its radius. |
| `AnchorZone(centre, radius)` | Where a bridge may end. Any route end inside the zone connects; the T2 hold and break distances are measured from the zone's edge. |
| `EdgeSpec(id, from, to, fromAnchor, toAnchor)` | A gap between two islands. `EdgeSpec.Between(id, fromIsland, toIsland, fromZone, toZone)` puts the anchors on the rims facing each other. Pip builds from `From` when he can reach it, otherwise from `To`. |
| `LedgeSpec(edge, island, narrow, wide, wideningTicks)` | V2. Every bridge on `edge` is capped to the ledge width (whole deck) until Pip widens it before the finale. |
| `PuzzleSpec(kind, edges, traveler, travelerFrom, rule)` | One puzzle. `rule` (what a bridge must pass) defaults to the traveler's own rules. |
| `ValleySpec(islands, edges, puzzles, obstacles, ledge, camp, sanctuary, visitors)` | The valley. `obstacles` are boxes (the rock) checked for headroom. `FinaleRoute()` is Bram's route as (edge, from, to). `visitors` (optional, D8) are the visitors in the order they come. |
| `VisitorSpec(id, traveler, from, edges...)` | D8. A visitor (R2): who travels (`Walker`, `Group` or `Cart`; their rules are the need), the island they set off from, and one to four distinct edges touching it that they cross (one traveler per edge, away from `from`). |

`ValleySpec` refuses: anything but six puzzles in the order Board, First Bridge, Width, Fork, Arch, Finale; a
Board with more than one edge; puzzle edges that don't touch `travelerFrom` (except the finale); a finale that is
not the cart from the camp along a path of edges to the sanctuary; a ledge edge not built in both First Bridge
and the finale; more than five visitors (the save holds five), duplicate visitor ids, or a visitor edge that does
not touch the visitor's island.

`BridgeValidator.Validate(tree, startZone, endZones, kind, settings, clearance)` is the zone form of validation:
every leaf must end in a zone and every zone must be reached. The point form is unchanged (it wraps points in
zones of radius `ConnectDistance`).

### `ValleySimulation` (`Assets/Palmfolk/Core/ValleySimulation.cs`)

The six puzzles of one valley (V1 to V3, E4). `new ValleySimulation(spec, settings, resume)`; `resume` is a
`ValleyProgress` (saved in `SaveData.Valley`: `CompletedPuzzles`, `PipIsland`, `LedgeWidened`, `Bridges` by edge id,
and the visitor fields).

Each puzzle runs through `Stage` (`DirectorStage`):

1. `Walking`: Pip walks over built bridges to the island he builds from (a straight walk if no bridge path exists).
2. `Interacting`: an `EdgeSession` (`Session`) runs M1's hand loop on one edge: Board carries Pip on the hand;
   the others build. A bridge that fails the puzzle's rule is refused and removed; the old bridge on that edge,
   if any, stays until a replacement passes (R4). Edges are built one after another; `PendingEdges` lists what
   is left.
3. `AwaitingWithdrawal`: until no hand is near a built bridge.
4. `TravelersCrossing` (`Travelers`) or, in the finale, `BramCrossing` (`Bram`).
5. The next puzzle, or `Complete`.

The finale first has Pip walk to the ledge and widen it (`Widening`, `WideningProgress`), then checks Bram's route
against the cart's rules (V3). Edges that fail, are missing or were damaged in a save are in `FailingEdges` and
are rebuilt; edges off the route are never asked for. Bram then crosses each bridge on the route.

`Check(route, kind)` and `Passes(edge, kind)` validate stored bridges. `M1Simulation` stays for the M1 slice.

### Forks and arches (D4)

- **Fork shape (`HandRoutes.BestFork`, `Fork`, `Clusters`).** Index to little fingers are grouped into surfaces:
  neighbours with a gap under 1 cm merge. A fork is a curve tree: a trunk from the wrist to a junction at the
  metacarpal joints (width: the branches' widths added), then one branch per destination from the junction through
  the knuckle to the fingertips of one surface. Fingers not aimed at a destination aren't built. `ForkLayout` fixes
  which fingers make each branch when the hand is first read; fingers merging or splitting later is a shape change
  (the build pauses, T3).
- **Fork puzzle.** When the Fork's open edges from Pip's island have source anchors that overlap, one `EdgeSession`
  builds them all (`IsFork`, `Edges`, `Destinations`, `ActiveTree`). Content should give the Fork's edges one shared
  anchor; if the anchors don't overlap by at least 5 mm, the edges are built one at a time.
- **Building a tree (`TreeBuild`, now `EdgeSession.Build`).** One curve builds exactly as before. A fork builds its
  trunk first, then all branches at once from the frozen junction, in `BuildTicks` overall. `FrozenTree()` is the
  scaffold; `FrozenRoute()` is the trunk plus the first branch.
- **Per-branch judgement (T4).** Each branch is validated with the trunk against its own anchors. Branches that
  pass are baked; each edge stores the trunk plus its branch, and the edges of one fork share the same trunk `Route`
  object (draw it once). A refused branch's edge stays open and is built next on its own. The refused scaffold is
  `ValleySimulation.RefusedScaffold` with `RefusedFailures` (curve 0 trunk, k+1 branch k) until the next
  interaction starts: glow the failing branch, then take it down.
- **Validation.** Width, slope and clearance are checked per segment of every curve; the maximum length counts the
  walk from the root to each leaf.
- **Arch and H1.** The capacity table is in `GameSettings` (`WalkerRule`, `GroupRule`, `CartRule`: minimum width,
  maximum slope, minimum clearance) with `EndRampLength` (0 = no end ramps; otherwise deck within that distance of
  an anchored end isn't slope-checked). The rock, span and anchor heights are valley content. Clearance is judged
  on the deck geometry only, so any hand shape that clears the rock passes (I7).

### `ValleyPresentation`

What the views draw for a valley game: `new ValleyPresentation(palmfolkGame, alpha)` (or from a
`ValleySimulation`). Fields: `Puzzle`, `PuzzleIndex`, `Stage`, `Complete`, `Pip`, `Bram`, `Travelers` (`Kind`,
`Destination`, `Position`, `Arrived`), `Bridges` by edge id, `FailingEdges` (glow, V3), `ActiveEdge`, `Source`,
`Destination` (anchor zones), `Rule`, `LedgeWidth`, `WideningProgress`, `ActiveEdges`, `HandTree`, `ScaffoldTree`,
`RefusedScaffold` (D4), then the M1 hand and build fields
(`HandOwner`, `FilteredHandRoute`, `GhostRoute`, `Scaffold`, `BuildPhase`, `BuildProgress`,
`ScaffoldRemovalProgress`, `RouteTier`, `Refusals`), the E19 fields, and the planting fields (`PlantingStage`,
`Pose`, `PlantProgress`, `PlantingHand`). `WorldPresentation(PalmfolkGame)` throws for a valley game.

### Tracking loss and quality (D7: T5, T18, T19)

- **T5.** When the hand carrying Pip or building is lost (missing, or its route joints invalid or low-confidence),
  `Recovering` is set and `GhostRoute` is the still, carved ghost to draw; Bram gestures toward it. `RecoveryPhase`
  is `Frozen` (Pip holds still), `Aligning` (the owning hand is back within 2 cm; 300 ms to go), `Blending` (Pip
  moves onto the live hand over 200 ms) or `Returning` (after 8 s: Pip walks back to where the interaction
  started and the scaffold comes down). Events: `TrackingLost`, `RecoveryAligned`, `RecoveryCompleted`,
  `ReturningToCheckpoint`. Only the owning hand can realign; Pip never moves to a hand that appears elsewhere.
- **T18.** `QualityPrompt`: show the one line "Hand tracking works best in a well-lit room, with your hand in
  view." It comes up once per session, after 20 s of poor tracking while the valley waits on the player, and goes
  after 3 s of good tracking (`QualityPromptDue` is raised when it comes up). `StillnessLoosened`: show the
  loosened-stillness icon; from then on the stillness tolerance is one step looser for the session. Connection
  distances never loosen. Poor tracking means the owning hand's route joints while a hand owns the interaction,
  otherwise both hands. `EdgeSession.PoorTracking` is the per-tick value.
- **T19 (Developer builds).** `GameRecorder.Dropouts` counts, per puzzle, dropouts (the owning hand lost for
  250 ms or more while carrying or building) and disruptive ones (Pip froze or the build paused). Session
  recording writes them to `session-*-dropouts.txt` next to the recording.

### Visitors and the living valley (D8: R1 to R4, E16)

After the finale (`IsComplete`: six puzzles done) the valley keeps running:

- **E16.** As the sanctuary lights, the first visitor is called. Each later launch calls the next visitor not yet
  helped; within a session the bell (`InputCommand.RingBell`) calls the next one at once. The bell is ignored
  while a visitor is here. A visitor still being helped when the game closed carries on after a relaunch.
- **R2 and V3.** On a call (`VisitorCalled`), every bridge on the visitor's way in from the sanctuary (fewest
  edges) and every edge they need is checked against the visitor's rules. Failing bridges are in
  `FailingEdges` (glow, `RebuildNeeded` each) and are rebuilt; missing ones are built; the way in comes first.
  Several needed edges from one island with a shared source anchor are built with one fork hand. `Visitor` and
  `VisitorIndex` say who is being helped; `Stage` runs as for a puzzle.
- **R4.** A rebuilt bridge replaces the old one only once it passes; a refused attempt leaves the old one standing.
- **The walk in.** Once the way in is passable, the visitor walks from the sanctuary to their island
  (`VisitorArrival`; `VisitorArrived` when they get there). Their travelers set off only after that and after the
  player's hand has withdrawn. A need already met just crosses and settles: nothing is built.
- **R3.** When the travelers arrive (`VisitorSettled`), the visitor's window lights and stays lit (`LitWindows`,
  bit i for visitor i), and each bridge they crossed carries their mark (`BridgeMarks`, by edge, bit i).
  `VisitorsHelped` has the same bits. Then `Stage` is `Complete` until the bell or the next launch.
- **R1.** `Residents`: each settled visitor's travelers go back and forth across the bridges they asked for, and
  Bram (`Bram`) goes back and forth between the sanctuary and the camp on the finale route. They rest 3 s at each
  end, and never step onto a bridge that is missing or waiting to be rebuilt. On a relaunch they are already on
  their way.

`ValleyPresentation` adds `Visitor`, `VisitorIndex`, `VisitorFigure` (the visitor walking in and waiting; their
travelers are in `Travelers`), `Residents`, `Windows` (how many windows to draw), `LitWindows` and `BridgeMarks`.
The runner saves on `VisitorCalled` and `VisitorSettled`. All of it is in `ValleyProgress` already (Save v2).

### Events

`GameEventKind` gains two values, appended at the end so existing numbers don't change:

| Event | When |
|---|---|
| `Planted` | The tick the valley is planted or replanted. Read `Pose` and `LastPlanting`. |
| `ReplantStarted` | The tick a `Replant` command takes effect. |

D3 appends `PuzzleStarted`, `PuzzleCompleted`, `LedgeWidening`, `LedgeWidened`, `RebuildNeeded` (once per
failing edge), `TravelersStarted`, `TravelerArrived`, `CrossingCompleted` (each of Bram's bridges). The valley
also raises the M1 hand and build events, `PersistentBridgeObserved`, `BramStarted` and `BramFinished`.
D8 appends `VisitorCalled`, `VisitorArrived` and `VisitorSettled`.

`StartupPromptDue` is also raised during planting, once, when the startup text becomes due.

## What a resting palm is

Build plan default 5, all in `GameSettings`:

- **Flat:** the palm plane (wrist, index knuckle, little knuckle) within `PlantMaximumTiltDegrees` (20) of level,
  palm up or down; index to little fingers straight (`FingerStraightness`, 0.9, straight-line over along-the-bones
  length from knuckle to tip).
- **Still:** average filtered palm speed under `StillnessThreshold()` (3 cm/s at the default tolerance step, 4.5 at
  the loosest) over `PlantTicks` (60, one second). A jump faster than three times that restarts the second.
- **Not:** controllers in hand, no focus, or no tracked hand. These reset the second and show the E19 waiting state.

The hand already resting keeps its turn; otherwise the left hand is read first.

## Changes

- **D2, Nov 10.** Added `PalmfolkGame`, `ValleyStage`, `ValleyPose`, `HandMeasure`, `PlantingDetector`,
  `PlantingResult`; `InputCommand.Replant`; `GameEventKind.Planted` and `ReplantStarted`; planting and scale
  settings in `GameSettings`; `Rotation.AboutUp`, `Inverse` and `*`; the `WorldPresentation` fields above.
  Nothing removed or renamed.
- **D3, Nov 10.** Added `ValleyModel.cs` (`AnchorZone`, `IslandSpec`, `EdgeSpec`, `LedgeSpec`, `PuzzleKind`,
  `PuzzleSpec`, `ValleySpec`), `EdgeSession`, `ValleySimulation`, `DirectorStage`, `ValleyProgress`, `Traveler`,
  `ValleyPresentation`, `TravelerView`; `ObstacleField` (split from `LevelGeometry`, which uses it); the zone
  overload of `BridgeValidator.Validate` and `BridgeValidator.MinimumWidth`; `PalmfolkGame.Simulation` and
  `Valley`; the events above. `PalmfolkGame.Puzzle` is `null` for a valley game. Nothing removed or renamed.
- **D4, Nov 10.** Added `ForkLayout`, `HandRoutes.Clusters`, `BestFork`, `Fork`, `Spine`; `TreeBuild`;
  `CapacityRule`, `GameSettings.WalkerRule`, `GroupRule`, `CartRule`, `Rule(kind)`, `EndRampLength`;
  `EdgeSession.Edges`, `Destinations`, `IsFork`, `ActiveTree`, `Results`, `RefusedTree`, `RefusedEdges`, the fork
  constructor and `Cap(CurveTree, cap)`; `ValleySimulation.RefusedScaffold` and `RefusedFailures`; the
  `ValleyPresentation` fields above. Changed: `EdgeSession.Build` is a `TreeBuild` (same `Phase`, `Progress`,
  `RemovalProgress`, `FrozenRoute()`); `MaximumRouteLength` counts root to leaf. `ValleySpec` now requires two to four
  Fork edges.
- **D5, Nov 10.** Added `SaveData`, `PlayerSettings`, `ValleyMode`, `SaveCodec.EncodeSave`, `DecodeSave` and
  `CurrentVersion`; `SaveRepository.Save(SaveData)` and `Load(out SaveData)`; `InputCommand.Restart` and `RingBell`;
  `PalmfolkGame.Open`, `Capture`, `Apply` and the pose constructor; `ValleyProgress.CurrentVisitor`,
  `VisitorsHelped`, `LitWindows`, `BridgeMarks`; `ValleySimulation.SetStillnessTolerance`,
  `M1Simulation.SetStillnessTolerance`, `PlantingDetector.SetWorldScale` and `WorldScale`. Changed: the save file is
  version 2 (version 1 still loads); `Load(out var x)` is now ambiguous, so name the type. Resuming drops unknown
  edges instead of throwing.
- **D6, Nov 10.** Added `LiveFrameSource` (Core); the Platform assembly (`PalmfolkRunner`, `IHandReader`,
  `XRHandsReader`, `HandJointMap`, `RawJoint`, `ConfidencePolicy`); `GameRecording`, `GameRecorder`,
  `GameRecordingCodec`, `GameReplay`, `DecisionDigest` (Developer). Renamed: `PlayMode` is now `ValleyMode`
  (it clashed with `UnityEngine.PlayMode`). Changed: a `ValleyPose` built from a forward that is already level and
  unit length keeps it exactly, so a pose read back from a save maps hands exactly as before.
- **D7, Nov 10.** Added `ValleyPresentation.QualityPrompt`, `StillnessLoosened`, `Recovering`, `RecoveryPhase`;
  `ValleySimulation.QualityPromptShowing` and `StillnessLoosened`; `HintWatcher.QualityPromptShowing` and
  `PromptClearTicks`; `EdgeSession.PoorTracking`; `ValleyDropouts` and `GameRecorder.Dropouts` (Developer).
  Changed: in the valley, T18's poor tracking is the owning hand's while a hand owns the interaction (before, it
  was both hands always); after boarding, Pip rides the fingertip until the build starts, instead of standing
  where he boarded.
- **D8, Nov 10.** Added `VisitorSpec`; `ValleySpec.Visitors` and its optional last constructor parameter;
  `ValleySimulation.Visitor`, `VisitorIndex`, `VisitorArrival`, `Residents`, `VisitorsHelped`, `LitWindows`,
  `BridgeMarks`; the `ValleyPresentation` fields above; `GameEventKind.VisitorCalled`, `VisitorArrived` and
  `VisitorSettled`. Changed: `ValleySimulation.IsComplete` now means the six puzzles are done (the valley keeps
  running for visitors), so `Stage` is no longer always `Complete` when it is true. Events raised while a
  `ValleySimulation` opens (`RebuildNeeded`, `VisitorCalled`) are raised again on the first tick, so a view
  subscribed after opening sees them. `PalmfolkRunner` saves on the two visitor events.
- **D9, Nov 10.** Added `ModeFlow` and `AnchorSearch` (Core); `ISpatialAnchors`, `IPassthrough`,
  `PalmfolkRunner.Anchors`, `Passthrough`, `Flow`, `WaitingForAnchor`, `ChooseMode` and `ModeChanged`; the
  `Palmfolk.Platform.Meta` assembly (`MetaTableMode`, `MetaPassthrough`, `MetaSpatialAnchors`);
  `LiveInputSetup.EnableTableMode`, `AddTableMode` and `TableModeEnabled`. Changed: `PalmfolkRunner.Open(valley,
  storage)` may leave `Game` null for up to 5 s while it looks for the anchor (`Opened` fires when the game opens),
  and runs a Table save as VR when there is no passthrough. `Capture` writes the flow's mode and anchor.
- **D10, Nov 10.** Added `Gaze.cs` (`GazeSource`, `GazeSample`, `NeedSign`, `GazeTarget`, `NeedSigns`,
  `LookWatcher`, `GazeButton`, `LookAndPinch`); `HandRoutes.PalmNormal`; `IGazeReader`; `PalmfolkRunner.Gaze`,
  `GazeEnabled`, `Looks`, `Pointer`, `GazeButtons`, `LastGaze`, `GazeSource`, `NeedSignChanged`, `GazeSelected`;
  `MetaGaze` (the Meta assembly now also references `Oculus.Interaction`). Nothing removed or renamed.
- **D11, Nov 10.** Hot paths reuse their buffers. Added `HandSample.Copy()`, `HandFrame.Copy()`,
  `HandSampleBuffer` (a hand sample refilled joint by joint), `HandRoutes.CanCreate` and `HandRoutes.BestChoice`
  (the checks and choice of `Create` and `Best` without building the route), `NeedSigns.Targets(game, pose, list)`
  (fills a list; keeps the same `GazeTarget` while nothing changes) and `HandJointMap.Convert(joints, matrix,
  policy, buffer)`. Changed, to keep in mind:
  - A sample or frame you didn't build may be refilled on the next frame or tick: `IHandReader.Read`'s sample,
    `LiveFrameSource`'s frame and hands, and the filtered hands inside a tick. Copy one to keep it; recorders do.
  - `HandFrame`'s properties are read-only now (use the constructor).
  - `ValleyPresentation.Bridges` and `BridgeMarks` are shared read-only snapshots, replaced only when a bridge
    changes.
  - The live hand route in `ValleySimulation` is still a fresh object each tick (views and the build keep it).
- **View contracts, Nov 12.** Added:
  - `GameEventKind.ConnectionLocked` (last in the enum), `EdgeSession.ConnectionLocked` and
    `ValleyPresentation.ConnectionLocked` (E17). Raised when the hand holds every anchor, once per hold, and again
    after a pause or a loss. The Board's carry locks when the fingertip holds the next island.
  - `Traveler.Support`, `SupportCursor`, `PreviousSupport` and `PreviousSupportCursor`; `TravelerView.Support` and
    `SupportCursor` (interpolated; a supported view's `Position` is `Support.Evaluate(SupportCursor)`). Also
    `ValleyPresentation.BramWalk`, and `PipCursor` on `FilteredHandRoute` while a hand carries Pip.
  - `Recovery.GhostHand` and `ValleyPresentation.GhostHand`: the owning hand as last read whole, valley-local
    (null after resuming from a save).
  - `SolutionHint` (`Shape`, `Tree`, `Rise`), `HintShape`, `EdgeSession.Hint` and `ValleyPresentation.Hint` (E8).
    A bridge that passes the current rules, found once per interaction, or null while a ledge is too narrow.
  - `SessionChange`, `GameRecording.Changes`, `GameRecorder.NoteSettings` and `NoteMode`;
    `PalmfolkRunner.SettingsApplied`.
  Changed: recordings are format 3 (`.pfr3`). a traveler's `Position` (and `ValleyPresentation.Bram`) on a bridge
  is now the point on the deck rather than a straight-line blend between ticks (tested within 2 mm of it).
