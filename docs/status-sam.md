# Sam status

Work items: [`team-plan.md`](team-plan.md#sams-work). Labels: headless, Unity editor, simulator, Quest 2.

## Done

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

## Next

- Owner authorized work. Next: S3 stats/x-ray, then remaining Unity items in team-plan order.

## Blocked

- S7 executable puzzle/visitor data awaits D3's published content types. Layout numbers can proceed.
- S2 full valley/visitor views await D3 presentation fields. The M1 contract exposes a route, not a full
  recovery skeleton or puzzle-specific hint pose; these need a published input/solution binding, not guessed joints.
- S7's Stargazer gap is diagonal mostly in depth (25° off forward). T14 and V5 cannot be certified
  from coordinates. Actual Fork/Arch clearance also needs D4, H1 and owner-run reach/comfort checks.

## Questions for the owner

- Headset and target-device verification remain owner-run. No Quest 2, Quest 3/3S or simulator result yet.
- S10: run `testing/tooling.md`'s owner APK command after the reviewed Slice 0 configuration is committed.
  It deliberately refuses uncommitted project assets or mismatched release/developer defines. Final
  APK manifest and offline Table-mode anchor behavior remain unverified until that owner pass.
- S12: choose target launch date, credit display names and privacy-policy URL; create the Competition
  invite and test access with a second account. Real final-build video/screenshots and P9 assets are pending.
- The older build plan reserves pushing for the owner and gates later work on M1. The current owner
  instruction explicitly authorizes item-by-item pushes and source preparation; no headset gate or
  M1 sign-off is treated as passed by that authorization.
