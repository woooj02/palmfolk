# Audio and captions binding

Original procedural placeholders only: footsteps, hammer, rope creak, connection, completed bridge,
refusal, visitor arrival and lantern. Every cue has an in-world caption. No external sample or asset added.
These are not final polished sounds, an audible quality pass, or headset spatial-audio evidence.

`AudioPresenter.Consume(events, frame)` maps actual section commits, completed bridges, refusals and
the M1 lantern finish. Supply the corresponding tick's presentation, so positions are not silently taken
from a later frame when processing a backlog. Call `FootContact(worldPosition)` on the animation's
actual contact, and `Play` for explicit platform/director connection/visitor signals. Do not map boarding
or build-start to a missing connection-lock event. Such events are not currently exposed by Core.

Footsteps use a spatial source at the contact point, not the player's head. Independent captions remain
with each cue and expire; toggling captions off hides every active caption. Voice instances are reused
after their sound and caption finish. Do not call `Play` every rendered frame for a persistent state.

Unbound: exact connection-lock event, visitor arrivals, foot contacts from final rigs, night music progression
and real scene integration. Sound-alone completion recognition, caption legibility and spatial placement
need the owner to listen/inspect in editor and on Quest 2. Original synthesized placeholders still need P11 polish.
