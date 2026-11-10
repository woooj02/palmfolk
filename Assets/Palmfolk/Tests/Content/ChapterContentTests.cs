using System.Linq;
using NUnit.Framework;
using Palmfolk.Core;
using Palmfolk.Core.Content;

namespace Palmfolk.Tests.Content
{
    public class ChapterContentTests
    {
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
