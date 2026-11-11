#if PALMFOLK_DEVELOPMENT && UNITY_EDITOR
using NUnit.Framework;
using Palmfolk.Core;
using Palmfolk.Core.Content;
using Palmfolk.Developer;
using Palmfolk.Platform;
using UnityEngine;

namespace Palmfolk.Tests.Content
{
    public class LiveStatsTests
    {
        [Test] public void StatsCanDetachBeforeTheGameOpens()
        {
            var root = new GameObject("Stats before startup");
            try
            {
                var runner = root.AddComponent<PalmfolkRunner>(); var stats = root.AddComponent<LiveStatsBinding>();
                stats.Configure(runner); Assert.DoesNotThrow(() => stats.Configure(null)); Assert.That(runner.StepTarget, Is.Null);
            }
            finally { Object.DestroyImmediate(root); }
        }
        private sealed class StatsTestHandReader : IHandReader
        {
            public HandSample Current;
            public bool ControllersInHand => false;
            public HandSample Read(HandSide side) => Current?.Side == side ? Current : null;
        }
        [Test] public void MeasuredFpsAndDropoutsNeedNoRecorderAndPreserveTheTickTarget()
        {
            var root = new GameObject("Measured stats test");
            try
            {
                var runner = root.AddComponent<PalmfolkRunner>(); var hands = new StatsTestHandReader(); runner.Hands = hands;
                runner.Open(ChapterOne.Greybox(), new SaveData { Mode = ValleyMode.VR }, ValleyPose.Identity);
                var original = runner.StepTarget; var stats = root.AddComponent<LiveStatsBinding>(); stats.Configure(runner);
                var player = new TestPlayer(); double time = 0;
                for (int i = 0; i < 1500 && runner.Game.Valley.Session?.Owner == null; i++)
                { hands.Current = player.Hand(runner.Game.Valley, runner.Game.Tick + 1); runner.Frame(1d / 60, time += 1d / 60); }
                Assert.That(runner.Game.Valley.Session?.Owner, Is.Not.Null);
                hands.Current = null; for (int i = 0; i < 20; i++) runner.Frame(1d / 60, time += 1d / 60);
                stats.Refresh(); Assert.That(stats.Dropouts.Count, Is.EqualTo(1)); Assert.That(stats.Dropouts.DisruptiveCount, Is.EqualTo(1));
                Assert.That(root.GetComponentInChildren<StatsPanel>().Display, Does.Contain("dropouts: 1 / disruptive: 1"));
                // A separate synthetic rendered-time window verifies the calculation, not hardware performance.
                stats.Configure(null); stats.Configure(runner);
                for (int i = 0; i < 50; i++) stats.FrameRendered(0.02);
                stats.Refresh(); Assert.That(stats.FramesPerSecond.Value, Is.EqualTo(50).Within(1e-6));
                Assert.That(root.GetComponentInChildren<StatsPanel>().Display, Does.Contain("FPS: 50.0").And.Contain("Unity editor / VR").And.Contain("gaze: None"));
                stats.Configure(null); Assert.That(runner.StepTarget, Is.SameAs(original));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
#endif
