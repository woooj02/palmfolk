# Palmfolk: team plan

Two developers work in parallel and merge to `main` on GitHub: the **Lead** and the **Partner**. Both are equally
capable. The split is by kind of work, not by skill: the Lead takes the Core simulation, geometry and SDK platform
work, where one mistake breaks everything downstream. The Partner takes everything the player sees and hears, the
content, the tooling and the evidence. Each side is about half the hours.

Read first, in this order: [`requirements-v1.3.md`](requirements-v1.3.md),
[`requirements-v1.3-quality.md`](requirements-v1.3-quality.md), [`build-plan.md`](build-plan.md),
[`m1-gameplay.md`](m1-gameplay.md) (what Core already does and its integration ports), then this file. IDs such as
T3, E21 or MR3 refer to the requirements. If this file and those disagree, they win.

## Where things stand (Nov 10)

- `main` has the v1.3 requirements and build plan, plus the M1 gameplay core merged from `m1-gameplay`: hand
  routes, connection and ownership, section-commit builds, validation and refusals, recovery, saves, fixed-tick
  replay and developer metrics. 59 developer-mode and 51 release-mode headless tests pass with .NET 8 on Windows.
- That core was written against v1.2: one gap, one bridge, world-space anchors. v1.3 needs a valley of islands,
  planting, six puzzles, visitors, gaze and Table mode.
- Nothing has run in Unity or on a headset yet. Unity is not installed on the owner's PC yet.
- Deadline: **Wed Nov 18, 1:00 PM MST.** Team target: Nov 18, 1:00 AM. Dates are in the build plan.

## Rules for both

- **Requirements are frozen.** Don't edit `requirements-*.md` or `build-plan.md`. `tools/check_source.py` pins
  their hashes. Gaps and questions go in your status file (below) for the owner.
- **Scope:** nothing beyond the requirements. When something must give, use the build plan's cut order.
- **Honesty:** label every result as headless, Unity editor, simulator, or Quest 2. Never call something working on a
  headset unless the owner ran it there. Quest 2 never stands in for Quest 3/3S.
- **Original work only:** no Meta sample code and no third-party code or assets without asking the owner. Record the
  licence of anything approved in `THIRD-PARTY-NOTICES.md`.
- **Commits:** same author identity as the existing history, plain messages, no credit or co-author lines, and no
  tool or assistant names in commits, branches, PRs or files.
- **Keep out of Git:** keys, keystores, `.env`, APKs, playtest recordings. Read `git status` and the diff first.
- **Owner only:** installing tools, accepting terms, the Meta app, the signing key, headset tests and judging feel.
  Ask in your status file; keep working on what isn't blocked.

## How we merge

1. Work in **your own clone** (for example `E:\palmfolk-partner`), never in the other developer's folder.
2. One work item, one branch, named `area-topic` (`core-planting`, `views-bridge-mesh`). Branch from fresh `main`.
3. Before merging: rebase on `main`, then run both test configurations:
   ```bash
   dotnet test tools/CoreChecks/Tests/Palmfolk.Tests.csproj --configuration Release
   dotnet test tools/CoreChecks/Tests/Palmfolk.Tests.csproj --configuration Release -p:DeveloperFeatures=false
   ```
   Once Unity is installed, also compile in batch mode (P10 adds the script). CI (`Source checks`) must pass.
4. Merge small and often, at least once a day. With the GitHub CLI: open a PR and merge it when CI is green.
   Without it: `git merge --no-ff` locally, re-run the tests, push `main`.
5. A change inside the other developer's paths needs their review. Say why in the PR or commit message.

### Who owns which paths

| Lead | Partner | Shared (small changes, mention them) |
|---|---|---|
| `Assets/Palmfolk/Core/**` except `Content/` | `Assets/Palmfolk/Views/**`, `UI/**`, `Audio/**`, `Art/**` | `Packages/manifest.json`, `ProjectSettings/**` |
| `Assets/Palmfolk/Platform/**` (input, valley origin, anchors, passthrough, gaze) | `Assets/Palmfolk/Core/Content/**` (valley, puzzle and visitor data) | `Assets/Palmfolk/Developer/**` (Lead: recording; Partner: stats panel) |
| `Assets/Palmfolk/Tests/EditMode/**` (Core rules) | `Assets/Palmfolk/Tests/Fixtures/**`, `Tests/Content/**` | `Assets/Scenes/**` (scene changes go through one owner at a time) |
| `tools/CoreChecks/**` | `Assets/Palmfolk/Editor/**`, `tools/**` (other), `.github/**` | `README.md`, `docs/m1-progress.md` |
| `docs/core-api.md` | `docs/testing/**`, `submission/**`, `THIRD-PARTY-NOTICES.md` | |

### The contract between us

Core never references Unity. Views only read Core state; they never change it. Core talks to views through
`IPalmfolkView` / `WorldPresentation` and the `GameEvent` stream (see `m1-gameplay.md`, ports 2 and 3). The Lead owns
those types and documents every change in `docs/core-api.md` before merging it. Views built against today's M1
`WorldPresentation` stay valid: the valley version adds fields and doesn't remove them. Content files use the data
types the Lead defines in L4. Until those land, write layout numbers in `docs/testing/valley-layout.md`.

### Status files

Each developer keeps their own file and updates it with every merge: `docs/status-lead.md` and
`docs/status-partner.md`. Each lists done, in progress, blocked, questions for the owner, and verification labels.
Separate files mean no merge conflicts. Read the other file before starting each work item.

## Lead work

In order. Items marked **Unity** need the editor installed; the rest are headless and tested with `dotnet test`.

| # | Item | Covers | Done when |
|---|---|---|---|
| L1 | Merge `m1-gameplay` into `main` under v1.3. Repoint the doc hash pins. Write this plan | — | `main` has the core, tests and CI pass. **Done Nov 10** |
| L2 | **Valley-local space and planting.** Flat-palm detector (default 5), valley pose (place, height, facing), hand-size calibration from bone lengths to world scale, replanting that keeps every bridge | E21, T6, A4, slice 2 | Tests: plant on table, lap and air poses; replant mid-puzzle keeps bridges bit-for-bit in valley space |
| L3 | **Valley model and puzzle director.** Replace the single-gap `M1Simulation` with islands, zone anchors and edges; traveler kinds (walker, group, cart); six-puzzle state machine; ledge widening (V2); Bram's three-crossing route (V1); softlock check (V3). Publish the data types for Partner's content | V1–V3, E4, slice 11 core | All M1 tests still pass; Board → Finale playable headless with fixtures; V3 corruption test passes |
| L4 | **Fork and Arch geometry.** Spread fingers become a branching curve tree (gap under 1 cm merges); per-branch width, slope and clearance; slope refusal per branch; Arch clearance over the rock (H1 numbers stay configurable) | T4, I1, I7, H1 | Fork: one steep branch refused, the other passes. Arch: any clearing route passes, any blocked one fails |
| L5 | **Save v2.** Every bridge's curve tree in valley space, ledge state, visitor progress, lit windows, mode, anchor ID, settings. Migrates v1 saves | T9, MR3 | Round-trip and corruption tests; a v1 save loads |
| L6 | **Live input and the clock (Unity).** XR Hands → `IHandFrameSource` with explicit joint mapping, validity, confidence and radii (H3 log); `FixedTickRunner` driver; valley origin from the planted pose | T1, T12, T17a, H3, slices 0–1 | The owner sees Core reacting to real hands on Quest 2; a recording replays with identical decisions in the editor |
| L7 | **Recovery across the whole valley.** T5 for every route and traveler; forced-loss test at every point of every route; T18 quality prompts | T5, T18, T19 | Automated: no traveler ever reaches an island it hadn't reached before |
| L8 | **Visitors and the living valley.** Need evaluation against existing bridges; rebuild flow (old bridge kept until the new one passes); bell; windows; travelers walking your bridges on relaunch | R1–R4, E16 | All five visitors completable one-handed, headless; V3 check before each visitor |
| L9 | **Table mode platform (Unity).** Passthrough on and off, spatial anchor saved at planting and found within 5 s or replant, airplane-mode check (default 6), automatic VR fallback | MR1–MR3, MR5, slice 12 | The valley returns to the same spot after relaunch on Quest 2 (owner-run) |
| L10 | **Gaze (Unity + Core).** ISDK gaze source, real or head-synthesized (GZ3); 300 ms look → need-sign event; look-and-pinch targeting for Partner's waystone | GZ1–GZ3, E9, slice 13 | Works with head direction on Quest 2; gaze off leaves every puzzle solvable |
| L11 | **Performance and release safety.** Profile the tick and the bake (no hitch), allocation-free hot paths, release build compiles out recording | T7, T10 | 72 fps headroom on Quest 2 noted as Quest 2; release checks pass |

The Lead also reviews any Partner change that touches Core types or the save format.

## Partner work

In order. P1, P7, P10–P12 need no Unity, so start there today.

| # | Item | Covers | Done when |
|---|---|---|---|
| P1 | **Synthetic hand fixtures.** A generator for `HandFrame` sequences: one finger, flat hand, spread, curved hand; small, medium and large hands; tremor noise; tracking dropouts; withdraw motions. Both developers' tests use it | T1–T5 tests | Lead's L2–L4 tests use it; deterministic from a seed |
| P2 | **Views, greybox (Unity).** `IPalmfolkView` MonoBehaviour: islands, Pip, Bram and cart, apprentice; live route; see-through preview with capacity icon; scaffold and frozen sections; bridge built from the curve tree (planks vs rope, knuckle posts at joints, knots at fingertips); refusal glow plus shape and motion; ghost hand; a hint hand that looks clearly different. Interpolated between ticks | I2, I6, E12, E17, A7, T4, T5, E8 | Driven by headless replays in the editor; rope and plank clearly differ in a greyscale capture |
| P3 | **Stats panel and x-ray (Unity).** Developer stats: fps, dropouts per puzzle, gaze source, mode, build version, behind `PALMFOLK_DEVELOPMENT`. X-ray, which ships: route graph, thresholds, curve tree, frozen vs uncommitted, progress | T16, T20, GZ3 | Toggle works; release build has no stats panel |
| P4 | **Waystone and settings (Unity).** Poke with any finger of either hand; at most four targets in a row, each at least 6° wide with 1° gaps; pause, resume, restart, replant, mode, settings, x-ray, bell, quit. Settings: stillness tolerance (3 steps), world scale, reduced motion, captions, high contrast. Settings fields go to the Lead for Save v2 | A1, A4–A6, A8, T8 | A full session's menu actions with the left hand only, then the right only |
| P5 | **Audio and captions.** `GameEvent` → cue map; footsteps placed at the finger and timed to steps; hammer taps tied to build progress; connect chime; refusal; visitor arrival; in-world captions for each. Placeholder sounds must be original or licence-checked | P2, A6, E17 | Every cue has a caption; sound alone tells you a bridge finished |
| P6 | **Travelers' body language.** Greybox rigs and an animation state map from Core state and events: wave, hesitate, test a plank, crouch and grip, hammer, celebrate, look back, head shake on refusal; contact shadows | E5, E7, P2 | Each state is readable at 3 cm in the editor |
| P7 | **Valley content.** Layout that passes V5 (arc around the palm, 12–15 cm gaps, 60 × 45 cm footprint), island heights for E6, anchors placed per T14, the rock, the narrow ledge; six puzzles and five visitors as data once L3's types land | V1, V5, E6, T14, R2 | A content test walks Bram's route with the cart's rules; every gap reachable seated |
| P8 | **Onboarding and startup visuals (Unity).** Seed-lantern, ghost palm demo, put-down-controllers animation, the short E19 and T18 text lines | E21, E19, T18, P8 | Three startup states shown correctly in the editor |
| P9 | **Night, light and the VR valley (Unity).** Night valley environment for VR mode; dimming one step per puzzle (MR4, never hiding the table edge); finale light-run along every bridge; five sanctuary windows | MR4, MR5, E13, R3 | Reduced motion makes dimming fade instead of pulse |
| P10 | **Tooling and CI.** Batch-mode Unity compile and Edit Mode test script; release manifest check (no INTERNET, no recording symbols, only allowed permissions); build version stamped with the source revision; APK build script for the owner | T10, T17a | CI and the scripts run; release checks fail on a planted bad manifest |
| P11 | **Owner test sheets.** Quest 2 steps for each slice 1–14 (like `slice-0-quest2.md`), the P12 matrix sheet, the E18 FoV check procedure, the M1 sign-off report template | P12, E18, M1 | The owner can run each slice test without asking |
| P12 | **Submission pack.** `submission/devpost-draft.md` (P5 fields), credits and licence notices (P9), the video shot list (P4), screenshot list | P4, P5, P9 | Every field has a draft; claims marked with their device |

## Order of work

| When (MST) | Lead | Partner | Owner |
|---|---|---|---|
| Nov 10 | L1 done; L2 | P1, P7 layout numbers, P10 CI pass | Install Unity 6000.0.66f2 with Android modules; slice 0 on Quest 2 |
| Nov 11 | L3, L6 once Unity is in | P2 views on M1 state, P11 | M1 checks on Quest 2; Meta app and Competition channel; Devpost draft |
| Nov 12 | L4, L5 | P2, P6, P7 content data | Slice tests as they land |
| Nov 13 | L7, L8 | P4, P5, P8 | Playground build (M1b, H5) |
| Nov 14 | L9, L10 | P3, P9 | M3 decisions: H4, H5, H6 |
| Nov 15 | L11, fixes | P12, fixes | **Feature freeze** |
| Nov 16–17 | Release blockers, fixes from tests | Release blockers, polish in judge order, video support | Matrix, round 2, video, upload |

The build plan still gates sign-off: M1 must be proven on Quest 2 by the owner, and anything not finished and tested
by the deadline is cut (P11), following the cut order.
