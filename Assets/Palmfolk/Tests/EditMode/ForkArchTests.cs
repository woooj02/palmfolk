using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Palmfolk.Core;
using Palmfolk.Fixtures;

namespace Palmfolk.Tests
{
    // D4: Fork and Arch geometry (T4, I1, I7, H1).
    public class ForkGeometryTests
    {
        private static readonly GameSettings Settings = new GameSettings();
        private static Vec3[] Lookouts() => new[] { "C-L1", "C-L2" }.Select(id => TestValley.Edges().First(e => e.Id == id).ToAnchor.Centre).ToArray();

        [Test] public void SpreadFingersSplitAndTogetherFingersMerge()
        {
            // Sam's fixtures: a flat hand is one surface; a spread hand is four.
            var flat = new HandSequence(HandShape.Flat, HandSize.Medium, HandSide.Left).Sample(0);
            var spread = new HandSequence(HandShape.Spread, HandSize.Medium, HandSide.Left).Sample(0);
            Assert.That(HandRoutes.Clusters(flat, Settings).Select(c => c.Length), Is.EqualTo(new[] { 4 }));
            Assert.That(HandRoutes.Clusters(spread, Settings).Select(c => c.Length), Is.EqualTo(new[] { 1, 1, 1, 1 }));
            // A flat hand can't fork: one surface for two ends.
            Assert.That(HandRoutes.BestFork(flat, flat.Joints[1].Position, new[] { Vec3.Zero, Vec3.Up }, Settings, out _), Is.Null);
        }

        [Test] public void ASpreadHandBecomesATrunkWithOneBranchPerEnd()
        {
            var source = TestValley.ForkAnchor().Centre; var ends = Lookouts();
            var hand = TestPlayer.Forked(source, ends, HandSide.Left);
            var tree = HandRoutes.BestFork(hand, source, ends, Settings, out var layout);
            Assert.That(tree, Is.Not.Null);
            Assert.That(tree.Curves.Count, Is.EqualTo(3)); Assert.That(tree.Parents, Is.EqualTo(new[] { -1, 0, 0 }));
            Assert.That(Vec3.Distance(tree.Curves[0].Start, source), Is.LessThan(1e-9));
            for (int k = 0; k < 2; k++)
            {
                Assert.That(Vec3.Distance(tree.Curves[k + 1].End, ends[k]), Is.LessThan(0.002), $"branch {k}");
                Assert.That(tree.Curves[k + 1].MinimumWidth, Is.EqualTo(0.04).Within(1e-9), "two merged fingers");
                Assert.That(layout.Branches[k].Count, Is.EqualTo(2));
            }
            Assert.That(tree.Curves[0].MinimumWidth, Is.EqualTo(0.08).Within(1e-9));
            // Either hand makes the same fork.
            var right = HandRoutes.BestFork(TestPlayer.Forked(source, ends, HandSide.Right), source, ends, Settings, out _);
            for (int k = 0; k < 2; k++) Assert.That(Vec3.Distance(right.Curves[k + 1].End, tree.Curves[k + 1].End), Is.LessThan(0.002));
            // Re-reading with the same layout gives the same shape; closing the fingers is a shape change.
            Assert.That(HandRoutes.Fork(hand, layout, Settings).Curves.Count, Is.EqualTo(3));
            var closed = new HandSequence(HandShape.Flat, HandSize.Medium, HandSide.Left).Sample(0);
            Assert.That(HandRoutes.Fork(closed, layout, Settings), Is.Null);
        }

        [Test] public void EachBranchIsJudgedOnItsOwn()
        {
            var source = TestValley.ForkAnchor().Centre; var ends = Lookouts();
            var tree = HandRoutes.BestFork(TestPlayer.Forked(source, ends, HandSide.Left, kinks: new[] { 0.06, 0 }), source, ends, Settings, out _);
            var start = new AnchorZone(source, 0.02); var clearance = new ObstacleField();
            var steep = new CurveTree(new[] { tree.Curves[0], tree.Curves[1] }, new[] { -1, 0 });
            var fine = new CurveTree(new[] { tree.Curves[0], tree.Curves[2] }, new[] { -1, 0 });
            var failures = BridgeValidator.Validate(steep, start, new[] { new AnchorZone(ends[0], 0.03) }, TravelerKind.Walker, Settings, clearance);
            Assert.That(failures.Select(f => (f.Reason, f.Curve)), Has.Member((Refusal.TooSteep, 1)));
            Assert.That(failures.Any(f => f.Curve == 0), Is.False, "the trunk is fine");
            Assert.That(BridgeValidator.Validate(fine, start, new[] { new AnchorZone(ends[1], 0.03) }, TravelerKind.Walker, Settings, clearance), Is.Empty);
            // The whole tree against both zones: only branch 1's segments fail.
            var whole = BridgeValidator.Validate(tree, start, new[] { new AnchorZone(ends[0], 0.03), new AnchorZone(ends[1], 0.03) }, TravelerKind.Walker, Settings, clearance);
            Assert.That(whole.Select(f => f.Curve).Distinct(), Is.EqualTo(new[] { 1 }));
        }

        [Test] public void TooLongCountsTheWholeWalk()
        {
            var settings = new GameSettings { MaximumRouteLength = 0.13 };
            var source = TestValley.ForkAnchor().Centre; var ends = Lookouts();
            var tree = HandRoutes.BestFork(TestPlayer.Forked(source, ends, HandSide.Left), source, ends, settings, out _);
            Assert.That(tree.Curves[2].Length, Is.LessThan(0.13)); Assert.That(tree.Curves[0].Length + tree.Curves[2].Length, Is.GreaterThan(0.13));
            var failures = BridgeValidator.Validate(tree, new AnchorZone(source, 0.02), ends.Select(e => new AnchorZone(e, 0.03)).ToArray(), TravelerKind.Walker, settings, new ObstacleField());
            Assert.That(failures.Where(f => f.Reason == Refusal.TooLong).Select(f => f.Curve), Has.Member(2));
        }
    }

    public class ForkPuzzleTests
    {
        private static (ValleySimulation, TestPlayer) ToFork(TestPlayer player)
        {
            var game = new ValleySimulation(TestValley.Chapter1());
            Assert.That(player.RunUntil(game, () => game.PuzzleIndex == 3 && game.Stage == DirectorStage.Interacting), Is.True);
            return (game, player);
        }

        [Test] public void OneSpreadHandBuildsBothLookouts()
        {
            var (game, player) = ToFork(new TestPlayer());
            Assert.That(game.Session.IsFork, Is.True); Assert.That(game.Session.Edges, Is.EqualTo(new[] { "C-L1", "C-L2" }));
            Assert.That(player.RunUntil(game, () => game.PuzzleIndex == 4), Is.True);
            Assert.That(player.Count(GameEventKind.BuildStarted), Is.EqualTo(3), "board needs none; first bridge, width and one fork build");
            var a = game.Bridges["C-L1"]; var b = game.Bridges["C-L2"];
            Assert.That(a.Curves.Count, Is.EqualTo(2)); Assert.That(a.Curves[0], Is.SameAs(b.Curves[0]), "one shared trunk");
            Assert.That(game.Passes("C-L1", TravelerKind.Walker) && game.Passes("C-L2", TravelerKind.Walker), Is.True);
        }

        [Test] public void OneSteepBranchIsRefusedAndTheOtherPasses()
        {
            // First fork attempt: the branch to L1 has a raised knuckle (too steep for a walker).
            var player = new TestPlayer { Kinks = (edges, attempt) => attempt == 0 ? edges.Select(e => e == "C-L1" ? 0.06 : 0).ToArray() : null };
            var (game, _) = ToFork(player);
            int refused = player.Count(GameEventKind.Refused);
            Assert.That(player.RunUntil(game, () => game.Bridges.ContainsKey("C-L2")), Is.True);
            Assert.That(player.Count(GameEventKind.Refused), Is.EqualTo(refused + 1));
            Assert.That(game.Bridges.ContainsKey("C-L1"), Is.False); Assert.That(game.PendingEdges, Is.EqualTo(new[] { "C-L1" }));
            Assert.That(game.RefusedFailures.Select(f => f.Reason), Has.Member(Refusal.TooSteep));
            Assert.That(game.RefusedFailures.All(f => f.Curve == 1), Is.True, "only L1's branch glows");
            Assert.That(new ValleyPresentation(game, 1).RefusedScaffold, Is.SameAs(game.RefusedScaffold));
            // The open lookout is then built on its own, and the puzzle finishes.
            Assert.That(player.RunUntil(game, () => game.Session != null && game.Session.Build != null), Is.True);
            Assert.That(game.Session.IsFork, Is.False); Assert.That(game.Session.Edge, Is.EqualTo("C-L1"));
            Assert.That(game.RefusedScaffold, Is.Null);
            Assert.That(player.RunUntil(game, () => game.PuzzleIndex == 4), Is.True);
            Assert.That(game.Passes("C-L1", TravelerKind.Walker), Is.True);
        }

        [Test] public void FingersClosingMidBuildPausesTheFork()
        {
            var (game, player) = ToFork(new TestPlayer());
            Assert.That(player.RunUntil(game, () => game.Session.Build?.Progress > 0.4), Is.True);
            var sections = game.Session.Build.FrozenSections;
            var closed = TestPlayer.Shaped(game.Session.Source.Centre, game.Session.Destination.Centre, true, 0, HandSide.Left);
            for (int i = 0; i < 20; i++) game.Step(new HandFrame(game.Tick + 1, (game.Tick + 1) * 16667, closed, null));
            Assert.That(game.Session.Recovery ?? (object)game.Session.Build, Is.Not.Null);
            Assert.That(game.Session.Build == null || game.Session.Build.FrozenSections >= sections, Is.True, "frozen sections never go");
            Assert.That(player.RunUntil(game, () => game.PuzzleIndex == 4), Is.True);
        }

        [Test] public void ForkAnchorsThatDontOverlapAreBuiltOneAtATime()
        {
            // Rim anchors facing each lookout are 2 cm apart; with 1.2 cm zones they share no ground.
            var puzzles = TestValley.Puzzles();
            var c = TestValley.C; var edges = TestValley.Edges().Where(e => e.Id != "C-L1" && e.Id != "C-L2")
                .Concat(new[] { EdgeSpec.Between("C-L1", c, TestValley.L1, 0.012, 0.03), EdgeSpec.Between("C-L2", c, TestValley.L2, 0.012, 0.03) }).ToArray();
            var valley = new ValleySpec(new[] { TestValley.S, TestValley.A, c, TestValley.Sanctuary, TestValley.L1, TestValley.L2, TestValley.E }, edges, puzzles,
                new[] { TestValley.Rock() }, new LedgeSpec("S-A", "A", 0.015, 0.06, 180), "S", "Sanctuary");
            var game = new ValleySimulation(valley); var player = new TestPlayer();
            Assert.That(player.RunUntil(game, () => game.PuzzleIndex == 3 && game.Stage == DirectorStage.Interacting), Is.True);
            Assert.That(game.Session.IsFork, Is.False);
            Assert.That(player.RunUntil(game, () => game.PuzzleIndex == 4), Is.True);
        }

        [Test] public void AForkNeedsTwoToFourEdges()
        {
            var p = TestValley.Puzzles();
            p[3] = new PuzzleSpec(PuzzleKind.Fork, new[] { "C-L1" }, TravelerKind.Walker, "C");
            Assert.Throws<ArgumentException>(() => TestValley.Chapter1(p));
        }
    }

    public class ArchTests
    {
        private static readonly EdgeSpec Edge = TestValley.Edges().First(e => e.Id == "C-E");
        private static readonly Obstacle Rock = TestValley.Rock();

        // An independent answer: is any part of the deck (sampled densely across its width) inside the rock's
        // footprint and below its top? Returns the closest call too, so near-misses can be skipped.
        private static bool Blocked(Route route, out double margin)
        {
            bool blocked = false; margin = double.PositiveInfinity;
            for (int i = 0; i < route.Widths.Count; i++)
            {
                Vec3 a = route.Points[i], b = route.Points[i + 1];
                var flat = new Vec3(b.X - a.X, 0, b.Z - a.Z).Normalized; var side = new Vec3(-flat.Z, 0, flat.X);
                for (int s = 0; s <= 400; s++)
                    for (int w = -4; w <= 4; w++)
                    {
                        var p = Vec3.Lerp(a, b, s / 400.0) + side * (route.Widths[i] / 2 * w / 4.0);
                        double inside = Math.Min(Math.Min(p.X - Rock.Min.X, Rock.Max.X - p.X), Math.Min(p.Z - Rock.Min.Z, Rock.Max.Z - p.Z));
                        if (inside < -0.001) continue;
                        double above = p.Y - Rock.Max.Y;
                        margin = Math.Min(margin, Math.Max(Math.Abs(inside), Math.Abs(above)));
                        if (inside > 0 && above < 0) blocked = true;
                    }
            }
            return blocked;
        }

        private static Route RandomDeck(Random random)
        {
            Vec3 from = Edge.FromAnchor.Centre, to = Edge.ToAnchor.Centre;
            int count = random.Next(4, 9); var points = new Vec3[count]; var widths = new double[count - 1];
            double peak = random.NextDouble() * 0.05, centre = 0.3 + random.NextDouble() * 0.4, sideways = (random.NextDouble() - 0.5) * 0.03;
            var across = new Vec3(-(to - from).Z, 0, (to - from).X).Normalized;
            for (int i = 0; i < count; i++)
            {
                double t = (double)i / (count - 1);
                double bump = peak * Math.Max(0, 1 - Math.Abs(t - centre) / 0.5) * (random.NextDouble() < 0.2 ? random.NextDouble() : 1);
                points[i] = Vec3.Lerp(from, to, t) + new Vec3(0, i == 0 || i == count - 1 ? 0 : bump, 0) + across * (sideways * Math.Sin(Math.PI * t));
                if (i > 0) widths[i - 1] = 0.012 + random.NextDouble() * 0.06;
            }
            return new Route(points, widths);
        }

        [Test] public void AnyClearingRoutePassesAndAnyBlockedOneFails()
        {
            var random = new Random(4); var settings = new GameSettings(); var clearance = new ObstacleField(Rock);
            var start = Edge.FromAnchor; var ends = new[] { Edge.ToAnchor };
            int clearing = 0, blocked = 0;
            for (int n = 0; n < 3000; n++)
            {
                var deck = RandomDeck(random);
                bool hit = Blocked(deck, out double margin);
                if (margin < 0.002) continue; // too close to call either way
                var failures = BridgeValidator.Validate(CurveTree.Single(deck), start, ends, TravelerKind.Walker, settings, clearance);
                if (hit) { blocked++; Assert.That(failures.Select(f => f.Reason), Has.Member(Refusal.LowClearance), $"deck {n} is blocked"); continue; }
                bool otherwiseFine = !failures.Any(f => f.Reason != Refusal.LowClearance);
                if (!otherwiseFine) continue;
                clearing++; Assert.That(failures, Is.Empty, $"deck {n} clears the rock");
            }
            Assert.That(clearing, Is.GreaterThan(100)); Assert.That(blocked, Is.GreaterThan(100));
        }

        [Test] public void AnyHandShapeThatClearsIsAccepted()
        {
            // I7: an arch from a curved hand and a raised flat hand both pass; it's the geometry that counts.
            var settings = new GameSettings(); var clearance = new ObstacleField(Rock);
            foreach (var hand in new[]
            {
                TestPlayer.Shaped(Edge.FromAnchor.Centre, Edge.ToAnchor.Centre, true, 0.03, HandSide.Left),
                TestPlayer.Shaped(Edge.FromAnchor.Centre, Edge.ToAnchor.Centre, false, 0.03, HandSide.Right),
            })
            {
                var route = HandRoutes.Best(hand, Edge.FromAnchor.Centre, Edge.ToAnchor.Centre, settings);
                Assert.That(BridgeValidator.Validate(CurveTree.Single(route), Edge.FromAnchor, new[] { Edge.ToAnchor }, TravelerKind.Walker, settings, clearance), Is.Empty);
            }
        }

        [Test] public void TheCapacityTableAndEndRampsAreSettings()
        {
            // H1 may loosen or tighten the table; the rock and anchor heights are valley content.
            var settings = new GameSettings(); var clearance = new ObstacleField();
            var steepEnds = new Route(new[] { new Vec3(0, 0, 0), new Vec3(0.01, 0.009, 0), new Vec3(0.11, 0.009, 0), new Vec3(0.12, 0, 0) }, new[] { 0.02, 0.02, 0.02 });
            var start = new AnchorZone(steepEnds.Start, 0.02); var ends = new[] { new AnchorZone(steepEnds.End, 0.02) };
            Assert.That(BridgeValidator.Validate(CurveTree.Single(steepEnds), start, ends, TravelerKind.Walker, settings, clearance).Select(f => f.Reason), Has.Member(Refusal.TooSteep));
            settings.EndRampLength = 0.015;
            Assert.That(BridgeValidator.Validate(CurveTree.Single(steepEnds), start, ends, TravelerKind.Walker, settings, clearance), Is.Empty);
            settings.EndRampLength = 0; settings.WalkerRule = new CapacityRule(0.01, 45, 0.035);
            Assert.That(BridgeValidator.Validate(CurveTree.Single(steepEnds), start, ends, TravelerKind.Walker, settings, clearance), Is.Empty);
            settings.WalkerRule = new CapacityRule(0.03, 45, 0.035);
            Assert.That(BridgeValidator.Validate(CurveTree.Single(steepEnds), start, ends, TravelerKind.Walker, settings, clearance).Select(f => f.Reason), Has.Member(Refusal.TooNarrow));
            Assert.Throws<ArgumentException>(() => new GameSettings { CartRule = new CapacityRule(0.025, 95, 0.05) }.Validate());
            Assert.Throws<ArgumentException>(() => new GameSettings { EndRampLength = 0.2 }.Validate());
        }
    }
}
