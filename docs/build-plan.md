# Palmfolk: build plan

How to build what the requirements define: [`requirements-v1.3.md`](requirements-v1.3.md) (the game) and
[`requirements-v1.3-quality.md`](requirements-v1.3-quality.md) (accessibility, technical, polish). They say what must
be true; this file says what to do next. If they disagree, stop and ask the owner. IDs (T3, E21, MR3) refer to them.

**Current goal: M1.** Prove the core promise on a Quest 2. Nothing else starts until the owner signs off M1.

## Ground rules

- **Scope:** nothing beyond the requirements. Corrections and removals are the owner's call.
- **New work:** the rules allow no pre-existing code. Unity and Meta packages are dependencies. Don't copy Meta sample
  code or any third-party code or asset in without asking the owner, and record the licence of anything approved.
- **Honesty:** never report something as working on a headset unless it ran on one. Quest 2 results are labelled
  Quest 2 and never stand in for Quest 3/3S. Simulator results are labelled simulator.
- **Commits:** authored by the owner, plain messages, no tool credit lines in commits, PRs or files.
- **Owner only:** pushing, accepting terms, creating the Meta app, the signing key, headset testing, installing tools,
  and judging how it feels. Commit locally and ask for the rest.
- **Keep out of the repo:** keys, keystores, `.env`, playtest recordings, APKs. Read `git status` and the diff before
  every commit.

## Setup

- Build on Windows. Unity 6000.0.66f2 or later in the 6000.0 line, with Android Build Support, OpenJDK and Android SDK
  & NDK Tools. Pin the version in `ProjectSettings/ProjectVersion.txt`.
- Meta XR All-in-One SDK v207 on OpenXR. The Unity project lives at the repo root.
- Package name: `com.keepsrunning.palmfolk` (default; the owner confirms before the Meta app is created).
- Create the Meta app and the "Competition" release channel in the first two days, not the last: an upload needs app
  metadata, and that is when the privacy-policy question gets its answer. Start the Devpost draft now; it can be edited
  until the deadline.
- Optional, owner's choice: Meta XR Simulator (hand and gaze input without a headset, and allowed for footage), and
  the organisers' AI tooling (Meta VR CLI, Meta XR Operator). The CLI's `metavr device fov-sim enable --design-area`
  is one way to run the E18 check; the `OVRManager` "Simulate Device Field of View" toggle is the other.

## Architecture

```
Headset hand tracking ─┐
                       ├─► HandFrame ─► Core, fixed 60 Hz tick ─► state ─► Views, every rendered frame
Recorded session ──────┘    (joints,     planting, route graph, build,      (interpolate between ticks)
                            radii,       validation, travelers, visitors,
                            validity)    recovery, save data
```

- **Core** (`Assets/Palmfolk/Core`, own assembly): plain C#. Same input, same decisions. No PhysX, no scene objects,
  no wall-clock time (T17a). Everything is in valley-local coordinates, so the valley can be planted or re-anchored
  anywhere without touching a bridge (MR3).
- **Input:** one interface, with a live source and a replay source. Core can't tell them apart.
- **Platform layer** (thin, Unity side): where the valley's origin is in the world (planted pose, or a spatial
  anchor), passthrough on or off, gaze source. Core never sees it, which is what keeps VR mode a free fallback (MR5).
- **Views:** render Core state and nothing else, interpolated between ticks.
- **Developer-only code** (logging, recording, stats panel) sits behind one scripting define (T10).
- **Tests:** Unity Test Framework edit-mode tests on Core with synthetic and recorded `HandFrame`s.

## Defaults for gaps in the requirements

Use the default and report what the headset shows. The owner folds results into the requirements at M1.

| # | Gap | Default |
|---|---|---|
| 1 | M1 says Bram crosses, but a one-finger rope is too narrow for his cart | Flat hand makes a plank and Bram crosses. One finger makes a rope: Pip crosses, and it's refused for the cart |
| 2 | Gaps are 12–15 cm with a 2 cm connect range, so a route must be 8–11 cm long; one finger may be too short (estimate) | A route runs fingertip to wrist, through the finger and its metacarpal. M1 gap is 12 cm. Log real lengths, test 15 cm |
| 3 | Where width comes from when a route crosses the palm | The narrowest tier along the walkable route, using the fallback values until H3 is settled |
| 4 | Which API exposes OpenXR joint radii in this stack | Find it in slice 1 and log the values (H3). If missing or noisy, use the fallback |
| 5 | What counts as a flat, resting palm for planting | Palm normal within 20° of straight down or up, fingers extended, palm speed under 3 cm/s for 1 s |
| 6 | Whether a local anchor saves and returns with no INTERNET permission | Assume yes; prove it in airplane mode in slice 12. If it doesn't, Table mode keeps placement but asks to replant each launch, and I9 isn't claimed |
| 7 | Whether head-direction gaze is steady enough for 6° targets | Start at 6°, raise it if GZ2 misfires in H6 |

## Slices, in order

Each slice ends in a build the owner runs on the Quest 2. Fix problems before starting the next one.

| # | Slice | Covers | Done when |
|---|---|---|---|
| 0 | Project boots on Quest 2: hands visible, controllers off, hand tracking declared | T11, T12 | Owner sees both hands in the scene, no controller paired |
| 1 | `HandFrame`, live source, recording, replay source, stats panel, radius log | T13, T17a, T19, H3 | A recorded session replays in the editor with identical Core output in a test |
| 2 | Planting in VR: flat palm for 1 s places and scales a greybox valley of two islands | E21, T6 | Owner plants on a table, on a lap and in the air; replanting works |
| 3 | Route model; Pip stands on a finger and walks it | T1 | Owner moves a hand: no sliding, popping or sinking |
| 4 | Boarding and connecting to anchors, with hand ownership | T2, A3 | No flicker at the thresholds in tests; the other hand can't take over |
| 5 | Build: stillness, section commits, pause, resume, scaffold removal | T3 | Tests cover each T3 rule; a shape change never moves a frozen section |
| 6 | Bake, validate, show refusals; the hand withdraws and the bridge stays | T4, I1, I2 | Bridge within 1 cm of the filtered route; rope and plank clearly differ |
| 7 | Bram and the cart cross; capacity rules apply | Capacity table | Plank passes, rope refused for the cart (default 1) |
| 8 | Save and reload, valley-local | T9 | Quit, relaunch, replant somewhere else: the bridge is intact and Bram can still cross |
| 9 | Tracking loss: ghost hand, re-align, return to checkpoint | T5, T19 | Hide the hand, move it 20 cm, show it: no teleport. Forced-loss test passes |
| 10 | Headset pass: comfort, radii, feel | H1–H3 | Results written down; **owner decides whether M1 is proven** |
| 11 | Six puzzles, waystone, finale and the first visitor, in greybox | V1–V5, A1, A2, R1–R3 | S to the Sanctuary and the Gardener, playable with one hand |
| 12 | Table mode prototype: passthrough, anchor saved and reloaded, airplane-mode check | MR1–MR3, MR5, default 6 | The valley returns to the same spot after a relaunch on Quest 2 |
| 13 | Eyes: need signs on look, look-and-pinch waystone, gaze source on the stats panel | GZ1–GZ3 | Works with head direction on Quest 2; eye path tried in the simulator if installed |
| 14 | Remaining visitors, night dimming, x-ray, settings | R2, MR4, T16, A4–A8 | Each passes its row |

## Headset tests and gates

| ID | Question | How it's settled |
|---|---|---|
| **H1** | Does the Arch work? Set a real rock size, span and anchor heights. | Natural curved hands from 5 testers. If refused, change the rock, span, slope limit, or add end ramps. |
| **H2** | Where is the comfortable hand position, and does planting find it? | Testers plant and play with relaxed shoulders. Final E10 and E6 values come from this. |
| **H3** | Are Quest's joint radii usable for width? | Log them for different hands. If unstable, use the fallback. |
| **H4** | **After three bridges, do players want to make another?** The key question. | Watch unfamiliar players on the real opening. If they need repeated coaching or get bored, fix the core before anything else. |
| **H5** | **Does Table mode earn its place, and its default?** | Quest 2: the matrix cells for planting and anchors are clean (grayscale passthrough, labelled). Quest 3 or 3S tester: the valley stays put, returns after relaunch, hands read well over colour passthrough, 72 fps, and they prefer it to VR. Both pass: default. Quest 2 only: labelled option, or cut. Neither: cut. |
| **H6** | Do eyes help on a headset with no eye tracking? | Testers find GZ1 unprompted and GZ2 doesn't misfire. If look-to-ask confuses or misfires, it's cut and GZ2 stays. |
| **EX3** | Fingertip trace mode: draw a route with one fingertip instead of holding a shape. | Only if it solves an access problem found with a real tester, without weakening the main build. |
| **EX4** | Valley turning between puzzles so the next gap is centred. | Only if the arc layout can't pass V5. |

## Milestones

| # | Milestone | Done when |
|---|---|---|
| **M1** | Core promise on the headset (slices 0–10): plant, Pip boards, builds along your hand, you withdraw and the bridge stays, Bram crosses, it survives a relaunch, recovery works. | Proven on Quest 2 and it *feels* right to the owner. Requirements lock; numbers are set from these results. |
| **M1b** | Send the M1 build to a Quest 3 or 3S tester (T20). | Before M4. fps and dropout rates reported. The target device's numbers take priority over Quest 2's. |
| **M2** | Rough full journey (slices 11–13). | S to the Sanctuary and the first visitor with one hand, in VR mode; Table mode and eyes in prototype. |
| **M3** | Playtest round 1, then decide. | H4, H5 and H6 answered. Visitor designs set. Top 3 friction points fixed. |
| **M4** | Polish in the order a judge sees it: first 5 minutes, finale and first visitor, then the middle (slice 14). | Every shipped item passes P11. |
| **M5** | Final build: full matrix (P12), playtest round 2 on that exact build (T15), video (P4, P7), submission (P5). | No release blockers, T20 done on the final build, evidence and video from that build, by version. |

**Sign-off rule:** any change after M5 reopens M5 for everything it affects. Re-run the affected matrix cells and
release blockers, update the tested build version, and upload that build. If there's no time to re-validate, the
change doesn't ship.

**Cut order,** first to go at the top. Each cut removes the matching claims from the video and text:
MR2 scene snap → MR4 dimming → visitors 3–5 → GZ1 look-to-ask → Table mode → GZ2 look-and-pinch → visitors 1–2 →
Arch → Fork. Never cut: planting, First Bridge, Width, the finale, one-hand play, save and resume, recovery.

## M1 sign-off report

Each slice as Done, Verified (and how), Not verified or Blocked, plus: Quest 2 fps and dropouts per scene; H1–H3
results; route lengths and whether a 15 cm gap works; tolerances changed; corrections the requirements need.

## Dates

Fixed (MST): Wed Nov 11, 11 AM, XR Simulator session (its presenter's name matches the one judge Devpost lists) · Thu Nov 12, 11 AM, Top
Hand Tracking Apps · Fri Nov 13, 9 AM, Playtesting Playground (a build here is the best chance at M1b) · Fri Nov 13,
11 AM, Submission Walkthrough · **Wed Nov 18, 1:00 PM, deadline.** Our target: Nov 18, 1:00 AM.

Proposed (the owner sets them; the Nov 8 date for M1 has passed with no code):

| By (MST) | Goal |
|---|---|
| Wed Nov 11 | M1 proven on Quest 2. Meta app, Competition channel and Devpost draft exist |
| Fri Nov 13 | M2. A build at the Playground for M1b and H5 |
| Sat Nov 14 | M3 decisions: H4, H5, H6 |
| Sun Nov 15 | Feature freeze. Reliability, polish, video and text only from here |
| Mon Nov 16 | M4, final build candidate |
| Tue Nov 17 | M5: matrix, round 2, video, text, upload, invite link tested from a second account |
| Wed Nov 18, 1 AM | Submitted |

This plan holds more than nine days comfortably allow. The cut order is how it stays honest: a late M1 moves every
line below it, and cuts are taken from the top of the cut order, never from polish on what ships.
