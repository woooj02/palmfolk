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
block); capacity uses full cart validation against the configured level. Crossed animated bars identify refusal
segments without relying on colour. `ReducedMotion` keeps the shape but removes its pulse.

The recovery route is still and jointed. A separately supplied `HintRoute` uses disconnected moving beads;
it never changes game state. Full hand skeletons, a valid puzzle-specific hint pose and wider valley actors
need additional Core/puzzle data. Do not infer missing joints or invent a solution from a gesture label.

Editor preview: **Palmfolk > Presentation > Replay greybox**, then start the synthetic build/withdrawal.
The temporary objects are not saved; close the window before saving any scene. The M1 scene is untouched.
The Unity integration test drives the same sequence and checks persistence, actor positions, no colliders
and unchanged simulation ticks. Editor results do not verify live input, headset contact, visual readability
at 3 cm, or 72 fps. Future D3 contracts must preserve the current M1 fields and add valley data as documented.
