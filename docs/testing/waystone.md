# Waystone input port

`WaystoneView` creates four original carved targets with installed Interaction SDK `PlaneSurface`,
`CircleSurface` and `PokeInteractable` components. No SDK sample was copied. Its five four-target pages
cover every required action. Navigation changes only the displayed page; game actions emit a typed
`WaystoneRequest`. **Nothing changes Core, saves, mode or app lifecycle without the platform adapter.**

Place the post near the valley edge and call `LayoutForViewer(headPosition)` as the viewing distance or
world scale changes. With an unskewed uniform parent it faces the viewer and computes planar bounds
for exactly 6° targets/1° gaps; four span 27°. The 40 cm initial layout is only a placeholder before binding.
The live ten-tip ISDK binding is described below. The surface accepts any qualifying pointer.
The selection callback cannot infer handedness from an unbound pointer ID, so it reports unknown rather
than inventing one; the platform can call `RequestSlot` with the real hand from its pointer registry.

D10's resting look-and-pinch can call the same request method with the actual selected slot/hand and
palm-facing-headset guard. No gaze/pinch source is invented here. After accepting a request, the input
adapter queues it for a fixed tick and calls `Reflect` with **actual** settings/state. The UI never assumes
a request succeeded. Stillness has exactly three steps; world-scale requests cycle the published D2 range
in 0.25 increments (starting UI step, subject to owner fit testing). The runner adapter below handles
restart, bell, saved settings, mode and quit. Requests are not puzzle solutions.

## Live binding (Nov 11)

`WaystoneBinding` now connects the requests to D6–D10's runner. Pause, resume, restart, replant and bell
queue fixed-tick commands. Settings use `runner.Apply` and its save path; mode uses `ChooseMode`, reflecting
the actual running mode even when passthrough falls back to VR. Quit saves before closing the application.
`GazeButtons` supplies the four actual world centres and six-degree widths; `GazeSelected` selects the
matching slot. A white outline marks `Pointer.Hovered`. D10 owns the resting-pinch/system-palm guard.

`WaystonePokes` supplies ten ISDK `PokeInteractor`s from the runner's latest world-space fingertips and
reported radii (5 mm fallback if absent). Invalid or low-confidence tips, controller use and loss of focus
disable them. No SDK sample is imported. Poke pointer handedness remains unknown in the view's event;
that does not affect command dispatch or either-hand availability.

Editor checks cover the command queue, mode fallback, settings/save round-trip, quit callback, world gaze
targets and all ten tip mappings/loss. They do not constitute a live poke or hands-only headset pass.
Owner checks remain A1/GZ2/E9/E18, both hands alone and ten runs without accidental system-menu selections.

Recording limitation for Dario: the current format-2 recorder snapshots initial player settings, but
`runner.Apply` and `ChooseMode` are not tick-frame commands. Sessions that change settings/mode need a
published recorder event contract before their replay can be claimed identical. No recorder/save types
were changed in this binding.
