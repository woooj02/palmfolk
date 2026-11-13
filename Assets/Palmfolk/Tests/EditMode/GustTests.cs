using System.Linq;
using NUnit.Framework;
using Palmfolk.Core;

namespace Palmfolk.Tests
{
    public class GustTests
    {
        private static readonly Vec3 Origin = new Vec3(0, 0.01, 0.08), Target = new Vec3(0, 0.01, -0.08);

        // Synthetic.Hand stood on its edge: the palm faces along Z, so its plane cuts a wind blowing along Z.
        private static HandSample Upright(Vec3 offset = default)
        {
            var flat = Synthetic.Hand();
            var joints = flat.Joints.Select(j => j.WithPosition(new Vec3(j.Position.X, j.Position.Z, -j.Position.Y) + offset)).ToArray();
            return new HandSample(HandSide.Left, joints);
        }

        [Test] public void AnUprightHandAcrossTheWindShields()
        {
            Assert.That(GustField.Shields(Upright(), Origin, Target), Is.True);
        }

        [Test] public void AFlatHandOrAHandAsideDoesNotShield()
        {
            Assert.That(GustField.Shields(Synthetic.Hand(), Origin, Target), Is.False);
            Assert.That(GustField.Shields(Upright(new Vec3(0.3, 0, 0)), Origin, Target), Is.False);
            Assert.That(GustField.Shields(Upright(new Vec3(0, 0, 0.2)), Origin, Target), Is.False, "behind the gust");
            Assert.That(GustField.Shields(null, Origin, Target), Is.False);
        }

        [Test] public void AGustGathersThenHitsUnlessShielded()
        {
            foreach (bool shield in new[] { false, true })
            {
                var field = new GustField(); GameEventKind? last = null; int gathering = 0;
                for (long tick = 1; tick <= GustField.FirstDelay + GustField.GatherTicks + GustField.BlowTicks + 5 && last != GameEventKind.GustHit && last != GameEventKind.GustBlocked; tick++)
                {
                    var current = field.Current;
                    // The shield goes up beside whichever side the gust gathers on.
                    var hand = shield && current != null ? Upright(new Vec3(0, 0, -0.05)) : null;
                    var frame = Synthetic.Frame(tick, hand);
                    var e = field.Step(frame, Vec3.Zero, new Vec3(1, 0, 0));
                    if (e == GameEventKind.GustGathering) gathering++;
                    if (e.HasValue) last = e;
                }
                Assert.That(gathering, Is.EqualTo(1));
                Assert.That(last, Is.EqualTo(shield ? GameEventKind.GustBlocked : GameEventKind.GustHit));
                Assert.That(shield ? field.Blocked : field.Hits, Is.EqualTo(1));
            }
        }

        [Test] public void GustsSlowTheCrossingButNeverBlockTheGame()
        {
            var game = new ValleySimulation(TestValley.Chapter1(), new GameSettings { SkipBoard = true, Gusts = true }); var player = new TestPlayer();
            Assert.That(player.RunUntil(game, () => game.IsComplete), Is.True, $"stuck in {game.Puzzle.Kind} at {game.Stage}");
            Assert.That(player.Count(GameEventKind.GustHit), Is.GreaterThan(0));
        }
    }
}
