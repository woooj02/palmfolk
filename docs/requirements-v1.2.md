# Palmfolk: Requirements v1.2 (final, frozen Nov 4, 2026)

**Meta VR Start Developer Competition 2026 · Gaming track · New Experience division**
**Nov 4, 2026 · Deadline: Nov 18, 2026, 1:00 pm MST**

**Status:**
- **Concept:** locked.
- **Feature set:** frozen. This doc isn't edited again until the build has started. From then on, corrections and removals are allowed. Additions are not, except the four named experiments (EX1–EX4) through their gates.
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
| **v1.2** | **Final. Focused correction pass after two reviews.**<br>• **Pitch:** leads with the bridge that stays, shows your hand and gets reused. Prior art corrected.<br>• **Fixed:** cart rule, clearance, width source, scale rule, hand placement, valley dimensions, hint and recovery ghosts, video order.<br>• **Added:** FoV layout (E18), high contrast (A8), remote device check (T20).<br>• **Headset tests:** H1–H3 settle Arch dimensions, comfort and width data at M1.<br>• **Experiments after M1:** EX1–EX4, each with a gate.<br>• **Second review:** six core puzzles by default, startup safeguards (E19), early-hook target (E20), contact feedback (P2), submission package (P5), the core fun test (H4).<br>• **Devices:** Quest 2 is the development device and can't certify the judges' devices. Testing the submission build on Quest 3/3S is a production priority. |

---

## Reality check

No entry is a sure thing. Last year there were 650+ submissions, judging is subjective, and studios enter too.

Judges score the experience that works on their headset, not the document. This doc sets what has to be true. Only a build and playtests can show that it is.

## Prior art judges will recognize

- **Awesome Hand:** won Best Implementation of Hand Interactions in this competition in 2025. It has a stage where you use your hands as a bridge. This is the entry judges are most likely to compare us with.
- **Silhouette:** your hand's shadow can be a bridge creatures walk across, and it can block water.
- **Pocket Lands:** won the same 2025 award with a lap-sized diorama shaped by hand.
- **Tiny Castles (Meta):** tiny characters plus hands.
- **Moss:** a tiny hero helped by a giant player. Likely comparison for the mood, not the mechanic.
- **Make with Puppet:** a hand-tracking puppet pose game, also entered in this competition. https://devpost.com/software/make-with-puppet

A hand acting as a bridge is not new. Palmfolk's promise is the combination: **shape a bridge with your hand, withdraw it, and watch the travelers keep using what you created.** The bridge that stays, the recognizable hand shape and the later reuse lead the pitch and the first shot. We show that combination. We don't claim nobody has tried it.

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

**Crossings reused as-is are validated against the cart's full rule from the start.** That is the puzzle 4 bridge (A→C): it must meet the cart's width, slope and clearance, even though puzzle 4 only teaches width. The puzzle 2 bridge (S→A) is the deliberate exception: it must fail the cart, so the finale rebuild always happens (V2). The last crossing (C→Sanctuary) is built in the finale under the cart's rule.

**Definitions (starting values):**
- **Clearance:** free headroom above the deck along the whole route.
- **Slope:** measured along the deck. Whether Pip needs to add end ramps is decided by H1.
- **Width:** comes from the joint radii in OpenXR hand tracking, which give approximate finger surfaces. H3 confirms Quest returns usable values. Neighbouring finger surfaces with a gap under 1.0 cm along most of their length merge into one walkable surface; wider gaps become branches. The measured width then snaps to a tier (rope, or plank), so tracking noise can't change a bridge's class. The V2 ledge check compares tiers, not raw widths. **Fallback if radii aren't usable:** fixed values of 1.2 cm for one finger and 2.0 cm per merged finger.
- **Scale:** every distance in this doc is at default world scale. All of them scale together with the world (A4). Hand calibration (T6) sets the world scale at launch, so each gap fits the player's hand.

---

## The valley: connection map and finale route

```
                 [G Seed garden]                 [L1 Lookout]   [L2 Lookout]
                        |                               \          /
                  (P3 Dam ford)                       (P5 Fork: walkers)
                  walkers only                               \    /
[S Camp] ==P2== [A Ledge isle] ======P4====== [C Market isle] ---P6 Arch--- [E Rock isle]
   :  P1 Board:                                     |                        walkers*
   :  Pip walks S→A                                |  (P7 Shelter happens on C)
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
| **V5 CORRECTION** | **The valley has dimensions.** Active gaps are 12–15 cm at default scale (starting value). Each gap's position and direction are recorded in the map. | Every gap can be reached with either hand, seated, with no leaning, and with fingers pointing sideways (T14). If a fixed layout can't do this, EX4 is tried. |

---

## Innovation (25%)

| ID | Requirement | Pass test |
|---|---|---|
| I1 | The hand's geometry is the terrain. Route shape and bridge geometry come from the hand, not from triggering a gesture. | Two different hand shapes across the same gap give visibly different bridges in shape, width and branching. |
| I2 | Bridges keep the shape you held. | The finished bridge is within 1 cm of the **filtered, displayed hand route at the moment each section was committed** (T3). The reference is not the physical hand, which the tracker can't measure exactly. |
| I3 | Every puzzle is solved with a hand **shape**. No puzzle is solved by a button or a menu. | No puzzle can be solved with only a poke or a pinch. |
| I4 **REVISED** | Clearly different from Tiny Castles, Awesome Hand and Silhouette: the thing that stays is the point. Picking characters up is never the main verb. | Testers who weren't coached say some version of "it kept my hand" or "the bridge stayed," not "you pick up little people." |
| I5 | **Your bridges have consequences.** The finale runs over bridges you built earlier, and one of them has to be rebuilt (V1–V3). | Every finale route includes the player's own bridges. Testers mention this without being asked. |
| I6 **PRIMARY** | **The bridge shows your hand.** This is the main differentiator. Knuckle posts stand at the joint positions, and knots sit at the fingertips. | Testers recognize their own hand in a finished bridge without being told. |
| I7 **REVISED** | **Judge the result, not the gesture.** In Arch, the route has to clear the rock, connect both anchors and stay walkable. Any hand shape that produces that geometry succeeds. A curved hand is the readable solution, so the puzzle is laid out to suggest it, and the ghost-hand hint shows it. | Every valid geometry is accepted. To check the design intent, at least 3 of 5 testers use a curved hand without being told. |
| I8 **REVISED** | **The hand is a path by default.** Dam and roof are extra hand roles that ship only if their puzzles pass E14. | If they ship, testers describe their hand as more than a bridge. |

## Experience Design (25%)

| ID | Requirement | Pass test |
|---|---|---|
| E1 | First boarding without a text tutorial. | 4 of 5 testers who weren't coached get the scout onto their hand within 30 s of launch. |
| E2 | A complete arc in 10 minutes or less, from launch to the sanctuary lighting up. | Median Chapter 1 time for first-time players is 10 minutes or less. |
| E3 | Seated, within a 2-foot radius. | No leaning or standing needed. |
| E4 | Each puzzle adds exactly one new idea. | Testers can name what's new in each puzzle. |
| E5 | Travelers show their state through body language: wave, hesitate, test a plank, crouch and grip, hammer, celebrate, look back. | Testers can tell what a traveler needs without any text. |
| E6 | Limit arm fatigue. | Arms are raised for no more than about 60 s at a stretch. Shelter and Dam each need 20 s or less. A rest beat comes between puzzles. |
| E7 | Failure means a traveler hesitates, never death or punishment. | 0 traveler deaths across all playtests. |
| E8 **REVISED** | After about 10 s stuck, a moving translucent hint hand shows a working shape. It must look different from the still, carved recovery ghost (T5). | No tester stays stuck for more than 60 s. Testers can tell the two apart. |
| E9 **REVISED** | **No clash with the system gesture.** Nothing asks for a pinch while the palm faces the headset. Carrying is done palm-up toward the sky. Menus follow A1. | The system menu never opens by accident in 10 full runs, including while looking down at Pip on a palm-up hand. |
| E10 **REVISED** | **Comfortable hand placement inside the cameras' view:** relaxed shoulders, with elbows supported where possible. Starting values, adjustable with table height (A4): the active gap sits 30–45 cm in front of the chest, 10–30° below the head's forward direction. The Dam water surface is inside the same zone. H2 sets the final values. | Dropout rate below the T19 threshold. Testers report no shoulder strain, and E6 holds. |
| E11 **REVISED** | Travelers are about 3 cm tall at default scale. Their size changes only with world scale (A4). | At default size, testers can read every E5 state. |
| E12 | **Short builds that are fun to watch:** 2–3 s of stillness while Pip lays planks in time with hammer taps. A see-through preview of the resulting bridge, with its capacity icon, appears once the route connects. | Testers describe holding still as "helping." Fewer than 1 refused build per puzzle. |
| E13 | **Story without words.** In the finale, light runs along every bridge you built. | Testers can retell the story (lantern, night, home). |
| E14 **REVISED** | **Dam and Shelter must earn their place** by offering a choice, not only a surprising hand role.<br>• **Dam:** where you put the edge of your hand decides which line of stones comes up. One line leads to the garden; the other stops short, and the apprentice declines it.<br>• **Shelter:** rain moves across island C while two groups cross to different huts. One hand can cover one group, so you choose who to shelter first and move your hand with them. A group in the rain waits and huddles; nobody is hurt. | **After the first playtest round:** a puzzle comes in only if testers make an observable choice in it, it adds enjoyment beyond the six core puzzles, and it isn't rated the least enjoyable. Otherwise it stays out, which is the default. If only one comes in, prefer Shelter: blocking water with your hand already exists in Silhouette. |
| E15 | **Cold start in 10 s or less**, from the app icon to Pip waving. No title-screen gate. Players resume exactly where they left off. | Under 10 s on Quest 3 and 3S, across 10 launches. |
| E16 | **End-of-chapter hook:** a glimpse of the next valley, where a traveler is waiting. | Testers say they want more when asked afterward. |
| E17 | **Feedback when a connection forms.** The fingertip glows when it's within connect range. A chime and a puff of dust play when the connection locks. | No tester asks "is it working?" |
| **E18 NEW** | **FoV-aware layout.** Field of view differs across Meta VR devices, so everything needed for the current puzzle fits in a comfortable, narrower view: about ±25° around where the player looks for that puzzle. Turning the head is fine; leaning isn't. | Passes Meta's in-headset FoV simulation. The submission text says how. |
| **E19 NEW** | **Startup safeguards.** If controllers are in hand, have just been set down, or no usable hand is tracked, Pip waves and the introduction waits. A clear animation shows controllers being put down. Short text appears if that doesn't resolve it (P8). The introduction starts only when a usable hand is present. | Tested in three states: controllers held, controllers set down, no usable hand tracking. A first-time player reaches a usable hand without help. |
| **E20 NEW** | **Early hook.** The time from the first scene becoming visible to the first bridge staying after the hand withdraws is logged separately from boarding (T13). | Test targets: median under 90 s, most players under 2 minutes. |

### Chapter 1: six core puzzles, about 7 minutes (up to 8 and about 9 minutes if Dam and Shelter pass E14)

| # | Puzzle | New idea | Target |
|---|---|---|---|
| 1 | **Board** | Pip steps onto your fingertips and walks across your hand from S to A. The first thing you feel is his steps on your fingers, not a pick-up. | 0:45 |
| 2 | **First Bridge** | Point from the narrow ledge on A to S. Pip builds a rope walk. Withdraw your hand, and the apprentice crosses. **This is the hook.** | 1:30 |
| 3 | **Dam** *(playtest-gated)* | Place the edge of your hand in the stream to choose which stones surface (A→G). | 1:00 |
| 4 | **Width** | A group needs a plank road from A to C: flat hand, fingers together. | 1:00 |
| 5 | **Fork** | Two travelers, two lookouts: spread your fingers. Lookout anchors are zones, sized to fit any calibrated hand, either hand. One branch is too steep and gets refused, so you lower that finger. This puzzle also teaches slope. | 1:00 |
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
| **A8 NEW** | **High-contrast mode** for routes, anchors, travelers and refusal cues. | Every state can be told apart with the option on. |

## Technical Implementation (25%)

| ID | Requirement | Pass test |
|---|---|---|
| T1 | **Hand-route model.**<br>• Each finger is a polyline through its joints.<br>• The palm is a bounded polygon fitted to the wrist and metacarpals.<br>• The walkable side is the one facing up, measured against world gravity.<br>• Each traveler stores (surface, parameter) and is re-projected every frame, offset by the finger's radius. | No sliding, popping or sinking into the hand. |
| T2 | **Route graph with two thresholds:** connect below about 2 cm after holding 150 ms; break above about 4 cm. | No flickering connections. |
| T3 **REVISED** | **Bridge snapshot and build rules.**<br>• **Stillness:** filtered fingertip speed below 3 cm/s (One Euro filter), averaged over 0.5 s. These are starting values.<br>• **Commit as you build:** the bridge is built in sections from the anchor outward. Each section is frozen when progress passes it. Frozen sections never move again.<br>• **Uncommitted sections** follow the current filtered route.<br>• **Harmless tremor:** movement that stays within the stillness threshold and 1.5 cm of the last frozen section's end.<br>• **Larger movement or a shape change** pauses the build. Progress is kept; it never resets.<br>• **Resuming:** the route must reconnect within 2 cm of the last frozen section's end.<br>• **Connection breaks** (more than 4 cm at an anchor): the build pauses, and frozen sections stay as visible scaffold.<br>• **Abandoned scaffold** (no reconnect within 10 s, or the player starts somewhere else): it's taken down with a short animation. Nothing is lost except that attempt. | No accidental builds while moving. Deliberate holds always complete. A shape change partway through the build never changes a frozen section. |
| T4 **REVISED** | **Bridge structure and validation.**<br>• A bridge is a **tree of curves**: one curve for a single path, a shared junction plus one curve per branch for a fork.<br>• Each curve passes through the joint positions captured when its section was frozen. Planks, rope, posts and knots are placed along it.<br>• **Checks**, for each branch and against the capacity table: both ends anchored, width, slope, clearance, no self-crossing, maximum length.<br>• **Refusal is visible:** Pip shakes his head and the failing segment glows, with shape and motion cues per A7. | The shape matches the route as in I2. Every refusal case is readable. The bake causes no frame hitch. |
| T5 **REVISED** | **Tracking loss and recovery.**<br>1. **Loss:** the active hand is invalid or low-confidence. Affected travelers freeze and grip. The last good hand route stays visible as a **ghost hand** and becomes their temporary support.<br>2. **Guide:** the ghost hand shows where to return your hand.<br>3. **Re-align:** play resumes only when the live route is within 2 cm of the ghost route for 300 ms. Travelers then blend to the live hand over 200 ms.<br>4. **Fallback:** if alignment hasn't happened within 8 s, travelers visibly return to their **last safely reached checkpoint island**: the last island they stood on, reached by a valid route or carry. That is never an island they haven't reached yet, so losing tracking can't skip a puzzle. Any partly built bridge is taken down (T3), and the hand gives up control (A3). They never snap to a hand that appeared somewhere else.<br>**Example:** if Pip loses tracking while crossing your hand in Board, he returns to S, not A. | Hide your hand, move it 20 cm, show it again: no traveler teleports. Recovery is either re-alignment or a visible return to the checkpoint. Automated test: forcing tracking loss at every point along every route never lets a traveler reach an island it hadn't reached before. |
| T6 **REVISED** | **Automatic hand-size calibration** from bone lengths. It sets the world scale at launch, so every gap fits the player's hand. | Testers with any hand size complete every puzzle. |
| T7 **REVISED** | **Performance.**<br>• The competition's minimum is 60 fps.<br>• The target is **72 fps or more on Quest 3 and 3S**, which matches Meta's VRC minimum.<br>• Quest 2 is the development device. Its numbers guide the work but can't certify the target devices: Quest 3 renders about 30% more pixels by default, so its faster GPU doesn't mean the same headroom.<br>• This holds in every shipped scene, including the finale light-run, and the Dam water (a 64×32 flow grid) and rain if they ship. | 72 fps or more in every shipped scene on Quest 3 and 3S (T20). Quest 2 results are reported separately. |
| T8 **REVISED** | **Navigation with hands only,** via A1. When the system menu takes focus, the game pauses. | A full run with no controller ever paired. |
| T9 | **Save and resume,** including every bridge's curve tree and the ledge state (V2). | Take the headset off mid-puzzle, or quit and relaunch: state and bridges are intact, and the finale is still completable. |
| T10 **REVISED** | **Privacy, split by build type.**<br>• **Release build:** the recording code is compiled out, the manifest has no INTERNET permission, nothing is collected, and nothing is stored except save data.<br>• **Developer build:** local recording (T13) after the tester agrees verbally. The developer exports it over USB, and it's deleted after analysis. | Release-build manifest check: no INTERNET permission, no recording symbols. Works fully in airplane mode. |
| T11 **REVISED** | **Stack:** Unity 6000.0.66f2 or later, on Windows, with the Meta XR All-in-One SDK v207, on OpenXR. The Interaction SDK is used only for UI. The route, build and traveler systems are custom. Ubuntu isn't officially supported for this SDK. | Matches the SDKs suggested in the brief. |
| T12 | **Launches with hands only.** The app manifest declares hand tracking (controllers optional, high tracking frequency). | Plays with controllers switched off. |
| T13 | **Playtest logging (developer builds only):**<br>• time to board, time to the first persistent bridge (E20) and time stuck<br>• hint triggers and refused builds<br>• dropouts as defined in T19<br>• the replay record defined in T17 | Provides the numbers for E1, E2, E8, E12, E20, T14 and T19. |
| T14 | **Anchors placed for tracking.** Reaching for an anchor points the fingers sideways, never straight away from your head. | No anchor requires pointing straight away from your head. Dropout rates show no excess in any finger direction. |
| T15 **REVISED** | **Evidence in the submission,** from the **final build only.**<br>• Report N, how many testers were new to hand tracking, and the help given to each (none, verbal, or hands-on).<br>• Earlier rounds on different builds are reported separately, or not at all.<br>• Targets: 80% or more finish Chapter 1, median time 10 minutes or less, disruptive dropouts below the T19 threshold, 0 deaths. | The numbers appear on the video's closing card and in the submission text, with the build version. |
| T16 | **X-ray view:** shows the route graph, thresholds, the bridge's curve tree, frozen and uncommitted sections, and build progress. | Judges can turn it on and off. The video shows it for 5–8 s. |
| T17 **REVISED** | **Replay, split into two separate jobs.**<br>**T17a, test replay:**<br>• The route, build and traveler logic runs on a **fixed 60 Hz tick in custom kinematic code, with no PhysX in any gameplay-critical path.** That makes reproducing a session realistic.<br>• **Record:** timestamped joint poses, validity and confidence flags, calibration, initial level and save state, menu and interaction events, build version and settings.<br>• **Replay:** feeds the same inputs into the same ticks.<br>**T17b, filming replay:**<br>• Records the resulting traveler transforms, bridge states and animation events every frame, and plays them back exactly as recorded for the spectator camera.<br>• Labeled "replay of recorded play session" in the video. | **T17a:** a recorded failure reproduces the same gameplay decisions (connections, freezes, bakes, refusals) on the same build, and the regression test passes after the fix.<br>**T17b:** the filmed replay matches the original session's footage frame by frame. |
| T18 **REVISED** | **Recovering when tracking quality drops.** This doesn't assume the cause is a dark room; poor tracking can also come from the hand being blocked from view.<br>1. **First:** the ghost hand (T5) and Bram's gesture prompt the player to bring the hand back into the camera zone.<br>2. **If poor quality continues for 20 s or more:** one short line of text appears: "Hand tracking works best in a well-lit room, with your hand in view."<br>3. **Adjustment:** only the stillness tolerance loosens, at most one step, with a visible icon. Connection distances **never** loosen, so bridges never look disconnected. | In occlusion and dim-room tests, the player recovers in at most two prompts, and no bridge forms across a visible gap. |
| **T19 NEW** | **Dropout definition.**<br>• **Dropout:** the active hand is invalid or low-confidence for **250 ms or more** while it's supporting a traveler or building a route. Logged with which joints were affected, how long, and the finger direction.<br>• **Disruptive dropout:** one that froze a traveler or paused a build.<br>• **Threshold:** fewer than 1 disruptive dropout per puzzle. | Both counts are reported for each puzzle. |
| **T20 NEW** | **Target-device testing.** This is a production priority, whether the tester is a teammate or someone outside the team.<br>• **Numbers:** a developer build of the same code shows a small stats panel: fps, dropouts per puzzle and build version. It records nothing. The tester reads out or screenshots the numbers.<br>• **Access:** the tester installs the submission build from the Competition channel invite link and plays it. | At least one full Chapter 1 run on Quest 3 and one on Quest 3S, on the submission build. If that can't happen, the submission says so, and the risk stays open. |

## Polish & Presentation (25%)

| ID | Requirement | Pass test |
|---|---|---|
| P1 | **One art direction:** carved wood, rope and planks, lantern light, and stylized hands. Hand material: owner's choice. | Every screenshot looks like it's from the same game. |
| P2 **REVISED** | **Audio and contact.** Footsteps, hammer taps tied to build progress, rope creak, rain, the stream, the lantern chime, music that builds with darkness, and the connection chime. **Contact comes first:** foot planting, contact shadows, and footsteps placed at the finger and timed to each step. Effects stay restrained. | Sound alone tells you a bridge is finished. Compare two contact treatments with identical gameplay, using neutral questions: "What did you notice when Pip walked onto your hand?" "Did anything look or sound disconnected?" "Which version felt more convincing?" |
| P3 **REVISED** | **Release blockers** across the full test matrix (P12). Zero of each is allowed:<br>• crashes<br>• lost progress or bridges<br>• broken or impossible finale route<br>• navigation that needs a controller<br>• unintended system-menu opens<br>• a traveler teleporting or dying | 10 cold-start runs in a row, on each device, with no release blockers. |
| P4 **REVISED** | **About 90 s video.** Shot order:<br>1. Shape a bridge, withdraw your hand; the bridge stays.<br>2. A brief, synchronized comparison of the real hand and its bridge, if it reads well.<br>3. Bram crosses.<br>4. Shape montage.<br>5. Dam and Shelter, if they survived the cut.<br>6. Finale across *your* bridges.<br>7. X-ray.<br>8. Evidence card. | A viewer with no context can explain the game afterward. |
| P5 **REVISED** | **Submission text** of 500 words or less, including the evidence and build version. A tagline of 140 characters or less. Working tagline: "Shape a bridge with your hand. Withdraw it. Watch the travelers keep using what you created." | All fields complete. T1–T5 explained in plain language. The package also has the hand-interactions description, 3–5 real screenshots, a cover image, play notes (seated, good light, no controllers, about 10 minutes) and access instructions tested from a second account. |
| P6 | **Fixed asset list:**<br>• one valley with a stream<br>• Pip, Bram and the apprentice, with about 15 animations<br>• 3 signature effects, plus rain and water if Shelter or Dam ship<br>Nothing is added until the list is done. | No placeholders in shipped puzzles. |
| P7 | **Capture:** real point-of-view footage from the headset, plus labeled T17b replays. 1080p60 or better. | All footage is real gameplay. |
| P8 | **All UI is part of the world:** the waystone (A1) and carved signs. The exceptions are T18's single line of text and E19's short startup text. | No other on-screen UI. |
| P9 | **Store-ready finish:** icon, splash screen, title card, credits. | All present. |
| P10 | **Unbiased playtests:** 10 or more strangers, including people new to hand tracking, across at least 2 rounds. After each round, fix the top 3 friction points. | T15 targets met on the final build. |
| P11 **REVISED** | **Build order (see Milestones).** Build the whole journey rough first, then polish in the order a judge experiences it: the first 5 minutes, then the finale, then the middle. **Ship rule:** at the deadline, any puzzle that isn't finished is cut. A rough puzzle never ships. | Every shipped puzzle has final art, its sounds, its animations and its effect. |
| **P12 REVISED** | **Test matrix:**<br>• **Full matrix on Quest 2:** bright, normal and dim rooms; left-only, right-only and both hands; controllers off; default and adjusted scale; fresh install and resume from save.<br>• **Quest 3 and 3S:** T20 runs by remote testers, covering a full Chapter 1 with controllers off. | Each P3 blocker is checked in every Quest 2 cell. Quest 3/3S results are listed separately. |


## Headset tests (H1–H3 at M1, H4 at M3)

| ID | Question | How it's settled |
|---|---|---|
| **H1** | **Does the Arch work?** Set a real rock size, span and anchor heights. | Natural curved hands from 5 testers. If they're refused, change the rock, the span, the slope limit or add end ramps. |
| **H2** | **Where is the comfortable hand position?** | Testers play with relaxed shoulders. Final E10 values come from this. |
| **H3** | **Are Quest's joint radii usable for width?** | Log them for different hands. If they're not stable, use the fallback in Definitions. |
| **H4** | **After three bridges, do players want to make another?** This is the key question. | Watch unfamiliar players on the real opening. Do they try shapes unprompted, understand refusals, enjoy holding still and care about reaching the sanctuary? If they need repeated coaching or get bored, fix the core before art, polish or experiments. |

## Experiments after M1 (the only additions allowed after the freeze)

Each one runs only after M1 is proven. Each ships only if it passes its gate. If it doesn't, it's dropped and the game ships without it.

| ID | Experiment | Gate |
|---|---|---|
| **EX1** | **Table mode.** A small comparison prototype: the valley on a real table in passthrough, keeping the rendered wooden hand. | Testers find it clearer, more comfortable or more compelling than VR. The real table must change the experience. Needs a Quest 3/3S tester, because Quest 2 passthrough is grayscale. |
| **EX2** | **Return mission.** A new traveler arrives who needs an existing crossing improved, so your saved bridges start the next challenge. "Improve" means rebuild, because frozen sections never move (T3). No streaks, no penalty. | Testers who finish Chapter 1 want to do it, and it needs no new verb. |
| **EX3** | **Fingertip trace mode.** Draw a route with one fingertip instead of holding a shape. | It solves an access problem found with a real tester, and doesn't weaken the main build. |
| **EX4** | **Valley turning.** The valley turns between puzzles so the next gap is centred. | Tried only if a fixed layout can't pass V5. |

## Milestones (in order; no dates)

| # | Milestone | Done when |
|---|---|---|
| **M1** | **Prove the core promise on the headset.** Pip boards, builds along your hand, you withdraw your hand and the bridge stays, Bram crosses, and the bridge survives saving and reloading. Recovery from tracking loss (T5) works. Logging and T17a recording are on. | **Proven on Quest 2,** the headset you own: everything above works and *feels* right in your hands. **Fix it before doing anything else.** The requirements lock here, and the tolerances and dimensions are set from these headset results. |
| **M1b** | **Early target-device check.** Send the M1 build to a Quest 3 or 3S tester (T20). | **Must be done before M4.**<br>• **Check:** fps and T19 dropout rates are reported.<br>• **If the numbers differ:** re-tune. **The target device's numbers take priority over Quest 2's.** |
| M2 | **Rough full journey.** The six core puzzles in greybox (simple placeholder geometry). Dam and Shelter in greybox only for the E14 test. Finale route checked (V1–V3), waystone menu (A1), sanctuary. | The whole journey from S to the Sanctuary can be played with one hand. |
| M3 | **Playtest round 1, then decide.** | H4 answered. E14 gate decided for Dam and Shelter. Top 3 friction points fixed. |
| M4 | **Polish in the order a judge sees it:** first 5 minutes, then the finale, then the middle. Apply the P11 ship rule. | Every shipped puzzle passes P11. |
| M5 | **Final build:** run the full test matrix (P12), playtest round 2 on that exact build (T15), capture video (P7), write the submission (P5). | No release blockers across P12, and T20 done on the final build. Evidence and the video come from the final build, identified by its build version. Video and submission are complete. |

**Sign-off rule:** any change after M5 reopens M5 for everything the change affects. That includes a bug fix, Chapter 2 content and Option B. Re-run the affected cells of the test matrix (P12) and the release blockers (P3). Update the tested build version. The evidence in the submission (T15) names the build that was actually tested, and that's the build uploaded to the release channel. If there's no time to re-validate, the change doesn't ship.

---

## Awards (one entry, one prize)

| Award | Status | Based on |
|---|---|---|
| **Best New Gaming** | Primary | Everything |
| Best First Five Minutes | Targeted | E1, E8, E9, E12, E15, E17, P3 |
| Boldest Original Concept | Targeted | I1–I8 |
| Best Accessibility Forward | Secondary | A1–A8, T3, nothing on a timer. EX3 if it passes. |
| Best Reason to Come Back | Secondary, only if EX2 ships | EX2, I5, bridges that persist |
| Best Agentic Interaction | Not targeted | Travelers follow routes and don't make meaningful decisions |
| Judge's Choice | Open | Two winners, for standout entries that don't fit one category |

## Hard rules from the brief

- Start membership and Developer Access for the submitter.
- New Experience: conceived and built after Sept 24, 2026, with no pre-existing code.
- Fully usable with hands, end to end, including navigation.
- APK on a "Competition" release channel with a working invite URL, tested from a second account.
- Public YouTube or Vimeo video under 3 minutes.
- Submission form completed.
- No changes after the deadline, and the build stays accessible throughout judging.
- Policy compliance. ⚑ Check whether a privacy-policy URL is needed. If it is, have a simple one ready (release build collects no data; see T10).
- Judges use Quest 3 or 3S. Owning one isn't required; the FAQ says a headset isn't needed to compete.
  - Quest 2 is the development and test device. Its results are labelled Quest 2 and are never presented as Quest 3/3S results.
  - Wherever a pass test names Quest 3 or 3S, it is met by a T20 run. If no run happens, the submission says it wasn't tested, and the risk stays open.

## Out of scope

| Item | Reason |
|---|---|
| Base-building economy and upgrade trees | A judge won't see it in about 10 minutes, and it dilutes the hook. Option B may be added after M5 if time allows, which reopens M5 (see Decisions). |
| AI/LLM features | Adds nothing to the core verb |
| Passthrough as the only mode | Tracking error would show at 3 cm scale. Table mode is an experiment (EX1), and the VR game ships either way |
| Gaze interaction | Quest 3/3S have no eye tracking, and the bridge mechanic doesn't need it. Pip can look toward the headset using head position; that isn't a gaze feature and isn't claimed as one |
| Multiplayer | Network risk with no gain to the core |
| Hand-to-hand contact | Where tracking is weakest |
| Timers, leaderboards, fail states | Conflict with E7, accessibility and the calm tone |
| Procedural levels | Hand-authored puzzles score better |
| Controller polish | No judging credit |

## Decisions

- **Concept:** Palmfolk.
- **Chapter 1:** six core puzzles: Board, First Bridge, Width, Fork, Arch and Finale. Dam and Shelter come in only through E14.
- **Board:** Pip walks across your hand. Owner confirmed.
- **Feature set frozen** on Nov 4, 2026. Only EX1–EX4 can be added, and only through their gates. Corrections and removals are allowed once the build has started.
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
| **Originality** | Clear central hook: the bridge you shaped stays, shows your hand and gets reused. Awesome Hand and Silhouette already use hands or shadows as bridges, so this combination has to be shown, not claimed. | Testers who weren't coached say the bridge stayed or kept their hand (I4), and recognize their hand in it (I6). |
| **Experience** | The whole journey is defined and the finale route is checked on paper. Onboarding, comfort, and whether the core loop stays fun after three bridges haven't been tested. | E1, E2 and E8 targets met. H4 answered yes. No more than about 60 s with arms raised. |
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

**M1 on your Quest 2, built on Windows.** Pip builds along your hand, you withdraw it, Bram crosses, and the bridge survives a save and reload. Then send it to a Quest 3/3S tester (M1b) before polishing anything. That proves more than any further change to the paper.
