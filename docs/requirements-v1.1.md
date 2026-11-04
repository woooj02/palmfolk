# Palmfolk: Requirements v1.1 (final, competition scope frozen)

**Meta VR Start Developer Competition 2026 · Gaming track · New Experience division**
**Nov 4, 2026 · Deadline: Nov 18, 2026, 1:00 pm MST**

**Status:**
- **Concept:** locked.
- **Feature set:** frozen. Corrections and removals are allowed; additions are not.
- **Requirements:** they lock once Milestone 1 proves the core promise on the headset.
- **Decisions:** all made. Two items marked ⚑ are pending, but neither blocks the build.

---

## Version history

| Version | What changed |
|---|---|
| v0.2 | Added: E9 (system gesture), E10 (camera zone), E11 (scale), T12 (manifest), T13 (logging). Revised: T1, T3, T4 and the Span rule. |
| v0.3 | Added: I5–I7, E12–E16, T14–T16, P6–P10. Fixed: rope-walk contradiction; Span moved to Chapter 2; wrist tag moved to the back of the wrist. Chapter 1 set to 8 puzzles. |
| v0.4 | Added: I8, E17, T17, T18, P11. Dam replaced Slope. Feature list frozen. |
| **v0.5** | **External audit applied.**<br>• **Finale route:** the full route is mapped and checked (V1–V4).<br>• **Bridge capacity:** comes from geometry, not type labels.<br>• **I7:** now judges the result, not the gesture.<br>• **Accessibility:** one-hand play is now a requirement (A1–A7), with a menu either hand can reach.<br>• **Bridge snapshot:** when and how it's taken is defined (T3).<br>• **Branching bridges:** structure defined (T4).<br>• **Tracking:** T5 recovery sequence; T18 rewritten as tracking-quality recovery.<br>• **Replay:** T17 split into a test replay and a filming replay.<br>• **Build order:** P11 replaced by a milestone order.<br>• **Dam and Shelter:** must earn their place by playtest.<br>• **Measurement:** dropout definition, release blockers, test matrix, evidence rules, privacy split.<br>• **Scores:** projected judge scores removed and replaced with a status table.<br>• **Two contradictions fixed:** Multi-segment was listed in both chapters; the finale twist disappeared if the player built a plank in puzzle 2. |
| **v1.0** | **Final.** The I7 design check is kept. Character names and hand material are the owner's choice. Licensing is MIT. Base building is out of the competition build; Option B will be revisited only if time allows after M5. |
| **v1.1** | **Corrections from the second audit; scope frozen.**<br>• **Licensing:** MIT applies to original code only. Dependencies keep their own licenses.<br>• **Devices:** M1 is proven on Quest 2, and an early check on Quest 3/3S is required before M4.<br>• **T5:** the fallback goes to the last checkpoint the traveler safely reached, not the nearest island.<br>• **A3:** a hand keeps control for the whole interaction.<br>• **Sign-off:** any change after M5 reopens M5. |

---

## Reality check

No entry is a sure thing. Last year there were 650+ submissions, judging is subjective, and studios enter too.

Judges score the experience that works on their headset, not the document. This doc sets what has to be true. Only a build and playtests can show that it is.

## Prior art judges will recognize

- **Tiny Castles (Meta):** tiny characters plus hands.
- **Silhouette:** hand shadows guide creatures.
- **Make with Puppet:** finger puppets; also entered in this competition.

Palmfolk has to read as **"your hand is the world"** in its first shot.

---

## Concept

**Tiny travelers walk across your fingers, and the bridges they build stay after you let go.**

- **Pip, the scout:** light and curious. Rides your moving palm and walks your fingers. Builds a bridge in your hand's shape while you hold still.
- **Bram, the lantern-keeper:** old and careful. Pulls the lantern cart, and only crosses finished bridges that are wide enough.
- Names: owner's choice.

**Story (no words):** night is falling, and the lantern has to reach the sanctuary. Darkness advances one step for each solved puzzle, never on a clock.

**Core loop:**
1. Get the scout across.
2. Shape a route with your hand.
3. Hold still for 2–3 s while Pip builds.
4. Withdraw your hand. The bridge stays.
5. The carrier crosses.

**Product shape:** a handcrafted puzzle adventure. The sanctuary is a visual reward with no building choices, economy or upgrades.

**Base building** (choosing and upgrading buildings) is out of the competition build. It will be reconsidered only if time is left after M5, and adding it reopens M5 (see Decisions).

### Shape decides the bridge; geometry decides what it can carry

Each bridge is classified by its **measured geometry**, not by which gesture was detected. The names below make the shapes easy to read. What a bridge can carry comes **only** from its width, slope and clearance.

| Typical hand shape | Resulting geometry | Typical name | Typical capacity |
|---|---|---|---|
| One extended finger | Narrow, single path | Rope walk | One walker at a time |
| Flat hand, fingers together | Wide, single path | Plank road | Groups and the lantern cart |
| Fingers spread | Branching paths | Forked route | One walker per branch |
| Curved hand | Raised middle section | Arch | Depends on width. A narrow arch carries one walker; a wide arch can carry the cart. |

**Capacity rules (starting values, tuned in playtests):**

| Traveler | Minimum width | Maximum slope | Minimum clearance |
|---|---|---|---|
| Walker (Pip, apprentice, single carrier) | 1.0 cm | 35° | 3.5 cm |
| Group | 2.5 cm | 25° | 3.5 cm |
| Lantern cart | 2.5 cm | 25° | 5.0 cm |

The cart's slope limit equals the group's slope limit. That way, any plank road accepted in puzzle 4 is also valid for the cart.

---

## The valley: connection map and finale route

```
                 [G Seed garden]                 [L1 Lookout]   [L2 Lookout]
                        |                               \          /
                  (P3 Dam ford)                       (P5 Fork: walkers)
                  walkers only                               \    /
[S Camp] ==P2== [A Ledge isle] ======P4====== [C Market isle] ---P6 Arch--- [E Rock isle]
   :  P1 Board:                                     |                        walkers*
   :  Pip carried S→A                               |  (P7 Shelter happens on C)
                                                    |
                                           ==P8 final plank==
                                                    |
                                            [Sanctuary]

  ==  on Bram's route          --  side route, walkers         * a wide arch also qualifies
```

| ID | Requirement | Pass test |
|---|---|---|
| **V1** | **Bram's route uses exactly three crossings:**<br>• S→A: the P2 bridge, rebuilt in the finale<br>• A→C: the P4 plank road, reused as-is<br>• C→Sanctuary: built in the finale<br>All other bridges (Dam ford, Fork, Arch) are side routes. They are never needed by the cart. | Walk the route in the editor with the cart's capacity rules; every edge passes. |
| **V2** | **The finale twist always happens.** The P2 anchor ledge on island A is narrow (1.5 cm), so only a narrow route fits in puzzle 2. Between puzzles, Pip is visibly hammering the ledge wider. In the finale, the widened ledge accepts a plank, and the rope walk glows as "too narrow for the cart." | In 100% of runs, the finale asks for the S→A rebuild, whatever the player built in puzzle 2. |
| **V3** | **No softlocks.** Before the finale, the game checks every edge on Bram's route against the cart rules. Any edge that fails glows and must be rebuilt. The designed case is exactly one rebuild (S→A); the general check is a safety net. | Automated test: corrupt or alter each saved bridge in turn. The finale always remains completable. |
| **V4** | **Dam stones stay as a walkers-only ford.** Once your hand raises them, Pip wedges them in place. They are not on the cart's route. | After Dam, walkers can use the ford with no hand present. The cart never routes through it. |

---

## Innovation (25%)

| ID | Requirement | Pass test |
|---|---|---|
| I1 | The hand's geometry is the terrain. Route shape and bridge geometry come from the hand, not from triggering a gesture. | Two different hand shapes across the same gap give visibly different bridges in shape, width and branching. |
| I2 | Bridges keep the shape you held. | The finished bridge is within 1 cm of the **filtered, displayed hand route at the moment each section was committed** (T3). The reference is not the physical hand, which the tracker can't measure exactly. |
| I3 | Every puzzle is solved with a hand **shape**. No puzzle is solved by a button or a menu. | No puzzle can be solved with only a poke or a pinch. |
| I4 | Clearly different from Tiny Castles: picking characters up is never the main verb. | Testers who weren't coached say "you are the bridge," not "you pick up little people." |
| I5 | **Your bridges have consequences.** The finale runs over bridges you built earlier, and one of them has to be rebuilt (V1–V3). | Every finale route includes the player's own bridges. Testers mention this without being asked. |
| I6 | **The bridge shows your hand.** Knuckle posts stand at the joint positions, and knots sit at the fingertips. | Testers recognize their own hand in a finished bridge without being told. |
| I7 **REVISED** | **Judge the result, not the gesture.** In Arch, the route has to clear the rock, connect both anchors and stay walkable. Any hand shape that produces that geometry succeeds. A curved hand is the readable solution, so the puzzle is laid out to suggest it, and the ghost-hand hint shows it. | Every valid geometry is accepted. To check the design intent, at least 3 of 5 testers use a curved hand without being told. |
| I8 | **Three hand roles: path, dam and roof.** | Testers describe their hand as more than a bridge. Dam and Shelter must also pass E14. |

## Experience Design (25%)

| ID | Requirement | Pass test |
|---|---|---|
| E1 | First boarding without a text tutorial. | 4 of 5 testers who weren't coached get the scout onto their palm within 30 s of launch. |
| E2 | A complete arc in 10 minutes or less, from launch to the sanctuary lighting up. | Median Chapter 1 time for first-time players is 10 minutes or less. |
| E3 | Seated, within a 2-foot radius. | No leaning or standing needed. |
| E4 | Each puzzle adds exactly one new idea. | Testers can name what's new in each puzzle. |
| E5 | Travelers show their state through body language: wave, hesitate, test a plank, crouch and grip, hammer, celebrate, look back. | Testers can tell what a traveler needs without any text. |
| E6 | Limit arm fatigue. | Arms are raised for no more than about 60 s at a stretch. Shelter and Dam each need 20 s or less. A rest beat comes between puzzles. |
| E7 | Failure means a traveler hesitates, never death or punishment. | 0 traveler deaths across all playtests. |
| E8 | After about 10 s stuck, a ghost hand shows a working shape. | No tester stays stuck for more than 60 s. |
| E9 **REVISED** | **No clash with the system gesture.** Nothing asks for a pinch while the palm faces the headset. Carrying is done palm-up toward the sky. Menus follow A1. | The system menu never opens by accident in 10 full runs. |
| E10 | **Islands where the cameras can see your hands:** 30–45 cm in front of the chest, 10–30° below eye line. The Dam water surface is inside the same zone. | Dropout rate below the T19 threshold on Quest 3 and 3S. |
| E11 | Travelers are about 3 cm tall by default, adjustable from 2 to 5 cm. | At default size, testers can read every E5 state. |
| E12 | **Short builds that are fun to watch:** 2–3 s of stillness while Pip lays planks in time with hammer taps. A see-through preview of the resulting bridge, with its capacity icon, appears once the route connects. | Testers describe holding still as "helping." Fewer than 1 refused build per puzzle. |
| E13 | **Story without words.** In the finale, light runs along every bridge you built. | Testers can retell the story (lantern, night, home). |
| E14 **REVISED** | **Dam and Shelter must earn their place** by offering a choice, not only a surprising hand role.<br>• **Dam:** where you put the edge of your hand decides which line of stones comes up. One line leads to the garden; the other stops short, and the apprentice declines it.<br>• **Shelter:** rain moves across island C while two groups cross to different huts. One hand can cover one group, so you choose who to shelter first and move your hand with them. A group in the rain waits and huddles; nobody is hurt. | **After the first playtest round:** a puzzle stays only if testers make an observable choice in it and it isn't rated the least enjoyable. Otherwise it's cut, and Chapter 1 drops to 7 or 6 puzzles. That's acceptable. |
| E15 | **Cold start in 10 s or less**, from the app icon to Pip waving. No title-screen gate. Players resume exactly where they left off. | Under 10 s on Quest 3 and 3S, across 10 launches. |
| E16 | **End-of-chapter hook:** a glimpse of the next valley, where a traveler is waiting. | Testers say they want more when asked afterward. |
| E17 | **Feedback when a connection forms.** The fingertip glows when it's within connect range. A chime and a puff of dust play when the connection locks. | No tester asks "is it working?" |

### Chapter 1: up to 8 puzzles, about 9 minutes

| # | Puzzle | New idea | Target |
|---|---|---|---|
| 1 | **Board** | Pip steps onto your palm, and you carry him from S to A. | 0:45 |
| 2 | **First Bridge** | Point from the narrow ledge on A to S. Pip builds a rope walk. Withdraw your hand, and the apprentice crosses. **This is the hook.** | 1:30 |
| 3 | **Dam** *(playtest-gated)* | Place the edge of your hand in the stream to choose which stones surface (A→G). | 1:00 |
| 4 | **Width** | A group needs a plank road from A to C: flat hand, fingers together. | 1:00 |
| 5 | **Fork** | Two travelers, two lookouts: spread your fingers. One branch is too steep and gets refused, so you lower that finger. This puzzle also teaches slope. | 1:00 |
| 6 | **Arch** | A rock sits between C and E. Any valid route clears it; a curved hand is the readable way (I7). | 1:15 |
| 7 | **Shelter** *(playtest-gated)* | Rain on C. Choose which group to shelter, and move your hand with them. | 1:00 |
| 8 | **Finale** | Rebuild S→A as a plank road (V2). Bram crosses your P4 plank road. Build the last plank from C to the Sanctuary. Light runs along every bridge, then the next valley appears. | 1:30 |

**Chapter 2:**
- Multi-segment (3 or more chained builds)
- Span (in one-hand mode, an extra anchor rock appears)
- Order
- Drift (slow, looping, no time pressure)
- Remixes

Multi-segment is **only** in Chapter 2; the v0.4 duplicate has been removed. ⚑ Chapter 2 size is open, and Chapter 2 is built only after Milestone 4.

## Accessibility (supports Experience Design and the Accessibility Forward award)

| ID | Requirement | Pass test |
|---|---|---|
| **A1** | **A menu either hand can reach on its own:** a small waystone post on the near edge of the island table, poked with any finger of either hand. It covers start, pause, resume, restart, settings, x-ray and quit. The wrist tag is removed. | A full session, including every menu action, can be done with only the left hand, and separately with only the right hand. |
| **A2** | **Every Chapter 1 puzzle works with one hand.** Each puzzle's one-hand solution: P1–P8 are all single-hand by design (Span is in Chapter 2). | One tester who wasn't coached finishes Chapter 1 with each hand alone. |
| **A3** | **Either hand, with no setting needed. Ownership is locked during an interaction.**<br>• **Choosing a hand:** while idle, the active hand is whichever hand engages first: it's nearer the active anchor or traveler and it connects (T2).<br>• **Keeping control:** that hand keeps control the whole time it supports a traveler or builds. The other hand approaching never takes over.<br>• **Releasing control:** after the build completes or is cancelled, the traveler is set down, or recovery ends (T5).<br>• **Who can re-align:** during T5 recovery, only the hand that owns the interaction can re-align with the ghost hand. | Left-only, right-only and alternating runs all complete. Bringing the other hand close during a build or carry never interrupts it or transfers control. |
| **A4** | **Build tolerance.** Tremor-tolerant building (T3), adjustable stillness tolerance (3 steps), and adjustable world scale and table height. | A tester with mild tremor completes every build at default or adjusted settings. |
| **A5** | **Reduced motion** turns off camera moves in the end sequence, the swaying of the world, and particle bursts. | With the option on, there's no camera motion that the player didn't cause. |
| **A6** | **Captions** for all meaningful sounds (build done, connection locked, refusal, rain, chime). | Every audio cue in P2 has a caption when captions are on. |
| **A7** | **Cues that don't rely on colour.** Refusal, connection and capacity are each shown by shape and motion as well as colour. | Every state can be told apart in a greyscale capture. |

## Technical Implementation (25%)

| ID | Requirement | Pass test |
|---|---|---|
| T1 | **Hand-route model.**<br>• Each finger is a polyline through its joints.<br>• The palm is a bounded polygon fitted to the wrist and metacarpals.<br>• The walkable side is the one facing up, measured against world gravity.<br>• Each traveler stores (surface, parameter) and is re-projected every frame, offset by the finger's radius. | No sliding, popping or sinking into the hand. |
| T2 | **Route graph with two thresholds:** connect below about 2 cm after holding 150 ms; break above about 4 cm. | No flickering connections. |
| T3 **REVISED** | **Bridge snapshot and build rules.**<br>• **Stillness:** filtered fingertip speed below 3 cm/s (One Euro filter), averaged over 0.5 s. These are starting values.<br>• **Commit as you build:** the bridge is built in sections from the anchor outward. Each section is frozen when progress passes it. Frozen sections never move again.<br>• **Uncommitted sections** follow the current filtered route.<br>• **Harmless tremor:** movement that stays within the stillness threshold and 1.5 cm of the last frozen section's end.<br>• **Larger movement or a shape change** pauses the build. Progress is kept; it never resets.<br>• **Resuming:** the route must reconnect within 2 cm of the last frozen section's end.<br>• **Connection breaks** (more than 4 cm at an anchor): the build pauses, and frozen sections stay as visible scaffold.<br>• **Abandoned scaffold** (no reconnect within 10 s, or the player starts somewhere else): it's taken down with a short animation. Nothing is lost except that attempt. | No accidental builds while moving. Deliberate holds always complete. A shape change partway through the build never changes a frozen section. |
| T4 **REVISED** | **Bridge structure and validation.**<br>• A bridge is a **tree of curves**: one curve for a single path, a shared junction plus one curve per branch for a fork.<br>• Each curve passes through the joint positions captured when its section was frozen. Planks, rope, posts and knots are placed along it.<br>• **Checks**, for each branch and against the capacity table: both ends anchored, width, slope, clearance, no self-crossing, maximum length.<br>• **Refusal is visible:** Pip shakes his head and the failing segment glows, with shape and motion cues per A7. | The shape matches the route as in I2. Every refusal case is readable. The bake causes no frame hitch. |
| T5 **REVISED** | **Tracking loss and recovery.**<br>1. **Loss:** the active hand is invalid or low-confidence. Affected travelers freeze and grip. The last good hand route stays visible as a **ghost hand** and becomes their temporary support.<br>2. **Guide:** the ghost hand shows where to return your hand.<br>3. **Re-align:** play resumes only when the live route is within 2 cm of the ghost route for 300 ms. Travelers then blend to the live hand over 200 ms.<br>4. **Fallback:** if alignment hasn't happened within 8 s, travelers visibly return to their **last safely reached checkpoint island**: the last island they stood on, reached by a valid route or carry. That is never an island they haven't reached yet, so losing tracking can't skip a puzzle. Any partly built bridge is taken down (T3), and the hand gives up control (A3). They never snap to a hand that appeared somewhere else.<br>**Example:** if Pip loses tracking while being carried in Board, he returns to S, not A. | Hide your hand, move it 20 cm, show it again: no traveler teleports. Recovery is either re-alignment or a visible return to the checkpoint. Automated test: forcing tracking loss at every point along every route never lets a traveler reach an island it hadn't reached before. |
| T6 | **Automatic hand-size calibration** from bone lengths. | Testers with any hand size complete every puzzle. |
| T7 **REVISED** | **Performance.**<br>• The competition's minimum is 60 fps.<br>• The shipping target is **72 fps or more** on Quest 3 and 3S, which matches Meta's VRC minimum.<br>• This holds in every scene, including the Dam water (a 64×32 flow grid), the rain, and the finale light-run. | 72 fps or more in every scene of the test matrix. No hitches. |
| T8 **REVISED** | **Navigation with hands only,** via A1. When the system menu takes focus, the game pauses. | A full run with no controller ever paired. |
| T9 | **Save and resume,** including every bridge's curve tree and the ledge state (V2). | Take the headset off mid-puzzle, or quit and relaunch: state and bridges are intact, and the finale is still completable. |
| T10 **REVISED** | **Privacy, split by build type.**<br>• **Release build:** the recording code is compiled out, the manifest has no INTERNET permission, nothing is collected, and nothing is stored except save data.<br>• **Developer build:** local recording (T13) after the tester agrees verbally. The developer exports it over USB, and it's deleted after analysis. | Release-build manifest check: no INTERNET permission, no recording symbols. Works fully in airplane mode. |
| T11 | **Stack:** Unity with the Meta XR All-in-One SDK v207, on OpenXR. The Interaction SDK is used only for UI. The route, build and traveler systems are custom. | Matches the SDKs suggested in the brief. |
| T12 | **Launches with hands only.** The app manifest declares hand tracking (controllers optional, high tracking frequency). | Plays with controllers switched off. |
| T13 | **Playtest logging (developer builds only):**<br>• time to board and time stuck<br>• hint triggers and refused builds<br>• dropouts as defined in T19<br>• the replay record defined in T17 | Provides the numbers for E1, E2, E8, E12, T14 and T19. |
| T14 | **Anchors placed for tracking.** Reaching for an anchor points the fingers sideways, never straight away from your head. | No anchor requires pointing straight away from your head. Dropout rates show no excess in any finger direction. |
| T15 **REVISED** | **Evidence in the submission,** from the **final build only.**<br>• Report N, how many testers were new to hand tracking, and the help given to each (none, verbal, or hands-on).<br>• Earlier rounds on different builds are reported separately, or not at all.<br>• Targets: 80% or more finish Chapter 1, median time 10 minutes or less, disruptive dropouts below the T19 threshold, 0 deaths. | The numbers appear on the video's closing card and in the submission text, with the build version. |
| T16 | **X-ray view:** shows the route graph, thresholds, the bridge's curve tree, frozen and uncommitted sections, and build progress. | Judges can turn it on and off. The video shows it for 5–8 s. |
| T17 **REVISED** | **Replay, split into two separate jobs.**<br>**T17a, test replay:**<br>• The route, build and traveler logic runs on a **fixed 60 Hz tick in custom kinematic code, with no PhysX in any gameplay-critical path.** That makes reproducing a session realistic.<br>• **Record:** timestamped joint poses, validity and confidence flags, calibration, initial level and save state, menu and interaction events, build version and settings.<br>• **Replay:** feeds the same inputs into the same ticks.<br>**T17b, filming replay:**<br>• Records the resulting traveler transforms, bridge states and animation events every frame, and plays them back exactly as recorded for the spectator camera.<br>• Labeled "replay of recorded play session" in the video. | **T17a:** a recorded failure reproduces the same gameplay decisions (connections, freezes, bakes, refusals) on the same build, and the regression test passes after the fix.<br>**T17b:** the filmed replay matches the original session's footage frame by frame. |
| T18 **REVISED** | **Recovering when tracking quality drops.** This doesn't assume the cause is a dark room; poor tracking can also come from the hand being blocked from view.<br>1. **First:** the ghost hand (T5) and Bram's gesture prompt the player to bring the hand back into the camera zone.<br>2. **If poor quality continues for 20 s or more:** one short line of text appears: "Hand tracking works best in a well-lit room, with your hand in view."<br>3. **Adjustment:** only the stillness tolerance loosens, at most one step, with a visible icon. Connection distances **never** loosen, so bridges never look disconnected. | In occlusion and dim-room tests, the player recovers in at most two prompts, and no bridge forms across a visible gap. |
| **T19 NEW** | **Dropout definition.**<br>• **Dropout:** the active hand is invalid or low-confidence for **250 ms or more** while it's supporting a traveler or building a route. Logged with which joints were affected, how long, and the finger direction.<br>• **Disruptive dropout:** one that froze a traveler or paused a build.<br>• **Threshold:** fewer than 1 disruptive dropout per puzzle. | Both counts are reported for each puzzle. |

## Polish & Presentation (25%)

| ID | Requirement | Pass test |
|---|---|---|
| P1 | **One art direction:** carved wood, rope and planks, lantern light, and stylized hands. Hand material: owner's choice. | Every screenshot looks like it's from the same game. |
| P2 | **Audio:** footsteps, hammer taps tied to build progress, rope creak, rain, the stream, the lantern chime, music that builds with darkness, and the connection chime. | Sound alone tells you a bridge is finished. |
| P3 **REVISED** | **Release blockers** across the full test matrix (P12). Zero of each is allowed:<br>• crashes<br>• lost progress or bridges<br>• broken or impossible finale route<br>• navigation that needs a controller<br>• unintended system-menu opens<br>• a traveler teleporting or dying | 10 cold-start runs in a row, on each device, with no release blockers. |
| P4 | **About 90 s video.** Shot order:<br>1. Finger walk.<br>2. Withdraw your hand; the bridge stays.<br>3. Bram crosses.<br>4. Shape montage.<br>5. Dam and Shelter, if they survived the cut.<br>6. Finale across *your* bridges.<br>7. X-ray.<br>8. Evidence card. | A viewer with no context can explain the game afterward. |
| P5 | **Submission text** of 500 words or less, including the evidence and build version. A tagline of 140 characters or less. | All fields complete. T1–T5 explained in plain language. |
| P6 | **Fixed asset list:**<br>• one valley with a stream<br>• Pip, Bram and the apprentice, with about 15 animations<br>• 3 signature effects, plus rain and water<br>Nothing is added until the list is done. | No placeholders in shipped puzzles. |
| P7 | **Capture:** real point-of-view footage from the headset, plus labeled T17b replays. 1080p60 or better. | All footage is real gameplay. |
| P8 | **All UI is part of the world:** the waystone (A1) and carved signs. The one exception is T18's single line of text. | No other on-screen UI. |
| P9 | **Store-ready finish:** icon, splash screen, title card, credits. | All present. |
| P10 | **Unbiased playtests:** 10 or more strangers, including people new to hand tracking, across at least 2 rounds. After each round, fix the top 3 friction points. | T15 targets met on the final build. |
| P11 **REVISED** | **Build order (see Milestones).** Build the whole journey rough first, then polish in the order a judge experiences it: the first 5 minutes, then the finale, then the middle. **Ship rule:** at the deadline, any puzzle that isn't finished is cut. A rough puzzle never ships. | Every shipped puzzle has final art, its sounds, its animations and its effect. |
| **P12 NEW** | **Test matrix:**<br>• Devices: Quest 3 and Quest 3S.<br>• Rooms: bright, normal and dim.<br>• Hands: left-only, right-only and both.<br>• Controllers powered off.<br>• Default and adjusted scale.<br>• Fresh install and resume from save. | Each P3 blocker is checked in every cell. |

---

## Milestones (in order; no dates)

| # | Milestone | Done when |
|---|---|---|
| **M1** | **Prove the core promise on the headset.** Pip boards, builds along your hand, you withdraw your hand and the bridge stays, Bram crosses, and the bridge survives saving and reloading. Recovery from tracking loss (T5) works. Logging and T17a recording are on. | **Proven on Quest 2,** the headset you own: everything above works and *feels* right in your hands. **Fix it before doing anything else.** The requirements lock here, and the tolerances and dimensions are set from these headset results. |
| **M1b** | **Early target-device check.** Run the M1 build on a Quest 3 or 3S (borrowing is fine). | **Must be done before M4.**<br>• **Check:** fps and T19 dropout rates are logged, and T2/T3 tolerances are re-checked.<br>• **If the numbers differ:** re-tune on Quest 3/3S. **The target device's numbers take priority over Quest 2's.** |
| M2 | **Rough full journey.** All 8 puzzles in greybox (simple placeholder geometry). Finale route checked (V1–V3), waystone menu (A1), sanctuary. | The whole journey from S to the Sanctuary can be played with one hand. |
| M3 | **Playtest round 1, then cut or keep.** | E14 gate decided for Dam and Shelter. Top 3 friction points fixed. |
| M4 | **Polish in the order a judge sees it:** first 5 minutes, then the finale, then the middle. Apply the P11 ship rule. | Every shipped puzzle passes P11. |
| M5 | **Final build:** run the full test matrix (P12), playtest round 2 on that exact build (T15), capture video (P7), write the submission (P5). | No release blockers on Quest 3 **and** 3S. Evidence and the video come from the final build, identified by its build version. Video and submission are complete. |

**Sign-off rule:** any change after M5 reopens M5 for everything the change affects. That includes a bug fix, Chapter 2 content and Option B. Re-run the affected cells of the test matrix (P12) and the release blockers (P3). Update the tested build version. The evidence in the submission (T15) names the build that was actually tested, and that's the build uploaded to the release channel. If there's no time to re-validate, the change doesn't ship.

---

## Awards (one entry, one prize)

| Award | Status | Based on |
|---|---|---|
| **Best New Gaming** | Primary | Everything |
| Best First Five Minutes | Targeted | E1, E8, E9, E12, E15, E17, P3 |
| Boldest Original Concept | Targeted | I1–I8 |
| Best Accessibility Forward | Targeted, and recommended | A1–A7, T3, nothing on a timer |
| Best Reason to Come Back | Targeted | I5, E16, bridges that persist, the sanctuary growing |
| Best Agentic Interaction | Not targeted | Travelers follow routes and don't make meaningful decisions |

## Hard rules from the brief

- Start membership and Developer Access for the submitter.
- New Experience: conceived and built after Sept 24, 2026, with no pre-existing code.
- Fully usable with hands, end to end, including navigation.
- APK on a "Competition" release channel with a working invite URL, tested from a second account.
- Public YouTube or Vimeo video under 3 minutes.
- Submission form completed.
- No changes after the deadline, and the build stays accessible throughout judging.
- Policy compliance. ⚑ Check whether a privacy-policy URL is needed. If it is, have a simple one ready (release build collects no data; see T10).
- Final testing on Quest 3 and Quest 3S.
  - Quest 2 is for development and the M1 prototype only. It isn't used as evidence for target-device performance or tracking.
  - This matters because Quest 2 has a different chipset and different tracking cameras: fps and tracking results measured on it don't carry over.

## Out of scope

| Item | Reason |
|---|---|
| Base-building economy and upgrade trees | A judge won't see it in about 10 minutes, and it dilutes the hook. Option B may be added after M5 if time allows, which reopens M5 (see Decisions). |
| AI/LLM features | Adds nothing to the core verb |
| Mixed reality / passthrough | Adds tracking and room risk |
| Multiplayer | Network risk with no gain to the core |
| Hand-to-hand contact | Where tracking is weakest |
| Timers, leaderboards, fail states | Conflict with E7, accessibility and the calm tone |
| Procedural levels | Hand-authored puzzles score better |
| Controller polish | No judging credit |

## Decisions

- **Concept:** Palmfolk.
- **Chapter 1:** Board, First Bridge, Width, Fork, Arch and Finale are fixed. Dam and Shelter depend on E14 playtests.
- **Feature set frozen.** Corrections and removals are allowed.
- **Requirements lock at M1.** Tolerances and dimensions (thresholds, capacity table, timings) are starting values. Headset results set the final numbers: Quest 2 at M1, then Quest 3/3S at M1b, which takes priority.
- **Competition scope frozen.** No more concept rewrites.
- **I7 design check: kept.** Any valid route shape passes, but at least 3 of 5 testers should reach for a curved hand without being told.
- **Character names and hand material:** owner's choice. Not tracked here.
- **Licensing:** original Palmfolk code is MIT-licensed. Dependencies and assets keep their original licenses and required notices, for example the Unity terms, the Meta / Oculus SDK License, and third-party asset licenses. Required notices go in the credits (P9). Before any third-party asset goes in, check that its license allows redistribution in an APK.
- **Base building: out for now.** The sanctuary grows on its own.
  - **Later, only if time allows after M5:** the minimal Option B. At the end of Chapter 1, the player picks one of two sanctuary buildings and places it with their hand.
  - B always comes after the finale, so it can never change the first 10 minutes. If time runs short, B is dropped.
  - Adding B reopens M5 (sign-off rule).
  - Accepting this spec means base building is a later product decision, not part of the competition entry.
- ⚑ **Pending, but neither blocks the build:**
  - Chapter 2 size. Chapter 2 is built only after M4, and any Chapter 2 work after M5 reopens M5.
  - Privacy-policy URL (check at submission).

---

# Status scorecard

Projected judge scores have been removed. A plan can't earn a score; only a working build can. The earlier decimal scores (7.25 → 9.0) were false precision, and they were partly the reviewer grading his own fixes.

| Area | Current status | What would prove it |
|---|---|---|
| **Originality** | Clear central hook (the bridge you shaped stays and gets reused). The difference from Tiny Castles is argued but not yet shown. | Testers who weren't coached describe it as "you are the bridge" (I4) and recognize their hand in the bridge (I6). |
| **Experience** | The whole journey is defined and the finale route is checked on paper. Onboarding, comfort, and whether Dam and Shelter are fun haven't been tested. | E1, E2 and E8 targets met. E14 gate passed. No more than about 60 s with arms raised. |
| **Technical** | Architecture proposed: route model, snapshot rules, recovery, replay split. Key interactions haven't been proven on the headset. | M1 working on Quest 2, then confirmed on Quest 3/3S (M1b). T5 test with the hand moved 20 cm. T17a reproduces a real failure. 72 fps or more across the P12 matrix. |
| **Polish** | Art direction, asset list and video plan defined. Production quality not yet tested. | All shipped puzzles pass P11. Zero P3 release blockers across P12. |

## Design tests (pass or fail, checked at M5)

| Test | What has to be true |
|---|---|
| Hands-first | A full run with no controller paired (T8, T12) |
| Airplane seat | Seated, 2-foot radius (E3) |
| One bus stop | Chapter 1 satisfying in 10 minutes or less (E2) |
| Take it away | Fully original and offline (T10) |

## Next step

**M1 on your Quest 2.** Pip builds along your hand, you withdraw it, Bram crosses, and the bridge survives a save and reload. Then check it on a Quest 3/3S (M1b) before polishing anything. That proves more than any further change to the paper.
