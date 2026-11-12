using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Palmfolk.Core;

namespace Palmfolk.Tests
{
    // What Sam's views asked Core for (S2, S5, S6): the connection lock (E17), where each traveler stands on its
    // bridge, the whole hand behind the recovery ghost (T5) and a working shape for the hint hand (E8). Headless,
    // over the whole of chapter one and all five visitors.
    public class ViewContractTests
    {
        // Plays everything with the scripted player, calling `each` after every tick.
        private static ValleySimulation PlayAll(Action<ValleySimulation, TestPlayer> each)
        {
            var game = new ValleySimulation(TestValley.WithVisitors()); var player = new TestPlayer();
            for (int i = 0; i < 200000 && game.VisitorsHelped != 0b11111; i++)
            {
                long tick = game.Tick + 1;
                var command = game.IsComplete && game.Visitor == null && game.Stage == DirectorStage.Complete ? InputCommand.RingBell : InputCommand.None;
                game.Step(new HandFrame(tick, tick * 16667, player.Hand(game, tick), null, true, command));
                foreach (var e in game.Events)
                {
                    player.Events.Add(e);
                    if (e.Kind == GameEventKind.Refused && game.Session != null) { player.Attempts.TryGetValue(game.Session.Edge, out int a); player.Attempts[game.Session.Edge] = a + 1; }
                }
                each(game, player);
            }
            Assert.That(game.VisitorsHelped, Is.EqualTo(0b11111));
            return game;
        }

        // E17: every build starts on a locked connection, and the lock is raised once per hold, not every tick.
        [Test] public void EveryBuildStartsOnALockedConnection()
        {
            int builds = 0, locks = 0, carries = 0;
            PlayAll((game, player) =>
            {
                var kinds = game.Events.Select(e => e.Kind).ToList();
                locks += kinds.Count(k => k == GameEventKind.ConnectionLocked);
                if (kinds.Contains(GameEventKind.ConnectionLocked)) Assert.That(game.Session?.ConnectionLocked, Is.True, "locked when raised");
                if (kinds.Contains(GameEventKind.BuildStarted))
                {
                    builds++;
                    Assert.That(new ValleyPresentation(game, 1).ConnectionLocked, Is.True, "a build starts on a locked connection");
                }
                if (kinds.Contains(GameEventKind.CheckpointReached)) carries++;
            });
            Assert.That(builds, Is.GreaterThan(10)); Assert.That(carries, Is.EqualTo(1), "the Board");
            Assert.That(locks, Is.GreaterThanOrEqualTo(builds + carries)); Assert.That(locks, Is.LessThan(3 * (builds + carries)), "once per hold");
        }

        // S6: a traveler on a bridge stands on it: Position is the support evaluated at the cursor, matches the
        // simulation at alpha 1, and moves smoothly in between.
        [Test] public void TravelersStandOnTheBridgeTheyCross()
        {
            int supported = 0, pip = 0; var kinds = new HashSet<string>();
            PlayAll((game, player) =>
            {
                var late = new ValleyPresentation(game, 1); var half = new ValleyPresentation(game, 0.5);
                if (late.PipCursor.HasValue) { pip++; Assert.That(Vec3.Distance(late.FilteredHandRoute.Evaluate(late.PipCursor.Value), game.Pip), Is.LessThan(1e-9)); }
                var views = late.Travelers.Concat(late.Residents).Append(late.BramWalk).Append(late.VisitorFigure).ToList();
                var halves = half.Travelers.Concat(half.Residents).Append(half.BramWalk).Append(half.VisitorFigure).ToList();
                var sims = game.Travelers.Concat(game.Residents).Append(game.Bram).Append(game.VisitorArrival).ToList();
                for (int i = 0; i < views.Count; i++)
                {
                    var view = views[i]; if (view?.Support == null) continue;
                    supported++; kinds.Add(i < late.Travelers.Count ? "traveler" : i < late.Travelers.Count + late.Residents.Count ? "resident" : "bram");
                    Assert.That(Vec3.Distance(view.Position, sims[i].Position), Is.LessThan(1e-9), "alpha 1 is the simulation");
                    Assert.That(Vec3.Distance(view.Support.Evaluate(view.SupportCursor), view.Position), Is.LessThan(1e-12));
                    var between = halves[i];
                    Assert.That(between.Support, Is.SameAs(view.Support));
                    Assert.That(Vec3.Distance(between.Position, Vec3.Lerp(sims[i].PreviousPosition, sims[i].Position, 0.5)), Is.LessThan(0.002), "no jump between ticks");
                }
            });
            Assert.That(supported, Is.GreaterThan(1000)); Assert.That(pip, Is.GreaterThan(10), "Pip rides the Board hand");
            Assert.That(kinds, Is.EquivalentTo(new[] { "traveler", "resident", "bram" }));
        }

        // T5: on a loss mid-build the ghost has the whole hand as it was last read; it goes with the recovery.
        [Test] public void TheGhostKeepsTheWholeHand()
        {
            var game = new ValleySimulation(TestValley.Chapter1()); var player = new TestPlayer();
            Assert.That(player.RunUntil(game, () => game.PuzzleIndex == 1 && game.Session?.Build?.Progress > 0.3), Is.True);
            var last = player.Hand(game, game.Tick);
            game.Step(new HandFrame(game.Tick + 1, (game.Tick + 1) * 16667, null, null));
            var view = new ValleyPresentation(game, 1);
            Assert.That(view.Recovering, Is.True); Assert.That(view.GhostHand, Is.Not.Null);
            Assert.That(view.GhostHand.Side, Is.EqualTo(HandSide.Left)); Assert.That(view.GhostHand.Joints.Count, Is.EqualTo(26));
            for (int j = 0; j < 26; j++) Assert.That(Vec3.Distance(view.GhostHand.Joints[j].Position, last.Joints[j].Position), Is.LessThan(0.01), ((JointId)j).ToString());
            var ghost = view.GhostHand;
            game.Step(new HandFrame(game.Tick + 1, (game.Tick + 1) * 16667, null, null));
            Assert.That(new ValleyPresentation(game, 1).GhostHand, Is.SameAs(ghost), "the ghost holds still");
            Assert.That(player.RunUntil(game, () => game.Session?.Recovery == null), Is.True);
            Assert.That(new ValleyPresentation(game, 1).GhostHand, Is.Null);
        }

        // E8: every interaction of the valley has a hint of the right shape that passes the rules as the bake judges
        // them, except while a ledge is still too narrow for its traveler.
        [Test] public void EveryInteractionHasAWorkingHint()
        {
            EdgeSession seen = null; var shapes = new Dictionary<string, HintShape>(); int narrow = 0;
            PlayAll((game, player) =>
            {
                var session = game.Session;
                if (session == null || ReferenceEquals(session, seen)) return;
                seen = session;
                var hint = new ValleyPresentation(game, 1).Hint;
                string who = (game.Visitor?.Id ?? game.Puzzle.Kind.ToString()) + " " + string.Join("+", session.Edges);
                if (hint == null)
                {
                    Assert.That(session.WidthCap, Is.LessThan(game.Settings.Rule(session.Rule).MinimumWidth), who + ": no hint only on a narrow ledge");
                    narrow++; return;
                }
                Assert.That(hint, Is.SameAs(session.Hint), "found once");
                shapes[who] = hint.Shape;
                if (session.Carry) { Assert.That(hint.Shape, Is.EqualTo(HintShape.CarryFinger)); return; }
                for (int k = 0; k < session.Edges.Count; k++)
                {
                    var walk = session.IsFork ? new CurveTree(new[] { hint.Tree.Curves[0], hint.Tree.Curves[k + 1] }, new[] { -1, 0 }) : hint.Tree;
                    var failures = BridgeValidator.Validate(walk, session.Source, new[] { session.Destinations[k] }, session.Rule, game.Settings, game.Valley.Clearance);
                    Assert.That(failures.Select(f => f.Reason), Is.Empty, who);
                }
            });
            TestContext.Out.WriteLine(string.Join("\n", shapes.Select(p => p.Key + ": " + p.Value)));
            Assert.That(shapes.Where(p => p.Key.StartsWith("Board")).Select(p => p.Value).Distinct(), Is.EqualTo(new[] { HintShape.CarryFinger }));
            Assert.That(shapes.Where(p => p.Key.StartsWith("FirstBridge")).Select(p => p.Value).Distinct(), Is.EqualTo(new[] { HintShape.OneFinger }));
            Assert.That(shapes.Where(p => p.Key.StartsWith("Fork")).Select(p => p.Value).Distinct(), Is.EqualTo(new[] { HintShape.Spread }));
            Assert.That(shapes.Where(p => p.Key.StartsWith("Arch")).Select(p => p.Value).Distinct(), Is.EqualTo(new[] { HintShape.Arch }));
            Assert.That(shapes.Values.Distinct(), Is.SupersetOf(new[] { HintShape.CarryFinger, HintShape.OneFinger, HintShape.FlatHand, HintShape.Arch, HintShape.Spread }));
            Assert.That(narrow, Is.LessThanOrEqualTo(1), "only Width's ledge before it is widened");
        }
    }
}
