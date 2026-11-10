# Slice 0: Windows build and Quest 2 check

Goal (T11/T12): boot a VR scene and see both tracked hands with controllers off.
This slice has two greybox islands, a 12 cm gap and the SDK default hand visuals.
Pip, Bram, bridges, recording and the stats panel arrive in later slices.

## Get the source on Windows

```powershell
git clone --branch m1-slice-0 https://github.com/woooj02/palmfolk.git
cd palmfolk
```

If the repository is already cloned, fetch and switch to `m1-slice-0`.
Do not copy `Library`, `Temp` or local build outputs between operating systems.

## Checks available before Windows

These checks run without Unity, Android tools or a headset:

```bash
python3 -m venv .venv
.venv/bin/python -m pip install -r tools/requirements-checks.txt
.venv/bin/python tools/check_source.py
pwsh -NoProfile -File tools/Test-ManifestChecks.ps1
```

The last command needs PowerShell 7 on Linux; GitHub Actions already supplies it.
The workflow **Source checks** also runs the manifest tests on Windows PowerShell
5.1 in a hosted runner. It needs no Unity license, Meta app, signing key or secrets.
It does not build or verify the game. The manifest fixtures are synthetic XML.

To inspect a manifest already exported from an APK, on either platform:

```powershell
./tools/Test-Slice0Apk.ps1 -ManifestXml './path to/AndroidManifest.xml'
```

Use `-Source` only for the source template, whose Internet-permission removal marker
must disappear in the actual APK. The default APK mode below always checks the
manifest extracted from the built APK.

## First Unity import and build

1. In Unity Hub install **6000.0.66f2**, including **Android Build Support**,
   **OpenJDK** and **Android SDK & NDK Tools**. Add this repository's root folder
   as the Unity project. Any account sign-in or terms acceptance is the owner's step.
2. Open the project and let Package Manager finish resolving the pinned versions.
   All-in-One is 207.0.0. Do not import samples or upgrade packages. If Unity
   prompts to enable the Meta XR feature set, enable it. Keep the built-in render
   pipeline; this scene does not use URP. In **Project Settings > Player > Other
   Settings > Active Input Handling**, select **Input System Package (New)** and
   restart Unity if prompted.
3. Open **File > Build Profiles**, select **Android**, and **Switch Platform**.
   Wait for the platform import and compilation to finish.
4. Run **Palmfolk > Slice 0 > 1. Configure Android**. Wait for compilation.
5. Run **Palmfolk > Slice 0 > 2. Create Scene**. It saves
   `Assets/Palmfolk/Scenes/M1.unity` and adds it as the sole build scene.
   Rerunning this command refuses to overwrite the scene; open the existing scene instead.
6. Under **Project Settings > XR Plug-in Management**, Android must have **OpenXR**
   enabled. Under Android OpenXR, **Meta Quest Support** and **Meta XR Feature**
   must be enabled, with **Hand Interaction Profile** under Enabled Interaction
   Profiles. Run **Project Validation** and resolve errors before building.
   Do not enable passthrough, controller visuals, extra interaction systems or samples.
   Setup now refreshes OpenXR feature settings itself; manually opening the
   settings is for checking the result rather than initializing them.
7. Run **Palmfolk > Slice 0 > 3. Build APK**. Output:
   `Builds/Palmfolk-slice0.apk`. This uses local debug signing, with no custom
   signing key created or stored. A Competition-channel signing key is the owner's
   later step. The APK stays out of Git.
8. Check the **built APK's** manifest from PowerShell in the repo root:

   ```powershell
   powershell -ExecutionPolicy Bypass -File .\tools\Test-Slice0Apk.ps1
   ```

   If Unity is installed somewhere else, supply `-Sdk` with the SDK path from
   **Edit > Preferences > External Tools**. The script must print `PASS`.

After a successful import/build, keep the generated scene, materials, `.meta` files,
XR/Meta settings, `ProjectSettings` and `Packages/packages-lock.json` in Git. Review
the diff first. Do not commit APKs, keystores, local machine settings or recordings.

## Install and run on Quest 2

The headset must already have developer mode enabled. Use a USB data cable and
accept the USB debugging prompt on the headset. In PowerShell:

```powershell
$adb = 'C:\Program Files\Unity\Hub\Editor\6000.0.66f2\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe'
& $adb devices
& $adb install -r .\Builds\Palmfolk-slice0.apk
& $adb shell am start -n com.keepsrunning.palmfolk/com.unity3d.player.UnityPlayerActivity
```

Adjust `$adb` if the Unity installation path differs. The device must show as
`device`, not `unauthorized`. You can also launch Palmfolk from **Unknown Sources**
using hands after the controllers are off.

## What to look for

1. In normal room lighting, seated, enable the Quest's hand tracking. Switch
   controllers off/remove their batteries. For the plan's strict pass, test with
   no controller paired, if the headset allows that configuration.
2. Launch Palmfolk with hands. A dark VR background and two grey islands should
   appear in front of you; there should be no controller models or passthrough.
3. Hold both hands in view and open/close your fingers. Both SDK hand meshes
   should appear, correspond to the correct side, and follow your fingers.
4. Try left only, right only, then both. Turn your head normally: the islands
   should stay in the virtual world. Do not judge route behavior yet; there is none.
5. Quit and relaunch with controllers still off. Confirm it boots again with hands.

Report: **Quest 2**, Windows Unity version, whether build/manifest checks passed,
whether both hands appeared, whether left/right were correct, controller state
(off or fully unpaired), and any black screen, crash or visible rendering problem.
If it fails, include the first Unity Console error or a description of what the
headset showed. No playtest recording is needed for this check.

**Stop here. Slice 1 waits for the owner's result.**

## SDK references

- [Meta hands setup](https://developers.meta.com/vr/documentation/unity/unity-handtracking-hands-setup/)
- [Meta XR All-in-One v207](https://developers.meta.com/vr/downloads/package/meta-xr-sdk-all-in-one-upm/207.0/)
- [Unity OpenXR 1.17](https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.17/manual/index.html)
