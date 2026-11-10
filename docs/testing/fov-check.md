# E18 field-of-view check

**Procedure only; simulator/headset results Not run. No Glasses hardware used.**

Owner: use the already available FoV simulation, either OVRManager's **Simulate Device Field of View**
or the installed/authorized `metavr device fov-sim enable --design-area` command. Do not install or
sign in on this sheet's authority. Record the method, tool version, mask, build version and device.
The design area is 58° × 54° (full view 70° × 66°); the active-step target is within ±25° of look.

1. At the planted seated pose, enable the **design-area** mask, not just the full-view outline.
2. For each of the six puzzles capture the active anchor(s), live/ghost/hint hand, Pip and refusal/capacity
   sign before connection, during build and after withdrawal. Repeat both hands and adjusted scale.
3. Capture the finale path, first visitor and each shipped later visitor's current step. Head turns are
   allowed; leaning/standing to find required information is a failure. Count extra head turns separately.
4. Check every waystone row: at most four simultaneous targets, each ≥6° wide, ≥1° gaps. A four-target
   row uses at least 27°. Confirm the whole row and current action feedback fit the mask at its viewing distance.
5. Repeat in VR and Table with the intended text/contrast settings, table/lap/air heights. A render-only
   editor crop may support a layout calculation but cannot count as a headset FoV/reach pass.
6. Record each failed element, measured angular bounds, screenshot reference and proposed correction.
   Repeat after the correction, using the final build for submission evidence.

| Build / method / device | Puzzle or row / state | Hand / mode / scale | Angular bounds | Result / evidence |
|---|---|---|---|---|
| Not run | — | — | Not measured | Not run |

Public wording after an actual pass: “Checked using the design-area FoV simulation on [device], build
[version]; no Glasses hardware used.” Keep simulator, Unity editor and Quest 2 results separate.
