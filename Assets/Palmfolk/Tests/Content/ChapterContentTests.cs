using System.Linq;
using NUnit.Framework;
using Palmfolk.Core;
using Palmfolk.Core.Content;

namespace Palmfolk.Tests.Content
{
    public class ChapterContentTests
    {
        [TestCase(HandSide.Left)] [TestCase(HandSide.Right)]
        public void ShippedChapterHelpsAllFiveVisitorsAndKeepsTheirWindows(HandSide side)
        {
            var spec = ChapterOne.Greybox(); var game = new ValleySimulation(spec);
            var player = new TestPlayer { Side = side };
            Assert.That(player.RunUntil(game, () => game.IsComplete), Is.True);
            for (int i = 0; i < 5; i++)
            {
                if (i > 0) player.Step(game, 1, InputCommand.RingBell);
                int helped = i + 1;
                Assert.That(player.RunUntil(game, () => player.Count(GameEventKind.VisitorSettled) == helped, 40000), Is.True, spec.Visitors[i].Id);
            }
            var resumed = new ValleySimulation(spec, resume: game.Progress);
            var view = new ValleyPresentation(resumed, 1);
            Assert.That(view.Windows, Is.EqualTo(5)); Assert.That(view.LitWindows, Is.EqualTo(31));
            Assert.That(view.Residents.Count, Is.EqualTo(6));
            Assert.That(view.BridgeMarks.Values.Aggregate(0, (a, b) => a | b), Is.EqualTo(31));
        }
        [Test] public void ForkEdgesShareOneFullSourceZone()
        {
            var valley = ChapterOne.Greybox();
            Assert.That(valley.Edge("C-L1").FromAnchor.Centre, Is.EqualTo(valley.Edge("C-L2").FromAnchor.Centre));
            Assert.That(valley.Edge("C-L1").FromAnchor.Radius, Is.EqualTo(0.02));
            Assert.That(valley.Edge("G-G1").FromAnchor.Centre, Is.EqualTo(valley.Edge("G-G2").FromAnchor.Centre));
        }
        [Test] public void EveryReservedGapAndIslandFitsTheNumericalLayout()
        {
            var valley = ChapterOne.Greybox();
            Assert.That(valley.Islands.Count, Is.EqualTo(10)); Assert.That(valley.Edges.Count, Is.EqualTo(10));
            foreach (var island in valley.Islands)
            {
                Assert.That(island.Top.X - island.Radius, Is.GreaterThanOrEqualTo(-0.30));
                Assert.That(island.Top.X + island.Radius, Is.LessThanOrEqualTo(0.30));
                Assert.That(island.Top.Z - island.Radius, Is.GreaterThanOrEqualTo(-0.24));
                Assert.That(island.Top.Z + island.Radius, Is.LessThanOrEqualTo(0.21));
            }
            foreach (var edge in valley.Edges)
                Assert.That(Vec3.Distance(edge.FromAnchor.Centre, edge.ToAnchor.Centre), Is.InRange(0.12 - 1e-12, 0.15 + 1e-12), edge.Id);
            Assert.That(valley.FinaleRoute().Select(r => (r.From, r.To)), Is.EqualTo(new[] { ("S", "A"), ("A", "C"), ("C", "Sanctuary") }));
            Assert.That(valley.Puzzles[2].Rule, Is.EqualTo(TravelerKind.Cart));
        }
        [TestCase(HandSide.Left)] [TestCase(HandSide.Right)]
        public void ChapterContentCompletesThroughTheRealDirectorAndCartRules(HandSide side)
        {
            var game = new ValleySimulation(ChapterOne.Greybox());
            var player = new TestPlayer { Side = side };
            Assert.That(player.RunUntil(game, () => game.IsComplete), Is.True, "Playable candidate content, not a reach/comfort pass.");
            Assert.That(player.Count(GameEventKind.CrossingCompleted), Is.EqualTo(3));
            Assert.That(player.Count(GameEventKind.LedgeWidened), Is.EqualTo(1));
            foreach (var leg in game.Valley.FinaleRoute()) Assert.That(game.Passes(leg.Edge.Id, TravelerKind.Cart), Is.True, leg.Edge.Id);
        }
    }
}
