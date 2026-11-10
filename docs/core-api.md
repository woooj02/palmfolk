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
| `SaveData` | `Valley` (`ValleyProgress`, `null` before first play), `Mode` (`PlayMode.Table` or `VR`), `AnchorId` (Table mode's spatial anchor, at most 128 characters), `Pose` (last planted pose, world space), `Settings`, `M1` (the M1 slice's state), `LoadedVersion` (1 or 2). |
| `PlayerSettings` | `StillnessToleranceStep` (0 to 2, default 1), `WorldScale` (0.5 to 2), `ReducedMotion`, `Captions` (default on), `HighContrast`, `XRay`. Core applies the first two; views read the rest. |
| `SaveCodec.EncodeSave` / `DecodeSave` | Version 2 bytes. Decoding a version 1 save (the M1 slice) gives a `SaveData` with only `M1` set and `LoadedVersion` 1. Anything corrupt, truncated or out of range throws `InvalidDataException`. |
| `SaveRepository.Save(SaveData)` / `Load(out SaveData)` | Two slots, newest valid one wins; a corrupt newest slot falls back to the other. `Load(out M1Snapshot)` and `Save(M1Snapshot)` still work and keep the rest of the save. |

`ValleyProgress` also holds `CurrentVisitor` (-1: none), `VisitorsHelped` and `LitWindows` (bit i is visitor i)
and `BridgeMarks` (edge id to the visitors who crossed it), for D8. Resuming with a save from other content drops
bridges on edges the valley doesn't have; if Pip's island is gone he starts at the current puzzle's island.

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
| `ValleySpec(islands, edges, puzzles, obstacles, ledge, camp, sanctuary)` | The valley. `obstacles` are boxes (the rock) checked for headroom. `FinaleRoute()` is Bram's route as (edge, from, to). |

`ValleySpec` refuses: anything but six puzzles in the order Board, First Bridge, Width, Fork, Arch, Finale; a
Board with more than one edge; puzzle edges that don't touch `travelerFrom` (except the finale); a finale that is
not the cart from the camp along a path of edges to the sanctuary; a ledge edge not built in both First Bridge
and the finale.

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

### Events

`GameEventKind` gains two values, appended at the end so existing numbers don't change:

| Event | When |
|---|---|
| `Planted` | The tick the valley is planted or replanted. Read `Pose` and `LastPlanting`. |
| `ReplantStarted` | The tick a `Replant` command takes effect. |

D3 appends `PuzzleStarted`, `PuzzleCompleted`, `LedgeWidening`, `LedgeWidened`, `RebuildNeeded` (once per
failing edge), `TravelersStarted`, `TravelerArrived`, `CrossingCompleted` (each of Bram's bridges). The valley
also raises the M1 hand and build events, `PersistentBridgeObserved`, `BramStarted` and `BramFinished`.

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
- **D5, Nov 10.** Added `SaveData`, `PlayerSettings`, `PlayMode`, `SaveCodec.EncodeSave`, `DecodeSave` and
  `CurrentVersion`; `SaveRepository.Save(SaveData)` and `Load(out SaveData)`; `InputCommand.Restart` and `RingBell`;
  `PalmfolkGame.Open`, `Capture`, `Apply` and the pose constructor; `ValleyProgress.CurrentVisitor`,
  `VisitorsHelped`, `LitWindows`, `BridgeMarks`; `ValleySimulation.SetStillnessTolerance`,
  `M1Simulation.SetStillnessTolerance`, `PlantingDetector.SetWorldScale` and `WorldScale`. Changed: the save file is
  version 2 (version 1 still loads); `Load(out var x)` is now ambiguous, so name the type. Resuming drops unknown
  edges instead of throwing.
