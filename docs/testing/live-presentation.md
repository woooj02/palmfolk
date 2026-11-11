# Live presentation setup

Verification label: Unity editor, synthetic inputs/scripted state only. No headset or simulator pass.

After reviewing the Slice 0 scene/configuration in Sam's Unity project:

1. Run **Palmfolk > Platform > 1. Enable XR Hands**, then **2. Add Runner To Open Scene**.
2. For Table mode run **Palmfolk > Platform > 3. Enable Table Mode**.
3. Run **Palmfolk > Presentation > 1. Add Live Views To Open Scene**. It adds an unscaled root and
   references the existing runner/head. It does not save or replace the scene, install anything,
   accept terms, enable recording or edit a scene in another checkout.
4. Review and save the scene/configuration through the owner setup workflow. Do not commit the
   unreviewed Unity/Meta defaults listed in `unity-setup.md`.

For a developer build, enable `PALMFOLK_DEVELOPMENT`, then run **Palmfolk > Presentation > 2. Add
Measured Developer Stats** and review/save the scene. `LiveStatsBinding` wraps the existing tick target
and forwards it unchanged, so a consented recorder can remain attached. It uses Dario's exact T19
per-puzzle counters and actual rendered time, displays the runner's gaze source/running mode/build
version, and writes no files. FPS is unmeasured until a one-second window has elapsed. Text refreshes
at most four times a second. Stats and their tests are absent from the release assembly set.

These labels describe measurements from the current running process. A synthetic 50 fps test window
verifies arithmetic only; it is not a hardware result. D11's Core timings do not cover these new views,
audio, interaction or overlay costs. Profile the full build on the actual headset.

The child views are created at runtime. Keep the presentation root at unit scale and outside Valley
Origin: these views already apply Core's world pose. Planting/anchor-search invitations are placed
once 40 cm ahead and 15 cm down from the initial head pose; the camera is never moved. After planting,
the waystone sits at valley-local (0, 0.05, -0.22), faces the viewer and recomputes its angular targets.

`GamePresentation` consumes every accumulated event, then renders the interpolated state. Settings
are read from the runner's acknowledged player settings. Cues use that rendered frame's world pose;
events from several catch-up ticks share the latest position, so this is not a sub-frame audio-timing pass.
Foot contacts still come from the route-relative rigs, not wall-clock step guesses.

Visitors and residents come from `VisitorFigure`/`Residents`, bridge tokens from `BridgeMarks`, and
windows from the actual bitmask. Tokens use five shapes (leaf, cart, pair, star, lantern). Need signs
use rope, planks, a branched path and an arch, raised only when D10's watcher reports them. Gaze never
solves a puzzle. The waystone works without gaze via the ten ISDK poke tips.

`TableDimmingBinding` feeds the SDK's brightness adjustment from `NightView`. **Owner Verified Maximum
defaults to zero.** The owner may raise it only while running MR4's table-edge visibility check in the
actual room; record the verified value/device/lighting. Leaving Table mode restores neutral brightness.
Reduced motion uses the existing monotonic fade. No safe nonzero value has been measured here.

Remaining owner checks: live poke with each finger of either hand; complete left-only/right-only menus;
anchor found/lost/fallback; need signs at real viewing distance; resident/cart/mark readability at 3 cm;
sound/caption quality; room dimming and table-edge visibility; full Quest 2 M1 and Quest 3/3S quality runs.
The full recovery skeleton, solution hint pose and exact support cursor still need a published contract
before strict hand/contact guarantees can be made.
