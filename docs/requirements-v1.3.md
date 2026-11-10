# Palmfolk: Requirements v1.3, part 1: the game

**Meta VR Start Developer Competition 2026 · Gaming track · New Experience division · Deadline Nov 18, 2026, 1:00 pm MST**

Part 2 (accessibility, technical, polish, out of scope, decisions): [`requirements-v1.3-quality.md`](requirements-v1.3-quality.md).
Order of work, headset tests, gates and milestones: [`build-plan.md`](build-plan.md). Why v1.3 exists: [`../audit.md`](../audit.md).
v1.2 stays in `requirements-v1.2.md` as the Nov 4 record. Where the two disagree, v1.3 wins.

**Status:** concept locked. v1.3 is a judge pass made on Nov 9 at the owner's request, before any code exists. Rows marked
**v1.3** are new or changed; unmarked rows are v1.2 text. Numbers are starting values until the headset sets them (M1).

## What v1.3 changes, and why

The competition exists to fill Meta VR Glasses (spring 2027: eyes and hands, no controllers, narrower view) with apps.
The rubric asks for device capabilities that "drive repeat usage", a "habit-forming purpose", and "strategic use" of
gaze, hand tracking, passthrough, spatial anchoring and FoV-aware design. v1.2 answered with hand tracking alone and no
reason to return. v1.3 keeps the hook and builds those answers out of it:

| # | Change | Rubric line it answers |
|---|---|---|
| 1 | **Planting:** rest a flat palm for a second and the valley grows there. One gesture sets place, height and scale (E21) | Clear onboarding; seated; first five minutes |
| 2 | **Table mode:** the valley sits on your real table in passthrough, held by a saved spatial anchor, so it's still there tomorrow (MR1–MR5). Ships only through its gate | Purposeful passthrough ("a table becomes the play surface"); persistent anchors |
| 3 | **Visitors:** the return loop is core, not an experiment. Each visit brings a traveler your existing bridges can't serve (R1–R4) | Repeat usage; habit-forming purpose |
| 4 | **Eyes and hands:** look at a traveler to see what it needs; look and pinch for the menu (GZ1–GZ3) | Gaze interactions (ISDK v207); "eyes and hands" |
| 5 | **FoV:** layout fits Meta's 58° × 54° design area and is checked with the FoV simulator (E18) | FoV-aware design |
| 6 | **Cut:** Dam, Shelter, Chapter 2 and base building. Six puzzles, finished | "A focused, satisfying experience is a stronger showcase" (Meta, week 6 update) |

Build priority if anything has to give: core promise, then six puzzles, then visitors, then eyes, then Table mode.
VR mode ships either way, and a mode or feature that isn't finished and tested is cut, never shipped rough (P11).

## Prior art judges will recognize

- **Awesome Hand:** won Best Implementation of Hand Interactions here in 2025. Has a stage where your hands are a bridge.
- **Silhouette:** your hand's shadow can be a bridge creatures walk across, and it can block water.
- **Pocket Lands:** won the same 2025 award with a lap-sized diorama shaped by hand, in passthrough.
- **Tiny Castles (Meta), Moss:** tiny characters and a giant player. A comparison for mood, not mechanic.
- **Make with Puppet, Hand Shadows:** hand-shape games entered here. Different loops.

A hand acting as a bridge is not new. Palmfolk's promise is the combination: **shape a bridge with your hand, withdraw
it, and the travelers keep using it, on your table, the next day.** We show that; we don't claim nobody has tried it.
These names stay in our notes only: the rules bar brand names from the Project and its materials.

## Concept

**Tiny travelers walk across your fingers, and the bridges they build stay after you let go.**

- **Pip, the scout:** light and curious. Rides your palm, walks your fingers, builds a bridge in your hand's shape while you hold still.
- **Bram, the lantern-keeper:** old and careful. Pulls the lantern cart, and only crosses finished bridges that are wide enough.

**Story (no words):** night is falling, and the lantern has to reach the sanctuary. Darkness advances one step per solved
puzzle, never on a clock. Afterwards the valley is yours to keep: travelers arrive, and it grows (R1–R4).

**Core loop:** get the scout across → shape a route with your hand → hold still 2–3 s while Pip builds → withdraw your
hand; the bridge stays → the carrier crosses.

### Shape decides the bridge; geometry decides what it can carry

A bridge is classified by its **measured geometry**, not by a detected gesture. Capacity comes only from width, slope and clearance.

| Typical hand shape | Geometry | Name | Typical capacity |
|---|---|---|---|
| One extended finger | Narrow, single path | Rope walk | One walker at a time |
| Flat hand, fingers together | Wide, single path | Plank road | Groups and the lantern cart |
| Fingers spread | Branching paths | Forked route | One walker per branch |
| Curved hand | Raised middle | Arch | A narrow arch carries one walker; a wide arch can carry the cart |

| Traveler | Minimum width | Maximum slope | Minimum clearance |
|---|---|---|---|
| Walker (Pip, apprentice, single carrier) | 1.0 cm | 35° | 3.5 cm |
| Group | 2.5 cm | 25° | 3.5 cm |
| Lantern cart | 2.5 cm | 25° | 5.0 cm |

**Crossings reused as-is are validated against the cart's full rule from the start.** That is the Width bridge (A→C).
The First Bridge (S→A) is the deliberate exception: it must fail the cart, so the finale rebuild always happens (V2).

- **Clearance:** free headroom above the deck along the whole route.
- **Slope:** measured along the deck. Whether Pip adds end ramps is decided by H1.
- **Width:** from the OpenXR joint radii (H3 confirms Quest returns usable values). Neighbouring finger surfaces with a
  gap under 1.0 cm along most of their length merge into one surface; wider gaps become branches. Width then snaps to a
  tier (rope or plank), so noise can't change a bridge's class. **Fallback:** 1.2 cm for one finger, 2.0 cm per merged finger.
- **Scale:** every distance here is at default world scale and scales with the world (A4). Planting (E21) sets the scale.

## The valley

```
                    [G Garden isle]         [L1 Lookout]  [L2 Lookout]
                     (visitors, R2)                 \        /
                                                  (Fork: walkers)
[S Camp] ==First Bridge== [A Ledge isle] ====Width==== [C Market isle] ---Arch--- [E Rock isle]
   :  Board: Pip walks S→A :                                 |
                                                     ==Finale plank==
                                                             |
                                                       [Sanctuary]
     ==  on Bram's route       --  side route, walkers (a wide arch also carries the cart)
```

| ID | Requirement | Pass test |
|---|---|---|
| **V1** | **Bram's route uses exactly three crossings:** S→A (First Bridge, rebuilt in the finale), A→C (Width, reused as-is), C→Sanctuary (built in the finale). Fork and Arch are side routes the cart never needs. | Walk the route in the editor with the cart's rules; every edge passes. |
| **V2** | **The finale twist always happens.** The ledge on A is narrow (1.5 cm), so only a narrow route fits in First Bridge. Between puzzles Pip visibly hammers the ledge wider. In the finale the widened ledge accepts a plank, and the rope walk glows as "too narrow for the cart." | In 100% of runs the finale asks for the S→A rebuild, whatever the player built earlier. |
| **V3 v1.3** | **No softlocks.** Before the finale, and before each visitor (R2), the game checks every edge on the needed route against that traveler's rules. A failing edge glows and must be rebuilt. | Automated test: corrupt or alter each saved bridge in turn. The finale and every visitor remain completable. |
| **V5 v1.3** | **The valley has dimensions and fits a small table.** Active gaps are 12–15 cm. Islands sit on an arc around the planted palm, so each gap is at a similar reach. Footprint target: 60 cm wide × 45 cm deep at default scale. | Every gap can be reached with either hand, seated, no leaning, fingers pointing sideways (T14). If a fixed layout can't do this, EX4 is tried. |

(V4, the Dam ford, is removed with Dam.)

## Innovation (25%)

| ID | Requirement | Pass test |
|---|---|---|
| I1 | The hand's geometry is the terrain. Route shape and bridge geometry come from the hand, not from triggering a gesture. | Two different hand shapes across the same gap give visibly different bridges in shape, width and branching. |
| I2 | Bridges keep the shape you held. | The finished bridge is within 1 cm of the **filtered, displayed hand route at the moment each section was committed** (T3). |
| I3 | Every puzzle is solved with a hand **shape**. No puzzle is solved by a button, a menu, a pinch or a look. | No puzzle can be solved with only a poke, a pinch or gaze. |
| I4 | The thing that stays is the point. Picking characters up is never the main verb. | Uncoached testers say some version of "it kept my hand" or "the bridge stayed," not "you pick up little people." |
| I5 **v1.3** | **Your bridges have consequences.** The finale runs over bridges you built, one must be rebuilt (V1–V3), and every visitor's need is set by what you built before (R2). | Every finale and visitor route includes the player's own bridges. Testers mention this unasked. |
| I6 **PRIMARY** | **The bridge shows your hand.** Knuckle posts stand at the joint positions, and knots sit at the fingertips. | Testers recognize their own hand in a finished bridge without being told. |
| I7 | **Judge the result, not the gesture.** In Arch, the route has to clear the rock, connect both anchors and stay walkable. Any hand shape that produces that geometry succeeds. | Every valid geometry is accepted. At least 3 of 5 testers use a curved hand without being told. |
| **I9 v1.3** | **It stays in your world.** In Table mode the valley and every bridge hold their place on the real table across sessions (MR3). | A tester who returns the next day finds the valley where they left it and says so unasked. Only claimed if MR ships. |

(I8, extra hand roles, is removed with Dam and Shelter.)

## Experience Design (25%)

| ID | Requirement | Pass test |
|---|---|---|
| **E21 v1.3** | **Planting, the first ten seconds.** Pip waves from a floating seed-lantern. A ghost palm lowers once to show the move. Rest a flat palm on a table, or hold it flat in the air, for 1 s: the valley grows from under it. That one gesture sets the valley's place, height, facing and world scale (T6), and in Table mode writes the anchor (MR2). Replanting is always available from the waystone. | 4 of 5 uncoached testers plant within 20 s of the first scene. No text is shown. Works on a table, on a lap tray and in the air. |
| E1 | First boarding without a text tutorial. | 4 of 5 uncoached testers get the scout onto their hand within 30 s of planting. |
| E2 **v1.3** | A complete arc in 10 minutes or less, from launch to the sanctuary lighting up and the first visitor arriving. | Median Chapter 1 time for first-time players is 10 minutes or less. |
| E3 | Seated, within a 2-foot radius. | No leaning or standing needed. |
| E4 | Each puzzle adds exactly one new idea. | Testers can name what's new in each puzzle. |
| E5 | Travelers show their state through body language: wave, hesitate, test a plank, crouch and grip, hammer, celebrate, look back. | Testers can tell what a traveler needs without any text, and without using gaze (GZ1 only adds to it). |
| E6 **v1.3** | Limit arm fatigue. Islands stand just above the planted palm height, so the forearm or elbow can rest on the table while shaping. | Arms are unsupported for no more than about 60 s at a stretch. A rest beat comes between puzzles. |
| E7 | Failure means a traveler hesitates, never death or punishment. | 0 traveler deaths across all playtests. |
| E8 | After about 10 s stuck, a moving translucent hint hand shows a working shape. It must look different from the still, carved recovery ghost (T5). | No tester stays stuck for more than 60 s. Testers can tell the two apart. |
| E9 **v1.3** | **No clash with the system gesture.** Nothing asks for a pinch while the palm faces the headset. Carrying is palm-up toward the sky. Look-and-pinch (GZ2) works with the hand resting, palm down or sideways. | The system menu never opens by accident in 10 full runs, including while looking down at Pip on a palm-up hand. |
| E10 **v1.3** | **Comfortable hand placement inside the cameras' view.** The player chooses the spot by planting; the ghost palm suggests one 30–45 cm in front of the chest, 10–30° below the head's forward direction. H2 sets the final values. | Dropout rate below the T19 threshold. Testers report no shoulder strain, and E6 holds. |
| E11 | Travelers are about 3 cm tall at default scale. Their size changes only with world scale (A4). | At default size, testers can read every E5 state. |
| E12 | **Short builds that are fun to watch:** 2–3 s of stillness while Pip lays planks in time with hammer taps. A see-through preview of the bridge, with its capacity icon, appears once the route connects. | Testers describe holding still as "helping." Fewer than 1 refused build per puzzle. |
| E13 | **Story without words.** In the finale, light runs along every bridge you built. | Testers can retell the story (lantern, night, home). |
| E15 **v1.3** | **Cold start in 10 s or less**, from the app icon to Pip waving. No title-screen gate. Returning players land in their living valley (R1). | Under 10 s on Quest 3 and 3S, across 10 launches. |
| E16 **v1.3** | **The ending opens the loop.** As the sanctuary lights, the first visitor is already walking in (R2). | Testers who finish start helping the visitor without being asked, or say they want to. |
| E17 | **Feedback when a connection forms.** The fingertip glows within connect range. A chime and a puff of dust play when the connection locks. | No tester asks "is it working?" |
| E18 **v1.3** | **FoV-aware layout.** Everything needed for the current step fits inside ±25° of where the player looks, which is inside Meta's 58° × 54° design area for Meta VR Glasses (full view 70° × 66°). Turning the head is fine; leaning isn't. | Passes Meta's FoV simulation with the design-area mask, on every puzzle and the waystone. The submission says how it was checked, and that no Glasses hardware was used. |
| E19 **v1.3** | **Startup safeguards.** If controllers are in hand, have just been set down, or no usable hand is tracked, Pip waves and planting waits. An animation shows controllers being put down. Short text appears only if that doesn't resolve it (P8). Permission prompts are kept to the minimum the shipped mode needs. | Tested in three states: controllers held, controllers set down, no usable hand. A first-time player reaches planting without help. |
| E20 **v1.3** | **Early hook.** The time from the first scene to the first bridge staying after the hand withdraws is logged, including planting (T13). | Median under 90 s, most players under 2 minutes. |

(E14, the Dam and Shelter gate, is removed.)

### Chapter 1: six puzzles, about 7 minutes

| # | Puzzle | New idea | Target |
|---|---|---|---|
| 1 | **Board** | Pip steps onto your fingertips and walks across your hand from S to A. The first thing you feel is his steps on your fingers, not a pick-up. | 0:45 |
| 2 | **First Bridge** | Point from the narrow ledge on A to S. Pip builds a rope walk. Withdraw your hand, and the apprentice crosses. **This is the hook.** | 1:30 |
| 3 | **Width** | A group needs a plank road from A to C: flat hand, fingers together. | 1:00 |
| 4 | **Fork** | Two travelers, two lookouts: spread your fingers. Anchors are zones sized to fit any planted hand. One branch is too steep and gets refused, so you lower that finger. Teaches slope. | 1:00 |
| 5 | **Arch** | A rock sits between C and E. Any valid route clears it; a curved hand is the readable way (I7). | 1:15 |
| 6 | **Finale** | Rebuild S→A as a plank road (V2). Bram crosses your Width bridge. Build the last plank to the Sanctuary. Light runs along every bridge, and the first visitor walks in. | 1:30 |

## The return loop (v1.3; replaces EX2, Chapter 2 and base building)

| ID | Requirement | Pass test |
|---|---|---|
| **R1** | **You return to a living valley.** On every later launch the valley is as you left it, and travelers are already walking the bridges you built. | Relaunch: within the E15 time, at least one traveler is crossing a player-built bridge. |
| **R2** | **Visitors.** After the finale, each launch brings one visitor whose need at least one existing crossing can't meet, so you rebuild or add a crossing. Needs are stated only through the capacity table and reuse existing verbs. Ringing the waystone bell calls the next visitor at once: nothing is gated by a clock, a streak or a penalty. Five hand-authored visitors ship; starting designs below. | Each visitor is completable with one hand (A2) and needs no new instruction. Testers who finish Chapter 1 choose to help at least one. |
| **R3** | **Growth you can see.** Each visitor you help settles in: one sanctuary window lights and stays lit, and the bridge they used carries their mark. Five dark windows are visible from the finale on. | A returning tester can say how far along they are without any number on screen. |
| **R4** | **Rebuilding is the upgrade.** Frozen sections never move (T3), so improving a crossing means Pip takes the old one down and builds your new shape. | The old bridge is never lost before the new one passes validation. |

| # | Visitor (starting designs, set at M3) | Need | Verbs reused |
|---|---|---|---|
| 1 | Gardener, arrives with the finale | A new walker crossing A→G | Any shape, new gap |
| 2 | Merchant with a cart | C→E must carry a cart over the rock | Width + Arch |
| 3 | Twins | From G to two garden plots | Fork + slope |
| 4 | Stargazer | L1→L2, passing above the Fork | Clearance |
| 5 | Lantern-maker with a cart | The Gardener's A→G crossing, rebuilt as a plank road | Width, on your own earlier bridge |

## Eyes and hands (v1.3)

Built on the Interaction SDK's gaze components, so the same code reads real eye tracking where a device has it and head
direction where it doesn't. Quest 3 and 3S have no eye tracking: there, "look" means "face". The submission says so.

| ID | Requirement | Pass test |
|---|---|---|
| **GZ1** | **Look to ask.** Look at a traveler for about 300 ms and it turns to you and holds up a carved sign of what it needs (rope, plank, arch, branches), shown by shape and motion (A7). Looking away puts the sign down. Nothing else changes on a look. | Testers discover it unprompted and can say what the sign means. Every puzzle is still solvable if gaze is switched off. |
| **GZ2** | **Look and pinch on the waystone.** Every waystone action can be chosen by looking at it and pinching with either hand, wherever that hand rests. Poking still works (A1). At most four targets show at once, in one row, each at least 6° wide with 1° between them (Meta's minimum is 3° for real eye tracking; head direction is coarser). | A full session's menu actions are done by look and pinch with the hand resting on the table, left and right. No unintended selections in 10 runs. |
| **GZ3** | **Honest gaze source.** The game reads whether the gaze pose is real or synthesized from head pose, and works with either. The eye-tracked path is exercised only in Meta XR Simulator unless hardware is available. | The stats panel shows which source is active. The submission states what was tested on which device. |

## Table mode (v1.3; ships only through gate H5 in the build plan)

| ID | Requirement | Pass test |
|---|---|---|
| **MR1** | **Your table is the play surface.** The valley stands on the real surface under the planted palm, in passthrough, at the height that lets your forearm rest. The stylized wooden hand is still drawn over your own hand. | Testers rest an arm on the table without being told. Asked what the room added, they name the table. |
| **MR2** | **Placement never needs a room scan.** Planting (E21) is the only placement step. If a scene model with a table already exists, the valley snaps to that plane's height and near edge; Space Setup is never triggered. | Works in a room with no scene data, on tables of different heights, and with no table (palm in the air). |
| **MR3** | **It's still there tomorrow.** Planting saves a spatial anchor, and the save file keeps its ID. On relaunch the valley returns to the same spot with every bridge. If the anchor can't be found within 5 s, Pip waves and you replant; bridges are stored relative to the valley, so none are lost. | Relaunch 10 times across two days and one moved table: the valley returns within 2 cm, or asks to be replanted. No bridge is ever lost. |
| **MR4** | **Night falls on your room.** The room dims one step per solved puzzle and returns in the finale's light. It never dims so far that the table edge can't be seen. | Testers link the dimming to the story. With reduced motion on (A5), steps fade slowly instead of pulsing. |
| **MR5** | **VR mode always exists.** The same game in a fully rendered night valley, chosen on the waystone, and used automatically when passthrough isn't available or permitted. | Every puzzle, visitor and save works in both modes, and a save made in one opens in the other. |
