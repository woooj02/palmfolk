using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Palmfolk.Core;

namespace Palmfolk.Tests
{
    public static class Synthetic
    {
        public static readonly LevelGeometry Level = new LevelGeometry(new Vec3(-0.06, 0.006, 0), new Vec3(0.06, 0.006, 0));
        public static HandSample Hand(bool flat = true, HandSide side = HandSide.Left, Vec3 offset = default, uint invalid = 0)
        {
            var joints = new JointSample[26];
            for (int i = 0; i < 26; i++) joints[i] = Joint(new Vec3(0.06, 0, 0) + offset, 0.006);
            joints[0] = Joint(new Vec3(0.035, 0, 0.025) + offset, 0.03);
            joints[1] = Joint(new Vec3(0.06, 0, 0) + offset, 0.03);
            double[] x = { 0.03, 0.005, -0.015, -0.04, -0.06 };
            for (int finger = 1; finger <= 4; finger++)
                for (int bone = 0; bone < 5; bone++)
                {
                    double z = finger == 1 ? 0 : (flat ? (finger - 1) * 0.018 : (finger - 1) * 0.08);
                    if (side == HandSide.Right) z = -z;
                    joints[6 + (finger - 1) * 5 + bone] = Joint(new Vec3(x[bone], 0, z) + offset, 0.006);
                }
            for (int i = 2; i <= 5; i++) joints[i] = Joint(new Vec3(0.04 - (i - 2) * 0.02, 0, side == HandSide.Left ? -0.1 : 0.1) + offset, 0.006);
            for (int i = 0; i < 26; i++) if ((invalid & (1u << i)) != 0) joints[i] = new JointSample(joints[i].Position, Rotation.Identity, joints[i].Radius, JointFlags.None);
            return new HandSample(side, joints);
        }
        private static JointSample Joint(Vec3 p, double radius) => new JointSample(p, Rotation.Identity, radius, JointFlags.PositionValid | JointFlags.RotationValid | JointFlags.HighConfidence | JointFlags.RadiusValid);
        public static HandFrame Frame(long tick, HandSample left = null, HandSample right = null, bool focus = true, InputCommand command = InputCommand.None) => new HandFrame(tick, tick * 16667, left, right, focus, command);
        public static void Step(M1Simulation game, HandSample left = null, HandSample right = null, int count = 1)
        { for (int i = 0; i < count; i++) game.Step(Frame(game.Tick + 1, left, right)); }
        public static Route Line(double width = 0.04, double height = 0) => new Route(new[] { new Vec3(-0.06, height, 0), new Vec3(0, height, 0), new Vec3(0.06, height, 0) }, new[] { width, width });
    }
    public class GeometryTests
    {
        [Test] public void RouteOwnsItsDataAndCursorSticksToMovingSegment()
        {
            var points = new[] { Vec3.Zero, new Vec3(0.1, 0, 0), new Vec3(0.2, 0, 0) }; var widths = new[] { 0.04, 0.04 };
            var route = new Route(points, widths); points[0] = new Vec3(9, 9, 9); widths[0] = 0.001;
            var cursor = route.Advance(new RouteCursor(0, 0), 0.05);
            var shifted = new Route(route.Points.Select(p => p + new Vec3(0, 0.1, 0)).ToArray(), route.CopyWidths());
            Assert.That(route.Start, Is.EqualTo(Vec3.Zero)); Assert.That(route.MinimumWidth, Is.EqualTo(0.04));
            Assert.That(shifted.Evaluate(cursor).Y, Is.EqualTo(0.1)); Assert.That(shifted.Evaluate(cursor).X, Is.EqualTo(0.05).Within(1e-9));
        }
        [TestCase(false, 0.012)] [TestCase(true, 0.08)]
        public void FallbackWidthComesFromMergedGeometry(bool flat, double width)
        {
            var route = HandRoutes.Create(Synthetic.Hand(flat), 1, false, new GameSettings());
            Assert.That(route.MinimumWidth, Is.EqualTo(width).Within(1e-9)); Assert.That(route.Length, Is.EqualTo(0.12).Within(1e-9));
        }
        [Test] public void MeasuredRadiiAndMirroredHandsWork()
        {
            foreach (HandSide side in Enum.GetValues(typeof(HandSide)))
            {
                var route = HandRoutes.Create(Synthetic.Hand(true, side), 1, false, new GameSettings { UseJointRadii = true });
                Assert.That(route, Is.Not.Null); Assert.That(route.MinimumWidth, Is.GreaterThanOrEqualTo(0.025));
            }
        }
        [Test] public void MissingJointAndPalmDownRefuseRoute()
        {
            Assert.That(HandRoutes.Create(Synthetic.Hand(invalid: 1u << 10), 1, false, new GameSettings()), Is.Null);
            var hand = Synthetic.Hand(); var inverted = hand.Joints.Select(j => j.WithPosition(new Vec3(j.Position.X, -j.Position.Y, -j.Position.Z))).ToArray();
            Assert.That(HandRoutes.Create(new HandSample(HandSide.Left, inverted), 1, false, new GameSettings()), Is.Null);
        }
        [Test] public void WidthHysteresisDoesNotPromoteNoise()
        {
            var watcher = new WidthWatcher(); Assert.That(watcher.Step(0.026), Is.EqualTo(WidthTier.Rope));
            Assert.That(watcher.Step(0.028), Is.EqualTo(WidthTier.Plank)); Assert.That(watcher.Step(0.026), Is.EqualTo(WidthTier.Plank));
            Assert.That(watcher.Step(0.0249), Is.EqualTo(WidthTier.Rope));
        }
    }
    public class WatcherTests
    {
        [Test] public void ConnectionNeedsNineTicksAndUsesBreakHysteresis()
        {
            var c = new ConnectionWatcher(); var s = new GameSettings();
            for (int i = 0; i < 8; i++) c.Step(0.019, true, s);
            Assert.That(c.Connected, Is.False); c.Step(0.019, true, s); Assert.That(c.Connected, Is.True);
            for (int i = 0; i < 100; i++) c.Step(i % 2 == 0 ? 0.021 : 0.039, true, s);
            Assert.That(c.Connected, Is.True); c.Step(0.041, true, s); Assert.That(c.Connected, Is.False);
            for (int i = 0; i < 20; i++) c.Step(0.02, true, s); Assert.That(c.Connected, Is.False);
        }
        [Test] public void StillnessRequiresFullWindowAndRejectsMotion()
        {
            var w = new StillnessWatcher(30);
            for (int i = 0; i < 30; i++) Assert.That(w.Step(Vec3.Zero, 0.03), Is.False);
            Assert.That(w.Step(Vec3.Zero, 0.03), Is.True); Assert.That(w.Step(new Vec3(0.1, 0, 0), 0.03), Is.False);
        }
        [Test] public void FilterResetDoesNotSmoothAcrossTrackingGap()
        {
            var f = new HandFilter(); var s = new GameSettings(); f.Step(Synthetic.Hand(), s); f.Step(null, s);
            var moved = f.Step(Synthetic.Hand(offset: new Vec3(0.2, 0, 0)), s);
            Assert.That(moved[JointId.Wrist].Position.X, Is.EqualTo(0.26).Within(1e-9));
        }
        [Test] public void QualityAdjustmentIsOneStepAndDoesNotChangeConnections()
        {
            var h = new HintWatcher(); var s = new GameSettings(); int prompts = 0;
            for (int i = 0; i < 5000; i++) { h.Step(false, true); if (h.QualityPromptDue) prompts++; }
            Assert.That(prompts, Is.EqualTo(1)); Assert.That(h.StillnessAdjustment, Is.EqualTo(1)); Assert.That(s.ConnectDistance, Is.EqualTo(0.02));
        }
    }
    public class BuildTests
    {
        [Test] public void WidthOnlyShapeChangePauses()
        {
            var b = new BridgeBuild(Synthetic.Line(), new GameSettings()); b.Step(Synthetic.Line(), true, true);
            b.Step(Synthetic.Line(0.012), true, true); Assert.That(b.Phase, Is.EqualTo(BuildPhase.Paused));
        }
        [Test] public void ShapeChangePausesAndFrozenSectionsNeverMove()
        {
            var s = new GameSettings { BuildTicks = 12, StillnessTicks = 2 };
            var original = new Route(new[] { Vec3.Zero, new Vec3(0.04, 0, 0), new Vec3(0.08, 0, 0), new Vec3(0.12, 0, 0) }, new[] { 0.04, 0.04, 0.04 });
            var b = new BridgeBuild(original, s); for (int i = 0; i < 4; i++) b.Step(original, true, true);
            var frozen = b.Capture().FrozenPoints; var p = original.CopyPoints(); p[3] += new Vec3(0, 0.03, 0);
            var changed = new Route(p, original.CopyWidths()); b.Step(changed, true, true);
            Assert.That(b.Phase, Is.EqualTo(BuildPhase.Paused)); Assert.That(b.Progress, Is.EqualTo(4.0 / 12));
            for (int i = 0; i < 20; i++) b.Step(changed, true, true);
            Assert.That(b.Phase, Is.EqualTo(BuildPhase.Complete));
            for (int i = 0; i < frozen.Length; i++) Assert.That(b.FrozenRoute().Points[i], Is.EqualTo(frozen[i]));
            Assert.That(b.FrozenRoute().End.Y, Is.EqualTo(0.03));
        }
        [Test] public void DisconnectedScaffoldExpiresAndRemovalIsNotInstant()
        {
            var s = new GameSettings { BuildTicks = 10, ScaffoldTicks = 4, RemovalTicks = 3 }; var route = Synthetic.Line(); var b = new BridgeBuild(route, s);
            for (int i = 0; i < 5; i++) b.Step(route, true, true);
            b.Step(route, false, true); for (int i = 0; i < 4; i++) b.Step(null, false, false);
            Assert.That(b.Phase, Is.EqualTo(BuildPhase.Removing)); Assert.That(b.FrozenSections, Is.EqualTo(1));
            for (int i = 0; i < 3; i++) b.Step(null, false, false); Assert.That(b.Phase, Is.EqualTo(BuildPhase.Removed));
        }
        [Test] public void ResumeRequiresJoiningTheFrozenEnd()
        {
            var s = new GameSettings { BuildTicks = 10, StillnessTicks = 2 }; var route = Synthetic.Line(); var b = new BridgeBuild(route, s);
            for (int i = 0; i < 5; i++) b.Step(route, true, true);
            b.Step(route, false, false);
            var away = new Route(route.Points.Select(p => p + new Vec3(0, 0, 0.03)).ToArray(), route.CopyWidths());
            for (int i = 0; i < 30; i++) b.Step(away, true, true);
            Assert.That(b.Phase, Is.EqualTo(BuildPhase.Paused)); Assert.That(b.Progress, Is.EqualTo(0.5));
        }
    }
    public class ValidationTests
    {
        private static IReadOnlyList<ValidationFailure> Check(Route route, TravelerKind kind, params Obstacle[] obstacles) => BridgeValidator.Validate(CurveTree.Single(route), route.Start, route.End, kind, new GameSettings(), new LevelGeometry(route.Start, route.End, obstacles));
        [Test] public void RopePassesWalkerButRefusesCartAndGroup()
        {
            var route = Synthetic.Line(0.012); Assert.That(Check(route, TravelerKind.Walker), Is.Empty);
            Assert.That(Check(route, TravelerKind.Cart).Any(f => f.Reason == Refusal.TooNarrow), Is.True);
            Assert.That(Check(route, TravelerKind.Group).Any(f => f.Reason == Refusal.TooNarrow), Is.True);
        }
        [Test] public void ClearanceChecksBetweenJointsAndAcrossDeckWidth()
        {
            var route = Synthetic.Line(); var rock = new Obstacle(new Vec3(-0.025, 0.04, 0.01), new Vec3(-0.015, 0.08, 0.015));
            Assert.That(Check(route, TravelerKind.Cart, rock).Any(f => f.Reason == Refusal.LowClearance), Is.True);
            Assert.That(Check(route, TravelerKind.Walker, rock), Is.Empty);
        }
        [Test] public void H1ArchClearsRockAndSlopeUsesGeometry()
        {
            var arch = new Route(new[] { new Vec3(-0.06, 0, 0), new Vec3(0, 0.02, 0), new Vec3(0.06, 0, 0) }, new[] { 0.04, 0.04 });
            var rock = new Obstacle(new Vec3(-0.005, -0.01, -0.02), new Vec3(0.005, 0.015, 0.02));
            Assert.That(Check(arch, TravelerKind.Cart, rock), Is.Empty);
            var steep = new Route(new[] { new Vec3(-0.06, 0, 0), new Vec3(0, 0.04, 0), new Vec3(0.06, 0, 0) }, new[] { 0.04, 0.04 });
            Assert.That(Check(steep, TravelerKind.Walker), Is.Empty);
            Assert.That(Check(steep, TravelerKind.Cart).Any(f => f.Reason == Refusal.TooSteep), Is.True);
        }
        [Test] public void DiagonalDeckDoesNotCollideWithRockBeyondItsEnd()
        {
            var route = new Route(new[] { new Vec3(-0.05, 0, -0.05), new Vec3(0.05, 0, 0.05) }, new[] { 0.04 });
            var beyond = new Obstacle(new Vec3(0.055, 0.01, 0.055), new Vec3(0.065, 0.04, 0.065));
            Assert.That(Check(route, TravelerKind.Cart, beyond), Is.Empty);
            var overDeck = new Obstacle(new Vec3(0.04, 0.01, 0.04), new Vec3(0.045, 0.04, 0.045));
            Assert.That(Check(route, TravelerKind.Cart, overDeck).Any(f => f.Reason == Refusal.LowClearance), Is.True);
        }
        [Test] public void SelfCrossingAndMaximumLengthRefuse()
        {
            var x = new Route(new[] { new Vec3(-0.05, 0, -0.05), new Vec3(0.05, 0, 0.05), new Vec3(-0.05, 0, 0.05), new Vec3(0.05, 0, -0.05) }, new[] { 0.04, 0.04, 0.04 });
            Assert.That(Check(x, TravelerKind.Cart).Any(f => f.Reason == Refusal.SelfCrossing), Is.True);
            Assert.That(Check(x, TravelerKind.Cart).Any(f => f.Reason == Refusal.TooLong), Is.True);
        }
        [Test] public void TreeRejectsCyclesAndUnsharedJunctions()
        { Assert.Throws<ArgumentException>(() => new CurveTree(new[] { Synthetic.Line(), Synthetic.Line() }, new[] { -1, 1 })); }
    }
    public class SimulationTests
    {
        [Test] public void ControllersHeldKeepStartupWaiting()
        {
            var game = new M1Simulation(Synthetic.Level);
            for (int i = 0; i < 100; i++) game.Step(new HandFrame(i, i * 16667, Synthetic.Hand(), null, controllersInHand: true));
            Assert.That(game.Owner, Is.Null); Assert.That(game.Pip, Is.EqualTo(Synthetic.Level.Start));
            Synthetic.Step(game, Synthetic.Hand(), count: 20); Assert.That(game.Owner, Is.EqualTo(HandSide.Left));
        }
        [Test] public void StartupPromptWaitsAndClearsWhenHandsBecomeUsable()
        {
            var game = new M1Simulation(Synthetic.Level); Synthetic.Step(game, count: 599); Assert.That(game.ShowStartupText, Is.False);
            Synthetic.Step(game); Assert.That(game.ShowStartupText, Is.True);
            Assert.That(game.Events.Any(e => e.Kind == GameEventKind.StartupPromptDue), Is.True);
            Synthetic.Step(game, Synthetic.Hand()); Assert.That(game.ShowStartupText, Is.False);
        }
        [Test] public void FinishedM1DoesNotEmitStuckHints()
        {
            var game = new M1Simulation(Synthetic.Level); Synthetic.Step(game, Synthetic.Hand(), count: 1000);
            Assert.That(game.Phase, Is.EqualTo(M1Phase.Complete));
            for (int i = 0; i < 800; i++) { Synthetic.Step(game); Assert.That(game.Events.Any(e => e.Kind == GameEventKind.HintDue), Is.False); }
        }
        [Test] public void RopeRefusesCartThenWideReplacementCompletes()
        {
            var game = new M1Simulation(Synthetic.Level); bool refused = false;
            for (int i = 0; i < 1200; i++)
            {
                Synthetic.Step(game, Synthetic.Hand(false)); refused |= game.Events.Any(e => e.Kind == GameEventKind.Refused);
                Assert.That(game.BramCheckpoint, Is.Zero);
                if (refused && game.Phase == M1Phase.Build) break;
            }
            Assert.That(refused, Is.True); Assert.That(game.Bridge.Curves[0].MinimumWidth, Is.EqualTo(0.012));
            for (int i = 0; i < 1400 && game.Phase != M1Phase.Complete; i++)
            {
                Synthetic.Step(game, Synthetic.Hand());
                if (game.Build != null && game.Build.Phase == BuildPhase.Building) Assert.That(game.Refusals, Is.Empty);
            }
            Assert.That(game.Phase, Is.EqualTo(M1Phase.Complete));
        }
        [Test] public void MidBuildSaveKeepsFrozenPrefixAndResumesAfterAlignment()
        {
            var game = new M1Simulation(Synthetic.Level);
            while ((game.Build == null || game.Build.FrozenSections < 2) && game.Tick < 1000) Synthetic.Step(game, Synthetic.Hand());
            Assert.That(game.Build, Is.Not.Null); var prefix = game.Build.Capture().FrozenPoints;
            var restored = M1Simulation.Restore(SaveCodec.Decode(SaveCodec.Encode(game.Capture())), Synthetic.Level, new GameSettings());
            Assert.That(restored.Recovery, Is.Not.Null);
            for (int i = 0; i < 1000 && restored.Phase != M1Phase.Complete; i++) Synthetic.Step(restored, Synthetic.Hand());
            Assert.That(restored.Phase, Is.EqualTo(M1Phase.Complete));
            for (int i = 0; i < prefix.Length; i++) Assert.That(restored.Bridge.Curves[0].Points[i], Is.EqualTo(prefix[i]));
        }
        [Test] public void BuilderRecoversAtBuildProgressInsteadOfReturningToRouteStart()
        {
            var game = new M1Simulation(Synthetic.Level);
            while ((game.Build == null || game.Build.Progress < 0.5) && game.Tick < 1000) Synthetic.Step(game, Synthetic.Hand());
            var position = game.Pip; Synthetic.Step(game); Synthetic.Step(game, Synthetic.Hand(), count: 30);
            Assert.That(Vec3.Distance(game.Pip, position), Is.LessThan(0.001));
            Synthetic.Step(game, Synthetic.Hand()); Assert.That(Vec3.Distance(game.Pip, position), Is.LessThan(0.001));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void LossDuringEachBuildSectionReturnsToAWithoutTeleport(int committed)
        {
            var game = new M1Simulation(Synthetic.Level);
            while ((game.Build == null || game.Build.FrozenSections < committed) && game.Tick < 1000) Synthetic.Step(game, Synthetic.Hand());
            Assert.That(game.PipCheckpoint, Is.EqualTo(1)); var position = game.Pip;
            for (int tick = 0; tick < 550; tick++)
            {
                Synthetic.Step(game, null, Synthetic.Hand(side: HandSide.Right));
                Assert.That(Vec3.Distance(position, game.Pip), Is.LessThanOrEqualTo(0.003)); position = game.Pip;
            }
            Assert.That(game.Pip, Is.EqualTo(Synthetic.Level.End)); Assert.That(game.PipCheckpoint, Is.EqualTo(1));
            Assert.That(game.Bridge, Is.Null);
        }
        [Test] public void SaveDuringReturnContinuesToLastCheckpoint()
        {
            var game = new M1Simulation(Synthetic.Level); Synthetic.Step(game, Synthetic.Hand(), count: 100); Synthetic.Step(game, count: 500);
            Assert.That(game.Recovery.Phase, Is.EqualTo(RecoveryPhase.Returning)); var position = game.Pip;
            var restored = M1Simulation.Restore(SaveCodec.Decode(SaveCodec.Encode(game.Capture())), Synthetic.Level, new GameSettings());
            Assert.That(restored.Pip, Is.EqualTo(position)); Synthetic.Step(restored, count: 70);
            Assert.That(restored.Pip, Is.EqualTo(Synthetic.Level.Start)); Assert.That(restored.Owner, Is.Null);
        }
        [Test] public void AlteredCompletedBridgeReopensBuildInsteadOfCertifyingCart()
        {
            var game = new M1Simulation(Synthetic.Level); Synthetic.Step(game, Synthetic.Hand(), count: 1000);
            var state = game.Capture(); Assert.That(state.Phase, Is.EqualTo(M1Phase.Complete));
            var p = state.Bridge.Curves[0]; state.Bridge = CurveTree.Single(new Route(p.CopyPoints(), Enumerable.Repeat(0.012, p.Widths.Count).ToArray(), p.Finger, p.Reversed));
            var restored = M1Simulation.Restore(state, Synthetic.Level, new GameSettings());
            Assert.That(restored.Phase, Is.EqualTo(M1Phase.Build)); Assert.That(restored.BramCheckpoint, Is.Zero);
        }
        [Test] public void WrongTickAndNonfiniteInputAreRejected()
        {
            var game = new M1Simulation(Synthetic.Level);
            Assert.Throws<ArgumentException>(() => game.Step(Synthetic.Frame(1)));
            Assert.Throws<ArgumentException>(() => new JointSample(new Vec3(double.NaN, 0, 0), Rotation.Identity, 0.01, JointFlags.None));
        }
        [TestCase(HandSide.Left)] [TestCase(HandSide.Right)]
        public void CompleteM1WithEitherHand(HandSide side)
        {
            var game = new M1Simulation(Synthetic.Level); var hand = Synthetic.Hand(true, side); bool built = false, crossed = false;
            for (int i = 0; i < 1200 && game.Phase != M1Phase.Complete; i++)
            {
                Synthetic.Step(game, side == HandSide.Left ? hand : null, side == HandSide.Right ? hand : null);
                built |= game.Events.Any(e => e.Kind == GameEventKind.BridgeBaked); crossed |= game.Events.Any(e => e.Kind == GameEventKind.BramFinished);
            }
            Assert.That(game.Phase, Is.EqualTo(M1Phase.Complete)); Assert.That(built && crossed, Is.True); Assert.That(game.BramCheckpoint, Is.EqualTo(1));
        }
        [Test] public void OtherHandCannotStealOwnership()
        {
            var game = new M1Simulation(Synthetic.Level); Synthetic.Step(game, Synthetic.Hand(), count: 20);
            Assert.That(game.Owner, Is.EqualTo(HandSide.Left)); Synthetic.Step(game, Synthetic.Hand(), Synthetic.Hand(side: HandSide.Right), 60);
            Assert.That(game.Owner, Is.EqualTo(HandSide.Left));
        }
        [Test] public void HysteresisDoesNotLetPipStepOffAcrossALargerGap()
        {
            var game = new M1Simulation(Synthetic.Level); Synthetic.Step(game, Synthetic.Hand(), count: 190);
            Synthetic.Step(game, Synthetic.Hand(offset: new Vec3(0.03, 0, 0)), count: 120);
            Assert.That(game.PipCheckpoint, Is.Zero); Assert.That(game.Phase, Is.EqualTo(M1Phase.Board));
            Synthetic.Step(game, Synthetic.Hand(), count: 100); Assert.That(game.PipCheckpoint, Is.EqualTo(1));
        }
        [Test] public void TrackingLossAtEveryRoutePositionNeverSkipsCheckpoint()
        {
            for (int point = 0; point < 8; point++)
            {
                var game = new M1Simulation(Synthetic.Level); Synthetic.Step(game, Synthetic.Hand(), count: 20 + point * 25);
                Assert.That(game.PipCheckpoint, Is.EqualTo(0)); Synthetic.Step(game);
                var frozen = game.Pip; var moved = Synthetic.Hand(offset: new Vec3(0.2, 0, 0));
                Synthetic.Step(game, moved, count: 100); Assert.That(game.Pip, Is.EqualTo(frozen));
                Synthetic.Step(game, moved, count: 500); Assert.That(game.PipCheckpoint, Is.EqualTo(0));
                Assert.That(game.Pip, Is.EqualTo(Synthetic.Level.Start)); Assert.That(game.Owner, Is.Null);
            }
        }
        [Test] public void RealignmentNeedsOwningHandAndBlends()
        {
            var game = new M1Simulation(Synthetic.Level); Synthetic.Step(game, Synthetic.Hand(), count: 80); Synthetic.Step(game);
            var frozen = game.Pip; Synthetic.Step(game, null, Synthetic.Hand(side: HandSide.Right), 40); Assert.That(game.Pip, Is.EqualTo(frozen));
            Synthetic.Step(game, Synthetic.Hand(), count: 17); Assert.That(game.Recovery.Phase, Is.EqualTo(RecoveryPhase.Aligning));
            Synthetic.Step(game, Synthetic.Hand(), count: 20); Assert.That(game.Recovery, Is.Null); Assert.That(game.Owner, Is.EqualTo(HandSide.Left));
        }
        [Test] public void FocusPauseCannotAdvanceGameOrSnapReturnedHand()
        {
            var game = new M1Simulation(Synthetic.Level); Synthetic.Step(game, Synthetic.Hand(), count: 60); var position = game.Pip;
            for (int i = 0; i < 300; i++) game.Step(Synthetic.Frame(game.Tick + 1, Synthetic.Hand(), focus: false));
            Assert.That(game.Pip, Is.EqualTo(position)); Synthetic.Step(game, Synthetic.Hand(offset: new Vec3(0.2, 0, 0)));
            Assert.That(game.Pip, Is.EqualTo(position));
        }
        [Test] public void SaveReloadPreservesBridgeAndCartCanFinish()
        {
            var game = new M1Simulation(Synthetic.Level); while (game.Bridge == null && game.Tick < 1000) Synthetic.Step(game, Synthetic.Hand());
            Assert.That(game.Bridge, Is.Not.Null); var data = SaveCodec.Encode(game.Capture());
            var restored = M1Simulation.Restore(SaveCodec.Decode(data), Synthetic.Level, new GameSettings());
            Synthetic.Step(restored, count: 600); Assert.That(restored.Phase, Is.EqualTo(M1Phase.Complete));
            Assert.That(SaveCodec.Encode(restored.Capture()), Is.Not.Empty);
        }
        [Test] public void SaveMidCartCrossingResumesForwardAtItsSavedCursor()
        {
            var game = new M1Simulation(Synthetic.Level);
            while ((!game.CartCrossing || game.Bram.X < -0.015) && game.Tick < 1000) Synthetic.Step(game, Synthetic.Hand());
            Assert.That(game.CartCrossing, Is.True); var before = game.Bram;
            var restored = M1Simulation.Restore(SaveCodec.Decode(SaveCodec.Encode(game.Capture())), Synthetic.Level, new GameSettings());
            Synthetic.Step(restored);
            Assert.That(restored.Bram.X, Is.GreaterThan(before.X)); Assert.That(Vec3.Distance(before, restored.Bram), Is.LessThanOrEqualTo(0.001));
        }
        [Test] public void CorruptionAndTruncationDoNotBecomeNewGame()
        {
            var bytes = SaveCodec.Encode(new M1Simulation(Synthetic.Level).Capture()); bytes[20] ^= 1;
            Assert.Throws<InvalidDataException>(() => SaveCodec.Decode(bytes)); Assert.Throws<InvalidDataException>(() => SaveCodec.Decode(bytes.Take(30).ToArray()));
        }
        [Test] public void DualSlotSaveRecoversOlderValidStateAfterInterruptedWrite()
        {
            var path = Path.Combine(Path.GetTempPath(), "palmfolk-save-" + Guid.NewGuid().ToString("N"));
            try
            {
                var storage = new DirectorySaveStorage(path); var repository = new SaveRepository(storage); var game = new M1Simulation(Synthetic.Level);
                repository.Save(game.Capture()); Synthetic.Step(game, Synthetic.Hand(), count: 20); repository.Save(game.Capture());
                File.WriteAllBytes(Path.Combine(path, "save-b.bin"), new byte[] { 1, 2 });
                Assert.That(repository.Load(out var state), Is.EqualTo(SaveLoadStatus.RecoveredBackup)); Assert.That(state.Tick, Is.EqualTo(-1));
            }
            finally { if (Directory.Exists(path)) Directory.Delete(path, true); }
        }
        [Test] public void CorruptGenerationCannotSelectAnOlderSaveAsNewer()
        {
            var path = Path.Combine(Path.GetTempPath(), "palmfolk-generation-" + Guid.NewGuid().ToString("N"));
            try
            {
                var repository = new SaveRepository(new DirectorySaveStorage(path)); var game = new M1Simulation(Synthetic.Level);
                repository.Save(game.Capture()); Synthetic.Step(game, count: 10); repository.Save(game.Capture());
                var file = Path.Combine(path, "save-a.bin"); var bytes = File.ReadAllBytes(file); bytes[0] = 127; File.WriteAllBytes(file, bytes);
                Assert.That(repository.Load(out var state), Is.EqualTo(SaveLoadStatus.RecoveredBackup)); Assert.That(state.Tick, Is.EqualTo(9));
            }
            finally { if (Directory.Exists(path)) Directory.Delete(path, true); }
        }
        [Test] public void ThirdSaveAtomicallyReplacesOlderSlotAndLoadsNewestState()
        {
            var path = Path.Combine(Path.GetTempPath(), "palmfolk-replace-" + Guid.NewGuid().ToString("N"));
            try
            {
                var repository = new SaveRepository(new DirectorySaveStorage(path)); var game = new M1Simulation(Synthetic.Level);
                repository.Save(game.Capture()); Synthetic.Step(game, count: 5); repository.Save(game.Capture());
                Synthetic.Step(game, count: 5); repository.Save(game.Capture());
                Assert.That(repository.Load(out var state), Is.EqualTo(SaveLoadStatus.Loaded)); Assert.That(state.Tick, Is.EqualTo(9));
            }
            finally { if (Directory.Exists(path)) Directory.Delete(path, true); }
        }
        [Test] public void PresentationInterpolatesBothTravelerAndRoute()
        {
            var game = new M1Simulation(Synthetic.Level); Synthetic.Step(game, Synthetic.Hand(), count: 40);
            Synthetic.Step(game, Synthetic.Hand(offset: new Vec3(0, 0.01, 0)));
            var view = new WorldPresentation(game, 0.5);
            Assert.That(view.Pip, Is.EqualTo(Vec3.Lerp(game.PreviousPip, game.Pip, 0.5)));
            Assert.That(view.FilteredHandRoute.Start, Is.EqualTo(Vec3.Lerp(game.PreviousRoute.Start, game.ActiveRoute.Start, 0.5)));
        }
        [Test] public void FixedRunnerRetainsBacklogAndMissingInputs()
        {
            var game = new M1Simulation(Synthetic.Level); var runner = new FixedTickRunner(); var source = new QueueSource();
            Assert.That(runner.Advance(1, source, game), Is.Zero);
            for (int i = 0; i < 60; i++) source.Queue.Enqueue(Synthetic.Frame(i));
            int stepped = runner.Advance(0, source, game); Assert.That(stepped, Is.EqualTo(8));
            for (int i = 0; i < 8; i++) stepped += runner.Advance(0, source, game);
            Assert.That(stepped, Is.EqualTo(60)); Assert.That(game.Tick, Is.EqualTo(59));
        }
        [Test] public void RenderRateChangesDoNotChangeSimulationDecisions()
        {
            byte[] expected = null; List<GameEventKind> expectedEvents = null;
            foreach (int hz in new[] { 72, 90, 120 })
            {
                var game = new M1Simulation(Synthetic.Level); var runner = new FixedTickRunner(); var source = new QueueSource(); var events = new List<GameEventKind>();
                for (int tick = 0; tick < 1100; tick++) source.Queue.Enqueue(Synthetic.Frame(tick, Synthetic.Hand()));
                while (game.Tick < 1099) { runner.Advance(1.0 / hz, source, game); events.AddRange(runner.Events.Select(e => e.Kind)); }
                var state = SaveCodec.Encode(game.Capture());
                if (expected == null) { expected = state; expectedEvents = events; }
                else { Assert.That(state, Is.EqualTo(expected)); Assert.That(events, Is.EqualTo(expectedEvents)); }
            }
        }
        public sealed class QueueSource : IHandFrameSource
        {
            public readonly Queue<HandFrame> Queue = new Queue<HandFrame>();
            public bool TryRead(long tick, out HandFrame frame) { frame = Queue.Count > 0 ? Queue.Dequeue() : null; return frame != null; }
        }
#if !PALMFOLK_DEVELOPMENT && !UNITY_EDITOR
        [Test] public void ReleaseAssemblyContainsNoRecordingOrStatsTypes()
        { Assert.That(System.Reflection.Assembly.Load("Palmfolk.Developer").GetTypes(), Is.Empty); }
#endif
    }
}
