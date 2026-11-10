# Owner test sheets

**Status: procedures prepared; every headset/simulator result is Not run.** S11 is documentation,
not M1 sign-off. Run slices in order on Quest 2; fix each failed slice before moving to the next.
The owner handles installation, headset actions, consent and judgment. Stop for discomfort.

Use the current reviewed build from `main`, not the historical branches named in the old Slice 0
guide. On this PC Unity is already installed/licensed: use `../unity-setup.md` and `tooling.md`.
Do not reinstall, sign in or accept terms as part of these checks. Configure only the owner's folder.

For each sheet, copy this record into an owner result file:

| Field | Record |
|---|---|
| Result | Not run / Pass / Fail / Blocked |
| Verification | Quest 2 (never infer Quest 3/3S) |
| Date; build version; full revision; APK hash | Not recorded |
| Mode; lighting; planting; hand; scale; lifecycle | Not recorded |
| Controller state; consent if recording | Not recorded |
| Steps passed; failed step and expected/observed result | Not recorded |
| fps; per-puzzle total/disruptive dropouts | Not recorded |
| Anonymous tester number; new to hand tracking; help | Not recorded |
| Evidence reference (kept outside Git) | Not recorded |

Use good lighting and a comfortable seated position initially, with controllers off. A missing feature
is **Blocked**, never Pass. Developer recording needs verbal consent and local USB export/deletion;
target-device stats-only sessions record nothing. Release tests use the actual submission APK.

## Slice 0 — boot and hands

Use [`../slice-0-quest2.md`](../slice-0-quest2.md)'s headset checks, with the current installed SDK path
from `unity-setup.md`. Both hands must follow the correct side with controllers off. No later gameplay
is implied. Record the result before Slice 1.

## Slice 1 — input, replay, radii (T13/T17a/T19/H3)

Prerequisite: live hand adapter/clock and developer recording wired into the scene.

1. With verbal consent, begin a local developer session. Show left and right hands in turn and flex
   every finger. Read fps, gaze source, mode and version; ensure the actual source revision is shown.
2. During a supported traveler/build, hide the active hand for less than 250 ms, then for at least
   250 ms. Verify only the second qualifies for T19; log joints, direction and duration for both.
3. Export locally by USB. Replay on the exact same build in the editor, comparing gameplay decisions
   and tick/event order, not just a similar animation. Label that comparison **Unity editor**.
4. Record each joint radius and validity for several poses/hand sizes, without assuming radii exist.
   Repeat with missing/unusable radii and confirm the documented fallback. Delete recording after analysis.

Pass: live input is correctly mapped, replay decisions match, stats and dropout counters are meaningful.
H3 is a measurement report, not an automatic choice of width source.

## Slice 2 — plant and replant (E21/T6)

1. Fresh start: observe the seed-lantern and one ghost-palm demonstration without instructions.
2. Rest a flat palm for 1 s on a table. Repeat on lap tray and in air, each hand separately.
3. Build a bridge, note its shape, then replant at a different position, height and facing.
4. Repeat with different hand sizes and adjusted world scale. Do not stretch the player's reach to fit.

Pass: placement/facing/scale follow planting, bridges retain valley-local shape, no progress is lost.
Record time to plant; E21 needs 4/5 uncoached within 20 s. Note H2 comfort and supported forearm.

## Slice 3 — hand contact (T1)

1. Let Pip board the fingertip and walk across each available finger/palm surface.
2. Slowly translate/rotate the hand, then change finger curvature while Pip stands on a segment.
3. Inspect from above and the side, both hands, default and adjusted scale.

Pass: feet stay on the displayed surface with no sliding, popping, sinking or arbitrary jumping.
Record any joint/segment/direction that fails. A headless cursor test cannot replace this pass.

## Slice 4 — connection and ownership (T2/A3)

1. Approach an anchor, hold below 2 cm for 150 ms, then hover around that threshold.
2. Move out to 3 cm, then beyond 4 cm. Repeat slowly and with mild natural tremor.
3. While the active hand supports Pip/builds, bring the other hand closer. Try to steal/recover control.
4. Complete/cancel/recover, then engage with the other hand from idle.

Pass: no flicker; connection survives to the break threshold; ownership never changes mid-interaction;
either hand can engage after release. Log distances from diagnostics, not visual estimates alone.

## Slice 5 — build sections (T3)

1. Connect a flat route and hold still for 2–3 s; watch preview/capacity and hammer progress.
2. During another build, change a distal finger after an earlier section freezes. Note frozen posts.
3. Move more than the allowed tolerance, then reconnect within 2 cm of the last frozen section end.
4. Break beyond 4 cm; wait 10 s without reconnecting, then repeat by starting elsewhere.

Pass: moving never accidentally finishes a build, committed sections never move, pause keeps progress,
reconnection resumes, abandoned scaffold visibly comes down. Harmless tremor does not reset progress.

## Slice 6 — persistent bridge and refusals (T4/I1/I2/I6)

1. Build one-finger and flat-hand routes, then withdraw. Compare frozen geometry against the displayed
   filtered route at each section's commit; use a replay/diagnostic overlay to measure the 1 cm bound.
2. Change the live hand far away. The finished bridge must keep its shape and remain usable.
3. Present unanchored, too narrow/steep/long, blocked and self-crossing routes using the test scene.
4. Capture a greyscale view: rope/plank, capacity, refusal and connection must differ by shape/motion.

Pass: persistence and measured shape bound; joint posts/tip knots show the held hand; every refusal
identifies its failing segment; no visible bake hitch. Synthetic invalid routes remain labelled headless.

## Slice 7 — cart capacity

1. Build a rope walk. Pip can cross; Bram and the cart must hesitate/refuse it, never fall.
2. Replace with a flat-hand plank. Withdraw and watch Bram/cart complete the crossing.
3. Try a wide but steep or low-clearance bridge: width alone must not accept the cart.

Pass: full cart rule (≥2.5 cm width, ≤25° slope, ≥5 cm headroom), readable body language, zero deaths.

## Slice 8 — save/resume (T9)

1. Save after a bridge completes; remove headset and resume, then quit/relaunch.
2. Repeat mid-build and while a traveler is hand-supported. Return the hand 20 cm away.
3. Replant at a different pose, then restart the app. Compare every saved bridge/checkpoint/progress.
4. Run the corruption/backup case through the developer harness; label its result separately.

Pass: no bridge/progress loss; live support requires ghost realignment; no traveler attaches to a new
arbitrary hand. In VR, no claim of physical-world persistence; Table anchors are Slice 12.

## Slice 9 — tracking loss (T5/T18/T19)

1. Lose tracking while walking, building and carrying. Observe freeze/grip and the still recovery ghost.
2. Return the owning hand 20 cm away; it must not capture the traveler. Return to within 2 cm,
   hold 300 ms and watch the 200 ms blend. The other hand must not realign for it.
3. Repeat without realigning for 8 s. The traveler visibly returns to its last safe island;
   partial scaffold comes down and ownership releases.
4. Continue poor tracking for 20 s; check the exact short lighting prompt and one-step stillness
   loosening icon. Connections must retain 2/4 cm thresholds. Distinguish moving hint and still ghost.

Pass: no teleport, death or new checkpoint from recovery. Attach the exhaustive forced-loss headless
regression result separately; a few manual spots cannot prove every point on every route.

## Slice 10 — M1 headset judgment (H1/H2/H3)

1. Run the complete M1 promise: plant → board → build → withdraw → Bram → relaunch → recovery.
2. H1: five testers try natural curved routes over recorded rock bounds/span/anchor heights.
   Record valid/refused geometry and any proposed ramps/tolerance change; owner decides.
3. H2: table/lap/air, both hands and reach directions; note shoulder strain and unsupported-arm time
   (about 60 s maximum between rest beats), lengths and whether a 15 cm gap works.
4. H3: review Slice 1 radius evidence, stability and fallback. Record Quest 2 fps and dropout counts.

Pass only when the owner signs `m1-signoff.md`. Quest 2 M1 does not certify Quest 3/3S performance.

## Slice 11 — six puzzles, waystone, first visitor

1. Fresh left-only run: Board → First Bridge → Width → Fork → Arch → Finale. Repeat right-only.
2. Confirm the First Bridge ledge forces a rope; hammer widening enables the finale rebuild in every run.
3. Follow Bram across exactly S→A, A→C, C→Sanctuary. Width must already satisfy the cart's full rule.
4. A bad Fork branch is refused independently; Arch accepts any clearing/walkable geometry.
5. Use every waystone action by poke. Finish the Gardener, save/relaunch and see the living valley.
6. In a controlled developer save, invalidate each needed bridge in turn. The route check must offer
   a rebuild and preserve completion, never silently use the corrupt edge.

Pass: one-handed journey/visitor, no softlock, all six puzzles add their intended idea, zero deaths.
Log first boarding, persistent bridge time, hints, refusals, chapter time and help per anonymous tester.

## Slice 12 — Table mode and anchors (MR1–MR3/MR5)

1. Owner grants only shipped-mode permissions. Plant on tables of different heights, on lap and in air.
   Repeat without scene data; no room scan/Space Setup should be requested.
2. Build/save, quit and relaunch ten times across two days. Measure return error (≤2 cm), or confirm
   replant is requested within 5 s when localization fails; bridges must survive either outcome.
3. Move the table, repeat, and verify the valley never ends up unreachable. Test replant/recalibration.
4. Owner enables airplane mode and repeats local anchor save/restore with the release APK.
5. Switch VR↔Table, deny/unavailable passthrough and load each mode's save into the other.

Pass: full game/save compatibility, safe fallback, correct local persistence without network.
Quest 2 passthrough is grayscale. H5 default status needs separate Quest 3/3S evidence and preference.

## Slice 13 — eyes/waystone (GZ1–GZ3/E9)

1. On Quest 2 (head direction), look at a traveler for 300 ms; see its need sign, then look away.
   Disable gaze and solve every puzzle without it.
2. Use every waystone action by look-and-pinch with the hand resting palm down/sideways, each hand alone.
3. Read gaze source in stats. Each visible row has at most four targets, ≥6° widths and ≥1° gaps.
4. Run ten full menu/game sessions, counting unintended selections and accidental system-menu opens.
5. If already installed/owner-authorized, exercise real-eye source in the simulator separately.

Pass: no required gaze puzzle, no misfires/system clashes, correct source label. Never call head-direction
results eye tracking or label simulator results hardware tests. Owner decides H6.

## Slice 14 — visitors, night, settings, x-ray

1. Call all five visitors by bell with no time gate. Complete each with either hand alone, verify the
   proper need/rebuild, one new sanctuary window and bridge mark, then save/relaunch.
2. Check each puzzle's night step; Table edge remains visible. Finale light follows every built bridge.
3. Reduced motion: no induced camera moves/sway/bursts; dimming fades. High contrast: all needed states
   readable over a cluttered table. Captions: every meaningful sound has an in-world caption.
4. Change three stillness steps/world scale, pause/resume/restart/replant/mode/quit and restore settings.
5. X-ray shows graph, 2/4 cm thresholds, curve tree, frozen/uncommitted sections and progress. Toggle off.
   Release still has x-ray and has no developer stats/recording code.

Pass: every shipped visitor/feature complete; run P12/P3 on the exact final build. Apply the cut order
to unfinished features; do not mark a reduced/cut test as Passed.

## Final evidence and target devices

Use `quest2-matrix.md` for all 324 Quest 2 cells. Check every P3 blocker in every cell, and perform
ten cold starts in a row for each shipped device/mode. Quest 3 and Quest 3S each need a full Chapter 1
in each shipped mode on the same submission code/build, controllers off, plus Competition invite install.
Record fps, per-puzzle dropouts, exact version and any limitations separately by device.
