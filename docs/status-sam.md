# Sam status

Work items: [`team-plan.md`](team-plan.md#sams-work). Labels: headless, Unity editor, simulator, Quest 2.

Latest verification, Nov 12 with Dario's new view contracts: **headless** 234 developer / 218 release
tests; Python 3.12 source checks (97 C# files). **Unity editor**: compile exit 0/no `error CS`; connection/
recording item passed 291 developer tests, support-cursor item passed 272 release tests. Both Unity gates
and manifest regressions will be rerun after the ghost/hint binding below.
No simulator, Quest 2, Quest 3/3S,
APK, installed build or M1 sign-off is claimed. Individual results below are historical by item.

## Done

- **S6 support cursors, Nov 12 — Unity editor.** Travelers, Bram, residents and the arriving visitor now
  pass their actual `Support`/`SupportCursor` to foot planting; Pip's Board carry uses `PipCursor`.
  No nearest-route search chooses their deck/branch. Group members have route-relative lanes. Moving
  hand geometry retains stance, persistent-support changes reset it, frozen recovery holds the feet
  without new landings and the returning phase walks. Tests cover an ambiguous self-crossing route,
  translating support without false steps, and real traveler/Bram cursor delivery. Legacy M1 and the
  builder scaffold retain projection (they are not `TravelerView` ports). Headless 234 / 218, source
  checks, compile and 272 release Edit Mode tests pass. Device contact/readability is not verified.

- **S5 connection feedback and format-3 menu replay, Nov 12 — Unity editor.** Pulled `f7237df` and
  read Dario's note. Actual `ConnectionLocked` events now drive the connection chime/caption and a short
  source/destination dust burst, once per hold; flags/proximity do not trigger repeated effects. Reduced
  motion leaves the fading specks still. Added a synthetic in-memory test using the actual waystone,
  runner and `SessionRecording`: settings/mode changes encode/decode in format 3 and replay with identical
  decisions/final save. No recording files are written by the test. Removed the obsolete waystone caveat.
  Headless 234 / 218, source checks, compile and 291 developer Edit Mode tests pass. No listening/headset pass.

- **Privacy page requested by owner, Nov 11 — published web page, not a headset result.**
  Published https://palmfolk-privacy.curiousnote2.chatgpt.site with Aryan Neopane as developer and
  contact@keepsrunning.com as contact. Public access/deployment, unauthenticated HTTP 200, expected
  content and narrow browser-panel readability verified. Source/record: `testing/privacy-policy.html`
  and `testing/privacy-publication.md`. The page is explicitly pre-release; final APK/privacy disclosures
  remain owner verification. Submission draft was left unchanged as requested.

- **S3 measured overlay and integration cleanup, Nov 11 — Unity editor.** Added `LiveStatsBinding` in
  Sam's shared Developer/Stats subfolder. It forwards the existing tick target and uses Dario's exact T19
  per-puzzle dropout/disruption counters independently of any recorder. FPS comes from rendered time;
  gaze source, running mode/fallback and build version come from the runner/application. It writes no files.
  Tests force a real owning-hand dropout, check the overlay count, synthetic FPS arithmetic, detachment
  before startup and restoration of the original tick target. Release compiles out stats and their tests.
  Also fixed detached poke tips, dimming re-enable, and cart bodies/wheels on actual cart travelers.
  Full checks: headless 228 / 214, source/28 manifest checks, Unity compile and 283 / 264 Edit Mode tests.
  The synthetic FPS number is not a device measurement; full-scene headset profiling remains open.

- **S2/S5/S8/S9 live composition, Nov 11 — Unity editor.** Added the scene setup menu and
  `GamePresentation` bound to runner Opened/Ticked/Presented; actual saved settings control the views,
  x-ray, captions and music. D8 supplies visitor/resident figures, shape-coded bridge marks and five
  bitmask-driven windows, including on reload. D10 supplies four distinct carved need-sign shapes.
  Anchor search shows Pip waving and the search line; a lost anchor returns to the existing planting
  invitation. T18's quality line/loosened-stillness icon use actual presentation flags. Arrival/settling
  sounds use actual visitor events/locations. Table dimming now calls the SDK brightness API but remains
  capped at zero until owner verification. Headless 228 / 214, source checks, compile and 263 release
  Edit Mode tests pass. Setup and limits: `testing/live-presentation.md`. No shared scene/settings changed.

- **S4 runner binding, Nov 11 — Unity editor.** `WaystoneBinding` queues pause/resume/restart/replant/bell,
  applies and saves every setting, chooses mode through D9 (reflecting real VR fallback), and saves before quit.
  It supplies D10's four world-space gaze buttons, dispatches selections and draws the hovered outline.
  `WaystonePokes` maps ten actual ISDK poke interactors to either hand's usable tips/radii and disables them
  on invalid tracking, controller use or focus loss. Editor tests cover command timing, settings/save
  round-trip, mode mapping/fallback, quit callback, gaze centres/detach and both five-tip mappings/loss.
  Headless 228 / 214, source checks, Unity compile and 259 release Edit Mode tests pass. Live poke and a
  full left-only/right-only session still need the owner. Format-2 replay currently lacks dynamic settings/
  mode changes: Dario needs to publish a recorder contract before those sessions can claim identical replay.

- **S7 visitor content, Nov 11 — headless and Unity editor.** Updated from main through D11.
  The shipped `ChapterOne` now includes all five frozen visitor mappings, using D8's public types.
  New content regressions play the chapter and help all five visitors with each hand, then reload
  and check all five windows, resident routes and bridge marks. Headless: 228 developer / 214 release;
  Python 3.12 source checks pass; Unity batch compile exit 0/no `error CS`. Physical reach/H1 remains unverified.

- **Unity setup, Nov 10 — Unity editor (batch mode).** Imported only this worktree with the installed
  Unity 6000.0.66f2. Initial import exited 0 with no `error CS` lines. Unity-generated defaults remain
  uncommitted as required by `unity-setup.md`.
- **S1, Nov 10 — headless and Unity editor (batch compile).** Added `Palmfolk.Fixtures.HandSequence`:
  four shapes, three synthetic sizes, mirrored hands, pose placement, seeded bounded tremor,
  explicit tracking-fault windows and withdrawals. Usage: `testing/hand-fixtures.md`.
  Both headless configurations pass: 67 developer / 59 release tests. Python 3.12 source checks pass.
  Unity fixture/test assembly compile exited 0, with no `error CS` lines. Tests exercise an M1
  build/withdraw/cart crossing and loss/far-return recovery. No headset result is claimed.
  Dario's D2 tests now consume the fixtures (see Dario's status); D3/D4 adoption remains his work.
- **S7 layout handoff, Nov 10 — headless calculations only.** `testing/valley-layout.md` supplies
  island/deck coordinates, radii, anchor construction, ten 12–15 cm gaps, six puzzle and five visitor
  mappings within a 60 × 45 cm bound. Main cart route has exactly three crossings. Source checks
  and both headless configurations pass (67 / 59). No runtime content or reach/FoV pass claimed.
- **S10 tooling, Nov 10 — headless and Unity editor.** Added compile/Edit Mode wrappers, owner APK
  command, source-revision bundle version/evidence report and release recording-define build gate.
  The manifest validator now enforces shipped-mode permission allowlists; 28 regression checks pass,
  including planted bad manifests. Windows CI parses owner scripts and both existing CI platforms
  exercise the validator. Local Unity compile: exit 0/no `error CS`; Edit Mode: 58/58 pass.
  Both headless configurations and Python source checks pass (67 / 59). APK script syntax parsed;
  no APK, signing, installed build, final manifest or device/privacy behavior verified.
- **S11, Nov 10 — documentation prepared, headless checks only.** `testing/owner-checks.md` covers
  Quest 2 slices 1–14 and links Slice 0. Added all 324 P12 matrix cells, E18 FoV procedure and
  M1 sign-off template. Every device result is explicitly Not run. After rebasing D2, headless 99 / 91 and source
  checks pass; the owner still executes the procedures and judges M1.
- **S12 draft pack, Nov 10 — documentation/headless only.** Added `submission/devpost-draft.md`
  (tagline, short project text, interaction answer, play notes, metadata and explicit evidence fields),
  timed real-footage/screenshot list and credits/P9 checklist. No unsupported device claims or invented
  metrics. Publication, actual assets, final licence audit, launch date and channel access remain open.
- **S2 M1 adapter, Nov 10 — Unity editor.** `PalmfolkView` reads interpolated presentations, applies
  valley pose/scale and draws actors, live/preview/frozen/persistent routes, every curve in a bridge tree,
  joint posts/tip knots, still recovery route, distinct moving hint and segment refusal crosses.
  Synthetic replay integration test passes; Unity 91/91 Edit Mode tests, compile exit 0/no `error CS`.
  Rope and plank QA renders inspected for structural distinction. These are editor/synthetic images,
  never submission or headset footage. Full S2 remains open for D3 valley data and hand/hint bindings.
- **S3 M1 diagnostics, Nov 10 — Unity editor.** Shipping `XRayView` toggles M1 routes/bridge tree,
  frozen joint marks, progress and solid/dashed connect/break rings. Added only Sam's stats subfolder
  under shared Developer; it writes no files and shows unbound measurements honestly. Explicit editor
  define setup/restore verifies stats presence in developer mode (102/102 tests) and absence with
  shipping x-ray intact in release mode (93/93). Batch compile exited 0/no `error CS`.
  Headless 99 / 91 and Python source checks pass. Actual gaze/mode/fps/per-puzzle counters still need
  the platform/director binding; a full valley graph awaits D3 presentation fields.
- **S5 cue/caption implementation, Nov 10 — Unity editor.** Original synthesized placeholders for
  eight cue kinds, spatial emitters, reusable voices and in-world captions. M1 section commits,
  bridge completion, refusals and lantern finish map to distinct cues. Tests cover caption completeness,
  finite/bounded waveforms and meaningful event mapping; 95/95 Unity Edit Mode tests and batch compile
  pass (exit 0/no `error CS`). Headless 99 / 91 and source checks pass. No audible or headset-quality
  pass claimed. Binding guide: `testing/audio.md`; original-code notice added without external assets.
- **S6 greybox state/rig implementation, Nov 10 — Unity editor.** Original 3 cm bodies with arms,
  feet/contact-shadow shapes and nine poses: wave, hesitate, test, grip, hammer, celebrate, look back,
  refuse and walk. Persistent states read the frame; transient gestures consume fixed-tick events.
  Actor facing follows travel and cart placement follows Bram. Compile exits 0/no `error CS`;
  96/96 Unity Edit Mode tests pass, including replay/rig checks. Headless 99 / 91 and source checks pass.
  These are greybox poses, not final art/animations or a completed contact/readability pass.
- **S8 startup implementation, Nov 10 — Unity editor.** Seed-lantern, waving Pip, one descending
  flat-palm demo, controller put-down silhouettes, delayed short E19 prompts and the exact T18 line.
  `Planted` drives presentation-only valley growth; reduced motion shows full scale immediately.
  Three startup states and growth/Core-pose separation tested: 98/98 Unity Edit Mode tests pass;
  batch compilation exits 0/no `error CS`. Headless 99 / 91 and source checks pass. Platform scene
  wiring, actual prompt placement, legibility, cold-start and planting times remain unverified.
- **S9 night presentation port, Nov 10 — Unity editor.** Rough VR base, five sanctuary windows,
  per-puzzle night targets, reduced-motion monotonic fade and finale lighting along supplied curve trees.
  The component accepts director state, never infers puzzle progress. Table dimming defaults to zero
  until an owner-tested edge-visibility limit and platform callback exist. After rebasing D3: headless
  136 / 128, source checks and 136/136 Unity Edit Mode tests pass; compile exits 0/no `error CS`.
  No headset, final visual quality or Table behavior is verified. See `testing/night.md`.
- **S7 executable chapter, Nov 10 — headless / Unity editor.** `Core/Content/ChapterOne.cs` uses
  D3's published `ValleySpec`: ten islands/edges and all six puzzle definitions. Actual director tests
  complete with either hand and validate exactly three cart crossings. Full cart rule is explicit for
  Width. Greybox rock remains a provisional H1 value; measured obstacles can be passed explicitly.
  Also flattened the S1 Curved fixture's first bend below the walker's slope limit, as Dario identified.
  Headless 141 / 133, source checks, batch compile (exit 0/no `error CS`) and 141/141 Unity tests pass.
  Physical reach/comfort, true D4 branching and visitor data remain open.
- **S2/S3/S5/S8/S9 valley binding, Nov 10 — Unity editor.** Added `ValleyView` against actual
  `ValleyPresentation`, sharing M1's rope/plank geometry. Draws every island/obstacle, saved tree,
  traveler/group, ledge width, preview and refusal; x-ray uses the real edge graph/zones. Startup,
  night and audio accept valley frames; night restores light for the finale and uses all saved bridges.
  Full-chapter presentation integration test passes. Final source: headless 141 / 133, Python checks,
  compile exit 0/no `error CS`, 142/142 Unity Edit Mode tests. No platform/shared scene was changed.
- **S4 input/view port, Nov 10 — Unity editor.** Original carved waystone with actual installed ISDK
  plane/circle/poke components, five pages of four targets, all required actions and typed requests.
  Distance-aware geometry tests prove 6° targets/1° gaps; left/right request routing and unsafe pinch
  guard pass. UI reflects acknowledged state and never changes Core itself. Compile exits 0/no
  `error CS`; 143/143 Unity tests, headless 141 / 133 and source checks pass. Full session remains
  blocked on live all-finger interactors, D10 gaze and D3/D5/platform command/save handling.
- **S6 contact follow-up, Nov 10 — Unity editor.** Added route-coordinate stance feet, a 12-tick
  swing/landing, route-aligned soles/shadow shapes and world-space foot-contact callbacks for Pip,
  Bram and group travelers. Synthetic tests prove moving-hand reprojection without false steps,
  landing timing and valley-to-world conversion. Compile exits 0/no `error CS`; 146/146 Unity tests,
  headless 141 / 133 and source checks pass. No live foot-contact, legibility or audio quality pass claimed.
- **S5/S9 audio follow-up, Nov 10 — Unity editor.** Original night loop follows the actual director
  count/finale and pauses with the game; meaningful changes have captions. Added a scoped contact-audio
  binder for world-space rig landings. Repeated taps/steps extend one caption instead of many labels.
  No external samples/assets. Compile exits 0/no `error CS`; 148 release / 157 developer Unity tests
  pass, including recording/stats gate checks, original samples and scoped binding. Headless 141 / 133,
  source checks and 28 manifest regressions pass. Listening/headset quality remains owner verification.
- **S4/A8 and S10 material follow-up, Nov 10 — Unity editor.** Added acknowledged high-contrast render
  settings to M1/full-valley views, bright unlit routes/actors, dark islands and a solid backdrop only for
  high-contrast Table mode. Committed five original material templates so opaque/transparent/emissive/
  unlit/line references are explicit, with a build preflight for missing assets. No generated settings/default
  assets committed. Small shared README change adds status/integration links. Full Unity checks pass
  (160 developer / 151 release); shader behavior and cluttered-table readability still need the owner APK pass.

- **S2/S7 D4 integration, Nov 10 — headless / Unity editor.** Fork and future Twins edges share full
  source zones. Live/frozen/refused branch trees render correctly; shared trunks draw once before/after
  save round-trip; tip knots only mark leaves. Capacity icons use actual configurable rules and distinguish
  walker, group, cart or refusal. Headless 164 / 156, source checks, compile and 175 Unity tests pass.
  Cleaned Unity-generated material whitespace. Wide natural Fork spread remains an owner check.

## In progress

- Current runtime bindings are implemented and editor-checked. Release preparation and owner-run
  M1/quality review remain below; greybox art/audio and missing presentation signals are not signed off.
  Dario's D6–D11 changes are integrated; the previous command/save/visitor/gaze dependency blocks are cleared.

## Blocked

- S2 full valley integration is implemented. The contracts expose a route, not a full
  recovery skeleton or puzzle-specific hint pose; these need a published input/solution binding, not guessed joints.
- S3 measured labels and the full valley graph are bound. Device performance and readability remain owner checks.
- S4 commands/settings/mode/gaze and ten poke tips are bound. A full session with either hand alone,
  including real ISDK poke and system-menu rejection, remains an owner headset check.
- S5 connection-lock, foot-contact, visitor and night-music ports are bound. Owner must judge audio/caption quality and
  sound-alone build-completion recognition; procedural cues remain placeholders until polished.
- S6 authoritative traveler/carrying-hand support and foot callbacks are bound. Surface transitions,
  final contact treatment/props and 3 cm readability still need owner editor/headset review; do not claim
  a live sliding-free pass. The legacy M1/builder-scaffold fallback still uses projection.
- S9 counts/bridges/music and D8 visitor windows are bound. Final art/audio review remains open.
  Actual passthrough dimming stays at zero until the owner supplies a table-edge visibility limit.
- S7's Stargazer gap is diagonal mostly in depth (25° off forward); the Lookout Fork spans about 68°.
  T14 and V5 cannot be certified from coordinates. D4 geometry is integrated; H1 and owner-run natural
  spread/reach/comfort remain open.

## Questions for the owner

- Headset and target-device verification remain owner-run. No Quest 2, Quest 3/3S or simulator result yet.
- **S10 APK blocker, checked Nov 11 at clean tracked revision `9168158`.** Ran
  `powershell -ExecutionPolicy Bypass -File tools/Build-OwnerApk.ps1 -Configuration Release -TableMode`.
  Preflight refused: "Commit the reviewed project setup or remove untracked project assets before
  stamping a build." Sam still has the Unity/Meta defaults under Assets/Oculus, Assets/Resources,
  Assets/XR and ProjectSettings, which the owner's rules say to leave uncommitted. They were neither
  committed nor removed. Owner question: please review/commit the intended Slice 0 scene/configuration
  so it can be rebased and the stamped APK built. No APK or final merged manifest is claimed.
  Offline anchors, installation, signing/account actions and device checks remain owner-only.
- Owner chose Nov 13–15, 2026 as the target launch window and Aryan Neopane as the sole display credit.
  Submission draft work is deferred at the owner's request. The public privacy page is published at
  https://palmfolk-privacy.curiousnote2.chatgpt.site with contact@keepsrunning.com. The tracked policy and
  publication record are included. Competition invite/second-account access, real footage/
  screenshots and P9 assets remain pending.
- The older build plan reserves pushing for the owner and gates later work on M1. The current owner
  instruction explicitly authorizes item-by-item pushes and source preparation; no headset gate or
  M1 sign-off is treated as passed by that authorization.
