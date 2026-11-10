using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Palmfolk.Core;

namespace Palmfolk.Tests
{
    // D6: the live frame source between the platform's rendered frames and Core's 60 Hz ticks.
    public class LiveInputTests
    {
        // Plays a whole valley the way PalmfolkRunner does: one hand sample per rendered frame, FixedTickRunner
        // stepping `target` at 60 Hz. The hand is planted on a table first, then plays in world space.
        public static class LivePlay
        {
            public static int Run(PalmfolkGame game, ITickSimulation target, double hz, Func<PalmfolkGame, bool> done, int frameLimit = 400000,
                Action<LiveFrameSource, long> onFrame = null, Func<long, HandSample, HandSample> distort = null, ValleyPose replantAt = null)
            {
                var source = new LiveFrameSource(); var clock = new FixedTickRunner(); var player = new TestPlayer();
                var table = new Planted.Placement { Height = 0.74, YawDegrees = 37, HandLength = 0.2, Offset = new Vec3(0.2, 0, -0.35) };
                int frames = 0;
                for (; frames < frameLimit && !done(game); frames++)
                {
                    long next = game.Tick + 1; HandSample hand;
                    if (game.Stage == ValleyStage.Planting) hand = table.Hand(next);
                    else { var local = player.Hand(game.Valley, next); hand = local == null ? null : game.Pose.ToWorld(local); }
                    if (distort != null) hand = distort(next, hand);
                    bool right = hand?.Side == HandSide.Right;
                    source.Sample(right ? null : hand, right ? hand : null, (long)(frames / hz * 1e6), true, false);
                    onFrame?.Invoke(source, frames);
                    clock.Advance(1 / hz, source, target);
                    foreach (var e in clock.Events)
                    {
                        player.Events.Add(e);
                        if (e.Kind == GameEventKind.Refused && game.Valley?.Session != null)
                        { player.Attempts.TryGetValue(game.Valley.Session.Edge, out int a); player.Attempts[game.Valley.Session.Edge] = a + 1; }
                    }
                }
                return frames;
            }
        }

        private static HandSample Hand(HandSide side, double x) => Planted.Map(Synthetic.Hand(true, side), j => j.Position + new Vec3(x, 0, 0));

        [Test] public void NothingIsReadBeforeTheFirstSample()
        {
            var source = new LiveFrameSource();
            Assert.That(source.TryRead(0, out var frame), Is.False); Assert.That(frame, Is.Null);
            source.Sample(null, null, 5, true, false);
            Assert.That(source.TryRead(0, out frame), Is.True); Assert.That(frame.Left, Is.Null); Assert.That(frame.Tick, Is.EqualTo(0));
        }

        [Test] public void TicksBetweenSamplesHoldTheLatestSample()
        {
            var source = new LiveFrameSource(); var a = Hand(HandSide.Left, 0); var b = Hand(HandSide.Left, 0.01);
            source.Sample(a, null, 1000, true, false);
            Assert.That(source.TryRead(1, out var f1), Is.True); Assert.That(source.TryRead(2, out var f2), Is.True);
            Assert.That(f1.Left, Is.SameAs(a)); Assert.That(f2.Left, Is.SameAs(a)); Assert.That(f2.TimestampMicroseconds, Is.EqualTo(1000));
            source.Sample(b, null, 900, false, true); // the clock stepped back
            Assert.That(source.TryRead(3, out var f3), Is.True);
            Assert.That(f3.Left, Is.SameAs(b)); Assert.That(f3.TimestampMicroseconds, Is.EqualTo(1000)); Assert.That(f3.HasFocus, Is.False); Assert.That(f3.ControllersInHand, Is.True);
            Assert.Throws<ArgumentException>(() => source.Sample(Hand(HandSide.Right, 0), null, 2000, true, false));
        }

        [Test] public void CommandsGoOutOnePerTickInOrder()
        {
            var source = new LiveFrameSource(); source.Sample(null, null, 0, true, false);
            Assert.That(source.Request(InputCommand.Pause) && source.Request(InputCommand.Resume) && source.Request(InputCommand.None), Is.True);
            var commands = Enumerable.Range(1, 3).Select(t => { source.TryRead(t, out var f); return f.Command; }).ToArray();
            Assert.That(commands, Is.EqualTo(new[] { InputCommand.Pause, InputCommand.Resume, InputCommand.None }));
            for (int i = 0; i < LiveFrameSource.MaximumQueuedCommands; i++) Assert.That(source.Request(InputCommand.Restart), Is.True);
            Assert.That(source.Request(InputCommand.Restart), Is.False);
            source.ClearCommands(); Assert.That(source.QueuedCommands, Is.EqualTo(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => source.Request((InputCommand)99));
        }

        [TestCase(72.0)] [TestCase(90.0)] [TestCase(50.0)]
        public void TheClockRunsSixtyTicksASecondAtAnyFrameRate(double hz)
        {
            var game = new PalmfolkGame(TestValley.Chapter1()); var source = new LiveFrameSource(); var clock = new FixedTickRunner();
            for (int frame = 0; frame < hz * 10; frame++) { source.Sample(null, null, (long)(frame / hz * 1e6), true, false); clock.Advance(1 / hz, source, game); }
            Assert.That(game.Tick + 1, Is.InRange(599, 600));
        }

        [Test] public void AWholeValleyPlaysFromLiveSamplesAtSeventyTwoHertz()
        {
            var game = new PalmfolkGame(TestValley.Chapter1());
            LivePlay.Run(game, game, 72, g => g.Valley.IsComplete);
            Assert.That(game.Valley.IsComplete, Is.True); Assert.That(game.Plantings, Is.EqualTo(1));
        }
    }
}
