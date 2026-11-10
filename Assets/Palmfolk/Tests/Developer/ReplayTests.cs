#if PALMFOLK_DEVELOPMENT
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Palmfolk.Core;
using Palmfolk.Developer;

namespace Palmfolk.Tests
{
    public class ReplayTests
    {
        [Test] public void RecordingLimitDoesNotStopGameplayAndIsDisclosed()
        {
            var run = new RecordedRun(true, "v", Synthetic.Level, new GameSettings(), new M1Simulation(Synthetic.Level).Capture(), 3);
            for (int i = 0; i < 30; i++) run.Step(Synthetic.Frame(i, Synthetic.Hand()));
            Assert.That(run.State.Tick, Is.EqualTo(29)); Assert.That(run.RecordingLimitReached, Is.True);
            var recording = RecordingCodec.Decode(RecordingCodec.Encode(run.Finish()), "v");
            Assert.That(recording.Partial, Is.True); Assert.That(recording.Frames.Count, Is.EqualTo(3));
        }
        [Test] public void RecordedFailureReplaysTheSameDecisionsAndStateAtEveryTick()
        {
            var settings = new GameSettings();
            var initial = new M1Simulation(Synthetic.Level).Capture();
            var live = new RecordedRun(true, "test-build", Synthetic.Level, settings, initial);
            var expected = new byte[1100][]; var decisions = new GameEventKind[1100][];
            for (int tick = 0; tick < expected.Length; tick++)
            {
                var hand = tick >= 80 && tick < 100 ? null : Synthetic.Hand(offset: tick >= 100 && tick < 650 ? new Vec3(0.2, 0, 0) : Vec3.Zero);
                live.Step(Synthetic.Frame(tick, hand)); expected[tick] = SaveCodec.Encode(live.State); decisions[tick] = live.Events.Select(e => e.Kind).ToArray();
            }
            var session = RecordingCodec.Decode(RecordingCodec.Encode(live.Finish()), "test-build");
            var replay = new ReplayFrameSource(session); var game = M1Simulation.Restore(session.InitialState, session.Level, session.Settings);
            int count = 0;
            while (replay.TryRead(game.Tick + 1, out var frame))
            {
                game.Step(frame); Assert.That(SaveCodec.Encode(game.Capture()), Is.EqualTo(expected[count]), "state at tick " + count);
                Assert.That(game.Events.Select(e => e.Kind), Is.EqualTo(decisions[count]), "decisions at tick " + count); count++;
            }
            Assert.That(count, Is.EqualTo(expected.Length));
        }
        [Test] public void RecordingCarriesConfidenceRadiiRotationFocusAndMenuInputs()
        {
            var run = new RecordedRun(true, "v", Synthetic.Level, new GameSettings(), new M1Simulation(Synthetic.Level).Capture());
            run.Step(new HandFrame(0, 0, Synthetic.Hand(invalid: 1u << 10), null, false, InputCommand.Pause, true));
            var result = RecordingCodec.Decode(RecordingCodec.Encode(run.Finish()), "v");
            Assert.That(result.Frames[0].Left.InvalidMask, Is.EqualTo(1u << 10));
            Assert.That(result.Frames[0].Left[JointId.Wrist].Radius, Is.EqualTo(0.03));
            Assert.That(result.Frames[0].Left[JointId.Wrist].Rotation.W, Is.EqualTo(1));
            Assert.That(result.Frames[0].HasFocus, Is.False); Assert.That(result.Frames[0].Command, Is.EqualTo(InputCommand.Pause));
            Assert.That(result.Frames[0].ControllersInHand, Is.True);
            Assert.That(result.Calibration.WorldScale, Is.EqualTo(1));
        }
        [Test] public void RecordingRequiresConsentAndMatchingBuild()
        {
            var initial = new M1Simulation(Synthetic.Level).Capture();
            Assert.Throws<InvalidOperationException>(() => new RecordedRun(false, "v", Synthetic.Level, new GameSettings(), initial));
            var run = new RecordedRun(true, "v", Synthetic.Level, new GameSettings(), initial);
            var bytes = RecordingCodec.Encode(run.Finish());
            Assert.Throws<InvalidDataException>(() => RecordingCodec.Decode(bytes, "different"));
            Assert.Throws<InvalidDataException>(() => RecordingCodec.Decode(bytes.Take(10).ToArray(), "v"));
            bytes[40] ^= 1; Assert.Throws<InvalidDataException>(() => RecordingCodec.Decode(bytes, "v"));
        }
        [Test] public void DropoutsCountAt250msOnlyWhileSupportingAndOncePerEpisode()
        {
            var watcher = new DropoutWatcher();
            for (int i = 0; i < 300; i++) watcher.Step(i, HandSide.Left, false, 1, true, Vec3.Up);
            Assert.That(watcher.Count, Is.Zero);
            for (int i = 0; i < 14; i++) watcher.Step(i, HandSide.Left, true, 1, true, Vec3.Up);
            Assert.That(watcher.Count, Is.Zero); watcher.Step(14, HandSide.Left, true, 2, true, Vec3.Up);
            Assert.That(watcher.Count, Is.EqualTo(1)); Assert.That(watcher.DisruptiveCount, Is.EqualTo(1));
            for (int i = 15; i < 60; i++) watcher.Step(i, HandSide.Left, true, 4, true, Vec3.Up);
            watcher.EndEpisode(); Assert.That(watcher.Count, Is.EqualTo(1)); Assert.That(watcher.Completed.Single().AffectedJoints, Is.EqualTo(7));
            Assert.That(watcher.Completed.Single().DurationTicks, Is.EqualTo(60));
        }
        [Test] public void MetricsKeepBakeAndWithdrawnBridgeSeparate()
        {
            var metrics = new PlaytestMetrics(); metrics.Observe(new[] { new GameEvent(10, GameEventKind.Boarded), new GameEvent(100, GameEventKind.BridgeBaked) });
            Assert.That(metrics.BoardingTick, Is.EqualTo(10)); Assert.That(metrics.FirstPersistentBridgeTick, Is.Null);
            metrics.Observe(new[] { new GameEvent(130, GameEventKind.PersistentBridgeObserved) });
            Assert.That(metrics.FirstPersistentBridgeTick, Is.EqualTo(130));
            for (int i = 0; i < 80; i++) metrics.FrameRendered(1.0 / 72);
            Assert.That(metrics.FramesPerSecond, Is.EqualTo(72).Within(1e-6));
        }
        [Test] public void RecorderWiresDropoutsAndIdleTimeWithoutLoggingIdleLosses()
        {
            var run = new RecordedRun(true, "v", Synthetic.Level, new GameSettings(), new M1Simulation(Synthetic.Level).Capture());
            for (int i = 0; i < 700; i++) run.Step(Synthetic.Frame(i));
            Assert.That(run.Dropouts.Count, Is.Zero); Assert.That(run.Metrics.StuckTicks, Is.GreaterThanOrEqualTo(600));
            for (int i = 700; i < 730; i++) run.Step(Synthetic.Frame(i, Synthetic.Hand()));
            for (int i = 730; i < 750; i++) run.Step(Synthetic.Frame(i));
            Assert.That(run.Dropouts.Count, Is.EqualTo(1)); Assert.That(run.Dropouts.DisruptiveCount, Is.EqualTo(1));
        }
        [Test] public void RadiusWatcherReportsDataWithoutClaimingUsability()
        {
            var watcher = new RadiusWatcher(); watcher.Observe(Synthetic.Hand()); watcher.Observe(Synthetic.Hand(invalid: 1u << 10));
            var stats = watcher.Summary(HandSide.Left, JointId.IndexTip);
            Assert.That(stats.Valid, Is.EqualTo(1)); Assert.That(stats.Missing, Is.EqualTo(1)); Assert.That(stats.Mean, Is.EqualTo(0.006));
        }
        [Test] public void UnusedFingerLossIsLoggedWithoutCallingItDisruptive()
        {
            var run = new RecordedRun(true, "v", Synthetic.Level, new GameSettings(), new M1Simulation(Synthetic.Level).Capture());
            for (int i = 0; i < 40; i++) run.Step(Synthetic.Frame(i, Synthetic.Hand()));
            for (int i = 40; i < 60; i++) run.Step(Synthetic.Frame(i, Synthetic.Hand(invalid: 1u << 25)));
            run.Step(Synthetic.Frame(60, Synthetic.Hand()));
            Assert.That(run.Dropouts.Count, Is.EqualTo(1)); Assert.That(run.Dropouts.DisruptiveCount, Is.Zero);
            Assert.That(run.Dropouts.Completed.Single().AffectedJoints, Is.EqualTo(1u << 25));
        }
    }
}
#endif
