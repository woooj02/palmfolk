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
