# Palmfolk

A hands-only puzzle adventure for Meta Quest 3 and 3S. Tiny travelers walk across your fingers, and the bridges
they build keep the shape of your hand after you let go.

Entry for the Meta VR Start Developer Competition 2026 (Gaming track, New Experience division).

Status: the M1 gameplay core (written against v1.2) is implemented and tested without Unity. Windows
scene/input integration, Android builds and Quest 2 verification are pending. The scene generator still
creates the slice 0 hands/islands scene. No headset behavior is verified yet.

Requirements: [`docs/requirements-v1.3.md`](docs/requirements-v1.3.md) and
[`docs/requirements-v1.3-quality.md`](docs/requirements-v1.3-quality.md). Order of work:
[`docs/build-plan.md`](docs/build-plan.md). Where v1.3 and v1.2 disagree, v1.3 wins.

Who does what: [`docs/team-plan.md`](docs/team-plan.md). Progress and verification status:
[`docs/m1-progress.md`](docs/m1-progress.md). Core algorithms, test commands and integration ports:
[`docs/m1-gameplay.md`](docs/m1-gameplay.md).

Open the repository root with **Unity 6000.0.66f2 on Windows**, with Android Build Support, OpenJDK and
Android SDK & NDK Tools installed. Follow [`docs/slice-0-quest2.md`](docs/slice-0-quest2.md) to generate the
scene, build and test. Run the Core tests without Unity (needs the .NET 8 SDK):

```bash
dotnet test tools/CoreChecks/Tests/Palmfolk.Tests.csproj --configuration Release
```
