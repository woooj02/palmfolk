using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Palmfolk.Core;

namespace Palmfolk.Tests
{
    // D8: visitors and the living valley (R1-R4, E16, V3), played one-handed by the headless test player.
    public class VisitorTests
    {
        private static readonly Dictionary<int, ValleyProgress> through = new Dictionary<int, ValleyProgress>();

        // The chapter's progress once visitors 0..helped-1 have settled (helped 0: just after the finale).
        private static ValleyProgress Through(int helped)
        {
            if (through.TryGetValue(helped, out var cached)) return cached.Copy();
            ValleyProgress progress;
            if (helped == 0)
            {
                var game = new ValleySimulation(TestValley.WithVisitors()); var player = new TestPlayer();
                Assert.That(player.RunUntil(game, () => game.IsComplete), Is.True, "chapter");
                progress = game.Progress;
            }
            else
            {
                var game = new ValleySimulation(TestValley.WithVisitors(), resume: Through(helped - 1)); var player = new TestPlayer();
                Assert.That(Help(game, player), Is.True, $"visitor {helped - 1}");
                progress = game.Progress;
            }
            through[helped] = progress.Copy();
            return progress;
        }

        private static bool Help(ValleySimulation game, TestPlayer player, int limit = 40000)
        {
            int before = player.Count(GameEventKind.VisitorSettled);
            return player.RunUntil(game, () => player.Count(GameEventKind.VisitorSettled) > before, limit);
        }

        private static void Wait(ValleySimulation game, TestPlayer player, int ticks) => player.Step(game, ticks);

        [Test] public void TheValleyRefusesBadVisitors()
        {
            var v = TestValley.Visitors();
            Assert.Throws<ArgumentException>(() => TestValley.WithVisitors(v.Concat(new[] { new VisitorSpec("Sixth", TravelerKind.Walker, "A", "A-C") }).ToArray()), "at most five");
            Assert.Throws<ArgumentException>(() => TestValley.WithVisitors(new[] { new VisitorSpec("Lost", TravelerKind.Walker, "A", "L1-L2") }), "edge must touch From");
            Assert.Throws<ArgumentException>(() => TestValley.WithVisitors(new[] { v[0], new VisitorSpec("Gardener", TravelerKind.Cart, "C", "C-E") }), "unique ids");
            Assert.Throws<ArgumentException>(() => new VisitorSpec("Twice", TravelerKind.Walker, "G", "G-G1", "G-G1"), "distinct edges");
            Assert.That(TestValley.WithVisitors().Visitors.Count, Is.EqualTo(5));
            Assert.That(TestValley.Chapter1().Visitors, Is.Empty);
        }

        // Done-when for D8: all five visitors, one hand, the bell between them; each comes with a real need.
        [Test] public void AllFiveVisitorsAreCompletableWithEitherHand([Values] HandSide side)
        {
            var game = new ValleySimulation(TestValley.WithVisitors()); var player = new TestPlayer { Side = side };
            Assert.That(player.RunUntil(game, () => game.IsComplete), Is.True, "chapter");
            // E16: the first visitor is already called as the sanctuary lights.
            Assert.That(game.VisitorIndex, Is.EqualTo(0));
            for (int i = 0; i < 5; i++)
            {
                var visitor = game.Visitor;
                Assert.That(visitor, Is.Not.Null, $"visitor {i}"); Assert.That(game.VisitorIndex, Is.EqualTo(i));
                Assert.That(game.Check(visitor.Edges, visitor.Traveler), Is.Not.Empty, $"{visitor.Id} has a need");
                Assert.That(Help(game, player), Is.True, $"{visitor.Id} helped");
                Assert.That(game.Visitor, Is.Null); Assert.That(game.Stage, Is.EqualTo(DirectorStage.Complete));
                Assert.That(game.LitWindows, Is.EqualTo((1 << (i + 1)) - 1));
                foreach (var id in visitor.Edges) Assert.That(game.Passes(id, visitor.Traveler), Is.True, $"{visitor.Id} {id}");
                // Nobody comes until the bell rings; then the next visitor is called at once.
                Wait(game, player, 120); Assert.That(game.Visitor, Is.Null);
                int called = player.Count(GameEventKind.VisitorCalled);
                player.Step(game, 1, InputCommand.RingBell);
                Assert.That(player.Count(GameEventKind.VisitorCalled), Is.EqualTo(i < 4 ? called + 1 : called));
            }
            Assert.That(game.VisitorsHelped, Is.EqualTo(0b11111)); Assert.That(game.LitWindows, Is.EqualTo(0b11111));
            Assert.That(game.BridgeMarks["A-G"], Is.EqualTo(0b10001)); Assert.That(game.BridgeMarks["C-E"], Is.EqualTo(0b10));
            Assert.That(game.BridgeMarks["G-G1"], Is.EqualTo(0b100)); Assert.That(game.BridgeMarks["G-G2"], Is.EqualTo(0b100));
            Assert.That(game.BridgeMarks["L1-L2"], Is.EqualTo(0b1000));
            Assert.That(player.Count(GameEventKind.VisitorSettled), Is.EqualTo(5)); Assert.That(player.Count(GameEventKind.VisitorArrived), Is.EqualTo(5));
            var view = new ValleyPresentation(game, 0);
            Assert.That(view.Windows, Is.EqualTo(5)); Assert.That(view.LitWindows, Is.EqualTo(0b11111)); Assert.That(view.Visitor, Is.Null);
            Assert.That(view.Residents.Count, Is.EqualTo(6)); // one per visitor edge: A-G (twice), C-E, G-G1, G-G2, L1-L2
        }

        [Test] public void TheBellIsIgnoredWhileAVisitorIsHere()
        {
            var game = new ValleySimulation(TestValley.WithVisitors(), resume: Through(0)); var player = new TestPlayer();
            Assert.That(game.VisitorIndex, Is.EqualTo(0));
            for (int i = 0; i < 300; i++) player.Step(game, 1, i % 30 == 0 ? InputCommand.RingBell : InputCommand.None);
            Assert.That(game.VisitorIndex, Is.EqualTo(0)); Assert.That(player.Count(GameEventKind.VisitorCalled), Is.EqualTo(1));
            Assert.That(Help(game, player), Is.True); Assert.That(game.VisitorIndex, Is.EqualTo(-1));
        }

        // R2: each launch after the finale brings the next visitor; a visitor in progress carries on after a relaunch.
        [Test] public void EachLaunchBringsTheNextVisitorAndAResumeCarriesOn()
        {
            for (int helped = 0; helped < 5; helped++)
            {
                var progress = Through(helped);
                var game = new ValleySimulation(TestValley.WithVisitors(), resume: progress); var player = new TestPlayer();
                Assert.That(game.VisitorIndex, Is.EqualTo(helped), $"launch {helped}");
                player.Step(game);
                Assert.That(player.Count(GameEventKind.VisitorCalled), Is.EqualTo(1), "called on the first tick");
                Assert.That(player.Count(GameEventKind.PuzzleStarted), Is.Zero);
            }
            var done = new ValleySimulation(TestValley.WithVisitors(), resume: Through(5));
            Assert.That(done.Visitor, Is.Null); Assert.That(done.Stage, Is.EqualTo(DirectorStage.Complete)); Assert.That(done.LitWindows, Is.EqualTo(0b11111));

            // Mid-visitor: the Twins' first bridge is built when the game closes.
            var first = new ValleySimulation(TestValley.WithVisitors(), resume: Through(2)); var p1 = new TestPlayer();
            Assert.That(p1.RunUntil(first, () => first.Bridges.ContainsKey("G-G1") || first.Bridges.ContainsKey("G-G2")), Is.True);
            var saved = first.Progress; Assert.That(saved.CurrentVisitor, Is.EqualTo(2));
            var resumed = new ValleySimulation(TestValley.WithVisitors(), resume: saved); var p2 = new TestPlayer();
            Assert.That(resumed.VisitorIndex, Is.EqualTo(2));
            Assert.That(Help(resumed, p2), Is.True); Assert.That(resumed.LitWindows, Is.EqualTo(0b111));
        }

        // R1: on relaunch, travelers are already walking the player's bridges.
        [Test] public void TravelersWalkYourBridgesOnRelaunch([Values(1, 3, 5)] int helped)
        {
            var game = new ValleySimulation(TestValley.WithVisitors(), resume: Through(helped)); var player = new TestPlayer { Side = HandSide.Right };
            var residents = new HashSet<string>(); bool bramWalked = false;
            for (int i = 0; i < 600; i++)
            {
                player.Step(game);
                foreach (var r in game.Residents) if (!r.Arrived) residents.Add(r.Destination);
                if (game.Bram != null && !game.Bram.Arrived) bramWalked = true;
            }
            Assert.That(bramWalked, Is.True, "Bram");
            Assert.That(residents, Is.Not.Empty, "a visitor on their bridge");
            Assert.That(game.Residents.Count, Is.EqualTo(TestValley.Visitors().Take(helped).Sum(v => v.Edges.Count)));
            var view = new ValleyPresentation(game, 0.5);
            Assert.That(view.Residents.Count, Is.EqualTo(game.Residents.Count)); Assert.That(view.VisitorFigure != null, Is.EqualTo(helped < 5), "the next visitor walks in"); Assert.That(view.Windows, Is.EqualTo(5));
        }

        // R4: the Merchant's cart needs C-E rebuilt; a refused attempt leaves the old bridge standing.
        [Test] public void TheOldBridgeStaysUntilTheNewOnePasses()
        {
            var game = new ValleySimulation(TestValley.WithVisitors(), resume: Through(1));
            var player = new TestPlayer { VisitorPlan = (v, edge, attempt) => edge == "C-E" && attempt == 0 ? TestPlayer.Shape.Arch : TestPlayer.DefaultVisitorPlan(v, edge, attempt) };
            Assert.That(game.Visitor.Id, Is.EqualTo("Merchant"));
            var old = game.Bridges["C-E"];
            Assert.That(game.FailingEdges, Is.EqualTo(new[] { "C-E" }));
            Assert.That(player.RunUntil(game, () => player.Count(GameEventKind.Refused) > 0), Is.True, "refused");
            Assert.That(game.Bridges["C-E"], Is.SameAs(old)); Assert.That(game.FailingEdges, Does.Contain("C-E"));
            Assert.That(game.Residents.Count(r => !r.Arrived && r.Destination == "E"), Is.Zero, "nobody walks a bridge being rebuilt");
            Assert.That(Help(game, player), Is.True);
            Assert.That(game.Bridges["C-E"], Is.Not.SameAs(old)); Assert.That(game.Passes("C-E", TravelerKind.Cart), Is.True);
            Assert.That(game.FailingEdges, Is.Empty);
        }

        // A need already met: the visitor walks in, crosses and settles, and nothing is built.
        [Test] public void AMetNeedJustCrossesAndSettles()
        {
            var setup = new ValleySimulation(TestValley.WithVisitors(), resume: Through(0));
            var builder = new TestPlayer { VisitorPlan = (v, e, a) => TestPlayer.Shape.Plank };
            Assert.That(Help(setup, builder), Is.True);
            var progress = setup.Progress; progress.VisitorsHelped |= 0b1110; progress.LitWindows |= 0b1110;
            var game = new ValleySimulation(TestValley.WithVisitors(), resume: progress); var player = new TestPlayer();
            Assert.That(game.Visitor.Id, Is.EqualTo("Lantern-maker"));
            Assert.That(game.Check(game.Visitor.Edges, TravelerKind.Cart), Is.Empty);
            bool built = false;
            Assert.That(player.RunUntil(game, () => { built |= game.Session != null; return player.Count(GameEventKind.VisitorSettled) > 0; }), Is.True);
            Assert.That(built, Is.False); Assert.That(player.Count(GameEventKind.VisitorArrived), Is.EqualTo(1));
            Assert.That(player.Count(GameEventKind.TravelersStarted), Is.EqualTo(1));
            Assert.That(game.LitWindows, Is.EqualTo(0b11111));
        }

        // V3 before each visitor: any one saved bridge missing, or swapped for another edge's, is found and rebuilt
        // when the visitor needs it, and the visitor can still be helped.
        [Test] public void ACorruptedBridgeBeforeAnyVisitorStaysCompletable([Range(0, 4)] int visitor, [Values] bool swap)
        {
            var clean = Through(visitor);
            foreach (var id in clean.Bridges.Keys.ToList())
            {
                var progress = clean.Copy();
                if (swap) progress.Bridges[id] = clean.Bridges[clean.Bridges.Keys.First(k => k != id)];
                else progress.Bridges.Remove(id);
                var game = new ValleySimulation(TestValley.WithVisitors(), resume: progress); var player = new TestPlayer { Side = visitor % 2 == 0 ? HandSide.Left : HandSide.Right };
                var spec = game.Visitor;
                Assert.That(Help(game, player), Is.True, $"{spec.Id} with {id} {(swap ? "swapped" : "missing")}");
                foreach (var e in spec.Edges) Assert.That(game.Passes(e, spec.Traveler), Is.True, $"{spec.Id} {e} after {id}");
            }
        }
    }
}
