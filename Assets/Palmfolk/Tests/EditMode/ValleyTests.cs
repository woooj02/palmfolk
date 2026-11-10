using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Palmfolk.Core;

namespace Palmfolk.Tests
{
    // World-space hands for planting: the synthetic hand turned, tilted, resized and placed in a room.
    public static class Planted
    {
        public static readonly double SyntheticHandLength = HandMeasure.Length(Synthetic.Hand()).Value;
        private static readonly Vec3 PalmCentre = new Vec3(0.035, 0, 0.025);

        public sealed class Placement
        {
            public double Height = 0.75, YawDegrees, TiltDegrees, HandLength = SyntheticHandLength, Noise;
            public bool PalmUp;
            public Vec3 Offset = new Vec3(0.1, 0, 0.4);
            public Vec3 Drift; // metres per second
            public Vec3 Apply(Vec3 p, long tick = 0, int joint = 0)
            {
                var r = (p - PalmCentre) * (HandLength / SyntheticHandLength);
                if (PalmUp) r = new Vec3(r.X, -r.Y, -r.Z);
                double t = TiltDegrees * Math.PI / 180, y = YawDegrees * Math.PI / 180;
                r = new Vec3(r.X, r.Y * Math.Cos(t) - r.Z * Math.Sin(t), r.Y * Math.Sin(t) + r.Z * Math.Cos(t));
                r = new Vec3(r.X * Math.Cos(y) + r.Z * Math.Sin(y), r.Y, -r.X * Math.Sin(y) + r.Z * Math.Cos(y));
                return r + Offset + new Vec3(0, Height, 0) + Drift * (tick * GameSettings.Delta) + Jitter(tick, joint) * Noise;
            }
            public HandSample Hand(long tick = 0, HandSide side = HandSide.Left, bool flat = true) => Map(Synthetic.Hand(flat, side), j => Apply(j.Position, tick, j.Index));
            public Vec3 Palm => Apply(PalmCentre);
            public Vec3 Forward
            {
                get { var f = Apply(new Vec3(0.005, 0, 0.018)) - Apply(new Vec3(0.06, 0, 0)); return new Vec3(f.X, 0, f.Z).Normalized; }
            }
        }

        // Deterministic noise in [-1, 1] per tick, joint and axis.
        public static Vec3 Jitter(long tick, int joint)
        {
            double N(int axis) { unchecked { uint h = (uint)(tick * 73856093L) ^ (uint)(joint * 19349663) ^ (uint)(axis * 83492791); h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15; return (h % 20001) / 10000.0 - 1; } }
            return new Vec3(N(1), N(2), N(3));
        }

        public static HandSample Map(HandSample hand, Func<(Vec3 Position, int Index), Vec3> f)
        {
            var joints = new JointSample[26];
            for (int i = 0; i < 26; i++) joints[i] = hand.Joints[i].WithPosition(f((hand.Joints[i].Position, i)));
            return new HandSample(hand.Side, joints);
        }

        public static HandFrame Frame(long tick, HandSample left = null, HandSample right = null, InputCommand command = InputCommand.None, bool controllers = false, bool focus = true) =>
            new HandFrame(tick, tick * 16667, left, right, focus, command, controllers);

        // Steps a detector with a placement until it plants; returns the result and the tick it planted on.
        public static (PlantingResult Result, long Tick) Run(PlantingDetector detector, Placement placement, int ticks = 300, HandSide side = HandSide.Left)
        {
            for (long tick = 0; tick < ticks; tick++)
            {
                var hand = placement.Hand(tick, side);
                var result = detector.Step(side == HandSide.Left ? Frame(tick, hand) : Frame(tick, null, hand));
                if (result != null) return (result, tick);
            }
            return (null, -1);
        }

        public static Vec3 Rotate(Rotation q, Vec3 v)
        {
            var r = q * new Rotation(v.X, v.Y, v.Z, 0) * q.Inverse;
            return new Vec3(r.X, r.Y, r.Z);
        }
    }

    public class ValleyPoseTests
    {
        private static readonly Vec3[] Points = { Vec3.Zero, new Vec3(0.03, 0.006, -0.12), new Vec3(-0.25, 1.1, 0.7), new Vec3(1e-7, -3, 2.5) };

        [Test] public void IdentityPoseIsExact()
        {
            var pose = ValleyPose.Identity;
            foreach (var p in Points) { Assert.That(pose.ToValley(p), Is.EqualTo(p)); Assert.That(pose.ToWorld(p), Is.EqualTo(p)); }
            var hand = Synthetic.Hand();
            var local = pose.ToValley(hand);
            for (int i = 0; i < 26; i++) { Assert.That(local.Joints[i].Position, Is.EqualTo(hand.Joints[i].Position)); Assert.That(local.Joints[i].Radius, Is.EqualTo(hand.Joints[i].Radius)); }
        }

        [Test] public void PoseRoundTripsAndKeepsUpAndHandedness()
        {
            var pose = new ValleyPose(new Vec3(0.3, 0.75, -0.2), new Vec3(2, 0.4, 2), 1.2);
            Assert.That(pose.Forward.Y, Is.EqualTo(0)); Assert.That(pose.Forward.Length, Is.EqualTo(1).Within(1e-12));
            var expectedRight = Vec3.Cross(Vec3.Up, pose.Forward);
            Assert.That(Vec3.Distance(pose.Right, expectedRight), Is.LessThan(1e-12));
            Assert.That(pose.ToValley(pose.Origin), Is.EqualTo(Vec3.Zero));
            Assert.That(Vec3.Distance(pose.ToValley(pose.Origin + pose.Forward * 1.2), new Vec3(0, 0, 1)), Is.LessThan(1e-12));
            Assert.That(Vec3.Distance(pose.ToValley(pose.Origin + pose.Right * 1.2), new Vec3(1, 0, 0)), Is.LessThan(1e-12));
            Assert.That(Vec3.Distance(pose.ToValley(pose.Origin + Vec3.Up * 1.2), new Vec3(0, 1, 0)), Is.LessThan(1e-12));
            foreach (var p in Points)
            {
                Assert.That(Vec3.Distance(pose.ToWorld(pose.ToValley(p)), p), Is.LessThan(1e-12));
                Assert.That(Vec3.Distance(pose.ToValley(pose.ToWorld(p)), p), Is.LessThan(1e-12));
            }
            Assert.That(pose.ToValleyLength(0.06), Is.EqualTo(0.05).Within(1e-15)); Assert.That(pose.ToWorldLength(0.05), Is.EqualTo(0.06).Within(1e-15));
        }

        [Test] public void RotationMatchesTheFrameAndJointsCarryOver()
        {
            var pose = new ValleyPose(new Vec3(0.3, 0.75, -0.2), new Vec3(1, 0, 0), 1.5);
            Assert.That(pose.YawRadians, Is.EqualTo(Math.PI / 2).Within(1e-12));
            Assert.That(Vec3.Distance(Planted.Rotate(pose.Rotation, new Vec3(0, 0, 1)), pose.Forward), Is.LessThan(1e-12));
            Assert.That(Vec3.Distance(Planted.Rotate(pose.Rotation, new Vec3(1, 0, 0)), pose.Right), Is.LessThan(1e-12));
            var tilt = new Rotation(Math.Sin(0.2), 0, 0, Math.Cos(0.2));
            var joint = new JointSample(new Vec3(0.4, 0.8, 0.1), pose.Rotation * tilt, 0.006, JointFlags.PositionValid | JointFlags.RotationValid | JointFlags.HighConfidence | JointFlags.RadiusValid);
            var local = pose.ToValley(joint);
            Assert.That(local.Radius, Is.EqualTo(0.004).Within(1e-15)); Assert.That(local.Flags, Is.EqualTo(joint.Flags));
            Assert.That(Math.Abs(local.Rotation.X - tilt.X) + Math.Abs(local.Rotation.Y) + Math.Abs(local.Rotation.Z) + Math.Abs(local.Rotation.W - tilt.W), Is.LessThan(1e-12));
            var back = pose.ToWorld(local);
            Assert.That(Vec3.Distance(back.Position, joint.Position), Is.LessThan(1e-12)); Assert.That(back.Radius, Is.EqualTo(0.006).Within(1e-15));
        }

        [Test] public void FramesKeepEverythingButSpace()
        {
            var pose = new ValleyPose(new Vec3(0.1, 0.7, 0.3), new Vec3(-1, 0, 0.5), 0.9);
            var frame = new HandFrame(12, 200004, Synthetic.Hand(), null, false, InputCommand.Pause, true);
            var local = pose.ToValley(frame);
            Assert.That((local.Tick, local.TimestampMicroseconds, local.Right, local.HasFocus, local.Command, local.ControllersInHand), Is.EqualTo((12L, 200004L, (HandSample)null, false, InputCommand.Pause, true)));
            Assert.That(Vec3.Distance(local.Left[JointId.Palm].Position, pose.ToValley(frame.Left[JointId.Palm].Position)), Is.EqualTo(0));
        }

        [Test] public void InvalidPosesAreRefused()
        {
            Assert.Throws<ArgumentException>(() => new ValleyPose(Vec3.Zero, Vec3.Up, 1));
            Assert.Throws<ArgumentException>(() => new ValleyPose(Vec3.Zero, new Vec3(0, 0, 1), 0));
            Assert.Throws<ArgumentException>(() => new ValleyPose(new Vec3(double.NaN, 0, 0), new Vec3(0, 0, 1), 1));
            Assert.Throws<ArgumentException>(() => new ValleyPose(Vec3.Zero, new Vec3(double.PositiveInfinity, 0, 1), 1));
        }
    }

    public class HandMeasureTests
    {
        [TestCase(0.15, 0.15 / 0.18)] [TestCase(0.18, 1.0)] [TestCase(0.21, 0.21 / 0.18)] [TestCase(0.10, 0.75)] [TestCase(0.30, 1.3)]
        public void BoneLengthsSetTheScale(double length, double scale)
        {
            var hand = new Planted.Placement { HandLength = length }.Hand();
            Assert.That(HandMeasure.Length(hand), Is.EqualTo(length).Within(1e-12));
            Assert.That(HandMeasure.Scale(length, new GameSettings()), Is.EqualTo(scale).Within(1e-12));
        }

        [Test] public void WorldScaleSettingMultipliesAndIsBounded()
        {
            Assert.That(HandMeasure.Scale(0.18, new GameSettings { WorldScaleAdjustment = 1.5 }), Is.EqualTo(1.5).Within(1e-12));
            Assert.That(HandMeasure.Scale(0.30, new GameSettings { WorldScaleAdjustment = 0.8 }), Is.EqualTo(1.04).Within(1e-12));
            Assert.Throws<ArgumentException>(() => new GameSettings { WorldScaleAdjustment = 3 }.Validate());
            Assert.Throws<ArgumentException>(() => new GameSettings { MinimumHandScale = 2 }.Validate());
            Assert.Throws<ArgumentException>(() => new GameSettings { PlantMaximumTiltDegrees = 90 }.Validate());
            Assert.Throws<ArgumentException>(() => new GameSettings { PlantTicks = 0 }.Validate());
        }

        [Test] public void UntrackedChainHasNoLength()
        {
            Assert.That(HandMeasure.Length(Synthetic.Hand(invalid: 1u << (int)JointId.MiddleDistal)), Is.Null);
            Assert.That(HandMeasure.Length(null), Is.Null);
        }
    }

    public class PlantingTests
    {
        public static IEnumerable<TestCaseData> Poses()
        {
            yield return new TestCaseData(new Planted.Placement { Height = 0.75, YawDegrees = 0, Noise = 0.0015 }).SetName("Table, palm down");
            yield return new TestCaseData(new Planted.Placement { Height = 0.62, YawDegrees = 35, TiltDegrees = 15, HandLength = 0.2, Noise = 0.0015 }).SetName("Lap, tilted 15 degrees");
            yield return new TestCaseData(new Planted.Placement { Height = 1.1, YawDegrees = -120, PalmUp = true, HandLength = 0.16, Noise = 0.0015 }).SetName("Air, palm up");
            yield return new TestCaseData(new Planted.Placement { Height = 0.9, YawDegrees = 70, Drift = new Vec3(0.01, 0, 0), Noise = 0.001 }).SetName("Air, slow drift under 3 cm per s");
        }

        [TestCaseSource(nameof(Poses))]
        public void FlatRestingPalmPlantsThereWithFacingAndScale(Planted.Placement placement)
        {
            var settings = new GameSettings();
            var (result, tick) = Planted.Run(new PlantingDetector(settings), placement);
            Assert.That(result, Is.Not.Null, "did not plant");
            Assert.That(tick, Is.InRange(settings.PlantTicks, settings.PlantTicks + 5));
            Assert.That(result.Hand, Is.EqualTo(HandSide.Left));
            var palm = placement.Palm + placement.Drift * (tick * GameSettings.Delta);
            Assert.That(Vec3.Distance(result.Pose.Origin, palm - Vec3.Up * settings.PalmHalfThickness), Is.LessThan(0.006));
            Assert.That(Vec3.Dot(result.Pose.Forward, placement.Forward), Is.GreaterThan(Math.Cos(2 * Math.PI / 180)));
            Assert.That(result.HandLength, Is.EqualTo(placement.HandLength).Within(0.002));
            Assert.That(result.Pose.Scale, Is.EqualTo(HandMeasure.Scale(placement.HandLength, settings)).Within(0.02));
        }

        [Test] public void ProgressFillsOverOneSecond()
        {
            var detector = new PlantingDetector(); var placement = new Planted.Placement();
            for (long tick = 0; tick < 30; tick++) Assert.That(detector.Step(Planted.Frame(tick, placement.Hand(tick))), Is.Null);
            Assert.That(detector.Progress, Is.EqualTo(30.0 / 61).Within(1e-12)); Assert.That(detector.Hand, Is.EqualTo(HandSide.Left));
            Assert.That(detector.Step(Planted.Frame(30, null)), Is.Null);
            Assert.That(detector.Progress, Is.EqualTo(0)); Assert.That(detector.Hand, Is.Null);
            var (result, tick2) = Planted.Run(detector, placement);
            Assert.That(result, Is.Not.Null); Assert.That(tick2, Is.EqualTo(60));
        }

        public static IEnumerable<TestCaseData> Refusals()
        {
            yield return new TestCaseData(new Planted.Placement { TiltDegrees = 30 }, false).SetName("Tilted 30 degrees");
            yield return new TestCaseData(new Planted.Placement { PalmUp = true, TiltDegrees = 75 }, false).SetName("Palm on its side");
            yield return new TestCaseData(new Planted.Placement { Drift = new Vec3(0.05, 0, 0) }, false).SetName("Moving at 5 cm per s");
            yield return new TestCaseData(new Planted.Placement { Drift = new Vec3(0, 0, -0.4) }, false).SetName("Sweeping at 40 cm per s");
            yield return new TestCaseData(new Planted.Placement(), true).SetName("Curled fingers");
        }

        [TestCaseSource(nameof(Refusals))]
        public void OnlyAFlatStillPalmPlants(Planted.Placement placement, bool curl)
        {
            var detector = new PlantingDetector();
            for (long tick = 0; tick < 300; tick++)
            {
                var hand = placement.Hand(tick);
                if (curl) hand = Planted.Map(hand, j => j.Index % 5 == 0 && j.Index >= 10 ? j.Position + Vec3.Up * 0.03 : j.Position);
                Assert.That(detector.Step(Planted.Frame(tick, hand)), Is.Null, $"planted at tick {tick}");
            }
        }

        [Test] public void ControllersInHandWaitAndPromptOnce()
        {
            var detector = new PlantingDetector(); var hand = new Planted.Placement().Hand(); int prompts = 0;
            for (long tick = 0; tick < 700; tick++)
            {
                Assert.That(detector.Step(Planted.Frame(tick, hand, controllers: true)), Is.Null);
                Assert.That(detector.WaitingForHands, Is.True); Assert.That(detector.Progress, Is.EqualTo(0));
                if (detector.StartupPromptDue) { prompts++; Assert.That(tick, Is.EqualTo(599)); }
            }
            Assert.That(prompts, Is.EqualTo(1)); Assert.That(detector.ShowStartupText, Is.True);
            Assert.That(detector.Step(Planted.Frame(700, hand)), Is.Null);
            Assert.That(detector.WaitingForHands, Is.False); Assert.That(detector.ShowStartupText, Is.False);
        }

        [Test] public void NoUsableHandWaitsAndFocusLossRestarts()
        {
            var detector = new PlantingDetector(); var placement = new Planted.Placement();
            detector.Step(Planted.Frame(0, Synthetic.Hand(invalid: 3)));
            Assert.That(detector.WaitingForHands, Is.True);
            for (long tick = 1; tick < 40; tick++) detector.Step(Planted.Frame(tick, placement.Hand(tick)));
            Assert.That(detector.Progress, Is.GreaterThan(0.5));
            detector.Step(Planted.Frame(40, placement.Hand(40), focus: false));
            Assert.That(detector.Progress, Is.EqualTo(0));
        }

        [Test] public void EitherHandPlantsAndTheRestingHandKeepsItsTurn()
        {
            var placement = new Planted.Placement();
            var (right, _) = Planted.Run(new PlantingDetector(), placement, side: HandSide.Right);
            Assert.That(right, Is.Not.Null); Assert.That(right.Hand, Is.EqualTo(HandSide.Right));

            var detector = new PlantingDetector(); var other = new Planted.Placement { Offset = new Vec3(-0.3, 0, 0.4) };
            for (long tick = 0; tick < 20; tick++) detector.Step(Planted.Frame(tick, null, placement.Hand(tick, HandSide.Right)));
            PlantingResult result = null;
            for (long tick = 20; tick < 200 && result == null; tick++) result = detector.Step(Planted.Frame(tick, other.Hand(tick), placement.Hand(tick, HandSide.Right)));
            Assert.That(result.Hand, Is.EqualTo(HandSide.Right));
            Assert.That(Vec3.Distance(result.Pose.Origin, placement.Palm - Vec3.Up * 0.015), Is.LessThan(0.003));
        }

        [Test] public void StillnessToleranceStepLoosensPlanting()
        {
            var placement = new Planted.Placement { Drift = new Vec3(0.036, 0, 0) };
            Assert.That(Planted.Run(new PlantingDetector(), placement).Result, Is.Null);
            Assert.That(Planted.Run(new PlantingDetector(new GameSettings { StillnessToleranceStep = 2 }), placement).Result, Is.Not.Null);
        }
    }

    public class PalmfolkGameTests
    {
        private static readonly Planted.Placement Table = new Planted.Placement { Height = 0.74, YawDegrees = 37, HandLength = 0.2, Offset = new Vec3(0.2, 0, -0.35) };

        private static void Step(PalmfolkGame game, HandSample left, InputCommand command = InputCommand.None, List<GameEventKind> events = null)
        {
            game.Step(Planted.Frame(game.Tick + 1, left, null, command));
            events?.AddRange(game.Events.Select(e => e.Kind));
        }
        private static void PlantWith(PalmfolkGame game, Planted.Placement placement, List<GameEventKind> events = null)
        {
            for (int i = 0; i < 300 && game.Stage == ValleyStage.Planting; i++) Step(game, placement.Hand(game.Tick + 1), events: events);
            Assert.That(game.Stage, Is.EqualTo(ValleyStage.Playing), "did not plant");
        }
        private static HandSample Valley(PalmfolkGame game) => game.Pose.ToWorld(Synthetic.Hand());

        [Test] public void StartsByPlantingAndThePuzzleWaits()
        {
            var game = new PalmfolkGame(Synthetic.Level); var events = new List<GameEventKind>();
            Assert.That(game.Stage, Is.EqualTo(ValleyStage.Planting)); Assert.That(game.Pose, Is.Null);
            Step(game, Table.Hand(0));
            var view = new WorldPresentation(game, 1);
            Assert.That(view.Stage, Is.EqualTo(ValleyStage.Planting)); Assert.That(view.Pose, Is.Null); Assert.That(view.Paused, Is.False);
            PlantWith(game, Table, events);
            Assert.That(events.Count(e => e == GameEventKind.Planted), Is.EqualTo(1)); Assert.That(game.Plantings, Is.EqualTo(1));
            Assert.That(game.Puzzle.Owner, Is.Null); Assert.That(game.Puzzle.Phase, Is.EqualTo(M1Phase.Board));
            Assert.That(game.Pose.Scale, Is.EqualTo(0.2 / 0.18).Within(0.02));
            view = new WorldPresentation(game, 1);
            Assert.That(view.Stage, Is.EqualTo(ValleyStage.Playing)); Assert.That(view.Pose, Is.SameAs(game.Pose));
        }

        [Test] public void ControllersInHandShowInPresentationWhilePlanting()
        {
            var game = new PalmfolkGame(Synthetic.Level);
            game.Step(Planted.Frame(0, Table.Hand(), controllers: true));
            var view = new WorldPresentation(game, 1);
            Assert.That(view.WaitingForHands, Is.True); Assert.That(view.PutControllersDown, Is.True);
        }

        [Test] public void PuzzleInAPlantedValleyMatchesThePlainSimulation()
        {
            var game = new PalmfolkGame(Synthetic.Level); PlantWith(game, Table);
            var reference = new M1Simulation(Synthetic.Level);
            for (long t = 0; t <= game.Tick; t++) reference.Step(Synthetic.Frame(t, focus: false));
            var gameEvents = new List<GameEventKind>(); var referenceEvents = new List<GameEventKind>();
            for (int i = 0; i < 1000; i++)
            {
                Step(game, Valley(game), events: gameEvents);
                reference.Step(Synthetic.Frame(reference.Tick + 1, Synthetic.Hand())); referenceEvents.AddRange(reference.Events.Select(e => e.Kind));
                Assert.That(Vec3.Distance(game.Puzzle.Pip, reference.Pip), Is.LessThan(1e-9), $"tick {game.Tick}");
            }
            Assert.That(reference.Phase, Is.EqualTo(M1Phase.Complete));
            Assert.That(game.Puzzle.Phase, Is.EqualTo(M1Phase.Complete));
            Assert.That(gameEvents, Is.EqualTo(referenceEvents));
            AssertSameShape(game.Puzzle.Bridge, reference.Bridge, 1e-9);
        }

        [Test] public void ReplantingAfterTheBridgeKeepsItBitForBit()
        {
            var game = new PalmfolkGame(Synthetic.Level); PlantWith(game, Table);
            for (int i = 0; i < 2000 && game.Puzzle.Bridge == null; i++) Step(game, Valley(game));
            var bridge = game.Puzzle.Bridge; Assert.That(bridge, Is.Not.Null);
            var before = bridge.Curves.Select(c => c.CopyPoints()).ToArray(); var firstPose = game.Pose;
            var events = new List<GameEventKind>();

            Step(game, Valley(game), InputCommand.Replant, events);
            Assert.That(game.Stage, Is.EqualTo(ValleyStage.Planting)); Assert.That(events, Has.Member(GameEventKind.ReplantStarted));
            Assert.That(game.Pose, Is.SameAs(firstPose));
            var lap = new Planted.Placement { Height = 0.6, YawDegrees = -80, TiltDegrees = 10, HandLength = 0.16, Offset = new Vec3(-0.4, 0, 0.1), Noise = 0.001 };
            PlantWith(game, lap, events);
            Assert.That(game.Plantings, Is.EqualTo(2)); Assert.That(game.Pose, Is.Not.SameAs(firstPose));
            Assert.That(Vec3.Distance(game.Pose.Origin, firstPose.Origin), Is.GreaterThan(0.3));
            Assert.That(game.Pose.Scale, Is.Not.EqualTo(firstPose.Scale).Within(0.05));

            Assert.That(game.Puzzle.Bridge, Is.SameAs(bridge));
            for (int c = 0; c < before.Length; c++) Assert.That(bridge.Curves[c].CopyPoints(), Is.EqualTo(before[c]));

            for (int i = 0; i < 2000 && game.Puzzle.Phase != M1Phase.Complete; i++) Step(game, Valley(game));
            Assert.That(game.Puzzle.Phase, Is.EqualTo(M1Phase.Complete));
            for (int c = 0; c < before.Length; c++) Assert.That(game.Puzzle.Bridge.Curves[c].CopyPoints(), Is.EqualTo(before[c]));
        }

        [Test] public void ReplantingWhileCarryingPipRecoversThroughTheGhost()
        {
            var game = new PalmfolkGame(Synthetic.Level); PlantWith(game, Table);
            for (int i = 0; i < 2000 && game.Puzzle.Owner == null; i++) Step(game, Valley(game));
            Assert.That(game.Puzzle.Owner, Is.EqualTo(HandSide.Left));
            var events = new List<GameEventKind>();
            Step(game, Valley(game), InputCommand.Replant, events);
            Assert.That(game.Puzzle.Recovery, Is.Not.Null);
            PlantWith(game, new Planted.Placement { Height = 0.95, YawDegrees = 150, PalmUp = true, Offset = new Vec3(0, 0, 0.3) }, events);
            for (int i = 0; i < 3000 && game.Puzzle.Phase != M1Phase.Complete; i++) Step(game, Valley(game), events: events);
            Assert.That(events, Has.Member(GameEventKind.RecoveryCompleted));
            Assert.That(game.Puzzle.Phase, Is.EqualTo(M1Phase.Complete));
        }

        [Test] public void ReplantIsIgnoredWhilePlantingAndPauseHoldsPlanting()
        {
            var game = new PalmfolkGame(Synthetic.Level); var events = new List<GameEventKind>();
            Step(game, Table.Hand(0), InputCommand.Replant, events);
            Assert.That(events, Has.No.Member(GameEventKind.ReplantStarted));
            Step(game, Table.Hand(1), InputCommand.Pause);
            for (int i = 0; i < 200; i++) Step(game, Table.Hand(game.Tick + 1));
            Assert.That(game.Stage, Is.EqualTo(ValleyStage.Planting)); Assert.That(new WorldPresentation(game, 1).Paused, Is.True);
            Step(game, Table.Hand(game.Tick + 1), InputCommand.Resume);
            PlantWith(game, Table);
        }

        private static void AssertSameShape(CurveTree a, CurveTree b, double tolerance)
        {
            Assert.That(a, Is.Not.Null); Assert.That(b, Is.Not.Null);
            Assert.That(a.Curves.Count, Is.EqualTo(b.Curves.Count)); Assert.That(a.Parents, Is.EqualTo(b.Parents));
            for (int c = 0; c < a.Curves.Count; c++)
            {
                Assert.That(a.Curves[c].Points.Count, Is.EqualTo(b.Curves[c].Points.Count));
                for (int i = 0; i < a.Curves[c].Points.Count; i++) Assert.That(Vec3.Distance(a.Curves[c].Points[i], b.Curves[c].Points[i]), Is.LessThan(tolerance));
            }
        }
    }
}
