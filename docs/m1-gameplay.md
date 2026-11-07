# M1 gameplay core

The owner authorized gameplay algorithms before the Windows/headset passes.
`m1-gameplay` contains that work; `m1-slice-0` remains the earlier setup baseline.
The original build plan and requirements are unchanged. This changes the order of
source preparation, not what counts as a headset pass or M1 sign-off.

To get this version on another PC:

```bash
git clone --branch m1-gameplay https://github.com/woooj02/palmfolk.git
cd palmfolk
```

## Implemented

The same C# files in `Assets/Palmfolk/Core` compile as an engine-independent
.NET Standard 2.1 library and as a Unity assembly with no engine references.
`tools/CoreChecks` links these files directly; there is no second test implementation.

| Requirement | Core behavior |
|---|---|
| T1 | OpenXR-order 26-joint input; five joint-polylines through the metacarpals to the wrist; bounded palm polygon; upward-side check; radius offset; segment/fraction traveler attachment |
| T2, A3 | 9-tick connection hold below 2 cm; break above 4 cm; first qualifying hand owns the interaction; deterministic nearest-hand tie break, then left on an exact tie |
| T3 | One Euro filtering, 30-tick fingertip-speed window, immutable section commits, shape/width-change pause, retained progress, reconnect at frozen end, scaffold expiry and removal state |
| T4, capacity | Frozen curve-tree data; width, slope, clearance across the deck, anchoring, length and self-crossing checks; failing curve/segment and reason supplied to the view |
| T5 | Last-good ghost support, owning-hand alignment for 18 ticks, 12-tick blend, 480-tick timeout and gradual return to the last reached checkpoint |
| T8, E19 | Focus/menu pause; controllers-in-hand keep initial boarding waiting; tracked input must qualify before ownership |
| T9 | Partial build and recovery snapshots, frozen bridges, schema/checksum validation, two save slots, atomic file replacement, backup recovery and explicit corrupt-save status |
| T17a | Fixed 60 Hz stepping; same input interface for live/replay; no PhysX, Unity objects or wall-clock reads in Core; no silent tick dropping |
| T13, T19, H3 | Developer-only consent gate, binary input recording/replay, build/settings/level/calibration header, affected-joint dropout watcher, timing counters, fps accumulator and per-joint radius summaries |
| E8, T18 | Hint and persistent-poor-quality signals; stillness tolerance can loosen one step; connection distances stay fixed |
| M1 loop | Pip boards S→A, builds A→S, the bridge remains without hand input, Bram attempts S→A, rope is refused for his cart and a wider replacement can complete the crossing |

The curve-tree storage supports parent links required by T4/T9. M1 gameplay builds
one curve only; there is no fork puzzle, M2 journey, art, audio or experiment code.
H1 has synthetic geometry tests for a raised route over a specified rock. This
does not replace the five real testers or settle Arch dimensions.

## Run without Unity

Install the .NET 8 SDK, then from the repository root:

```bash
dotnet test tools/CoreChecks/Tests/Palmfolk.Tests.csproj --configuration Release
dotnet test tools/CoreChecks/Tests/Palmfolk.Tests.csproj --configuration Release -p:DeveloperFeatures=false
```

The first command tests the core plus consent-gated developer code. The second
repeats the core tests and checks that the developer assembly contains no types.
`Release` is a compiler optimization setting; `DeveloperFeatures=false` is what
removes developer code in these tests. In Unity, remove `PALMFOLK_DEVELOPMENT`
for a release player. Both the assembly constraint and C# guards use that same gate.

The **Source checks** workflow compiles and runs both configurations on hosted
Linux and Windows, in addition to the earlier source and manifest checks.
Unity Edit Mode uses the tests under `Assets/Palmfolk/Tests` and the pinned Unity
Test Framework. Actual Unity test discovery/compilation is still unverified here.

## Windows integration ports

These are explicit interfaces and data contracts, not dummy SDK implementations:

1. **Input:** implement `IHandFrameSource` using the installed SDK. Convert poses to
   the same world-space metres as the level anchors. Y is world up. Map joint IDs,
   validity, confidence and radius availability explicitly. Preserve rotations and
   timestamps for replay. Missing tracking is a frame with absent/invalid joints;
   it must not silently reuse the previous good frame. `TryRead=false` means no tick
   is available (for example replay EOF), not a tracking dropout.
2. **Clock:** drive `FixedTickRunner.Advance` with rendered-frame elapsed seconds.
   It keeps backlog and exposes interpolation alpha. Consume its accumulated
   `Events` after each advance so multi-tick frames do not lose animation cues.
   The caller supplies frame time; Core never reads a clock.
3. **View:** implement `IPalmfolkView`. `WorldPresentation` supplies interpolated
   Pip/Bram positions and hand route, ghost, frozen scaffold, removal progress,
   persistent curve tree and segment refusals. Render frozen data without moving
   it to match a later hand pose. Use the filtered route as the visible preview
   reference for I2. Connect the SDK default hand visual separately.
4. **Level:** construct `LevelGeometry` from the two anchor positions and measured
   obstacle bounds. Clearance clips each swept deck strip against obstacle bounds,
   including its width and diagonal end edges. No gameplay query uses
   PhysX. The provided fixtures use a 12 cm horizontal gap.
5. **Save:** pass `Application.persistentDataPath` to `DirectorySaveStorage` or
   implement `ISaveStorage` with atomic replacement. Use `SaveRepository`, not a
   direct overwrite. Handle `Corrupt` explicitly. On restore, a hand-supported
   traveler requires ghost alignment; it never attaches to an arbitrary new pose.
   Save/load on Quest storage and app suspend/resume still need device testing.
6. **Developer session:** start `RecordedRun` only after verbal consent, using the
   actual build version (including the source revision) and current saved state.
   Use a new version for every gameplay build. It restores that state before
   its first tick so live and replay start with identical filter history. Feed it
   one `HandFrame` per tick, then export `Finish()` through `RecordingCodec` locally.
   `RecordedRun` implements `ITickSimulation`, so the same `FixedTickRunner` can
   drive it; pass `RecordedRun.Presentation(alpha)` to the view in recording mode.
   Replay requires the same build version. The 36,000-frame capture cap marks the
   session partial and stops adding frames; gameplay and metrics keep running.
   Do not present a partial recording as a complete session.
7. **Stats without recording:** `PlaytestMetrics` and `DropoutWatcher` can be used
   without creating a recorder. They write no files. The view owns the small
   developer stats panel. Radius summaries report measurements only; H3 decides
   whether measured widths should replace fallback widths.

The existing generated Unity scene is still slice 0: hands and islands. These
ports have not been wired into that scene. No Unity SDK calls, scene character
bindings, APK, controller-status adapter, hand-radius API reading, animation,
stats UI or headset result is claimed as completed by these tests.

## Additional starting defaults

The requirements supply the connection, stillness and recovery thresholds. The
following implementation choices fill gaps and remain subject to headset tuning:

| Choice | Starting value |
|---|---|
| Build duration | 150 ticks / 2.5 s |
| Pip/Bram walking speed | 3 cm/s |
| Scaffold removal animation interval | 15 ticks / 250 ms |
| Visible checkpoint return | 60 ticks / 1 s |
| Maximum route length | 35 cm |
| One Euro minimum cutoff / beta / derivative cutoff | 1 Hz / 0.3 / 1 Hz |
| Width display hysteresis | enter plank at 2.7 cm, leave below 2.5 cm; capacity still checks measured width |
| Tolerance steps | 0.75×, 1×, 1.5× the starting stillness speed |
| Finger merging | gap below 1 cm at at least 3 of 5 longitudinal samples, excluding the shared wrist |
| Stuck-time telemetry | an idle episode counts once it reaches 10 s; pause time is excluded |
| Startup fallback text signal | 10 s waiting for controllers to be put down or a usable hand; clears once resolved |

Fallback widths remain 1.2 cm for one finger and 2 cm per merged finger. Measured
radii are disabled by default until H3. M1 world scale is 1; zero bone lengths in
recording metadata mean not measured. Calibration and world-scale controls remain
outside M1 as specified in the plan.

One Euro is implemented originally from the published algorithm; no implementation
code was imported. Algorithm reference: [Casiez, Roussel and Vogel](https://gery.casiez.net/1euro/).
Unity API profile: [Unity 6000.0 .NET compatibility](https://docs.unity3d.com/6000.0/Documentation/Manual/dotnet-profile-support.html).

## Headset verification remains in order

First run [slice 0](slice-0-quest2.md). After binding the ports, verify each later
slice on Quest 2: record/replay and radii; hand contact; connection/ownership;
freeze/pause/resume; persistent geometry/refusals; cart capacity; quit/reload;
20 cm tracking-loss recovery; H1/H2/H3 comfort and measurements.
Each result stays labelled Quest 2. The owner still signs off M1 on the headset.
