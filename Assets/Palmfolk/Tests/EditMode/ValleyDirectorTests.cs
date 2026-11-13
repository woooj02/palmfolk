using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Palmfolk.Core;
using Palmfolk.Fixtures;

namespace Palmfolk.Tests
{
    // Chapter 1 for tests, from Sam's candidate layout (docs/testing/valley-layout.md). The rock's bounds are a
    // test choice: H1 has not settled them.
    public static class TestValley
    {
        public static IslandSpec S = new IslandSpec("S", new Vec3(-0.27, 0.02, -0.15), 0.02);
        public static IslandSpec A = new IslandSpec("A", new Vec3(-0.11, 0.02, -0.10), 0.02);
        public static IslandSpec C = new IslandSpec("C", new Vec3(0.05, 0.02, -0.10), 0.02);
        public static IslandSpec Sanctuary = new IslandSpec("Sanctuary", new Vec3(0.21, 0.02, -0.15), 0.02);
        public static IslandSpec L1 = new IslandSpec("L1", new Vec3(0.21, 0.06, -0.10), 0.02);
        public static IslandSpec L2 = new IslandSpec("L2", new Vec3(0.14, 0.06, 0.05), 0.02);
        public static IslandSpec E = new IslandSpec("E", new Vec3(-0.10, 0.02, 0.01), 0.02);
        public const double Zone = 0.02, LookoutZone = 0.03;

        public static EdgeSpec[] Edges() => new[]
        {
            EdgeSpec.Between("S-A", A, S, Zone, Zone),
            EdgeSpec.Between("A-C", A, C, Zone, Zone),
            EdgeSpec.Between("C-Sanctuary", C, Sanctuary, Zone, Zone),
            Fork("C-L1", L1), Fork("C-L2", L2),
            EdgeSpec.Between("C-E", C, E, Zone, Zone),
        };

        // The Fork's edges share one anchor on C's rim, facing between the two lookouts, so one spread hand's wrist
        // sits in both (D4). Each lookout's anchor faces C.
        public static AnchorZone ForkAnchor()
        {
            var a = new Vec3(L1.Top.X - C.Top.X, 0, L1.Top.Z - C.Top.Z).Normalized; var b = new Vec3(L2.Top.X - C.Top.X, 0, L2.Top.Z - C.Top.Z).Normalized;
            return new AnchorZone(C.Top + (a + b).Normalized * C.Radius, Zone);
        }
        private static EdgeSpec Fork(string id, IslandSpec lookout)
        {
            var anchor = ForkAnchor();
            var toward = new Vec3(lookout.Top.X - anchor.Centre.X, 0, lookout.Top.Z - anchor.Centre.Z).Normalized;
            return new EdgeSpec(id, "C", lookout.Id, anchor, new AnchorZone(lookout.Top - toward * lookout.Radius, LookoutZone));
        }

        public static Obstacle Rock()
        {
            var edge = Edges().First(e => e.Id == "C-E");
            var middle = Vec3.Lerp(edge.FromAnchor.Centre, edge.ToAnchor.Centre, 0.5);
            return new Obstacle(new Vec3(middle.X - 0.008, 0, middle.Z - 0.008), new Vec3(middle.X + 0.008, 0.035, middle.Z + 0.008));
        }

        public static PuzzleSpec[] Puzzles() => new[]
        {
            new PuzzleSpec(PuzzleKind.Board, new[] { "S-A" }, TravelerKind.Walker, "S"),
            new PuzzleSpec(PuzzleKind.FirstBridge, new[] { "S-A" }, TravelerKind.Walker, "S"),
            new PuzzleSpec(PuzzleKind.Width, new[] { "A-C" }, TravelerKind.Group, "A", TravelerKind.Cart),
            new PuzzleSpec(PuzzleKind.Fork, new[] { "C-L1", "C-L2" }, TravelerKind.Walker, "C"),
            new PuzzleSpec(PuzzleKind.Arch, new[] { "C-E" }, TravelerKind.Walker, "C"),
            new PuzzleSpec(PuzzleKind.Finale, new[] { "S-A", "A-C", "C-Sanctuary" }, TravelerKind.Cart, "S"),
        };

        // D8: Chapter 1 with Sam's visitor islands (G, G1, G2) and edges from his layout, and the five starting
        // visitor designs (R2): Gardener, Merchant, Twins, Stargazer, Lantern-maker.
        public static IslandSpec G = new IslandSpec("G", new Vec3(-0.25, 0.02, 0.03), 0.025);
        public static IslandSpec G1 = new IslandSpec("G1", new Vec3(-0.105, 0.02, 0.15), 0.02);
        public static IslandSpec G2 = new IslandSpec("G2", new Vec3(-0.07, 0.02, 0.07), 0.02);
        public static VisitorSpec[] Visitors() => new[]
        {
            new VisitorSpec("Gardener", TravelerKind.Walker, "A", "A-G"),
            new VisitorSpec("Merchant", TravelerKind.Cart, "C", "C-E"),
            new VisitorSpec("Twins", TravelerKind.Walker, "G", "G-G1", "G-G2"),
            new VisitorSpec("Stargazer", TravelerKind.Walker, "L1", "L1-L2"),
            new VisitorSpec("Lantern-maker", TravelerKind.Cart, "A", "A-G"),
        };
        public static ValleySpec WithVisitors(VisitorSpec[] visitors = null)
        {
            var garden = new Vec3(G1.Top.X - G.Top.X, 0, G1.Top.Z - G.Top.Z).Normalized + new Vec3(G2.Top.X - G.Top.X, 0, G2.Top.Z - G.Top.Z).Normalized;
            var gardenSource = new AnchorZone(G.Top + new Vec3(garden.X, 0, garden.Z).Normalized * G.Radius, Zone);
            EdgeSpec Garden(string id, IslandSpec plot)
            { var rim = EdgeSpec.Between(id, G, plot, Zone, LookoutZone); return new EdgeSpec(id, "G", plot.Id, gardenSource, rim.ToAnchor); }
            var edges = Edges().Concat(new[] { EdgeSpec.Between("A-G", A, G, Zone, Zone), Garden("G-G1", G1), Garden("G-G2", G2), EdgeSpec.Between("L1-L2", L1, L2, Zone, Zone) }).ToArray();
            return new ValleySpec(new[] { S, A, C, Sanctuary, L1, L2, E, G, G1, G2 }, edges, Puzzles(), new[] { Rock() },
                new LedgeSpec("S-A", "A", 0.015, 0.06, 180), "S", "Sanctuary", visitors ?? Visitors());
        }

        public static ValleySpec Chapter1(PuzzleSpec[] puzzles = null, LedgeSpec ledge = null, string sanctuary = "Sanctuary") =>
            new ValleySpec(new[] { S, A, C, Sanctuary, L1, L2, E }, Edges(), puzzles ?? Puzzles(), new[] { Rock() },
                ledge ?? new LedgeSpec("S-A", "A", 0.015, 0.06, 180), "S", sanctuary);
    }

    // A headless player: holds a hand across whatever gap the director is asking for, and withdraws otherwise.
    public sealed class TestPlayer
    {
        public enum Shape { Plank, Rope, Straight, Arch, CartArch }
        // Which shape to use for an edge, by puzzle; attempt counts refusals on that edge so far.
        public Func<PuzzleKind, string, int, Shape> Plan = DefaultPlan;
        public HandSide Side = HandSide.Left;
        public double Tremor = 0.0003;
        public readonly Dictionary<string, int> Attempts = new Dictionary<string, int>();
        public readonly List<GameEvent> Events = new List<GameEvent>();

        // For visitors (D8): which shape to use, by visitor, edge and attempt.
        public Func<VisitorSpec, string, int, Shape> VisitorPlan = DefaultVisitorPlan;
        public static Shape DefaultVisitorPlan(VisitorSpec visitor, string edge, int attempt) =>
            visitor.Traveler == TravelerKind.Cart && edge == "C-E" ? Shape.CartArch : visitor.Traveler == TravelerKind.Walker ? Shape.Rope : Shape.Plank;
        // A deck that clears the rock within the cart's 25 degree slope: heights at fifths of the way across.
        public static readonly double[] CartArchProfile = { 0, 0.0125, 0.025, 0.025, 0.0125, 0 };

        public static Shape DefaultPlan(PuzzleKind puzzle, string edge, int attempt) =>
            puzzle == PuzzleKind.FirstBridge ? Shape.Rope : puzzle == PuzzleKind.Arch ? Shape.Arch : puzzle == PuzzleKind.Fork ? Shape.Straight : Shape.Plank;

        // For a fork: extra height at the knuckle of each branch (by the session's edges), to make a branch steep.
        public Func<string[], int, double[]> Kinks;

        // A fork hand: wrist on `source`, index and middle together toward one end, ring and little together toward
        // the other (two surfaces, each two fingers wide). The end further along the little-finger side gets ring
        // and little. kinks[k] raises branch k's knuckle.
        public static HandSample Forked(Vec3 source, Vec3[] ends, HandSide side, long tick = 0, double tremor = 0, double[] kinks = null)
        {
            if (ends.Length != 2) throw new ArgumentException("Two ends.");
            double sign = side == HandSide.Left ? 1 : -1;
            var middle = (ends[0] + ends[1]) * 0.5;
            var d = new Vec3(middle.X - source.X, 0, middle.Z - source.Z).Normalized;
            var lateral = Vec3.Cross(Vec3.Up, d).Normalized * sign;
            int indexEnd = Vec3.Dot(ends[0] - source, lateral) <= Vec3.Dot(ends[1] - source, lateral) ? 0 : 1;
            var jitter = Planted.Jitter(tick, 99) * tremor; var down = new Vec3(0, -0.006, 0);
            var flags = JointFlags.PositionValid | JointFlags.RotationValid | JointFlags.HighConfidence | JointFlags.RadiusValid;
            var joints = new JointSample[26];
            JointSample J(Vec3 p, double r = 0.006) => new JointSample(p + down + jitter, Rotation.Identity, r, flags);
            var wrist = source; var metacarpals = new Vec3[4];
            for (int f = 0; f < 4; f++) metacarpals[f] = wrist + d * 0.03 + lateral * ((f - 1.5) * 0.012);
            for (int g = 0; g < 2; g++)
            {
                int end = g == 0 ? indexEnd : 1 - indexEnd;
                var basePoint = (metacarpals[2 * g] + metacarpals[2 * g + 1]) * 0.5;
                var target = ends[end]; var along = target - basePoint;
                var across = Vec3.Cross(Vec3.Up, new Vec3(along.X, 0, along.Z)).Normalized * sign;
                double kink = kinks == null ? 0 : kinks[end];
                for (int k = 0; k < 2; k++)
                {
                    int f = 2 * g + k; var offset = across * (k == 0 ? -0.009 : 0.009);
                    double[] t = { 1, 0.75, 0.5, 0.25 }; // tip, distal, intermediate, proximal
                    for (int bone = 0; bone < 4; bone++)
                        joints[6 + f * 5 + (4 - bone)] = J(basePoint + along * t[bone] + offset * (t[bone] < 1 ? 1 : 0.5) + new Vec3(0, bone == 3 ? kink : 0, 0));
                    joints[6 + f * 5] = J(metacarpals[f]);
                }
            }
            joints[1] = J(wrist, 0.03); joints[0] = J(wrist + d * 0.02, 0.03);
            for (int bone = 0; bone < 4; bone++) joints[2 + bone] = J(wrist - lateral * 0.05 + d * (0.01 * bone));
            return new HandSample(side, joints);
        }

        public HandSample Hand(ValleySimulation game, long tick)
        {
            var session = game.Session;
            if (game.Stage != DirectorStage.Interacting || session == null) return null;
            Attempts.TryGetValue(session.Edge, out int attempt);
            var shape = game.Visitor != null ? VisitorPlan(game.Visitor, session.Edge, attempt) : Plan(game.Puzzle.Kind, session.Edge, attempt);
            if (session.IsFork) return Forked(session.Source.Centre, session.Destinations.Select(d => d.Centre).ToArray(), Side, tick, Tremor, Kinks?.Invoke(session.Edges.ToArray(), attempt));
            var from = session.Source.Centre; var to = session.Destination.Centre;
            bool level = Math.Abs(from.Y - to.Y) < 1e-9;
            // Level gaps use Sam's S1 fixtures; sloped and arched decks need a shaped hand (see Shaped).
            if (level && (shape == Shape.Plank || shape == Shape.Rope)) return Fixture(from, to, shape == Shape.Plank ? HandShape.Flat : HandShape.Spread, tick);
            if (shape == Shape.CartArch) return Shaped(from, to, true, 0, Side, tick, Tremor, CartArchProfile);
            return Shaped(from, to, shape == Shape.Plank, shape == Shape.Arch ? 0.03 : 0, Side, tick, Tremor);
        }

        // Sam's fixture hand, turned and placed so the index route runs from `from` toward `to`.
        public HandSample Fixture(Vec3 from, Vec3 to, HandShape shape, long tick)
        {
            double gap = new Vec3(to.X - from.X, 0, to.Z - from.Z).Length;
            var size = gap > 0.135 ? HandSize.Large : HandSize.Medium; double scale = size == HandSize.Large ? 1.2 : 1;
            double yaw = Math.Atan2(-(to.Z - from.Z), to.X - from.X);
            var tip = new Vec3(-0.06 * Math.Cos(yaw), 0, 0.06 * Math.Sin(yaw)) * scale;
            var origin = from - tip - new Vec3(0, 0.006, 0);
            return new HandSequence(shape, size, Side, seed: 7, tremorMetres: Tremor, origin: origin, yawDegrees: yaw * 180 / Math.PI, ticks: 36000).Sample(tick % 36000);
        }

        // A hand whose index chain lies on the line (or arch) from `from` (fingertip) to `to` (wrist).
        public static HandSample Shaped(Vec3 from, Vec3 to, bool flat, double arch, HandSide side, long tick = 0, double tremor = 0, double[] profile = null)
        {
            var d = from - to; var lateral = Vec3.Cross(Vec3.Up, d).Normalized * (side == HandSide.Left ? 1 : -1);
            double spacing = flat ? 0.018 : 0.08;
            var jitter = Planted.Jitter(tick, 99) * tremor;
            Vec3 At(double t) => Vec3.Lerp(from, to, t) + new Vec3(0, (profile == null ? arch * Math.Sin(Math.PI * t) : profile[(int)Math.Round(t * 5)]) - 0.006, 0) + jitter;
            var joints = new JointSample[26];
            var flags = JointFlags.PositionValid | JointFlags.RotationValid | JointFlags.HighConfidence | JointFlags.RadiusValid;
            double[] t = { 0.8, 0.6, 0.4, 0.2, 0 }; // metacarpal, proximal, intermediate, distal, tip
            for (int finger = 0; finger < 4; finger++)
                for (int bone = 0; bone < 5; bone++)
                    joints[6 + finger * 5 + bone] = new JointSample(At(t[bone]) + lateral * (finger * spacing), Rotation.Identity, 0.006, flags);
            joints[1] = new JointSample(At(1), Rotation.Identity, 0.03, flags);
            joints[0] = new JointSample(Vec3.Lerp(At(1), At(0.8) + lateral * 0.027, 0.5), Rotation.Identity, 0.03, flags);
            for (int bone = 0; bone < 4; bone++) joints[2 + bone] = new JointSample(At(1) - lateral * 0.05 + (d.Normalized * (0.01 * bone)), Rotation.Identity, 0.006, flags);
            return new HandSample(side, joints);
        }

        public void Step(ValleySimulation game, int ticks = 1, InputCommand command = InputCommand.None)
        {
            for (int i = 0; i < ticks; i++)
            {
                long tick = game.Tick + 1; var hand = Hand(game, tick);
                game.Step(new HandFrame(tick, tick * 16667, Side == HandSide.Left ? hand : null, Side == HandSide.Right ? hand : null, true, command));
                foreach (var e in game.Events)
                {
                    Events.Add(e);
                    if (e.Kind == GameEventKind.Refused && game.Session != null) { Attempts.TryGetValue(game.Session.Edge, out int a); Attempts[game.Session.Edge] = a + 1; }
                }
            }
        }
        public bool RunUntil(ValleySimulation game, Func<bool> done, int limit = 30000)
        {
            for (int i = 0; i < limit && !done(); i++) Step(game);
            return done();
        }
        public int Count(GameEventKind kind) => Events.Count(e => e.Kind == kind);
    }

    public class ValleySpecTests
    {
        [Test] public void Chapter1IsValidAndBramsRouteHasThreeCrossings()
        {
            var valley = TestValley.Chapter1();
            var route = valley.FinaleRoute();
            Assert.That(route.Select(r => (r.From, r.To)), Is.EqualTo(new[] { ("S", "A"), ("A", "C"), ("C", "Sanctuary") }));
            foreach (var edge in valley.Edges)
            {
                double gap = Vec3.Distance(new Vec3(edge.FromAnchor.Centre.X, 0, edge.FromAnchor.Centre.Z), new Vec3(edge.ToAnchor.Centre.X, 0, edge.ToAnchor.Centre.Z));
                Assert.That(gap, Is.InRange(0.12, 0.15), edge.Id); // V5 active gaps
            }
        }

        [Test] public void BrokenValleysAreRefused()
        {
            var p = TestValley.Puzzles();
            Assert.Throws<ArgumentException>(() => TestValley.Chapter1(new[] { p[1], p[0], p[2], p[3], p[4], p[5] }), "order");
            Assert.Throws<ArgumentException>(() => TestValley.Chapter1(p.Take(5).ToArray()), "six puzzles");
            var badFinale = (PuzzleSpec[])p.Clone(); badFinale[5] = new PuzzleSpec(PuzzleKind.Finale, new[] { "S-A", "C-Sanctuary" }, TravelerKind.Cart, "S");
            Assert.Throws<ArgumentException>(() => TestValley.Chapter1(badFinale), "finale must be a path");
            var walkerFinale = (PuzzleSpec[])p.Clone(); walkerFinale[5] = new PuzzleSpec(PuzzleKind.Finale, new[] { "S-A", "A-C", "C-Sanctuary" }, TravelerKind.Walker, "S");
            Assert.Throws<ArgumentException>(() => TestValley.Chapter1(walkerFinale), "finale is the cart");
            Assert.Throws<ArgumentException>(() => TestValley.Chapter1(ledge: new LedgeSpec("A-C", "A", 0.015, 0.06, 60)), "ledge must be First Bridge's edge");
            Assert.Throws<ArgumentException>(() => TestValley.Chapter1(sanctuary: "Nowhere"));
            Assert.Throws<ArgumentException>(() => new AnchorZone(Vec3.Zero, 0));
            Assert.Throws<ArgumentException>(() => new LedgeSpec("S-A", "A", 0.06, 0.015, 60));
        }

        [Test] public void ZoneAnchorsMatchPointAnchorsAtTheConnectDistance()
        {
            var settings = new GameSettings(); var route = Synthetic.Line();
            foreach (var shift in new[] { 0.0, 0.019, 0.021, 0.05 })
            {
                var start = route.Start + new Vec3(0, 0, shift);
                var points = BridgeValidator.Validate(CurveTree.Single(route), start, route.End, TravelerKind.Walker, settings, new ObstacleField());
                var zones = BridgeValidator.Validate(CurveTree.Single(route), new AnchorZone(start, settings.ConnectDistance), new[] { new AnchorZone(route.End, settings.ConnectDistance) }, TravelerKind.Walker, settings, new ObstacleField());
                Assert.That(zones.Select(f => (f.Reason, f.Curve, f.Segment)), Is.EqualTo(points.Select(f => (f.Reason, f.Curve, f.Segment))), $"shift {shift}");
            }
            var wide = new AnchorZone(route.Start + new Vec3(0.025, 0, 0), 0.03);
            Assert.That(wide.Contains(route.Start), Is.True);
            Assert.That(wide.ConnectionDistance(route.Start, settings), Is.EqualTo(0.015).Within(1e-12));
            Assert.That(wide.ConnectionDistance(wide.Centre, settings), Is.EqualTo(0));
        }

        [Test] public void EveryEndZoneMustBeReached()
        {
            var settings = new GameSettings(); var route = Synthetic.Line();
            var ends = new[] { new AnchorZone(route.End, 0.02), new AnchorZone(route.End + new Vec3(0, 0, 0.1), 0.02) };
            var failures = BridgeValidator.Validate(CurveTree.Single(route), new AnchorZone(route.Start, 0.02), ends, TravelerKind.Walker, settings, new ObstacleField());
            Assert.That(failures.Select(f => f.Reason), Is.EqualTo(new[] { Refusal.Unanchored }));
        }

        [Test] public void LedgeCapsTheDeck()
        {
            var route = Synthetic.Line(0.08);
            Assert.That(EdgeSession.Cap(route, 0.015).Widths, Is.All.EqualTo(0.015));
            Assert.That(EdgeSession.Cap(route, double.PositiveInfinity), Is.SameAs(route));
            Assert.That(EdgeSession.Cap(Synthetic.Line(0.012), 0.015).Widths, Is.All.EqualTo(0.012));
        }

        [Test] public void ShapedTestHandsMakeTheRoutesTheyClaim()
        {
            var edge = TestValley.Edges().First(e => e.Id == "C-E"); var settings = new GameSettings();
            var straight = HandRoutes.Best(TestPlayer.Shaped(edge.FromAnchor.Centre, edge.ToAnchor.Centre, true, 0, HandSide.Left), edge.FromAnchor.Centre, edge.ToAnchor.Centre, settings);
            var arch = HandRoutes.Best(TestPlayer.Shaped(edge.FromAnchor.Centre, edge.ToAnchor.Centre, true, 0.03, HandSide.Left), edge.FromAnchor.Centre, edge.ToAnchor.Centre, settings);
            var rope = HandRoutes.Best(TestPlayer.Shaped(edge.FromAnchor.Centre, edge.ToAnchor.Centre, false, 0, HandSide.Right), edge.FromAnchor.Centre, edge.ToAnchor.Centre, settings);
            Assert.That(straight.MinimumWidth, Is.EqualTo(0.08).Within(1e-9)); Assert.That(rope.MinimumWidth, Is.EqualTo(0.012).Within(1e-9));
            Assert.That(Vec3.Distance(straight.Start, edge.FromAnchor.Centre), Is.LessThan(0.007));
            var clearance = new ObstacleField(TestValley.Rock());
            var start = new AnchorZone(edge.FromAnchor.Centre, 0.02); var ends = new[] { new AnchorZone(edge.ToAnchor.Centre, 0.02) };
            Assert.That(BridgeValidator.Validate(CurveTree.Single(straight), start, ends, TravelerKind.Walker, settings, clearance).Select(f => f.Reason), Has.Member(Refusal.LowClearance));
            Assert.That(BridgeValidator.Validate(CurveTree.Single(arch), start, ends, TravelerKind.Walker, settings, clearance), Is.Empty);
        }
    }

    public class ValleyDirectorTests
    {
        private static (ValleySimulation Game, TestPlayer Player) Play(int puzzles, TestPlayer player = null, ValleyProgress resume = null)
        {
            var game = new ValleySimulation(TestValley.Chapter1(), null, resume); player = player ?? new TestPlayer();
            Assert.That(player.RunUntil(game, () => game.Progress.CompletedPuzzles >= puzzles), Is.True,
                $"stuck in {game.Puzzle.Kind} at {game.Stage}, tick {game.Tick}, pending {string.Join(",", game.PendingEdges)}");
            return (game, player);
        }

        [Test] public void BoardToFinaleIsPlayableHeadless()
        {
            var (game, player) = Play(6);
            Assert.That(game.IsComplete, Is.True); Assert.That(game.Stage, Is.EqualTo(DirectorStage.Complete));
            Assert.That(player.Count(GameEventKind.PuzzleStarted), Is.EqualTo(6)); Assert.That(player.Count(GameEventKind.PuzzleCompleted), Is.EqualTo(6));
            // V1: Bram crosses exactly three bridges, on his route.
            Assert.That(player.Count(GameEventKind.CrossingCompleted), Is.EqualTo(3)); Assert.That(game.Bram.Crossed, Is.EqualTo(3));
            Assert.That(Vec3.Distance(game.BramPosition, TestValley.Sanctuary.Top), Is.LessThan(1e-9));
            // V2: the ledge was widened and the rope rebuilt.
            Assert.That(player.Count(GameEventKind.LedgeWidened), Is.EqualTo(1)); Assert.That(player.Count(GameEventKind.RebuildNeeded), Is.EqualTo(2));
            foreach (var id in new[] { "S-A", "A-C", "C-Sanctuary" }) Assert.That(game.Passes(id, TravelerKind.Cart), Is.True, id);
            foreach (var id in new[] { "C-L1", "C-L2", "C-E" }) Assert.That(game.Passes(id, TravelerKind.Walker), Is.True, id);
            Assert.That(player.Count(GameEventKind.TravelerArrived), Is.EqualTo(5)); // apprentice, group, two at the Fork, Arch
        }

        [Test] public void SkippingBoardOpensOnTheFirstBridgeAndStillReachesTheFinale()
        {
            var game = new ValleySimulation(TestValley.Chapter1(), new GameSettings { SkipBoard = true }); var player = new TestPlayer();
            Assert.That(game.Puzzle.Kind, Is.EqualTo(PuzzleKind.FirstBridge)); Assert.That(game.PipIsland, Is.EqualTo("A"));
            Assert.That(player.RunUntil(game, () => game.IsComplete), Is.True, $"stuck in {game.Puzzle.Kind} at {game.Stage}");
            Assert.That(player.Count(GameEventKind.PuzzleCompleted), Is.EqualTo(5));
        }

        // Quest 2 finding, Nov 13: a hand that slipped right after connecting sat through the ghost-hand recovery
        // and a slow return to the checkpoint. With QuickRelease it is let go at once, and Pip walks back.
        [Test] public void AHandThatSlipsBeforeBuildingIsLetGoAtOnce()
        {
            foreach (bool quick in new[] { false, true })
            {
                var game = new ValleySimulation(TestValley.Chapter1(), new GameSettings { SkipBoard = true, QuickRelease = quick }); var player = new TestPlayer();
                Assert.That(player.RunUntil(game, () => game.Session != null && game.Session.Owner.HasValue, 2000), Is.True);
                int before = player.Events.Count;
                for (int i = 0; i < 120; i++)
                {
                    long tick = game.Tick + 1; var hand = player.Hand(game, tick);
                    // The same hand turned palm up: every joint is tracked, but it no longer makes a deck.
                    // Turned half a revolution about the forearm's line through the wrist (a rotation, not a mirror).
                    var w = hand.Joints[1].Position;
                    var flipped = new HandSample(hand.Side, hand.Joints.Select(j => j.WithPosition(new Vec3(j.Position.X, 2 * w.Y - j.Position.Y, 2 * w.Z - j.Position.Z))).ToArray());
                    var previous = game.Pip;
                    game.Step(new HandFrame(tick, tick * 16667, flipped, null, true, InputCommand.None)); player.Events.AddRange(game.Events);
                    Assert.That(Vec3.Distance(game.Pip, previous), Is.LessThan(0.005), "Pip never jumps");
                }
                var after = player.Events.Skip(before).Select(e => e.Kind).ToList();
                if (quick)
                {
                    Assert.That(after, Does.Contain(GameEventKind.HandReleased));
                    Assert.That(after, Does.Not.Contain(GameEventKind.ReturningToCheckpoint));
                    Assert.That(game.Session.Recovery, Is.Null);
                }
                else Assert.That(game.Session.Recovery != null || after.Contains(GameEventKind.ReturningToCheckpoint), Is.True);
            }
        }

        [Test] public void SkippingBoardMovesAnUnstartedSaveOnButLeavesProgressAlone()
        {
            var fresh = new ValleySimulation(TestValley.Chapter1(), new GameSettings { SkipBoard = true }, new ValleyProgress { PipIsland = "S" });
            Assert.That(fresh.Progress.CompletedPuzzles, Is.EqualTo(1)); Assert.That(fresh.PipIsland, Is.EqualTo("A"));
            var later = new ValleySimulation(TestValley.Chapter1(), new GameSettings { SkipBoard = true }, new ValleyProgress { CompletedPuzzles = 3, PipIsland = "C" });
            Assert.That(later.Progress.CompletedPuzzles, Is.EqualTo(3)); Assert.That(later.PipIsland, Is.EqualTo("C"));
        }

        [Test] public void FirstBridgeIsNarrowWhateverTheHand()
        {
            var player = new TestPlayer { Plan = (p, e, a) => TestPlayer.Shape.Plank };
            var (game, _) = Play(2, player);
            var bridge = game.Bridges["S-A"];
            Assert.That(bridge.Curves[0].MinimumWidth, Is.EqualTo(0.015).Within(1e-12));
            Assert.That(game.Passes("S-A", TravelerKind.Walker), Is.True); Assert.That(game.Passes("S-A", TravelerKind.Cart), Is.False);
            Assert.That(game.LedgeWidth, Is.EqualTo(0.015));
        }

        [Test] public void RopeIsRefusedForWidthAndAPlankPasses()
        {
            var player = new TestPlayer { Plan = (p, e, a) => p == PuzzleKind.Width && a == 0 ? TestPlayer.Shape.Rope : TestPlayer.DefaultPlan(p, e, a) };
            var (game, _) = Play(3, player);
            Assert.That(player.Attempts["A-C"], Is.EqualTo(1));
            Assert.That(game.Bridges["A-C"].Curves[0].MinimumWidth, Is.GreaterThanOrEqualTo(0.025));
        }

        [Test] public void ArchRefusesAStraightDeckOverTheRockAndAcceptsAnArch()
        {
            var player = new TestPlayer { Plan = (p, e, a) => p == PuzzleKind.Arch && a == 0 ? TestPlayer.Shape.Straight : TestPlayer.DefaultPlan(p, e, a) };
            var (game, _) = Play(5, player);
            Assert.That(player.Attempts["C-E"], Is.EqualTo(1));
            var top = game.Bridges["C-E"].Curves[0].Points.Max(p => p.Y);
            Assert.That(top, Is.GreaterThan(TestValley.Rock().Max.Y));
        }

        [Test] public void OldBridgeStaysUntilTheReplacementPasses()
        {
            // In the finale the first rebuild attempt of S-A is a rope again: refused for the cart; the old rope must stay.
            var player = new TestPlayer { Plan = (p, e, a) => p == PuzzleKind.Finale && e == "S-A" && a == 0 ? TestPlayer.Shape.Rope : TestPlayer.DefaultPlan(p, e, a) };
            var (game, _) = Play(5, player);
            var rope = game.Bridges["S-A"];
            Assert.That(player.RunUntil(game, () => player.Attempts.TryGetValue("S-A", out int a) && a == 1), Is.True);
            Assert.That(game.Bridges["S-A"], Is.SameAs(rope)); Assert.That(game.FailingEdges, Has.Member("S-A"));
            Assert.That(player.RunUntil(game, () => game.IsComplete), Is.True);
            Assert.That(game.Bridges["S-A"], Is.Not.SameAs(rope));
        }

        public static IEnumerable<TestCaseData> Corruptions()
        {
            foreach (var edge in new[] { "S-A", "A-C", "C-L1", "C-L2", "C-E" })
                foreach (var kind in new[] { "missing", "narrow", "steep", "moved" })
                    yield return new TestCaseData(edge, kind).SetName($"V3 {edge} {kind}");
        }

        private static ValleyProgress beforeFinale;
        private static ValleyProgress BeforeFinale() => (beforeFinale ?? (beforeFinale = Play(5).Game.Progress)).Copy();

        [TestCaseSource(nameof(Corruptions))]
        public void FinaleStaysCompletableWhateverHappenedToABridge(string edge, string kind)
        {
            var progress = BeforeFinale(); Assert.That(progress.CompletedPuzzles, Is.EqualTo(5));
            var tree = progress.Bridges[edge];
            // Every curve of the tree changes the same way; "steep" lifts the leaf's far end.
            CurveTree Map(Func<Route, bool, Route> change) => new CurveTree(tree.Curves.Select((r, i) => change(r, i == tree.Curves.Count - 1)).ToArray(), tree.Parents.ToArray());
            switch (kind)
            {
                case "missing": progress.Bridges.Remove(edge); break;
                case "narrow": progress.Bridges[edge] = Map((r, _) => new Route(r.CopyPoints(), r.CopyWidths().Select(w => w * 0.3).ToArray(), r.Finger, r.Reversed)); break;
                case "steep":
                    progress.Bridges[edge] = Map((r, leaf) =>
                    {
                        if (!leaf) return r;
                        var points = r.CopyPoints(); points[points.Length - 1] += new Vec3(0, 0.1, 0);
                        return new Route(points, r.CopyWidths(), r.Finger, r.Reversed);
                    }); break;
                case "moved": progress.Bridges[edge] = Map((r, _) => new Route(r.CopyPoints().Select(p => p + new Vec3(0.05, 0, 0)).ToArray(), r.CopyWidths(), r.Finger, r.Reversed)); break;
            }
            var (game, player) = Play(6, resume: progress);
            Assert.That(game.IsComplete, Is.True);
            Assert.That(player.Count(GameEventKind.CrossingCompleted), Is.EqualTo(3));
            foreach (var id in new[] { "S-A", "A-C", "C-Sanctuary" }) Assert.That(game.Passes(id, TravelerKind.Cart), Is.True, id);
            bool onRoute = edge == "S-A" || edge == "A-C";
            Assert.That(player.Count(GameEventKind.RebuildNeeded), Is.EqualTo(onRoute && edge == "A-C" ? 3 : 2));
            // Side routes are left as they were: the cart never needs them.
            if (!onRoute && kind != "missing") Assert.That(game.Bridges[edge], Is.SameAs(progress.Bridges[edge]));
        }

        [Test] public void TrackingLossMidBuildRecoversAndFinishes()
        {
            var player = new TestPlayer(); var game = new ValleySimulation(TestValley.Chapter1());
            Assert.That(player.RunUntil(game, () => game.PuzzleIndex == 2 && game.Session?.Build?.Progress > 0.3), Is.True);
            for (int i = 0; i < 40; i++) game.Step(new HandFrame(game.Tick + 1, (game.Tick + 1) * 16667, null, null));
            Assert.That(game.Session.Recovery, Is.Not.Null);
            Assert.That(player.RunUntil(game, () => game.IsComplete), Is.True);
            Assert.That(player.Count(GameEventKind.RecoveryCompleted), Is.GreaterThanOrEqualTo(1));
        }

        [Test] public void PauseFreezesTheValley()
        {
            var player = new TestPlayer(); var game = new ValleySimulation(TestValley.Chapter1());
            Assert.That(player.RunUntil(game, () => game.Stage == DirectorStage.TravelersCrossing), Is.True);
            player.Step(game, 1, InputCommand.Pause);
            var positions = game.Travelers.Select(t => t.Position).ToArray();
            player.Step(game, 120);
            Assert.That(game.IsPaused, Is.True); Assert.That(game.Travelers.Select(t => t.Position), Is.EqualTo(positions));
            player.Step(game, 1, InputCommand.Resume);
            Assert.That(player.RunUntil(game, () => game.IsComplete), Is.True);
        }

        [Test] public void EitherHandPlaysTheWholeValley()
        {
            var (game, _) = Play(6, new TestPlayer { Side = HandSide.Right });
            Assert.That(game.IsComplete, Is.True);
        }

        [Test] public void ControllersInHandHoldBoarding()
        {
            var game = new ValleySimulation(TestValley.Chapter1()); var player = new TestPlayer();
            for (long t = 0; t < 700; t++)
            {
                var hand = player.Hand(game, t);
                game.Step(new HandFrame(t, t * 16667, hand, null, true, InputCommand.None, true));
                Assert.That(game.Session.Owner, Is.Null);
            }
            Assert.That(game.WaitingForHands, Is.True); Assert.That(game.ShowStartupText, Is.True);
            Assert.That(new ValleyPresentation(game, 1).PutControllersDown, Is.True);
        }

        [Test] public void PresentationShowsTheActiveGapAndBridges()
        {
            var player = new TestPlayer(); var game = new ValleySimulation(TestValley.Chapter1());
            Assert.That(player.RunUntil(game, () => game.PuzzleIndex == 2 && game.Session?.Build?.Progress > 0.2), Is.True);
            var view = new ValleyPresentation(game, 0.5);
            Assert.That(view.Puzzle, Is.EqualTo(PuzzleKind.Width)); Assert.That(view.ActiveEdge, Is.EqualTo("A-C"));
            Assert.That(view.Rule, Is.EqualTo(TravelerKind.Cart)); Assert.That(view.Bridges.Keys, Is.EquivalentTo(new[] { "S-A" }));
            Assert.That(view.Scaffold, Is.Not.Null); Assert.That(view.HandOwner, Is.EqualTo(HandSide.Left));
            Assert.That(view.LedgeWidth, Is.EqualTo(0.015)); Assert.That(view.PlantingStage, Is.EqualTo(ValleyStage.Playing));
        }

        [Test] public void PlantedValleyPlaysThroughPalmfolkGame()
        {
            var game = new PalmfolkGame(TestValley.Chapter1()); var player = new TestPlayer();
            var table = new Planted.Placement { Height = 0.74, YawDegrees = 37, HandLength = 0.2, Offset = new Vec3(0.2, 0, -0.35) };
            for (int i = 0; i < 300 && game.Stage == ValleyStage.Planting; i++) game.Step(Planted.Frame(game.Tick + 1, table.Hand(game.Tick + 1)));
            Assert.That(game.Stage, Is.EqualTo(ValleyStage.Playing));
            Assert.That(new ValleyPresentation(game, 1).Pose, Is.SameAs(game.Pose));
            Assert.Throws<ArgumentException>(() => new WorldPresentation(game, 1));
            for (int i = 0; i < 30000 && !game.Valley.IsComplete; i++)
            {
                long tick = game.Tick + 1; var hand = player.Hand(game.Valley, tick);
                game.Step(Planted.Frame(tick, hand == null ? null : game.Pose.ToWorld(hand)));
            }
            Assert.That(game.Valley.IsComplete, Is.True);
        }
    }
}
