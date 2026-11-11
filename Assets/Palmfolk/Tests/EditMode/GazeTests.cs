using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Palmfolk.Core;

namespace Palmfolk.Tests
{
    // D10 (GZ1 to GZ3, E9), headless.
    public class GazeTests
    {
        private static readonly Vec3 Head = new Vec3(0, 1.2, 0);
        private static GazeSample At(Vec3 point, GazeSource source = GazeSource.Head) => new GazeSample(Head, point - Head, source);
        private static GazeSample Rotated(Vec3 point, double degrees)
        {
            // Exactly `degrees` away from the point, sideways.
            var d = (point - Head).Normalized; var side = Vec3.Cross(d, Vec3.Up).Normalized; double r = degrees * Math.PI / 180;
            return new GazeSample(Head, d * Math.Cos(r) + side * Math.Sin(r), GazeSource.Head);
        }

        [Test] public void GazeSamplesAreChecked()
        {
            Assert.Throws<ArgumentException>(() => new GazeSample(Head, Vec3.Zero, GazeSource.Head));
            Assert.Throws<ArgumentException>(() => new GazeSample(Head, new Vec3(0, 0, 1), GazeSource.None));
            Assert.Throws<ArgumentException>(() => new GazeSample(new Vec3(double.NaN, 0, 0), new Vec3(0, 0, 1), GazeSource.Eyes));
            Assert.That(new GazeSample(Head, new Vec3(0, 0, 5), GazeSource.Eyes).Direction, Is.EqualTo(new Vec3(0, 0, 1)));
            Assert.That(new GazeSample(Head, new Vec3(0, 0, 1), GazeSource.Eyes).DegreesTo(Head + new Vec3(1, 0, 1)), Is.EqualTo(45).Within(1e-9));
        }

        // GZ1: about 300 ms of looking raises the sign; looking away puts it down; a glance away under 200 ms doesn't.
        [Test] public void ALookOfThreeHundredMillisecondsRaisesTheSign()
        {
            var target = new GazeTarget("t", new Vec3(0, 0.8, 0.5), 0.03, NeedSign.Rope);
            var targets = new[] { target }; var watcher = new LookWatcher();
            for (int i = 0; i < 17; i++) Assert.That(watcher.Step(1 / 60.0, At(target.Position), targets), Is.False, $"frame {i}");
            Assert.That(watcher.Shown, Is.Null);
            Assert.That(watcher.Step(1 / 60.0, At(target.Position), targets), Is.True); Assert.That(watcher.Shown.Need, Is.EqualTo(NeedSign.Rope));
            for (int i = 0; i < 11; i++) watcher.Step(1 / 60.0, null, targets);
            Assert.That(watcher.Shown, Is.Not.Null, "a glance away under 200 ms");
            watcher.Step(1 / 60.0, At(target.Position), targets);
            for (int i = 0; i < 11; i++) watcher.Step(1 / 60.0, Rotated(target.Position, 30), targets);
            Assert.That(watcher.Shown, Is.Not.Null);
            Assert.That(watcher.Step(1 / 60.0, Rotated(target.Position, 30), targets), Is.True); Assert.That(watcher.Shown, Is.Null);
            // A target that goes away (its bridge is built) puts its sign down at once.
            for (int i = 0; i < 18; i++) watcher.Step(1 / 60.0, At(target.Position), targets);
            Assert.That(watcher.Shown, Is.Not.Null);
            Assert.That(watcher.Step(1 / 60.0, At(target.Position), Array.Empty<GazeTarget>()), Is.True); Assert.That(watcher.Shown, Is.Null);
        }

        [Test] public void HeadDirectionHitsWithinThreeDegreesAndTheNearestTargetWins()
        {
            var a = new GazeTarget("a", new Vec3(0, 0.8, 0.5), 0.005, NeedSign.Plank);
            var b = new GazeTarget("b", new Vec3(0.1, 0.8, 0.5), 0.005, NeedSign.Arch);
            Assert.That(LookWatcher.Hit(Rotated(a.Position, 2.5), new[] { a }), Is.SameAs(a), "a small target still gets 3 degrees");
            Assert.That(LookWatcher.Hit(Rotated(a.Position, 3.5), new[] { a }), Is.Null);
            Assert.That(LookWatcher.Hit(At(b.Position), new[] { a, b }), Is.SameAs(b));
            // Looking from one target to the other moves the sign after another 300 ms.
            var watcher = new LookWatcher();
            for (int i = 0; i < 18; i++) watcher.Step(1 / 60.0, At(a.Position), new[] { a, b });
            Assert.That(watcher.Shown.Id, Is.EqualTo("a"));
            for (int i = 0; i < 18; i++) watcher.Step(1 / 60.0, At(b.Position), new[] { a, b });
            Assert.That(watcher.Shown.Id, Is.EqualTo("b"));
        }

        // GZ1: every waiting traveler and visitor holds up the sign of what they need.
        [Test] public void EveryPuzzleAndVisitorShowsItsNeed()
        {
            var game = new ValleySimulation(TestValley.WithVisitors()); var player = new TestPlayer();
            var seen = new Dictionary<string, NeedSign>(); var pose = new ValleyPose(new Vec3(0.3, 0.75, -0.4), new Vec3(1, 0, 1), 1.2);
            bool targeted = true;
            for (int i = 0; i < 200000 && game.VisitorsHelped != 0b11111; i++)
            {
                player.Step(game, 1, game.IsComplete && game.Visitor == null && game.Stage == DirectorStage.Complete ? InputCommand.RingBell : InputCommand.None);
                var need = NeedSigns.Need(game);
                string key = game.Visitor?.Id ?? game.Puzzle.Kind.ToString();
                if (game.Stage == DirectorStage.Interacting && !seen.ContainsKey(key)) seen[key] = need;
                if (game.Stage == DirectorStage.Interacting && need != NeedSign.None && (game.Visitor == null || game.VisitorArrival != null))
                {
                    var targets = NeedSigns.Targets(game, pose);
                    targeted &= targets.Count == 1 && targets[0].Need == need && Math.Abs(targets[0].Radius - 0.036) < 1e-12;
                }
                if (game.Stage == DirectorStage.TravelersCrossing) Assert.That(NeedSigns.Targets(game, pose), Is.Empty, "no sign while crossing");
            }
            Assert.That(game.VisitorsHelped, Is.EqualTo(0b11111));
            Assert.That(targeted, Is.True);
            Assert.That(seen, Is.EquivalentTo(new Dictionary<string, NeedSign>
            {
                ["Board"] = NeedSign.None, ["FirstBridge"] = NeedSign.Rope, ["Width"] = NeedSign.Plank, ["Fork"] = NeedSign.Branches,
                ["Arch"] = NeedSign.Arch, ["Finale"] = NeedSign.Plank,
                ["Gardener"] = NeedSign.Rope, ["Merchant"] = NeedSign.Arch, ["Twins"] = NeedSign.Branches, ["Stargazer"] = NeedSign.Rope, ["Lantern-maker"] = NeedSign.Plank,
            }));
        }

        // A flat hand at `at`, fingers along +Z; `mirror` turns it over. Closed: thumb tip on the index tip.
        private static HandSample Hand(HandSide side, Vec3 at, bool mirror, bool closed)
        {
            var flags = JointFlags.PositionValid | JointFlags.RotationValid | JointFlags.HighConfidence | JointFlags.RadiusValid;
            var joints = new JointSample[26];
            Vec3 P(double x, double y, double z) => at + new Vec3(mirror ? -x : x, y, z);
            JointSample J(Vec3 p) => new JointSample(p, Rotation.Identity, 0.006, flags);
            joints[0] = J(P(0, 0, 0.02)); joints[1] = J(P(0, 0, -0.02));
            for (int f = 0; f < 4; f++)
            {
                double x = 0.02 - f * 0.013; double[] z = { 0.01, 0.04, 0.06, 0.075, 0.085 };
                for (int b = 0; b < 5; b++) joints[6 + f * 5 + b] = J(P(x, 0, z[b]));
            }
            var tip = closed ? P(0.025, 0, 0.085) : P(0.07, 0, 0.03);
            var root = P(0.035, 0, -0.005);
            for (int b = 0; b < 4; b++) joints[2 + b] = J(root + (tip - root) * (b / 3.0));
            return new HandSample(side, joints);
        }

        private static GazeButton[] Row(double y = 1.1, double z = 0.6)
        {
            // Four buttons, 6 degrees wide with 1 degree between, as seen from the head.
            var row = new List<GazeButton>(); double distance = Math.Sqrt((y - Head.Y) * (y - Head.Y) + z * z);
            for (int i = 0; i < 4; i++)
            {
                double degrees = (i - 1.5) * 7 * Math.PI / 180;
                row.Add(new GazeButton("b" + i, Head + new Vec3(Math.Sin(degrees) * z, y - Head.Y, Math.Cos(degrees) * z), 6));
            }
            return row.ToArray();
        }

        // GZ2: look at a button and pinch with either hand resting anywhere; once per pinch; the gaps choose nothing.
        [Test] public void LookAndPinchChoosesTheButtonUnderTheGazeWithEitherHand()
        {
            var buttons = Row(); var pointer = new LookAndPinch(); var lap = new Vec3(0.2, 0.7, 0.3);
            var faceUp = Enumerable.Range(0, 2).Select(m => Hand(HandSide.Left, lap, m == 1, false)).First(h => HandRoutes.PalmNormal(h).Value.Y > 0);
            bool mirrorDown = !(HandRoutes.PalmNormal(Hand(HandSide.Left, lap, false, false)).Value.Y < 0);
            HandSample Down(HandSide side, bool closed, Vec3? where = null) => Hand(side, where ?? lap, side == HandSide.Left ? mirrorDown : !mirrorDown, closed);
            Assert.That(HandRoutes.PalmNormal(Down(HandSide.Left, false)).Value.Y, Is.LessThan(0)); Assert.That(HandRoutes.PalmNormal(Down(HandSide.Right, false)).Value.Y, Is.LessThan(0));
            Assert.That(faceUp, Is.Not.Null);

            var look = At(buttons[1].Centre);
            Assert.That(pointer.Step(look, Down(HandSide.Left, false), null, buttons), Is.Null); Assert.That(pointer.Hovered, Is.EqualTo("b1"));
            Assert.That(pointer.Step(look, Down(HandSide.Left, true), null, buttons), Is.EqualTo("b1"));
            Assert.That(pointer.Step(look, Down(HandSide.Left, true), null, buttons), Is.Null, "held: once");
            Assert.That(pointer.Step(look, Down(HandSide.Left, false), null, buttons), Is.Null);
            // The right hand, resting on the other side.
            var right = new Vec3(-0.3, 0.72, 0.25); look = At(buttons[3].Centre);
            pointer.Step(look, null, Down(HandSide.Right, false, right), buttons);
            Assert.That(pointer.Step(look, null, Down(HandSide.Right, true, right), buttons), Is.EqualTo("b3"));
            pointer.Step(look, null, Down(HandSide.Right, false, right), buttons);
            // Halfway between two buttons is the 1 degree gap: nothing.
            var gap = Rotated(buttons[1].Centre, -3.5);
            Assert.That(LookAndPinch.Hover(gap, buttons), Is.Null);
            Assert.That(pointer.Step(gap, Down(HandSide.Left, true), null, buttons), Is.Null);
            pointer.Step(gap, Down(HandSide.Left, false), null, buttons);
            // Pinching with no gaze, or looking at nothing, chooses nothing.
            Assert.That(pointer.Step(null, Down(HandSide.Left, true), null, buttons), Is.Null); Assert.That(pointer.Hovered, Is.Null);
        }

        // E9: a pinch with the palm toward the headset belongs to the system menu and is never taken.
        [Test] public void APinchWithThePalmTowardTheHeadIsTheSystemGesture()
        {
            var buttons = Row(); var pointer = new LookAndPinch(); var look = At(buttons[2].Centre); var below = new Vec3(0, 0.8, 0.3);
            bool upMirror = HandRoutes.PalmNormal(Hand(HandSide.Left, below, false, false)).Value.Y < 0;
            Assert.That(LookAndPinch.FacesHead(Hand(HandSide.Left, below, upMirror, false), Head), Is.True);
            Assert.That(LookAndPinch.FacesHead(Hand(HandSide.Left, below, !upMirror, false), Head), Is.False);
            pointer.Step(look, Hand(HandSide.Left, below, upMirror, false), null, buttons);
            Assert.That(pointer.Step(look, Hand(HandSide.Left, below, upMirror, true), null, buttons), Is.Null);
            Assert.That(pointer.SystemGesturePinches, Is.EqualTo(1));
            // Still pinched while turning over doesn't count either: open first.
            Assert.That(pointer.Step(look, Hand(HandSide.Left, below, !upMirror, true), null, buttons), Is.Null);
            pointer.Step(look, Hand(HandSide.Left, below, !upMirror, false), null, buttons);
            Assert.That(pointer.Step(look, Hand(HandSide.Left, below, !upMirror, true), null, buttons), Is.EqualTo("b2"));
        }

        // Gaze is never an input to the valley: playing with a watcher and pointer running changes nothing.
        [Test] public void GazeNeverChangesTheValley()
        {
            var plain = new ValleySimulation(TestValley.Chapter1()); var a = new TestPlayer();
            Assert.That(a.RunUntil(plain, () => plain.IsComplete), Is.True);
            var watched = new ValleySimulation(TestValley.Chapter1()); var b = new TestPlayer(); var watcher = new LookWatcher(); var pose = ValleyPose.Identity;
            Assert.That(b.RunUntil(watched, () =>
            {
                var targets = NeedSigns.Targets(watched, pose);
                watcher.Step(1 / 60.0, targets.Count > 0 ? At(targets[0].Position) : null, targets);
                return watched.IsComplete;
            }), Is.True);
            Assert.That(watched.Tick, Is.EqualTo(plain.Tick));
            Assert.That(b.Events.Select(e => (e.Tick, e.Kind)), Is.EqualTo(a.Events.Select(e => (e.Tick, e.Kind))));
        }
    }
}
