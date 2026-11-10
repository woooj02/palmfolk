# Synthetic hands

Verification label: **headless**. These fixtures are original, synthetic test data, not recordings,
anatomical measurements, evidence of comfort, or verified headset input.

`Palmfolk.Fixtures.HandSequence` implements `IHandFrameSource`. Read any tick directly, or enumerate
`Frames()` from tick zero. The fixture assembly is Editor-only and is not included in an Android player.
Unity test assemblies using it must reference `Palmfolk.Fixtures`; the existing .NET test project
already includes all sources under `Assets/Palmfolk/Tests` without a project-file change.

```csharp
var hands = new HandSequence(HandShape.Flat, HandSize.Small, HandSide.Right,
    seed: 41, tremorMetres: 0.001, origin: new Vec3(0, 0.7, 0),
    withdrawStart: 400, withdrawEnd: 430, withdrawal: new Vec3(0, 0, 0.2),
    faults: new[] { new TrackingWindow(200, 218, TrackingFault.LowConfidence) });
foreach (var frame in hands.Frames()) simulation.Step(frame);
```

- Metres, Y up; medium index-tip-to-wrist span 12 cm. Small/large scale every joint and radius by
  0.8/1.2. These are test factors, not hand-size percentiles or the T6 calibration algorithm.
- Shapes: one extended index with other fingers curled, flat fingers together, fingers spread,
  and an elevated finger middle. Shape names do not override Core's geometric capacity rules.
- Left and right mirror the lateral axis. `palmDown`, `yawDegrees`, and `origin` support placement tests.
- Seeded bounded tremor translates the whole hand by at most the requested distance per tick.
  It deliberately preserves bone lengths; it does not model independent joint tracking noise.
- Withdrawal moves linearly during its tick interval and holds the end pose. It does not hide tracking.
- Tracking windows use inclusive start/exclusive end ticks and a 26-bit joint mask. Missing hand,
  invalid position, low confidence and unavailable radius remain distinct. A missing hand still
  returns a frame: only out-of-range ticks return `TryRead=false`.
- Frames carry integer microsecond timestamps from the exact 60 Hz tick index, without accumulated rounding.
- No global random state or sampling cursor. Repeated and out-of-order reads are identical.

Dario can adopt the generator in D2–D4 tests by adding the fixture assembly reference in his owned
test assembly. No existing Core/test file was changed here. Joint rotations express the whole-hand
pose only; per-bone orientation and physiological fidelity are outside this fixture's claims.
