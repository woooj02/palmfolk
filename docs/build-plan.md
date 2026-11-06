# Palmfolk: build plan

How to build what `requirements-v1.2.md` defines. The requirements say what must be true; this file says what to do
next. If they disagree, stop and ask the owner. Requirement IDs (T3, E9, H1) refer to that doc.

**Current goal: M1 only.** Prove the core promise on a Quest 2. Don't start M2 until the owner signs off M1.

## Ground rules

- **Scope:** no features beyond the requirements. EX1–EX4 wait until after M1. Requirements can be corrected or cut
  once the build has started, by the owner only.
- **New work:** the rules allow no pre-existing code. Don't bring in code from earlier projects. Unity and Meta
  packages are dependencies. Don't copy Meta sample code or any third-party code or asset into the project without
  asking the owner, and record the licence of anything approved.
- **Honesty:** never report something as working on a headset unless it ran on one. Quest 2 results are labelled
  Quest 2, and they never stand in for Quest 3/3S results.
- **Commits:** authored by the owner, with plain messages and no tool credit lines, in commits, PRs or files.
- **Owner only:** pushing, accepting terms, creating the Meta app, creating or storing the signing key, headset
  testing, and judging how it feels. Commit locally and ask for the rest.
- **Keep out of the repo:** keys, keystores, `.env`, playtest recordings and APKs. Read `git status` and the diff
  before every commit.

## Setup

- Build on Windows. Linux isn't officially supported for the Meta SDK.
- Unity 6000.0.66f2 or later in the 6000.0 line, with Android Build Support, OpenJDK and Android SDK & NDK Tools. Pin
  the exact version in `ProjectSettings/ProjectVersion.txt`.
- Meta XR All-in-One SDK v207 on OpenXR. Interaction SDK for UI only (T11).
- The Unity project lives at the repo root. The existing `.gitignore` already covers Unity's generated folders.
- Package name: `com.keepsrunning.palmfolk` (default; the owner confirms before the Meta app is created).

## Architecture

```
Headset hand tracking ─┐
                       ├─► HandFrame ─► Core, fixed 60 Hz tick ─► state ─► Views, every rendered frame
Recorded session ──────┘    (joints,     route graph, build,               (interpolate between ticks)
                            radii,       validation, travelers,
                            validity)    recovery, save data
```

- **Core** (`Assets/Palmfolk/Core`, own assembly): plain C#. Takes `HandFrame`s and gives the same decisions for the
  same input. No PhysX, no scene objects, no wall-clock time (T17a).
- **Input:** one interface, with a live source and a replay source. Core can't tell them apart.
- **Views:** render Core state and nothing else. Logic runs at 60 Hz and Quest 2 displays at 72 Hz or more, so
  interpolate or travelers will visibly stutter on the hand.
- **Developer-only code** (logging, recording, stats panel) sits behind one scripting define, so a release build
  compiles it out (T10).
- **Tests:** Unity Test Framework edit-mode tests on Core, using synthetic and recorded `HandFrame`s. This is how Core
  gets checked without a headset.

## M1 scope

**In:** one greybox scene in VR with two islands and one gap, Pip, Bram with the cart, capsule or box stand-ins,
the SDK's default hand visual, and a small stats panel (fps, dropouts, build version).

**Out:** art, audio, puzzles 3–8, forks, the waystone menu, calibration (T6), world scale, x-ray, filming replay
(T17b), passthrough.

## Defaults for gaps in the requirements

The requirements don't settle these. Use the default, and report what the headset shows. The owner folds the results
into the requirements at M1.

| # | Gap | Default for M1 |
|---|---|---|
| 1 | M1 says Bram crosses, but Bram's cart needs 2.5 cm width and a one-finger rope is narrower | Flat hand makes a plank and Bram crosses with the cart. One finger makes a rope: Pip crosses it, and it's refused for the cart |
| 2 | Active gaps are 12–15 cm with a 2 cm connect range, so a route must be at least 8–11 cm long. One finger from tip to knuckle may be too short (estimate, not measured) | A route runs from fingertip to wrist, through the finger and its metacarpal. M1 gap is 12 cm. Log real route lengths and test 15 cm |
| 3 | Where width comes from when a route crosses the palm | Width is the narrowest tier along the walkable route, using the fallback values until H3 is settled |
| 4 | Which API exposes OpenXR joint radii in this stack isn't confirmed | Find it in the first slice and log the values (H3). If it's missing or noisy, use the Definitions fallback |

## Slices, in order

Each slice ends in a build the owner runs on the Quest 2. Fix problems before starting the next one.

| # | Slice | Covers | Done when |
|---|---|---|---|
| 0 | Project boots on Quest 2: hands visible, controllers off, hand tracking declared | T11, T12 | Owner sees both hands in the scene, with no controller paired |
| 1 | `HandFrame`, live source, recording, replay source, stats panel, radius log | T13, T17a, T19, H3 | A recorded session replays in the editor and gives identical Core output in a test |
| 2 | Route model; Pip stands on a finger and walks it | T1 | Owner moves a hand: no sliding, popping or sinking |
| 3 | Boarding and connecting to anchors, with hand ownership | T2, A3 | No flicker at the thresholds in tests; other hand can't take over |
| 4 | Build: stillness, section commits, pause, resume, scaffold removal | T3 | Tests cover each T3 rule; a shape change never moves a frozen section |
| 5 | Bake the bridge, validate it, show refusals; hand withdraws and the bridge stays | T4, I1, I2 | Bridge within 1 cm of the filtered route; rope and plank clearly differ |
| 6 | Bram and the cart cross; capacity rules apply | Capacity table | Plank passes, rope refused for the cart (default 1) |
| 7 | Save and reload bridges | T9 | Quit and relaunch: the bridge is intact and Bram can still cross |
| 8 | Tracking loss: ghost hand, re-align, return to checkpoint | T5, T19 | Hide the hand, move it 20 cm, show it: no teleport. Forced-loss test passes |
| 9 | Headset pass: comfort position, radii, feel | H2, H3 | Results written down; owner decides whether M1 is proven |

## M1 sign-off

M1 is done only when the owner says it feels right on the headset. Tests alone don't prove it. The report lists each
slice as Done, Verified (and how), Not verified or Blocked, plus:

- Quest 2 fps and dropouts per scene, labelled Quest 2
- H3 result: radii usable or fallback used
- H2 result: the comfortable hand position
- Route lengths and whether a 15 cm gap works (default 2)
- Tolerances changed from their starting values
- Corrections the requirements need

Then the M1 build goes to a Quest 3/3S tester (M1b).

## Dates (proposed; the owner sets them)

Deadline: Nov 18, 12:00 PM PT = 1:00 PM MST. Our target: Nov 18, 1:00 AM MST.

| By (MST) | Goal |
|---|---|
| Sun Nov 8 | M1 proven on Quest 2 |
| Tue Nov 10 | M2: rough journey playable with one hand |
| Thu Nov 12 | M3: playtest round 1, H4 answered, E14 decided |
| Fri Nov 13 | M1b at the latest, using the Playtesting Playground if it fits |
| Sat Nov 14 | Feature freeze. Reliability, polish, video and text only from here |
| Mon Nov 16 | M4 done, final build candidate |
| Tue Nov 17 | M5: test matrix, round 2, video, text, upload to the Competition channel |
| Wed Nov 18, 1 AM | Submitted |

If M1 isn't proven by Nov 10, stop and have the owner re-plan which puzzles ship (P11 ship rule).
