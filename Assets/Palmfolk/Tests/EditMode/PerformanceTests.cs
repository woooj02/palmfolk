using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using Palmfolk.Core;

namespace Palmfolk.Tests
{
    // D11 (T4, T7): what a tick, a bake and a rendered frame cost, headless. Times are reported, not asserted: they
    // depend on the machine, and the numbers that count come from the headset. Allocations are counted exactly
    // (GC.GetAllocatedBytesForCurrentThread) and asserted where the code promises none.
    public class PerformanceTests
    {
        private sealed class Profile
        {
            private readonly Dictionary<string, (long Count, long Bytes, long Allocating, double Worst, double Total)> rows = new Dictionary<string, (long, long, long, double, double)>();
            public double WorstBake, WorstMs;
            public void Add(string key, long bytes, double ms)
            {
                rows.TryGetValue(key, out var s);
                rows[key] = (s.Count + 1, s.Bytes + bytes, s.Allocating + (bytes > 0 ? 1 : 0), Math.Max(s.Worst, ms), s.Total + ms);
                WorstMs = Math.Max(WorstMs, ms);
            }
            public (long Count, long Bytes, long Allocating) this[string key] => rows.TryGetValue(key, out var s) ? (s.Count, s.Bytes, s.Allocating) : (0, 0, 0);
            public IEnumerable<string> Keys => rows.Keys.ToList();
            public string Report() => string.Join("\n", rows.OrderBy(p => p.Key).Select(p =>
                $"{p.Key,-44} n {p.Value.Count,6}  allocating {p.Value.Allocating,5}  bytes/each {p.Value.Bytes / (double)p.Value.Count,8:F1}  mean {p.Value.Total / p.Value.Count * 1000,6:F1} us  worst {p.Value.Worst,5:F2} ms"))
                + $"\nworst bake tick {WorstBake:F2} ms, worst overall {WorstMs:F2} ms";
        }

        private static string Stage(ValleySimulation g)
        {
            string hand = g.Stage != DirectorStage.Interacting ? "" : g.Session?.Build != null ? "/building" : g.Session?.Owner != null ? "/hand" : "/reaching";
            return g.Stage + hand + (g.IsComplete ? "/after" : "");
        }
        private static bool HandHeld(string key) => key.Contains("/building") || key.Contains("/hand") || key.Contains("/reaching");

        // The valley tick alone: chapter one and all five visitors, played by the scripted player. Run twice so the
        // first run's JIT and list growth don't count.
        private static Profile Ticks()
        {
            Profile profile = null;
            for (int run = 0; run < 2; run++)
            {
                profile = new Profile();
                var game = new ValleySimulation(TestValley.WithVisitors()); var player = new TestPlayer();
                var watch = new Stopwatch();
                for (int i = 0; i < 200000 && game.VisitorsHelped != 0b11111; i++)
                {
                    long tick = game.Tick + 1; var hand = player.Hand(game, tick);
                    var frame = new HandFrame(tick, tick * 16667, hand, null, true,
                        game.IsComplete && game.Visitor == null && game.Stage == DirectorStage.Complete ? InputCommand.RingBell : InputCommand.None);
                    string key = Stage(game); var walks = game.Residents.ToList(); var arriving = game.VisitorArrival; var bram = game.Bram;
                    long before = GC.GetAllocatedBytesForCurrentThread(); watch.Restart();
                    game.Step(frame);
                    watch.Stop(); long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
                    double ms = watch.Elapsed.TotalMilliseconds;
                    // A resident (or Bram) setting off on a new walk, or the visitor setting off, is a change too: their way is worked out then.
                    bool setOff = !game.Residents.SequenceEqual(walks) || !ReferenceEquals(arriving, game.VisitorArrival) || !ReferenceEquals(bram, game.Bram);
                    profile.Add(game.Events.Count > 0 || Stage(game) != key || setOff ? key + " (changed)" : key, bytes, ms);
                    foreach (var e in game.Events)
                    {
                        if (e.Kind == GameEventKind.BridgeBaked) profile.WorstBake = Math.Max(profile.WorstBake, ms);
                        player.Events.Add(e);
                        if (e.Kind == GameEventKind.Refused && game.Session != null) { player.Attempts.TryGetValue(game.Session.Edge, out int a); player.Attempts[game.Session.Edge] = a + 1; }
                    }
                }
                Assert.That(game.VisitorsHelped, Is.EqualTo(0b11111));
            }
            return profile;
        }

        // Ticks where nothing changes and no hand holds Pip (walking, crossing, widening, residents) allocate nothing.
        // A tick that changes the stage or raises events may (a new session, a bake, a saved bridge).
        [Test] public void QuietTicksAllocateNothing()
        {
            var profile = Ticks();
            TestContext.Out.WriteLine(profile.Report());
            foreach (var key in profile.Keys.Where(k => !k.Contains("(changed)") && !HandHeld(k)))
                Assert.That(profile[key].Allocating, Is.Zero, key + "\n" + profile.Report());
            Assert.That(profile["Walking"].Count, Is.GreaterThan(1000), "the run covers the quiet stages");
            Assert.That(profile["TravelersCrossing/after"].Count, Is.GreaterThan(1000), "and the residents after the finale");
        }

        // While a hand holds Pip, a tick makes the live hand route (the snapshot the views draw and the build compares
        // against) and little else: well under the 7 KB a tick made before D11.
        [Test] public void HandTicksMakeOnlyTheLiveRoute()
        {
            var profile = Ticks();
            foreach (var key in profile.Keys.Where(k => !k.Contains("(changed)") && HandHeld(k)))
            {
                var row = profile[key];
                Assert.That(row.Bytes / (double)row.Count, Is.LessThan(1600), key + "\n" + profile.Report());
            }
            // The bake runs validation for the whole tree in one tick: no hitch (T4) on this machine.
            Assert.That(profile.WorstBake, Is.LessThan(5), profile.Report());
        }

        // A rendered frame without the engine: the live source, the 60 Hz clock and the planted game, need signs and
        // look and pinch with a head gaze, and the presentation the views read. Hands come from the test player
        // (their making is not counted: on the headset the reader refills its buffers).
        [Test] public void ARenderedFrameIsProfiled()
        {
            var game = new PalmfolkGame(TestValley.Chapter1()); var player = new TestPlayer();
            var table = new Planted.Placement { Height = 0.74, YawDegrees = 37, HandLength = 0.2, Offset = new Vec3(0.2, 0, -0.35) };
            for (int i = 0; i < 300 && game.Stage == ValleyStage.Planting; i++) game.Step(Planted.Frame(game.Tick + 1, table.Hand(game.Tick + 1)));
            Assert.That(game.Stage, Is.EqualTo(ValleyStage.Playing));
            var source = new LiveFrameSource(); var clock = new FixedTickRunner();
            var targets = new List<GazeTarget>(); var looks = new LookWatcher(); var pinch = new LookAndPinch();
            var buttons = new[] { new GazeButton("bell", game.Pose.ToWorld(new Vec3(0, 0.3, 0.3)), 8) };
            var profile = new Profile(); var watch = new Stopwatch();
            const double hz = 72;
            for (int frame = 0; frame < 200000 && !game.Valley.IsComplete; frame++)
            {
                var local = player.Hand(game.Valley, game.Tick + 1);
                var world = local == null ? null : game.Pose.ToWorld(local);
                var head = game.Pose.ToWorld(new Vec3(0, 0.35, -0.3));
                var t = NeedSigns.Targets(game.Valley, game.Pose);
                var gaze = new GazeSample(head, (t.Count > 0 ? t[0].Position : game.Pose.Origin) - head, GazeSource.Head);
                string key = Stage(game.Valley);
                long before = GC.GetAllocatedBytesForCurrentThread(); watch.Restart();
                source.Sample(world, null, (long)(frame / hz * 1e6), true, false);
                clock.Advance(1 / hz, source, game);
                long afterTick = GC.GetAllocatedBytesForCurrentThread();
                NeedSigns.Targets(game.Valley, game.Pose, targets);
                looks.Step(1 / hz, gaze, targets); pinch.Step(gaze, source.Left, source.Right, buttons);
                long afterGaze = GC.GetAllocatedBytesForCurrentThread();
                var view = new ValleyPresentation(game, clock.InterpolationAlpha);
                watch.Stop(); long end = GC.GetAllocatedBytesForCurrentThread();
                double ms = watch.Elapsed.TotalMilliseconds;
                bool changed = clock.Events.Count > 0 || Stage(game.Valley) != key;
                string suffix = changed ? " (changed)" : "";
                profile.Add("tick " + key + suffix, afterTick - before, ms);
                profile.Add("gaze " + key + suffix, afterGaze - afterTick, 0);
                profile.Add("view " + key + suffix, end - afterGaze, 0);
                GC.KeepAlive(view);
            }
            Assert.That(game.Valley.IsComplete, Is.True);
            TestContext.Out.WriteLine(profile.Report());
            // Gaze on a still target allocates nothing; the clock and game add nothing to the valley tick when quiet.
            foreach (var key in profile.Keys.Where(k => !k.Contains("(changed)") && !HandHeld(k) && (k.StartsWith("gaze") || k.StartsWith("tick"))))
                Assert.That(profile[key].Allocating, Is.Zero, key + "\n" + profile.Report());
            Assert.Pass(profile.Report());
        }
    }
}
