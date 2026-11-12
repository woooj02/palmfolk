#if UNITY_5_3_OR_NEWER && PALMFOLK_DEVELOPMENT
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Palmfolk.Core;
using Palmfolk.Developer;
using Palmfolk.Platform;
using Palmfolk.Platform.Developer;
using UnityEngine;

namespace Palmfolk.Tests
{
    // D6/T17a in the editor: a session recorded through the runner replays with identical decisions; the probe logs.
    public class SessionRecordingTests
    {
        private sealed class TableHand : IHandReader
        {
            private readonly PalmfolkRunner runner;
            private readonly Planted.Placement table = new Planted.Placement();
            public TableHand(PalmfolkRunner runner) { this.runner = runner; }
            public HandSample Read(HandSide side) => side == HandSide.Left ? table.Hand(runner.Game.Tick + 1) : null;
            public bool ControllersInHand => false;
        }

        private GameObject go; private string folder;
        [SetUp] public void Create() { go = new GameObject("runner"); folder = Path.Combine(Path.GetTempPath(), "palmfolk-session-" + Guid.NewGuid().ToString("N")); }
        [TearDown] public void Destroy() { UnityEngine.Object.DestroyImmediate(go); if (Directory.Exists(folder)) Directory.Delete(folder, true); }

        [Test] public void ARecordedRunnerSessionReplaysInTheEditor()
        {
            var runner = go.AddComponent<PalmfolkRunner>(); runner.Hands = new TableHand(runner);
            var recording = go.AddComponent<SessionRecording>(); recording.TesterConsented = true; recording.Runner = runner;
            var probe = go.AddComponent<CoreProbe>(); probe.Runner = runner;
            runner.Open(TestValley.Chapter1(), null);
            Assert.That(runner.StepTarget, Is.SameAs(recording.Recorder));
            for (int i = 0; i < 400; i++) runner.Frame(1 / 72.0, i / 72.0);
            Assert.That(runner.Game.Stage, Is.EqualTo(ValleyStage.Playing));
            Assert.That(probe.Logged, Is.GreaterThan(0));
            recording.Write(folder);
            var files = Directory.GetFiles(folder);
            Assert.That(files.Count(f => f.EndsWith(".pfr3")), Is.EqualTo(1)); Assert.That(files.Count(f => f.EndsWith("-radii.txt")), Is.EqualTo(1));
            var decoded = GameRecordingCodec.Decode(File.ReadAllBytes(files.Single(f => f.EndsWith(".pfr3"))), Application.version);
            Assert.That(decoded.Frames.Count, Is.EqualTo(runner.Game.Tick + 1));
            var replay = GameReplay.Run(decoded, TestValley.Chapter1());
            Assert.That(replay.SameDecisions && replay.SameFinalState, Is.True);
            Assert.That(File.ReadAllText(files.Single(f => f.EndsWith("-radii.txt"))), Does.StartWith("side,joint,valid"));
        }

        [Test] public void NothingIsRecordedWithoutConsent()
        {
            var runner = go.AddComponent<PalmfolkRunner>(); runner.Hands = new TableHand(runner);
            var recording = go.AddComponent<SessionRecording>(); recording.Runner = runner;
            runner.Open(TestValley.Chapter1(), null);
            Assert.That(recording.Recorder, Is.Null); Assert.That(runner.StepTarget, Is.SameAs(runner.Game));
            runner.Frame(0.1, 0); recording.Write(folder);
            Assert.That(Directory.Exists(folder), Is.False);
        }
    }
}
#endif
