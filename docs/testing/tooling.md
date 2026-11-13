# Local validation and owner builds

Run from your own worktree. Never point Unity at the other developer's checkout.
No script installs software, signs in, accepts terms, creates signing keys, uploads or touches a headset.

```powershell
dotnet test tools/CoreChecks/Tests/Palmfolk.Tests.csproj --configuration Release
dotnet test tools/CoreChecks/Tests/Palmfolk.Tests.csproj --configuration Release -p:DeveloperFeatures=false
py -3.12 tools/check_source.py
powershell -ExecutionPolicy Bypass -File tools/Test-ManifestChecks.ps1
powershell -ExecutionPolicy Bypass -File tools/Invoke-UnityChecks.ps1 -Mode Compile
powershell -ExecutionPolicy Bypass -File tools/Invoke-UnityChecks.ps1 -Mode EditMode
powershell -ExecutionPolicy Bypass -File tools/Invoke-UnityChecks.ps1 -Mode EditMode -Development
```

The Unity wrapper resolves the project from the script location, checks exit status and compiler errors,
and requires a nonempty all-passing NUnit XML for Edit Mode. It does not pass `-quit` to the test runner.
Logs/results stay in ignored `Logs/`. If the first clean import crashes once, use the documented setup
retry; the check itself never turns a crash into a pass. Do not run while that folder is open in Unity.
The wrapper configures the active editor target's single gate in a preliminary invocation, then verifies
the compiled stats type in the actual compile/test invocation. It restores the previous project-settings
bytes in `finally`. `-Development` requests inclusion; without it, absence. No Android setting change is
committed. Do not confuse this editor gate check with a final APK pass. Reopen/import after interrupted checks.

`-JobWorkers 2` optionally limits Unity's job workers for both setup and the requested check; zero (the
default) keeps Unity's normal choice. It changes neither the test set nor pass criteria. Startup failures
now identify `Logs/define-setup.log` rather than pointing at an old compile/test log. The Nov 12 native
startup crash also occurred with two workers, so this option is not a verified fix for that fault.

`Source checks` runs the headless tests on Linux and Windows and planted bad-manifest checks on both.
Unity checks run locally on the installed/licensed editor, not hosted CI; no CI licence is configured.

## Owner APK command

First complete the Slice 0 project configuration/scene steps. Commit or restore tracked changes.
The owner chooses Android scripting defines: `PALMFOLK_DEVELOPMENT` for Developer, absent for Release.
The script fails if the define does not match, rather than quietly shipping recording code.

```powershell
powershell -ExecutionPolicy Bypass -File tools/Build-OwnerApk.ps1 -Configuration Release
```

Use `-Configuration Developer` for consent-gated local recording; neither configuration enables the
network profiler. Supply `-TableMode` only when shipping anchors, `-SceneSnap` only when shipping MR2
scene snap. The manifest allowlist is hand tracking; additionally local anchors for Table mode and scene
permission for scene snap. Passthrough itself needs a feature declaration, not camera-image access.
INTERNET, CAMERA, microphone, storage and all other permissions fail, including versioned variants.

The build version is `0.1.0-<12-character revision>`, stored in the APK's bundle version. The ignored
JSON beside the APK records the full revision, defines, configuration, Unity version and final APK hash.
The temporary bundle-version setting is restored after building; no revision file is generated in Assets.
Both the script and the ordinary Build button reject a release with the recording define. The existing
Developer asmdef/guards and release headless test check that recording types compile out.

The script validates the **actual APK** manifest using the installed Android SDK's analyzer. Do not
equate synthetic manifest regressions or source guards with an APK pass. APK builds, final merged
permissions, signing, offline anchor restoration and headset behavior are still owner verification.
Do not commit APKs, build reports, signing material or generated Unity defaults.

Presentation material templates are committed under `Assets/Palmfolk/Art/Resources/Palmfolk`, separate
from Unity/Meta's generated default folders. A build preflight checks all five. If one is missing, restore
the reviewed asset; **Palmfolk > Presentation > Create material templates** creates only missing originals
and does not overwrite an existing material. Keep APK shader/render verification in the owner build pass.
