# M1 sign-off report template

**Status: Not signed off.** Only the owner can mark M1 proven on Quest 2 and judge its feel.
Source preparation, passing headless tests and editor tests do not pass this gate.

- Date / owner decision: Not recorded.
- Build version / full revision / APK hash: Not recorded.
- Device / OS / mode / settings / controller state: Not recorded.
- Current headless and editor results (link exact logs): Not recorded.

| Slice | Implementation | Verification label / evidence | Not verified / blocker |
|---|---|---|---|
| 0 boot/hands | Not assessed | Not run | — |
| 1 live/replay/radii | Not assessed | Not run | — |
| 2 planting | Not assessed | Not run | — |
| 3 route/contact | Not assessed | Not run | — |
| 4 connection/ownership | Not assessed | Not run | — |
| 5 section build | Not assessed | Not run | — |
| 6 bake/persistence/refusal | Not assessed | Not run | — |
| 7 cart crossing | Not assessed | Not run | — |
| 8 save/replant | Not assessed | Not run | — |
| 9 recovery | Not assessed | Not run | — |
| 10 headset feel | Not assessed | Not run | — |

For each scene/puzzle record Quest 2 fps minimum/typical, total/disruptive dropouts, route lengths,
whether 15 cm works, unsupported-arm intervals, startup time and first-persistent-bridge time.
Include left-only/right-only, table/lap/air and the 20 cm loss/reappearance case.

| Gate | Measurement and sample | Decision / requirement correction proposed |
|---|---|---|
| H1 Arch | Rock bounds, span, anchor heights; five testers' shape/refusal results | Pending owner |
| H2 Comfort | Position, height, facing, scale, reach direction and arm support | Pending owner |
| H3 Radii | Per-joint availability/ranges/stability and fallback | Pending owner |

List every changed tolerance with old/new value, evidence and affected regression tests. Requirements
remain frozen until the owner incorporates any correction. List open P3 blockers and lost-progress cases.
Owner decision: **Pending / Proven / Not proven**. If Not proven, identify the next correction and rerun.
M1b Quest 3/3S, H5/H6, final P12 and M5 remain separate gates after M1.
