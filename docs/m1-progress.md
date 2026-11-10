# M1 progress

## Owner decisions, Nov 10, 2026

- The `m1-gameplay` core is merged into `main`. The v1.3 requirements and build plan govern from here; the
  core was written against v1.2 and is being extended to v1.3 (valley, planting, puzzles, visitors).
- Two developers work in parallel, a Lead and a Partner, and both merge to `main` on GitHub.
  See [`team-plan.md`](team-plan.md). Each keeps a status file (`status-lead.md`, `status-partner.md`).
- Later slices may be prepared in parallel with M1 so the journey can be finished by the deadline.
  This changes the order of preparation only: every slice still needs its Quest 2 result, and M1 sign-off is
  still the owner's, on the headset.

## Owner decisions, Nov 6, 2026

- Prepare source on Linux; Unity/Meta setup, Android builds and headset tests run on Windows.
- Pushing to `woooj02/palmfolk` is authorized for the Windows handoff. Commits use
  Aryan Neopane and the existing owner email, with plain messages and no credit lines.
- Branch names use plain project names such as `m1-slice-0`, with no tool or assistant names.
- H1 stays in M1 as a small greybox Arch test. This resolves the mismatch between
  H1's M1 timing in the requirements and the build plan's exclusion of puzzles 3–8.
  The full Arch puzzle remains outside M1. Requirements and build plan are unchanged.
- Original instruction: stop after each slice for a Quest 2 result. The latest
  owner instruction authorizes preparing gameplay algorithms ahead of those tests;
  headset verification and M1 sign-off still require the owner's real results.

## Slice status

| Slice | Implementation | Verification |
|---|---|---|
| 0 | Source prepared: pinned dependencies, Windows scene/setup commands, build checks and APK manifest check | Unity import/compile not verified; APK not built; Quest 2 not verified |
| 1 | Input contracts, deterministic tick runner, consent-gated recording/replay, metrics/radius/dropout watchers | Compiled and tested headlessly; live SDK input, editor replay bindings and stats panel pending |
| 2–3 | Joint routes, surface cursor traversal, connection hysteresis and hand ownership | Synthetic tests; Unity visuals and headset contact unverified |
| 4–6 | Section commits, pause/resume/removal, curve bake, capacity/refusals and complete Pip/Bram M1 flow | Headless end-to-end and rule tests; Unity scene integration/headset behavior unverified |
| 7–8 | Partial-build/recovery saves, dual slots, ghost alignment/blend and safe checkpoint return | Headless regression tests; Quest storage, suspension and live tracking unverified |
| 9 | H1 synthetic arch/rock validation; H2/H3 measurement ports | Real Arch testers, comfort, radii and M1 sign-off pending |

Slice 0 is not Done until a Windows build boots on Quest 2 and the owner sees both
hands with controllers off. Source preparation may proceed under the latest owner
authorization; no later slice has a headset pass yet.

## Slice 0 implementation notes

- Unity 6000.0.66f2; Meta XR All-in-One 207.0.0; OpenXR 1.17.0;
  XR Plug-in Management 4.5.3; XR Hands 1.7.2.
- `Palmfolk > Slice 0` configures Android and generates the M1 scene on Windows.
  It uses SDK package prefab references, not imported sample assets. It does not
  overwrite an existing M1 scene.
- Default SDK hand meshes, OpenXR skeleton, no hand physics capsules. The two
  greybox islands have a 12 cm gap pointing sideways. Gameplay is not yet bound
  into this generated Unity scene; the separate core is described below.
- `PALMFOLK_DEVELOPMENT` is the single planned gate for developer-only code.
  Slice 0 contains no recording, stats panel or radius logging; these are slice 1.
- H3 API candidate: XR Hands
  [`XRHandJoint.TryGetRadius(out float radius)`](https://docs.unity3d.com/Packages/com.unity.xr.hands@1.7/api/UnityEngine.XR.Hands.XRHandJoint.html).
  An API's availability does not establish that Quest 2 returns useful values.
  Enable the XR Hands OpenXR hand-tracking feature in slice 1 and measure then.
- No signing key, Meta app, recordings or APK has been created or committed.

## Source checks on Linux, Nov 6, 2026

- C# and PowerShell syntax parsed without errors. This is not a Unity compile.
- Package versions confirmed in the official Unity registry; Core 207.0.0 lists
  Unity 6000.0.66f2 as its minimum and XR Hands 1.7.2 as a dependency.
- Source manifest checked for required hands, permission, HIGH frequency and
  Internet-permission removal. The generated and final APK manifests remain untested.
- Original plan and requirements match the source checkout byte for byte.
- Source asset GUIDs are unique, `.meta` files are present, and existing ignores
  cover APKs, keystores, `.env`, recordings and Unity-generated local folders.

## Quest 2 result

Pending. Follow [the test steps](slice-0-quest2.md) and record the actual headset
result here after the owner reports it. Quest 2 results never certify Quest 3/3S.

## Additional work before Windows, Nov 6, 2026

- Added `tools/check_source.py` for repeatable version-pin, asset GUID, private-file
  ignore, document-baseline and C# syntax checks. It uses pinned developer parsers;
  it is not a Unity compiler.
- Executed the manifest validator on Linux using portable PowerShell. Regression
  fixtures exercise invalid declarations and the file-based CLI. Fixtures are
  synthetic and never count as an APK or headset test.
- Added the **Source checks** GitHub Actions workflow: Linux source/manifest checks
  plus Windows PowerShell manifest checks, with read-only repository permissions.
  The workflow does not install Unity, accept terms, create keys or build an APK.
- Setup now initializes Android OpenXR features through the package's public API.
  Added build-time checks for the actual packaged hand visuals, handedness, anchors,
  OpenXR skeleton, hand material, camera rig and graphics configuration. These C#
  changes still need the first Windows Unity compile/build.
- The next product verification remains the slice 0 Windows build and the owner's
  Quest 2 hands test; gameplay source preparation is now separately authorized.

## Gameplay preparation

The `m1-gameplay` branch adds the engine-independent M1 algorithms, shared Unity/
headless NUnit tests, development/release compilation checks and cloud gameplay
tests on Linux and Windows. See [implementation and remaining ports](m1-gameplay.md).
The original requirements and build plan remain byte-for-byte unchanged.

Local verification: the .NET Standard 2.1 core and developer libraries compile;
59 developer-mode tests and 51 release-mode tests pass on Linux using .NET 8.
The release suite includes an assertion that no recording/stats types remain.
These are synthetic/headless results, not Unity, APK or headset passes.
