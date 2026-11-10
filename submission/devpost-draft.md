# Palmfolk submission draft

**Unpublished draft. No headset, simulator or target-device result is verified.** Replace all pending
fields using the exact final build. Do not submit this draft until the owner's evidence and access pass.

## Name and tagline

Name: **Palmfolk**

Tagline: **Shape a bridge with your hand. Let go, and a tiny world keeps the shape you made.**

## Project text (draft, under 500 words)

Palmfolk began with a small question: what if a bridge could remember your hand?

Tiny travelers step onto your fingers. Shape a route, hold still while the scout builds, then withdraw.
The bridge stays. A narrow path carries a walker; a wider, gentler route can carry a lantern cart.
Joint posts and fingertip knots are intended to make each finished bridge recognizable as your own.

The journey is designed for seated, hands-only play. Resting a flat palm plants the valley at a chosen
height and scale. Six puzzles introduce boarding, a first lasting bridge, width, branching, clearance
and a finale across the player's earlier bridges. Visitors are designed to give those bridges new uses
without clocks or penalties. VR is the baseline mode; Table mode and its persistence remain gated
on device testing.

The simulation uses tracked joint polylines and a bounded palm surface, running at a fixed 60 Hz.
Connections use separate enter and break distances. Building freezes sections as they complete,
so later movement cannot reshape finished work. Capacity checks use measured geometry. If tracking
is lost, a ghost route supports the traveler while the owning hand realigns, or the traveler returns
to the last safe island. The simulation is separated from presentation and supports deterministic replay.

Current verification is headless and Unity editor only. No headset gameplay, comfort, performance,
Table persistence, target-device result or playtest outcome is claimed in this draft. The final evidence
paragraph must state build version, device, N, newcomers, help, completion rate, median chapter time,
dropouts and deaths. Each result will name the actual tested device.

Next comes owner-run headset validation, improvements from observed friction, and completing only
the puzzles and features that meet the final quality and reliability gates. Unfinished features are cut.

## Hand-interactions answer (draft)

The hand is the terrain. Each finger becomes a joint polyline and the palm becomes a bounded surface.
Travelers attach to surface coordinates. Holding a connected, still shape freezes bridge sections;
withdrawing leaves them in place. Width, slope and headroom determine capacity, rather than a gesture
label. Tracking loss freezes support, shows a ghost and requires realignment before continuing.
Status: custom Core tested headless; live headset behavior unverified.

## Launch, play notes and access

- Target launch date: **Owner decision pending; no date promised.** Competition submission is not a launch date.
- Play notes draft: Seated, good lighting, room for relaxed arm support; a table if available. Controllers off.
  Intended chapter length about ten minutes. Shipped modes and minimum build/device support: pending final pass.
- Competition channel invite: owner-created URL pending. Second-account install/access test: **Not run**.
- Privacy-policy URL: owner decision/channel requirement check pending. No collection claim may be published
  until the final release APK and offline behavior pass T10.

## Final evidence paragraph (fill only after testing)

Build [version/full revision], [device], [mode]: N=[pending], new to hand tracking=[pending]. Help per tester
[none/verbal/hands-on, anonymous count]. Chapter completion=[pending], median=[pending], disruptive dropouts
per puzzle=[pending], deaths=[pending]. Quest 3 and Quest 3S runs [pending separately]. Report unavailable
target-device testing explicitly. Earlier build rounds are separate and do not supply the final card.

## Competition-channel metadata draft

- App name: Palmfolk.
- Description: A seated hands-only puzzle adventure where tiny travelers build bridges in the shape of your hand.
- Header image: real final-build capture pending; use `capture-list.md`, no invented gameplay.
- Cover/icon/splash/title/credits: final assets and licence review pending (P9); do not use a draft as final art.
- 3–5 screenshots and video: real final-build captures pending; device labels required.
- Technical stack review: pinned Unity 6000.0.66f2, Meta XR All-in-One 207, OpenXR; runtime platform claims
  must match what the shipped build actually calls. Do not imply every SDK feature is integrated.

Owner review before publication: remove cut features and unsupported claims, supply launch/access/privacy
decisions, verify word/character limits, licences and account access, and reopen M5 after any later change.
