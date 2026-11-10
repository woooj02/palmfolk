# Unity on the owner's PC

For Dario and Sam. Unity is installed and licensed on this PC. Nothing needs installing, signing in to or
accepting. Both folders use the same editor and the same licence.

## What is installed

| What | Where |
|---|---|
| Unity 6000.0.66f2 (changeset b20bc5da3050) | `E:\Unity\Editors\6000.0.66f2\Editor\Unity.exe` |
| Android Build Support, OpenJDK 17.0.9 | `E:\Unity\Editors\6000.0.66f2\Editor\Data\PlaybackEngines\AndroidPlayer\` (`OpenJDK\`) |
| Android SDK: platforms 34, 35, 36; build-tools 36.0.0; cmdline-tools 16.0; platform-tools; CMake 3.22.1 | `...\AndroidPlayer\SDK\` |
| Android NDK r27c | `...\AndroidPlayer\NDK\` |
| Unity Hub 3.22.6 (install folder set to `E:\Unity\Editors`) | `C:\Program Files\Unity Hub\` |
| Unity Personal licence | Active for this Windows user, so it covers both folders |

The SDK path is not the default in `tools\Test-Slice0Apk.ps1`. Pass it explicitly:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\Test-Slice0Apk.ps1 -Sdk 'E:\Unity\Editors\6000.0.66f2\Editor\Data\PlaybackEngines\AndroidPlayer\SDK'
```

## First open of your folder

1. Bring your branch up to date with `main` first. Commit `0a0bee3` added the built-in modules the Meta SDK
   needs. Without it, Unity stops with about 144 compile errors inside the Meta packages.

   ```powershell
   git fetch origin
   git rebase origin/main
   ```

2. Import and compile without the editor window (batch mode). Run this from your own folder. The first import
   downloads and compiles every package and takes several minutes. Later opens are fast.

   ```powershell
   $p = Start-Process 'E:\Unity\Editors\6000.0.66f2\Editor\Unity.exe' -Wait -PassThru -ArgumentList '-batchmode','-quit','-nographics','-projectPath',"$PWD",'-logFile',"$PWD\Logs\unity-batch.log"
   "exit code: $($p.ExitCode)"
   Select-String -Path Logs\unity-batch.log -Pattern 'error CS' | Select-Object -First 10
   ```

   Exit code `0` and no `error CS` lines means the project compiles. `Logs\` is ignored by Git.

   The first import can crash once while importing Meta's sample prefabs (exit code `-1073741819`,
   `Crash!!!` in the log). Run the same command again: it continues where it stopped and finishes in under a
   minute. That happened on a clean checkout here; the second run exited with `0`.

3. The owner can open a folder in the editor window too: Unity Hub > **Projects** > **Add** > pick
   `E:\palmfolk\Dario` or `E:\palmfolk\Sam`.

## Rules

- **Only open your own folder.** Each folder is a separate Unity project with its own `Library\`. Never open the
  other developer's folder in Unity, not even in batch mode.
- **One Unity per folder at a time.** Batch mode refuses to run on a folder that is already open in the editor
  window, and the other way round. The two folders can run at the same time.
- **Files Unity creates on the first open are not committed yet.** After the first open, Git shows new
  `ProjectSettings\*.asset` files, `Assets\Oculus\`, `Assets\Resources\` and `Assets\XR\`. These are Unity and
  Meta defaults. The configured versions are committed with the Slice 0 setup ([`slice-0-quest2.md`](slice-0-quest2.md)),
  so leave yours uncommitted until then. If a later `git rebase` stops with "untracked working tree files would be
  overwritten", delete the files it lists and rebase again. Unity recreates whatever it needs.
- **Don't upgrade packages or import samples.** Versions are pinned in `Packages\manifest.json`, and
  `Packages\packages-lock.json` fixes the resolved versions so both folders get the same ones.

## If something goes wrong

| Symptom | Cause and fix |
|---|---|
| `error CS1069: ... forwarded to assembly 'UnityEngine.<Name>Module'` | A built-in module is missing. Add `"com.unity.modules.<name>": "1.0.0"` to `Packages\manifest.json` (a shared file: mention it in the commit message) and reopen. |
| Exit code `-1073741819` and `Crash!!!` in the log during the first import | A crash in Unity's asset importer, not in our code. Run the same command again. If it crashes again at the same asset, put the log lines in your status file. |
| Empty folders `Assets\XR 1`, `XR 2`, ... or `Assets\XR\Settings 1.meta`, ... | Left behind by a failed import. Delete the empty duplicates and their `.meta` files. The real ones are `Assets\XR` and `Assets\XR\Settings`. |
| Licence errors in the log, or the Hub says "There was a problem activating a new personal license" | Usually the clock (next row) or a stuck Windows WMI service (row after). Put it in your status file for the owner. |
| Clock wrong after a reboot | The PC's hardware clock makes Windows start about 7 hours fast. Unity licensing checks timestamps. Check it: `(Invoke-WebRequest https://license.unity3d.com -UseBasicParsing -Method Head).Headers.Date` should match `(Get-Date).ToUniversalTime()`. If not, the owner clicks **Settings > Time & language > Date & time > Sync now**. |
| Unity hangs at licensing, or `Get-CimInstance Win32_BIOS -OperationTimeoutSec 15` times out | Windows WMI is stuck, and Unity's licensing helper freezes on it. Restarting the PC fixes it. That's the owner's step. |
