# Presentation adapter

`Palmfolk.Views.PalmfolkView` implements `IPalmfolkView`. Attach it below an unscaled identity container.
Call `Configure(LevelGeometry)` before the first `Present` so the M1 islands/obstacles correspond to Core.
The platform owner supplies `WorldPresentation(game, runner.InterpolationAlpha)` after fixed ticks and
consumes every accumulated event separately. The view never steps/mutates Core or reads hand SDK input.

The content root applies the published world origin, yaw and world scale. Actors, routes, posts and knots
then use valley-local coordinates. The existing presentation contract already interpolates actor/live-route
positions; frozen routes are drawn directly. Repeated frames reuse primitive pools and do not alter bridges.
No collider remains on a presentation primitive. This greybox is not final P1/P6 art or a performance pass.

The bridge drawer reads **every curve** in a `CurveTree`; a shared branch junction is Core data. Rope has
a thin cord deck and rails; planks have separate wide boards. Posts use captured route joints; a tip knot
respects the route's reversed flag. Transparent preview has a width/capacity shape (thin post versus cart-size
block with wheels); a group has three posts and no valid capacity has a cross. Capacity uses full width,
slope and clearance rules against the configured level. Crossed animated bars identify refusal
segments without relying on colour. `ReducedMotion` keeps the shape but removes its pulse.

The recovery route is still and jointed. A separately supplied `HintRoute` uses disconnected moving beads;
it never changes game state. Full hand skeletons, a valid puzzle-specific hint pose and wider valley actors
need additional Core/puzzle data. Do not infer missing joints or invent a solution from a gesture label.

Editor preview: **Palmfolk > Presentation > Replay greybox**, then start the synthetic build/withdrawal.
The temporary objects are not saved; close the window before saving any scene. The M1 scene is untouched.
The Unity integration test drives the same sequence and checks persistence, actor positions, no colliders
and unchanged simulation ticks. Editor results do not verify live input, headset contact, visual readability
at 3 cm, or 72 fps. Future D3 contracts must preserve the current M1 fields and add valley data as documented.

`XRayView` is a separate shipping `IPalmfolkView`: configure it with the same level/settings, feed each
frame and call `SetVisible`. Solid/dashed threshold rings, differing line thickness and frozen joint markers
remain distinguishable without colour. It displays the M1 routes and bridge tree supplied by Core; it does
not infer D3 connectivity or invent an undisclosed valley graph. Keep a diagnostic sign facing the viewer.

`Palmfolk.Developer.StatsPanel` lives in a nested, separately gated `Palmfolk.Stats` assembly under the
shared Developer path (Sam's stats ownership). It writes no files and creates no recorder. Platform code
calls `Show` with measured fps, total/disruptive per-puzzle counts, puzzle ID, actual gaze source, mode and
stamped version. Nulls explicitly display “unmeasured”/“unbound”. The release frame has x-ray but no stats
type. Actual FPS/labels and per-puzzle reset need Dario's platform/director binding.

`TravelerRig` supplies original 3 cm greybox bodies, arms, feet and contact-shadow shapes. Its nine
poses cover wave, hesitation, testing a plank, crouch/grip, hammering, celebration, looking back,
refusal/head shake and walking. Read-only frames choose persistent state; `PalmfolkView.Consume(events)`
supplies transient gestures per fixed tick. Actor facing follows the displayed travel direction and the
cart stays behind Bram. Reduced motion preserves static gesture shape.

This is a state/rig implementation, not a final animation/readability pass. `FootPlanting` now retains
stance feet as route coordinates, reprojects them as hand geometry moves and emits a contact after a
12-tick swing. Island stance uses valley-local contacts. Soles/shadow shapes tilt to the supported route.
The rig's `FootContact` callback supplies the **world** contact point through the current valley pose;
the platform can subscribe `audio.FootContact`. No clock-derived pretend contact is sent.
Synthetic tests check hand translation without sliding/false steps and one landing cue after a swing.
Surface transitions, final rigs/props and owner judgment at 3 cm still need editor/headset review.
No camera, physics or gameplay state is animated by these components.

## Full valley (D3)

Use `ValleyView` / `IValleyView` with `ValleyPresentation`, not `WorldPresentation`, for a full chapter.
It draws the actual `ValleySpec` islands/obstacles, all saved bridge trees by edge ID, group travelers,
Bram/cart, ledge width/progress, geometric capacity preview and per-edge/segment refusal crosses.
The same original rope/plank drawer is shared with M1. The hand-only overload accepts runner alpha
for between-tick route interpolation; frozen/scaffold data is never interpolated to a later hand.

`StartupView`, `NightView` and `XRayView` also implement the valley port. X-ray now uses the actual ten-edge
graph and active anchor zones. Night reads the director's solved count and all bridges, restores light in
the finale, and keeps visitor windows at the explicitly supplied count (D8 not yet published). Audio accepts
either presentation type but still needs the corresponding tick's position when consuming events.

Platform binding: after fixed ticks, construct `ValleyPresentation(game, alpha)`, consume every accumulated
gesture/startup event, and send the frame to the valley views. Call `ValleyView.Present(frame, alpha)` for
hand interpolation. Configure `NightView` with the content sanctuary position. Route UI commands through
the input adapter, never a view mutation of Core. No live driver or shared scene was edited in this work.

The preview is geometric: Core does not expose the destination connection watcher's locked state, so no
explicit connection-lock chime/event is fabricated. Visitor rigs, full recovery skeleton, solution hint pose,
true single-hand Fork and headset performance/feel remain open dependencies or owner checks.

D4 binding: the live and frozen scaffolds use full curve trees, refused branch indices address the correct
curve/segment, and rejected scaffold has a short removal. Persisted shared trunks draw once, including
after Save v2 reconstructs equal Route objects. Only leaf fingertips receive knots; a shared wrist/trunk
does not. X-ray and finale lighting also deduplicate trunks. Call `Configure(actualGameSettings)` on
`ValleyView` (or pass settings to M1 `Configure`) so H1's capacity rules match the actual simulation.

## Contrast and retained materials

After a setting is acknowledged, set `HighContrast` and `TableMode` on the active M1/valley view.
The high-contrast palette uses unlit bright routes/travelers and dark islands, preserving geometry-based
rope/plank/refusal cues. Only high-contrast **Table mode** enables the bounded solid backdrop; it is
hidden for ordinary Table mode and VR. These are render settings, not a claim of readability over a real table.

Five original material configurations in `Art/Resources/Palmfolk` reference Unity's opaque, transparent,
emissive, unlit and line shaders. Runtime material copies use the templates and a build preflight refuses
missing ones. The editor menu creates only missing templates and never overwrites reviewed art. No
graphics/project settings or external assets were committed. Final shader behavior, table-edge visibility,
contrast and performance still need the actual APK/owner pass.
