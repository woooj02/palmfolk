using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Palmfolk.Core;

namespace Palmfolk.Tests
{
    // D5: Save v2 (T9, MR3).
    public class SaveV2Tests
    {
        private static ValleyProgress finished;
        private static ValleyProgress Finished()
        {
            if (finished != null) return finished.Copy();
            var game = new ValleySimulation(TestValley.Chapter1()); var player = new TestPlayer();
            Assert.That(player.RunUntil(game, () => game.IsComplete), Is.True);
            finished = game.Progress;
            finished.CurrentVisitor = 2; finished.VisitorsHelped = 0b011; finished.LitWindows = 0b011;
            finished.BridgeMarks["S-A"] = 0b001; finished.BridgeMarks["C-L1"] = 0b010;
            return finished.Copy();
        }

        private static SaveData Full() => new SaveData
        {
            Valley = Finished(), Mode = PlayMode.VR, AnchorId = "3f2a9c1e-anchor", Pose = new ValleyPose(new Vec3(0.2, 0.74, -0.35), new Vec3(0.6, 0, 0.8), 1.1),
            Settings = new PlayerSettings { StillnessToleranceStep = 2, WorldScale = 1.25, ReducedMotion = true, Captions = false, HighContrast = true, XRay = true },
            M1 = new M1Simulation(Synthetic.Level).Capture(),
        };

        private static void AssertSameBridges(IReadOnlyDictionary<string, CurveTree> a, IReadOnlyDictionary<string, CurveTree> b)
        {
            Assert.That(b.Keys, Is.EquivalentTo(a.Keys));
            foreach (var id in a.Keys)
            {
                Assert.That(b[id].Parents, Is.EqualTo(a[id].Parents), id);
                for (int c = 0; c < a[id].Curves.Count; c++)
                {
                    Assert.That(b[id].Curves[c].Points, Is.EqualTo(a[id].Curves[c].Points), id);
                    Assert.That(b[id].Curves[c].Widths, Is.EqualTo(a[id].Curves[c].Widths), id);
                    Assert.That(b[id].Curves[c].Finger, Is.EqualTo(a[id].Curves[c].Finger));
                }
            }
        }

        [Test] public void EverythingRoundTripsBitForBit()
        {
            var save = Full(); var bytes = SaveCodec.EncodeSave(save);
            var back = SaveCodec.DecodeSave(bytes);
            Assert.That(SaveCodec.EncodeSave(back), Is.EqualTo(bytes));
            Assert.That(back.LoadedVersion, Is.EqualTo(2));
            AssertSameBridges(save.Valley.Bridges, back.Valley.Bridges);
            Assert.That(back.Valley.Bridges["C-L1"].Curves.Count, Is.EqualTo(2), "fork trees keep their trunk");
            Assert.That(back.Valley.CompletedPuzzles, Is.EqualTo(6)); Assert.That(back.Valley.LedgeWidened, Is.True);
            Assert.That(back.Valley.CurrentVisitor, Is.EqualTo(2)); Assert.That(back.Valley.VisitorsHelped, Is.EqualTo(3)); Assert.That(back.Valley.LitWindows, Is.EqualTo(3));
            Assert.That(back.Valley.BridgeMarks, Is.EquivalentTo(save.Valley.BridgeMarks));
            Assert.That(back.Mode, Is.EqualTo(PlayMode.VR)); Assert.That(back.AnchorId, Is.EqualTo("3f2a9c1e-anchor"));
            Assert.That(back.Pose.Origin, Is.EqualTo(save.Pose.Origin)); Assert.That(back.Pose.Forward, Is.EqualTo(save.Pose.Forward)); Assert.That(back.Pose.Scale, Is.EqualTo(1.1));
            Assert.That(back.Settings.StillnessToleranceStep, Is.EqualTo(2)); Assert.That(back.Settings.WorldScale, Is.EqualTo(1.25));
            Assert.That(back.Settings.ReducedMotion && !back.Settings.Captions && back.Settings.HighContrast && back.Settings.XRay, Is.True);
            Assert.That(SaveCodec.Encode(back.M1), Is.EqualTo(SaveCodec.Encode(save.M1)));
            // An empty save (first launch) round-trips too.
            Assert.That(SaveCodec.DecodeSave(SaveCodec.EncodeSave(new SaveData())).Valley, Is.Null);
        }

        [Test] public void AV1SaveLoadsAsTheM1Slice()
        {
            var game = new M1Simulation(Synthetic.Level); Synthetic.Step(game, Synthetic.Hand(), count: 40);
            var v1 = SaveCodec.Encode(game.Capture());
            var save = SaveCodec.DecodeSave(v1);
            Assert.That(save.LoadedVersion, Is.EqualTo(1)); Assert.That(save.Valley, Is.Null);
            Assert.That(SaveCodec.Encode(save.M1), Is.EqualTo(v1));
            Assert.That(save.Settings.StillnessToleranceStep, Is.EqualTo(1)); Assert.That(save.Mode, Is.EqualTo(PlayMode.Table));
            // Written back, it's a v2 save holding the same M1 state.
            var v2 = SaveCodec.DecodeSave(SaveCodec.EncodeSave(save));
            Assert.That(v2.LoadedVersion, Is.EqualTo(2)); Assert.That(SaveCodec.Encode(v2.M1), Is.EqualTo(v1));
        }

        [Test] public void ARepositoryOfV1FilesMigrates()
        {
            var path = Path.Combine(Path.GetTempPath(), "palmfolk-v1-" + Guid.NewGuid().ToString("N"));
            try
            {
                // Write a v1 slot exactly as the M1 build did: generation, v1 codec bytes, SHA-256 over both.
                Directory.CreateDirectory(path);
                var game = new M1Simulation(Synthetic.Level); Synthetic.Step(game, count: 7);
                using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
                {
                    writer.Write(3L); writer.Write(SaveCodec.Encode(game.Capture()));
                    using (var sha = System.Security.Cryptography.SHA256.Create()) writer.Write(sha.ComputeHash(stream.ToArray()));
                    File.WriteAllBytes(Path.Combine(path, "save-a.bin"), stream.ToArray());
                }
                var repository = new SaveRepository(new DirectorySaveStorage(path));
                Assert.That(repository.Load(out SaveData save), Is.EqualTo(SaveLoadStatus.Loaded));
                Assert.That(save.LoadedVersion, Is.EqualTo(1)); Assert.That(save.M1.Tick, Is.EqualTo(6));
                Assert.That(repository.Load(out M1Snapshot m1), Is.EqualTo(SaveLoadStatus.Loaded)); Assert.That(m1.Tick, Is.EqualTo(6));
                // Saving the valley keeps the M1 state and writes v2 into the other slot.
                save.Valley = Finished(); repository.Save(save);
                Assert.That(repository.Load(out SaveData again), Is.EqualTo(SaveLoadStatus.Loaded));
                Assert.That(again.LoadedVersion, Is.EqualTo(2)); Assert.That(again.M1.Tick, Is.EqualTo(6)); Assert.That(again.Valley.CompletedPuzzles, Is.EqualTo(6));
            }
            finally { if (Directory.Exists(path)) Directory.Delete(path, true); }
        }

        [Test] public void EveryCorruptByteIsCaught()
        {
            var bytes = SaveCodec.EncodeSave(Full()); var random = new Random(9);
            for (int n = 0; n < 400; n++)
            {
                var copy = (byte[])bytes.Clone(); int at = random.Next(copy.Length); copy[at] ^= (byte)(1 << random.Next(8));
                Assert.Throws<InvalidDataException>(() => SaveCodec.DecodeSave(copy), $"byte {at}");
            }
            foreach (int length in new[] { 0, 10, 43, 44, bytes.Length / 2, bytes.Length - 1 })
                Assert.Throws<InvalidDataException>(() => SaveCodec.DecodeSave(bytes.Take(length).ToArray()), $"length {length}");
            Assert.Throws<InvalidDataException>(() => SaveCodec.DecodeSave(bytes.Concat(new byte[] { 0 }).ToArray()));
            Assert.Throws<InvalidDataException>(() => SaveCodec.DecodeSave(null));
        }

        [Test] public void InvalidContentIsNeverWritten()
        {
            Assert.Throws<InvalidDataException>(() => SaveCodec.EncodeSave(new SaveData { AnchorId = "bad\nid" }));
            Assert.Throws<InvalidDataException>(() => SaveCodec.EncodeSave(new SaveData { Settings = new PlayerSettings { WorldScale = 3 } }));
            var v = Finished(); v.LitWindows = 64;
            Assert.Throws<InvalidDataException>(() => SaveCodec.EncodeSave(new SaveData { Valley = v }));
            v = Finished(); v.CompletedPuzzles = 7;
            Assert.Throws<InvalidDataException>(() => SaveCodec.EncodeSave(new SaveData { Valley = v }));
        }

        [Test] public void QuitMidPuzzleAndResumeKeepsBridgesAndFinishes()
        {
            // T9: quit during the Fork (one branch baked, the other refused), reload from disk, finish the valley.
            var player = new TestPlayer { Kinks = (edges, attempt) => attempt == 0 ? edges.Select(e => e == "C-L1" ? 0.06 : 0).ToArray() : null };
            var game = new ValleySimulation(TestValley.Chapter1());
            Assert.That(player.RunUntil(game, () => game.Bridges.ContainsKey("C-L2")), Is.True);
            var path = Path.Combine(Path.GetTempPath(), "palmfolk-resume-" + Guid.NewGuid().ToString("N"));
            try
            {
                var repository = new SaveRepository(new DirectorySaveStorage(path));
                repository.Save(new SaveData { Valley = game.Progress });
                Assert.That(repository.Load(out SaveData save), Is.EqualTo(SaveLoadStatus.Loaded));
                AssertSameBridges(game.Bridges, save.Valley.Bridges);
                var resumed = new ValleySimulation(TestValley.Chapter1(), null, save.Valley);
                Assert.That(resumed.Puzzle.Kind, Is.EqualTo(PuzzleKind.Fork)); Assert.That(resumed.PendingEdges, Is.EqualTo(new[] { "C-L1" }));
                var again = new TestPlayer();
                Assert.That(again.RunUntil(resumed, () => resumed.IsComplete), Is.True);
                Assert.That(again.Count(GameEventKind.CrossingCompleted), Is.EqualTo(3));
            }
            finally { if (Directory.Exists(path)) Directory.Delete(path, true); }
        }

        [Test] public void ASaveFromOtherContentDropsWhatNoLongerExists()
        {
            var progress = Finished(); progress.CompletedPuzzles = 5;
            progress.Bridges["Old-Edge"] = progress.Bridges["S-A"]; progress.BridgeMarks["Old-Edge"] = 1; progress.PipIsland = "Gone";
            var game = new ValleySimulation(TestValley.Chapter1(), null, progress);
            Assert.That(game.Bridges.ContainsKey("Old-Edge"), Is.False); Assert.That(game.Progress.BridgeMarks.ContainsKey("Old-Edge"), Is.False);
            Assert.That(game.PipIsland, Is.EqualTo("S"));
            Assert.That(new TestPlayer().RunUntil(game, () => game.IsComplete), Is.True);
        }

        [Test] public void ASaveMadeInOneModeOpensInTheOther()
        {
            // MR5: mode is only a setting; the bridges are valley-local.
            var table = Full(); table.Mode = PlayMode.Table;
            var opened = SaveCodec.DecodeSave(SaveCodec.EncodeSave(table)); opened.Mode = PlayMode.VR;
            var vr = PalmfolkGame.Open(TestValley.Chapter1(), opened, opened.Pose);
            Assert.That(vr.Stage, Is.EqualTo(ValleyStage.Playing)); Assert.That(vr.Pose, Is.SameAs(opened.Pose));
            AssertSameBridges(table.Valley.Bridges, vr.Valley.Bridges);
            // Table mode without a found anchor replants; the bridges are the same.
            var replant = PalmfolkGame.Open(TestValley.Chapter1(), opened);
            Assert.That(replant.Stage, Is.EqualTo(ValleyStage.Planting)); AssertSameBridges(table.Valley.Bridges, replant.Valley.Bridges);
            var captured = vr.Capture(opened.Settings, opened);
            Assert.That(captured.Mode, Is.EqualTo(PlayMode.VR)); Assert.That(captured.AnchorId, Is.EqualTo(opened.AnchorId));
            AssertSameBridges(table.Valley.Bridges, captured.Valley.Bridges);
        }

        [Test] public void PlayerSettingsApplyToPlay()
        {
            var save = new SaveData { Settings = new PlayerSettings { StillnessToleranceStep = 2, WorldScale = 1.5 } };
            var game = PalmfolkGame.Open(TestValley.Chapter1(), save, ValleyPose.Identity);
            Assert.That(game.Valley.Settings.StillnessToleranceStep, Is.EqualTo(2));
            Assert.That(game.Planting.WorldScale, Is.EqualTo(1.5));
            // Changing the world scale mid-game rescales the valley about its origin.
            game.Apply(new PlayerSettings { StillnessToleranceStep = 0, WorldScale = 0.75 });
            Assert.That(game.Pose.Scale, Is.EqualTo(0.5).Within(1e-12));
            Assert.That(game.Valley.Settings.StillnessToleranceStep, Is.EqualTo(0));
            Assert.Throws<ArgumentException>(() => game.Apply(new PlayerSettings { StillnessToleranceStep = 3 }));
        }

        [Test] public void RestartTakesTheScaffoldDownAndKeepsBridges()
        {
            var game = new ValleySimulation(TestValley.Chapter1()); var player = new TestPlayer();
            Assert.That(player.RunUntil(game, () => game.PuzzleIndex == 2 && game.Session?.Build?.Progress > 0.3), Is.True);
            var bridges = game.Bridges.Keys.ToList();
            player.Step(game, 1, InputCommand.Restart);
            Assert.That(player.Events.Select(e => e.Kind), Has.Member(GameEventKind.ScaffoldRemoving));
            Assert.That(game.Bridges.Keys, Is.EquivalentTo(bridges));
            Assert.That(player.RunUntil(game, () => game.IsComplete), Is.True);
        }
    }
}
