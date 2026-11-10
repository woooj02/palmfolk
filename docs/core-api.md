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
| `Puzzle` | The `M1Simulation`, in valley-local space. During planting it is held as if focus were lost: a hand that carried Pip goes into recovery, and the player realigns with the ghost afterwards. |
| `Planting` | `PlantingDetector`: `Progress` (0 to 1 over the resting second), `Hand`, `WaitingForHands`, `ShowStartupText`. |
| `LastPlanting` | `PlantingResult`: `Pose`, `Hand`, `HandLength` (metres, wrist to middle fingertip along the bones). |
| `Plantings` | How many times the valley has been planted. |
| `IsPaused` | The player's pause. |

Commands: `InputCommand.Replant` (new) starts replanting from `Playing`; it is ignored while already planting.
`Pause` and `Resume` work in both stages.

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

### Events

`GameEventKind` gains two values, appended at the end so existing numbers don't change:

| Event | When |
|---|---|
| `Planted` | The tick the valley is planted or replanted. Read `Pose` and `LastPlanting`. |
| `ReplantStarted` | The tick a `Replant` command takes effect. |

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
