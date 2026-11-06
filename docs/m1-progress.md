# M1 progress

## Owner decisions, Nov 6, 2026

- Prepare source on Linux; Unity/Meta setup, Android builds and headset tests run on Windows.
- Pushing to `woooj02/palmfolk` is authorized for the Windows handoff. Commits use
  Aryan Neopane and the existing owner email, with plain messages and no credit lines.
- Branch names use plain project names such as `m1-slice-0`, with no tool or assistant names.
- H1 stays in M1 as a small greybox Arch test. This resolves the mismatch between
  H1's M1 timing in the requirements and the build plan's exclusion of puzzles 3–8.
  The full Arch puzzle remains outside M1. Requirements and build plan are unchanged.
- Stop after each slice and wait for the owner's Quest 2 result before starting the next.

## Slice status

| Slice | Implementation | Verification |
|---|---|---|
| 0 | Source prepared: pinned dependencies, Windows scene/setup commands, build checks and APK manifest check | Unity import/compile not verified; APK not built; Quest 2 not verified |
| 1–8 | Not started | Not verified |
| 9 | Not started; include H1 greybox test plus H2/H3 | Not verified |

Slice 0 is not Done until a Windows build boots on Quest 2 and the owner sees both
hands with controllers off. Do not proceed to slice 1 before that result.

## Slice 0 implementation notes

- Unity 6000.0.66f2; Meta XR All-in-One 207.0.0; OpenXR 1.17.0;
  XR Plug-in Management 4.5.3; XR Hands 1.7.2.
- `Palmfolk > Slice 0` configures Android and generates the M1 scene on Windows.
  It uses SDK package prefab references, not imported sample assets. It does not
  overwrite an existing M1 scene.
- Default SDK hand meshes, OpenXR skeleton, no hand physics capsules. The two
  greybox islands have a 12 cm gap pointing sideways. No gameplay is implemented yet.
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
