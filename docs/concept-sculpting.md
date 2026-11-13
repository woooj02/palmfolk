# Palmfolk: hand-sculpting (concept change, Nov 13)

The owner changed the concept on Nov 13, after the first Quest 2 session: carrying Pip from one island to the
other "is a very lazy way of thinking". The requirements files stay as the record of v1.3; where they disagree
with this note, this note wins for the build. Ownership: Sam is away for a few days, so Dario carries Sam's paths
(Views, UI, Audio, Core/Content, Editor) until Sam is back.

## The promise

**Your hand sculpts the valley.** The shape you hold becomes the thing the tiny travelers need, and it stays
after you let go. No carrying, no pointing: every interaction is a hand shape.

| Hand shape | Becomes | Used in |
|---|---|---|
| Flat fingers, ring to ring | A bridge | First bridge, visitors |
| Fingers together, side by side | A wide bridge for a group or a cart | Width, finale |
| Fingers spread | A fork: one bridge to several islands | Fork |
| Curved hand over the rock | An arch | Arch |
| Upright hand, like a wall | A windbreak that shields travelers from a gust | Every crossing |

Bridges are built (hold still while Pip builds along your hand); walls are held in the moment (a gust gathers
for about 2.5 s at the valley's edge, then blows for 1 s). A gust that gets through makes the travelers brace and
wait: the game slows, it never fails you.

## What changed in the build

- **No carrying.** Live play skips Board: Pip starts across the first gap and the game opens on building.
- **Forgiving contact.** Live play connects within 4 cm of an anchor and breaks past 8 cm (was 2 cm and 4 cm),
  locks after 6 ticks (was 9), and accepts a slightly shakier hand for stillness. Quest 2 tracking wobbles by about
  a centimetre; the old window was too thin to find by feel. Core defaults and tests are unchanged.
- **The game says what to do.** Glowing rings on every anchor the hand must reach (gold, green once held), one
  line of text over the valley for the current moment and for why a bridge was refused, a larger gold Pip.
- **Slice 0's grey boxes are gone** from the M1 scene; they read as the islands.
- **Walls.** `GustField` (Core/Gusts.cs), `GameSettings.Gusts`, events `GustGathering`, `GustBlocked`,
  `GustHit`; `ValleyPresentation.Gust*` and `Bracing` for views.

## Still to do (owner's call on order)

1. Headset pass on the new opening and on walls (Quest 2).
2. ~~A bake moment~~: done Nov 13. A wave of golden sparks runs along the new bridge and lifts off (the existing
   build-done chime plays with it).
3. ~~Gust and shield sounds; a tally of shielded gusts~~: done Nov 13. `Cue.Wind` swells while a gust gathers,
   `Cue.Shielded` rings when a hand blocks it, `Cue.Gusted` rushes when it gets through; the prompt counts
   shielded gusts. Unity editor (batch build): compiles. Not heard on a headset yet.
4. Tidy the waystone (one compact menu, readable labels).
5. Video and submission text around the new promise.
