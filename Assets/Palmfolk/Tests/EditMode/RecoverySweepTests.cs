using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Palmfolk.Core;
using Palmfolk.Fixtures;

namespace Palmfolk.Tests
{
    // D7/T5: tracking loss forced at points along every route in the valley. A headless player plays Chapter 1;
    // the loss is injected at points spread over each hand interaction (Board's carry, every build, the fork,
    // the arch, the finale rebuilds) and each traveler walk. Whatever happens, no traveler teleports, Pip never
    // stands on an island he hadn't reached before, and the valley can still be finished.
    public class RecoverySweepTests
    {
        public enum Loss { Brief, LowConfidence, Moved, Long, OtherHand }

        // The exhaustive sweep is every 3 ticks (50 ms) in the headless checks (tools/CoreChecks), about 25 s. The
        // Unity editor runs the same sweep every 15 ticks: Mono is about eight times slower.
#if UNITY_5_3_OR_NEWER
        public const int Stride = 15;
#else
        public const int Stride = 3;
#endif

        // Pip, Bram and the travelers move at most this far in one tick. Walking is 0.5 mm a tick; the bound
        // leaves room for blends and the build front, and is far below any jump to a hand somewhere else.
        private const double MaximumStep = 0.005;

        // A window of ticks in the baseline run: a hand interaction (owner set) or a walk with no hand.
        public sealed class Window
        {
            public int Puzzle; public string Edge; public long Start, End; public bool Hand;
            public override string ToString() => $"puzzle {Puzzle} {Edge} {(Hand ? "hand" : "walk")} {Start}-{End}";
        }

        // Checks every tick: no teleport, Pip only on islands he has reached, and nothing reached while recovering.
        public sealed class Watch
        {
            private readonly HashSet<string> reached = new HashSet<string>();
            private readonly Dictionary<Traveler, Vec3> travelers = new Dictionary<Traveler, Vec3>();
            private Vec3? bram;
            private string island;
            public readonly List<string> Problems = new List<string>();
            public IReadOnlyCollection<string> Reached => reached;
            public Watch(ValleySimulation game) { reached.Add(game.PipIsland); island = game.PipIsland; }

            public void Observe(ValleySimulation game, bool recovering)
            {
                string at = $"tick {game.Tick}";
                if (Vec3.Distance(game.Pip, game.PreviousPip) > MaximumStep) Problems.Add($"{at}: Pip jumped {Vec3.Distance(game.Pip, game.PreviousPip):F4} m");
                foreach (var t in game.Travelers)
                {
                    if (travelers.TryGetValue(t, out var last) && Vec3.Distance(t.Position, last) > MaximumStep) Problems.Add($"{at}: a traveler jumped");
                    travelers[t] = t.Position;
                }
                if (game.Bram != null)
                {
                    if (bram.HasValue && Vec3.Distance(game.Bram.Position, bram.Value) > MaximumStep) Problems.Add($"{at}: Bram jumped");
                    bram = game.Bram.Position;
                }
                if (recovering)
                {
                    if (!reached.Contains(game.PipIsland)) Problems.Add($"{at}: Pip on {game.PipIsland} while recovering");
                    if (game.PipIsland != island) Problems.Add($"{at}: Pip's island changed while recovering");
                    foreach (var e in game.Events)
                        if (e.Kind == GameEventKind.CheckpointReached || e.Kind == GameEventKind.BridgeBaked || e.Kind == GameEventKind.PuzzleCompleted || e.Kind == GameEventKind.SectionCommitted)
                            Problems.Add($"{at}: {e.Kind} while recovering");
                }
                // An island counts as reached only by a carry or a baked bridge Pip stood at the end of, or a walk.
                reached.Add(game.PipIsland); island = game.PipIsland;
            }
        }

        public sealed class Outcome
        {
            public Window Window; public Loss Loss; public long LossTick;
            public bool OwnedAtLoss, Recovered, Returned, Complete;
            public string IslandAtLoss, IslandAfterRecovery;
            public List<string> Problems = new List<string>();
            public List<GameEvent> Events = new List<GameEvent>();
        }

        private static List<Window> baseline;
        private static long baselineTicks;

        // The undisturbed run, cut into windows.
        public static List<Window> Baseline()
        {
            if (baseline != null) return baseline;
            var player = new TestPlayer(); var game = new ValleySimulation(TestValley.Chapter1()); var watch = new Watch(game);
            var windows = new List<Window>(); Window open = null;
            while (!game.IsComplete && game.Tick < 40000)
            {
                player.Step(game); watch.Observe(game, false);
                bool hand = game.Session?.Owner != null;
                bool walk = !hand && (game.Stage == DirectorStage.Walking || game.Stage == DirectorStage.TravelersCrossing || game.Stage == DirectorStage.BramCrossing);
                string edge = hand ? game.Session.Edge : game.Stage.ToString();
                bool same = open != null && open.Hand == hand && open.Edge == edge && open.Puzzle == game.PuzzleIndex && open.End == game.Tick - 1;
                if ((hand || walk) && same) open.End = game.Tick;
                else if (hand || walk) { open = new Window { Puzzle = game.PuzzleIndex, Edge = edge, Start = game.Tick, End = game.Tick, Hand = hand }; windows.Add(open); }
            }
            Assert.That(game.IsComplete, Is.True, "the baseline run finishes");
            Assert.That(watch.Problems, Is.Empty, "the baseline run itself");
            baselineTicks = game.Tick; baseline = windows.Where(w => w.End - w.Start >= 10).ToList();
            return baseline;
        }

        private static HandSample MapJoints(HandSample hand, Func<JointSample, JointSample> f)
        {
            if (hand == null) return null;
            var joints = new JointSample[26]; for (int i = 0; i < 26; i++) joints[i] = f(hand.Joints[i]);
            return new HandSample(hand.Side, joints);
        }
        private static HandSample LowConfidence(HandSample hand) => MapJoints(hand, j => new JointSample(j.Position, j.Rotation, j.Radius, j.Flags & ~JointFlags.HighConfidence));
        private static HandSample Moved(HandSample hand) => MapJoints(hand, j => j.WithPosition(j.Position + new Vec3(0.2, 0, 0)));

        // Replays the baseline to `lossTick`, drops tracking in the given style, then lets the player finish.
        public static Outcome Run(Window window, long lossTick, Loss loss, bool finish = true)
        {
            var player = new TestPlayer(); var other = new TestPlayer { Side = HandSide.Right };
            var game = new ValleySimulation(TestValley.Chapter1()); var watch = new Watch(game);
            var outcome = new Outcome { Window = window, Loss = loss, LossTick = lossTick };
            while (game.Tick + 1 < lossTick) { player.Step(game); watch.Observe(game, false); }
            outcome.OwnedAtLoss = game.Session?.Owner != null; outcome.IslandAtLoss = game.PipIsland;
            int length = loss == Loss.Brief ? 10 : loss == Loss.LowConfidence ? 30 : loss == Loss.Moved ? 180 : loss == Loss.Long ? 560 : 150;
            for (int i = 0; i < length; i++)
            {
                long tick = game.Tick + 1; var hand = player.Hand(game, tick); HandSample left = null, right = null;
                if (loss == Loss.LowConfidence) left = LowConfidence(hand);
                else if (loss == Loss.Moved && i >= 30) left = Moved(hand);
                else if (loss == Loss.OtherHand) right = other.Hand(game, tick);
                // The tick recovery ends on counts as recovering: that is where a skip would happen.
                bool before = game.Session?.Recovery != null;
                game.Step(new HandFrame(tick, tick * 16667, left, right));
                outcome.Events.AddRange(game.Events);
                var phase = game.Session?.Recovery?.Phase;
                // While frozen, Pip holds still: he never follows the hand that came back somewhere else.
                if ((phase == RecoveryPhase.Frozen || phase == RecoveryPhase.Aligning) && !game.Pip.Equals(game.PreviousPip)) watch.Problems.Add($"tick {tick}: Pip moved while frozen");
                if (loss == Loss.OtherHand && game.Session?.Owner == HandSide.Right && outcome.OwnedAtLoss) watch.Problems.Add($"tick {tick}: the other hand took over");
                watch.Observe(game, before || game.Session?.Recovery != null);
            }
            // Back to normal: the owning hand returns where it was.
            for (int i = 0; i < 2000 && game.Session?.Recovery != null; i++)
            {
                player.Step(game); outcome.Events.AddRange(game.Events); watch.Observe(game, true);
            }
            outcome.IslandAfterRecovery = game.PipIsland;
            outcome.Recovered = outcome.Events.Any(e => e.Kind == GameEventKind.RecoveryCompleted);
            outcome.Returned = outcome.Events.Any(e => e.Kind == GameEventKind.ReturningToCheckpoint);
            if (finish)
            {
                for (int i = 0; i < 40000 && !game.IsComplete; i++) { player.Step(game); watch.Observe(game, false); }
                outcome.Complete = game.IsComplete;
            }
            outcome.Problems = watch.Problems;
            return outcome;
        }

        // Every `stride` ticks across a window, its first and last tick included.
        public static IEnumerable<long> Points(Window w, int stride)
        {
            for (long tick = w.Start; tick < w.End; tick += stride) yield return tick;
            yield return w.End;
        }

        [Test] public void TheBaselineCoversEveryRoute()
        {
            var windows = Baseline();
            var hands = windows.Where(w => w.Hand).ToList();
            Assert.That(hands.Select(w => w.Puzzle).Distinct(), Is.EquivalentTo(new[] { 0, 1, 2, 3, 4, 5 }), string.Join("; ", hands));
            Assert.That(hands.Where(w => w.Puzzle == 3).Select(w => w.Edge), Has.Member("C-L1"), "the fork");
            Assert.That(windows.Where(w => !w.Hand).Select(w => w.Edge), Is.SupersetOf(new[] { "Walking", "TravelersCrossing", "BramCrossing" }));
        }

        // The exhaustive sweep: every hand window, every Stride ticks, in every loss style.
        [Test] public void LosingTrackingAnywhereOnAHandRouteNeverSkipsAnIsland([Values] Loss loss)
        {
            var failures = new List<string>(); int runs = 0, owned = 0, recovered = 0, returned = 0;
            foreach (var window in Baseline().Where(w => w.Hand))
                foreach (long tick in Points(window, Stride))
                {
                    var o = Run(window, tick, loss); runs++;
                    if (o.OwnedAtLoss) { owned++; if (o.Recovered) recovered++; if (o.Returned) returned++; }
                    string at = $"{window} at {tick} ({loss})";
                    if (o.Problems.Count > 0) failures.Add($"{at}: {o.Problems[0]}");
                    if (!o.Complete) failures.Add($"{at}: valley not finished");
                    if (!o.OwnedAtLoss) continue;
                    // A short loss realigns; a long one returns Pip to his last island. Either way it resolves.
                    if ((loss == Loss.Brief || loss == Loss.LowConfidence || loss == Loss.Moved || loss == Loss.OtherHand) && o.Returned && !o.Events.Any(e => e.Kind == GameEventKind.Refused))
                        failures.Add($"{at}: a short loss returned to the checkpoint");
                    if (loss == Loss.Long && !o.Returned) failures.Add($"{at}: a long loss did not return to the checkpoint");
                    if (o.Returned && o.IslandAfterRecovery != o.IslandAtLoss) failures.Add($"{at}: returned to {o.IslandAfterRecovery}, not {o.IslandAtLoss}");
                }
            Assert.That(runs, Is.GreaterThan(1200 / Stride)); Assert.That(owned, Is.GreaterThan(1200 / Stride));
            // The sweep really exercises both ways out: short losses realign, long ones go back to the checkpoint.
            if (loss == Loss.Long) Assert.That(returned, Is.EqualTo(owned));
            else Assert.That(recovered, Is.GreaterThan(owned * 9 / 10), $"{recovered} of {owned} realigned");
            Assert.That(failures, Is.Empty, string.Join("\n", failures.Take(20)));
        }

        // Losing tracking while Pip walks, travelers cross or Bram crosses changes nothing: they walk bridges, not hands.
        [Test] public void LosingTrackingWhileTravelersWalkChangesNothing([Values(Loss.Brief, Loss.Long)] Loss loss)
        {
            var failures = new List<string>();
            foreach (var window in Baseline().Where(w => !w.Hand))
                foreach (long tick in Points(window, Math.Max(1, (int)(window.End - window.Start) / 3)))
                {
                    var o = Run(window, tick, loss);
                    if (o.Problems.Count > 0) failures.Add($"{window} at {tick}: {o.Problems[0]}");
                    if (!o.Complete) failures.Add($"{window} at {tick}: valley not finished");
                }
            Assert.That(failures, Is.Empty, string.Join("\n", failures.Take(20)));
        }

        // T5's example: losing tracking while Pip is carried in Board sends him back to S, not A.
        [Test] public void LosingTrackingDuringTheBoardCarryReturnsPipToS()
        {
            var carry = Baseline().First(w => w.Hand && w.Puzzle == 0);
            long middle = (carry.Start + carry.End) / 2;
            var o = Run(carry, middle, Loss.Long, finish: false);
            Assert.That(o.Returned, Is.True); Assert.That(o.Problems, Is.Empty);
            Assert.That(o.Events.Select(e => e.Kind), Has.No.Member(GameEventKind.CheckpointReached));
        }
    }

    // T18: the quality line and the loosened stillness, in the valley.
    public class QualityPromptTests
    {
        private static HandSample Dim(HandSample hand)
        {
            if (hand == null) return null;
            var joints = new JointSample[26];
            for (int i = 0; i < 26; i++) { var j = hand.Joints[i]; joints[i] = new JointSample(j.Position, j.Rotation, j.Radius, j.Flags & ~JointFlags.HighConfidence); }
            return new HandSample(hand.Side, joints);
        }

        [Test] public void TwentySecondsOfPoorTrackingShowsOneLineAndLoosensStillnessOnce()
        {
            var player = new TestPlayer(); var game = new ValleySimulation(TestValley.Chapter1()); double connect = game.Settings.ConnectDistance;
            Assert.That(player.RunUntil(game, () => game.PuzzleIndex == 1 && game.Session?.Build?.Progress > 0.2), Is.True);
            int prompts = 0; bool sawRecovering = false;
            for (int i = 0; i < 1300; i++)
            {
                long tick = game.Tick + 1; game.Step(new HandFrame(tick, tick * 16667, Dim(player.Hand(game, tick)), null));
                prompts += game.Events.Count(e => e.Kind == GameEventKind.QualityPromptDue);
                sawRecovering |= new ValleyPresentation(game, 1).Recovering;
                if (i == 1198) Assert.That(game.QualityPromptShowing, Is.False, "not before 20 s");
            }
            Assert.That(sawRecovering, Is.True, "the ghost and Bram's gesture come first");
            Assert.That(prompts, Is.EqualTo(1));
            var shown = new ValleyPresentation(game, 1);
            Assert.That(shown.QualityPrompt && shown.StillnessLoosened, Is.True);
            Assert.That(game.Settings.ConnectDistance, Is.EqualTo(connect), "connections never loosen");
            // Good tracking clears the line after 3 s; the icon stays. Poor tracking again never shows it twice.
            player.Step(game, HintWatcher.PromptClearTicks + 1);
            Assert.That(game.QualityPromptShowing, Is.False); Assert.That(game.StillnessLoosened, Is.True);
            for (int i = 0; i < 1300; i++)
            {
                long tick = game.Tick + 1; game.Step(new HandFrame(tick, tick * 16667, Dim(player.Hand(game, tick)), null));
                prompts += game.Events.Count(e => e.Kind == GameEventKind.QualityPromptDue);
            }
            Assert.That(prompts, Is.EqualTo(1)); Assert.That(game.QualityPromptShowing, Is.False);
            Assert.That(player.RunUntil(game, () => game.IsComplete), Is.True);
        }

        [Test] public void GoodTrackingNeverShowsTheLine()
        {
            var player = new TestPlayer(); var game = new ValleySimulation(TestValley.Chapter1());
            Assert.That(player.RunUntil(game, () => game.IsComplete), Is.True);
            Assert.That(player.Count(GameEventKind.QualityPromptDue), Is.Zero); Assert.That(game.StillnessLoosened, Is.False);
        }

        // Only the owning hand's route joints count while it owns the interaction: the other hand being out of
        // view is not poor tracking, and the owner going missing is, even with the other hand in view.
        [Test] public void PoorTrackingIsTheOwningHands()
        {
            var player = new TestPlayer(); var other = new TestPlayer { Side = HandSide.Right }; var game = new ValleySimulation(TestValley.Chapter1());
            Assert.That(player.RunUntil(game, () => game.PuzzleIndex == 1 && game.Session?.Owner == HandSide.Left), Is.True);
            player.Step(game); Assert.That(game.Session.PoorTracking, Is.False);
            long tick = game.Tick + 1; game.Step(new HandFrame(tick, tick * 16667, null, other.Hand(game, tick)));
            Assert.That(game.Session.PoorTracking, Is.True);
        }
    }
}
