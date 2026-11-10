# Waystone input port

`WaystoneView` creates four original carved targets with installed Interaction SDK `PlaneSurface`,
`CircleSurface` and `PokeInteractable` components. No SDK sample was copied. Its five four-target pages
cover every required action. Navigation changes only the displayed page; game actions emit a typed
`WaystoneRequest`. **Nothing changes Core, saves, mode or app lifecycle without the platform adapter.**

Place the post near the valley edge and call `LayoutForViewer(headPosition)` as the viewing distance or
world scale changes. With an unskewed uniform parent it faces the viewer and computes planar bounds
for exactly 6° targets/1° gaps; four span 27°. The 40 cm initial layout is only a placeholder before binding.
Bind all-finger/either-hand ISDK poke interactors in D6. The surface itself accepts any qualifying pointer.
The selection callback cannot infer handedness from an unbound pointer ID, so it reports unknown rather
than inventing one; the platform can call `RequestSlot` with the real hand from its pointer registry.

D10's resting look-and-pinch can call the same request method with the actual selected slot/hand and
palm-facing-headset guard. No gaze/pinch source is invented here. After accepting a request, the input
adapter queues it for a fixed tick and calls `Reflect` with **actual** settings/state. The UI never assumes
a request succeeded. Stillness has exactly three steps; world-scale requests cycle the published D2 range
in 0.25 increments (starting UI step, subject to owner fit testing). Back-end restart, bell, settings/save,
mode and quit handling still need the published Core/platform contract. Requests are not puzzle solutions.

Editor tests check angular geometry, left/right-labelled request parity and the unsafe pinch guard. They
do not verify live poke, head-direction targeting, saved settings or a full hands-only session. Owner
checks remain A1/GZ2/E9/E18, both hands alone and ten runs without accidental system-menu selections.
