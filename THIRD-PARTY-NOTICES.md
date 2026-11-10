# Dependency notices

The root MIT license covers original Palmfolk code only. Unity and Meta dependencies
retain their licenses. No sample code, sample scenes or external art has been copied
into `Assets`. The scene setup references SDK prefabs in their installed package.

| Dependency | Pinned version | License source |
|---|---|---|
| Unity Editor | 6000.0.66f2 | [Unity terms](https://unity.com/legal/terms-of-service) |
| Meta XR All-in-One SDK | 207.0.0 | [Meta SDK download and license notice](https://developers.meta.com/vr/downloads/package/meta-xr-sdk-all-in-one-upm/207.0/) and each installed package's license |
| Unity OpenXR Plugin | 1.17.0 | Installed package `LICENSE.md` and `Third Party Notices.md` |
| Unity XR Plug-in Management | 4.5.3 | Installed package `LICENSE.md` |
| Unity XR Hands | 1.7.2 | Installed package `LICENSE.md` |

All-in-One pulls in additional Meta packages, including Interaction SDK. Interaction
SDK is reserved for UI (T11); this slice does not instantiate its interactors.
Unity generates `Packages/packages-lock.json` on the first Windows import. Keep it
and the dependency license files available for the P9 credits pass. The owner handles
any terms acceptance presented during setup; no acceptance has been performed here.

Developer checks use tree-sitter and its C# grammar, pinned in
`tools/requirements-checks.txt`; their licenses stay in the installed distributions.
These tools and PowerShell run outside the Unity player and are not shipped in the APK.

Gameplay tests use Unity Test Framework 1.4.6 in the Editor. The separate headless
test project uses NUnit 3.14.0, NUnit3TestAdapter 4.6.0 and Microsoft.NET.Test.Sdk
17.11.1 through NuGet. Their installed packages retain their licenses. They are
not runtime gameplay code and are not shipped in the APK. All gameplay algorithms
in `Assets/Palmfolk/Core` and developer helpers were written for this project.

The placeholder sound cues in `Assets/Palmfolk/Audio` are synthesized from original
waveform/envelope code for this project. No external recordings, music or sample
assets were added. They follow the root licence for original code and still need
the owner's audio-quality pass before any release claim.
