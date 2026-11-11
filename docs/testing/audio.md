# Audio and captions binding

Original procedural placeholders only: footsteps, hammer, rope creak, connection, completed bridge,
refusal, visitor arrival and lantern. Every cue has an in-world caption. No external sample or asset added.
These are not final polished sounds, an audible quality pass, or headset spatial-audio evidence.

`AudioPresenter.Consume(events, frame)` maps actual section commits, completed bridges, refusals and
the M1 lantern finish. Supply the corresponding tick's presentation, so positions are not silently taken
from a later frame when processing a backlog. Call `FootContact(worldPosition)` on the animation's
actual contact, and `Play` for explicit platform/director connection/visitor signals. Do not map boarding
or build-start to a missing connection-lock event. Such events are not currently exposed by Core.

Footsteps use a spatial source at the contact point, not the player's head. Configure `ContactAudioBinding`
with the audio presenter and **only** the relevant view's content root; it binds newly created/pool rigs
without scanning the whole scene. Rig callbacks already supply world positions. Voices reuse after playback;
repeated same-kind sounds extend one stable caption instead of spawning many labels. Captions expire and
the toggle hides all of them. Do not call `Play` every rendered frame for a persistent state.

`NightMusic` is an original synthesized loop with captions, driven by D3 puzzle count/finale state. It fades
up as night advances, settles in the finale and pauses with the game. It implements the valley view port.
This is a placeholder arrangement to listen/review, not a finished music or headset-audio claim.
Music owns a child source/caption root; it never moves the platform's shared scene root or camera.

The live scene binds contact audio, visitor arrival/settling and music. Visitor events use the visitor or
sanctuary location; several catch-up ticks share the latest frame position, an explicit timing limit.
Unbound: exact connection-lock event. Sound-alone completion recognition, caption legibility and spatial placement
need the owner to listen/inspect in editor and on Quest 2. Original synthesized placeholders still need P11 polish.
