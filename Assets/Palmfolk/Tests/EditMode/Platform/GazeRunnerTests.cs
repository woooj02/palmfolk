#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Palmfolk.Core;
using Palmfolk.Platform;
using UnityEngine;

namespace Palmfolk.Tests
{
    // D10 in the Unity editor: the runner with a scripted head gaze (GZ1 to GZ3). The eye-tracked path needs Meta XR
    // Simulator or a headset with eye tracking.
    public class GazeRunnerTests
    {
        private sealed class ScriptedGaze : IGazeReader
        {
            public Func<GazeSample> Next;
            public GazeSample Read() => Next?.Invoke();
        }

        private GameObject go, origin; private PalmfolkRunner runner; private ScriptedGaze gaze;
        private static readonly Vec3 Head = new Vec3(0, 1.3, 0.2);

        [SetUp] public void Create()
        {
            go = new GameObject("runner"); origin = new GameObject("origin");
            runner = go.AddComponent<PalmfolkRunner>(); runner.ValleyRoot = origin.transform; runner.Hands = new PalmfolkRunnerTests.ScriptedHands(runner);
            runner.Passthrough = new TableModeTests.FakePassthrough(); runner.Gaze = gaze = new ScriptedGaze();
        }
        [TearDown] public void Destroy() { UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(origin); }

        private int Play(Func<bool> done, int frames = 400000)
        {
            int n = 0; for (; n < frames && !done(); n++) runner.Frame(1 / 72.0, n / 72.0);
            return n;
        }

        // Head gaze always on whoever waits: their sign comes up for each puzzle, and the valley plays exactly as with gaze off.
        [Test] public void LookingAtWhoWaitsShowsTheirNeedAndChangesNothing()
        {
            gaze.Next = () =>
            {
                var g = runner.Game; if (g?.Valley == null || g.Pose == null) return new GazeSample(Head, new Vec3(0, 0, 1), GazeSource.Head);
                var t = NeedSigns.Targets(g.Valley, g.Pose);
                return new GazeSample(Head, (t.Count > 0 ? t[0].Position : g.Pose.Origin) - Head, GazeSource.Head);
            };
            var signs = new List<NeedSign>(); runner.NeedSignChanged += t => { if (t != null) signs.Add(t.Need); };
            int chosen = 0; runner.GazeSelected += _ => chosen++;
            // A button the gaze keeps passing over: the building hands never pinch it (no unintended choice).
            runner.GazeButtons = () => new[] { new GazeButton("bell", runner.LastGaze == null ? Head + new Vec3(0, 0, 1) : runner.LastGaze.Origin + runner.LastGaze.Direction, 6) };
            runner.Open(TestValley.Chapter1(), null);
            Play(() => runner.Game.Valley.IsComplete);
            Assert.That(runner.Game.Valley.IsComplete, Is.True);
            long withGaze = runner.Game.Tick;
            Assert.That(signs.Distinct(), Is.EquivalentTo(new[] { NeedSign.Rope, NeedSign.Plank, NeedSign.Branches, NeedSign.Arch }));
            Assert.That(runner.GazeSource, Is.EqualTo(GazeSource.Head)); Assert.That(chosen, Is.Zero);
            Assert.That(runner.Looks.Shown, Is.Null, "no sign once the valley is done");

            // Gaze off: same valley, same ticks, no signs.
            runner.GazeEnabled = false; signs.Clear();
            runner.Open(TestValley.Chapter1(), null);
            Play(() => runner.Game.Valley.IsComplete);
            Assert.That(runner.Game.Tick, Is.EqualTo(withGaze)); Assert.That(signs, Is.Empty); Assert.That(runner.GazeSource, Is.EqualTo(GazeSource.None));
        }

        [Test] public void NoGazeReaderMeansNoSource()
        {
            runner.Gaze = null; runner.Open(TestValley.Chapter1(), null);
            Play(() => false, 200);
            Assert.That(runner.GazeSource, Is.EqualTo(GazeSource.None)); Assert.That(runner.Looks.Shown, Is.Null);
        }
    }
}
#endif
