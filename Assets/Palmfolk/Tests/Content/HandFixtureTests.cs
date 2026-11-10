using System;
using System.Linq;
using NUnit.Framework;
using Palmfolk.Core;
using Palmfolk.Fixtures;

namespace Palmfolk.Tests.Content
{
    public class HandFixtureTests
    {
        [TestCase(HandSide.Left)] [TestCase(HandSide.Right)]
        public void AllShapesAndSizesHaveUsableRoutes(HandSide side)
        {
            foreach (HandShape shape in Enum.GetValues(typeof(HandShape)))
                foreach (HandSize size in Enum.GetValues(typeof(HandSize)))
                {
                    var hand = new HandSequence(shape, size, side).Sample(0);
                    var route = HandRoutes.Create(hand, 1, false, new GameSettings());
                    Assert.That(hand.InvalidMask, Is.Zero);
                    Assert.That(route, Is.Not.Null, shape + " " + size);
                    Assert.That(route.Length, Is.GreaterThan(0.09));
                    if (shape == HandShape.Flat) Assert.That(route.MinimumWidth, Is.GreaterThanOrEqualTo(0.025));
                    if (shape == HandShape.OneFinger || shape == HandShape.Spread) Assert.That(route.MinimumWidth, Is.EqualTo(0.012));
                    if (shape == HandShape.Curved) Assert.That(route.Points.Max(p => p.Y), Is.GreaterThan(0.018));
                }
        }

        [Test] public void ScaleAndPalmFacingAreExplicit()
        {
            var small = new HandSequence(size: HandSize.Small).Sample(0);
            var large = new HandSequence(size: HandSize.Large).Sample(0);
            double Length(HandSample h) => Vec3.Distance(h[JointId.Wrist].Position, h[JointId.IndexTip].Position);
            Assert.That(Length(large) / Length(small), Is.EqualTo(1.5).Within(1e-12));
            foreach (HandSide side in Enum.GetValues(typeof(HandSide)))
                Assert.That(HandRoutes.Create(new HandSequence(side: side, palmDown: true).Sample(0), 1, false, new GameSettings()), Is.Null);
        }

        [Test] public void SeededTremorIsBoundedAndIndependentOfReadOrder()
        {
            var a = new HandSequence(seed: 29, tremorMetres: 0.001);
            var b = new HandSequence(seed: 29, tremorMetres: 0.001);
            var c = new HandSequence(seed: 30, tremorMetres: 0.001);
            var clean = new HandSequence().Sample(0);
            for (int tick = 99; tick >= 0; tick--)
            {
                Assert.That(a.Sample(tick).Joints.Select(j => j.Position), Is.EqualTo(b.Sample(tick).Joints.Select(j => j.Position)));
                Assert.That(Vec3.Distance(a.Sample(tick)[JointId.Wrist].Position, clean[JointId.Wrist].Position), Is.LessThanOrEqualTo(0.001));
            }
            Assert.That(a.Sample(17)[JointId.Wrist].Position, Is.Not.EqualTo(c.Sample(17)[JointId.Wrist].Position));
            a.TryRead(3, out var frame); Assert.That(frame.TimestampMicroseconds, Is.EqualTo(50000));
            Assert.That(a.TryRead(1800, out _), Is.False);
        }

        [Test] public void DropoutsAreFramesNotEndOfStreamAndFlagsStayDistinct()
        {
            uint tip = 1u << (int)JointId.IndexTip;
            var sequence = new HandSequence(faults: new[] {
                new TrackingWindow(2, 4, TrackingFault.MissingHand),
                new TrackingWindow(4, 5, TrackingFault.InvalidPosition, tip),
                new TrackingWindow(5, 6, TrackingFault.LowConfidence, tip),
                new TrackingWindow(6, 7, TrackingFault.MissingRadius, tip) });
            Assert.That(sequence.TryRead(2, out var frame), Is.True); Assert.That(frame.Left, Is.Null);
            Assert.That(sequence.Sample(4).InvalidMask, Is.EqualTo(tip));
            Assert.That(sequence.Sample(5)[JointId.IndexTip].Flags.HasFlag(JointFlags.PositionValid), Is.True);
            Assert.That(sequence.Sample(5).InvalidMask, Is.EqualTo(tip));
            Assert.That(sequence.Sample(6).InvalidMask, Is.Zero);
            Assert.That(sequence.Sample(6)[JointId.IndexTip].HasRadius, Is.False);
            Assert.That(sequence.Sample(7).InvalidMask, Is.Zero);
        }

        [Test] public void WithdrawalInterpolatesAndHoldsItsEndPose()
        {
            var s = new HandSequence(withdrawStart: 10, withdrawEnd: 30, withdrawal: new Vec3(0, 0, 0.2));
            Vec3 start = s.Sample(0)[JointId.Wrist].Position;
            Assert.That(s.Sample(10)[JointId.Wrist].Position, Is.EqualTo(start));
            Assert.That(s.Sample(20)[JointId.Wrist].Position.Z, Is.EqualTo(start.Z + 0.1).Within(1e-12));
            Assert.That(s.Sample(100)[JointId.Wrist].Position.Z, Is.EqualTo(start.Z + 0.2).Within(1e-12));
        }

        [Test] public void FixtureBuildSurvivesWithdrawalAndCartCrosses()
        {
            var level = new LevelGeometry(new Vec3(-0.06, 0.006, 0), new Vec3(0.06, 0.006, 0));
            var game = new M1Simulation(level, new GameSettings { TravelerSpeed = 0.3 });
            var sequence = new HandSequence(ticks: 900, withdrawStart: 400, withdrawEnd: 430, withdrawal: new Vec3(0, 0, 0.2));
            foreach (var frame in sequence.Frames()) game.Step(frame);
            Assert.That(game.Bridge, Is.Not.Null); Assert.That(game.Phase, Is.EqualTo(M1Phase.Complete));
        }

        [Test] public void FixtureLossFreezesAndFarReturnCannotTeleport()
        {
            var level = new LevelGeometry(new Vec3(-0.06, 0.006, 0), new Vec3(0.06, 0.006, 0));
            var game = new M1Simulation(level);
            var sequence = new HandSequence(ticks: 90, withdrawStart: 20, withdrawEnd: 35,
                withdrawal: new Vec3(0, 0, 0.2), faults: new[] { new TrackingWindow(20, 40, TrackingFault.MissingHand) });
            foreach (var frame in sequence.Frames())
            {
                game.Step(frame);
                if (frame.Tick == 20) Assert.That(game.Recovery, Is.Not.Null);
            }
            Assert.That(game.Recovery, Is.Not.Null); Assert.That(game.PipCheckpoint, Is.Zero);
            Assert.That(game.Pip.Z, Is.EqualTo(0).Within(1e-9));
        }
    }
}
