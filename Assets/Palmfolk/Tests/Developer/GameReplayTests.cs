#if PALMFOLK_DEVELOPMENT
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Palmfolk.Core;
using Palmfolk.Developer;

namespace Palmfolk.Tests
{
    // D6/T17a: a live session through PalmfolkGame (planting, world-space hands, a save) replays with identical decisions.
    public class GameReplayTests
    {
        private static GameRecording recorded;
        private static PalmfolkGame recordedGame;

        private static GameRecording Record()
        {
            if (recorded != null) return recorded;
            var save = new SaveData(); var settings = new GameSettings();
            var game = PalmfolkGame.Open(TestValley.Chapter1(), save, null, settings);
            var recorder = new GameRecorder(true, "test-build", "test-chapter", game, save, null, settings);
            // A pause and resume in the middle, as the waystone would send them.
            LiveInputTests.LivePlay.Run(game, recorder, 72, g => g.Valley.IsComplete,
                onFrame: (source, frame) => { if (frame == 3000) source.Request(InputCommand.Pause); if (frame == 3100) source.Request(InputCommand.Resume); });
            Assert.That(game.Valley.IsComplete, Is.True);
            recordedGame = game; recorded = recorder.Finish();
            return recorded;
        }

        // D7/T19: a 330 ms loss mid-build is one disruptive dropout for that puzzle; a 130 ms one is not a dropout.
        [Test] public void DropoutsAreCountedPerPuzzle()
        {
            var save = new SaveData(); var settings = new GameSettings();
            var game = PalmfolkGame.Open(TestValley.Chapter1(), save, null, settings);
            var recorder = new GameRecorder(true, "test-build", "test-chapter", game, save, null, settings);
            long longFrom = -1, shortFrom = -1;
            LiveInputTests.LivePlay.Run(game, recorder, 60, g => g.Valley.IsComplete, distort: (tick, hand) =>
            {
                var v = game.Valley;
                if (longFrom < 0 && v.PuzzleIndex == 1 && v.Session?.Build?.Progress > 0.3) longFrom = tick;
                if (shortFrom < 0 && v.PuzzleIndex == 2 && v.Session?.Build?.Progress > 0.3) shortFrom = tick;
                bool drop = (longFrom >= 0 && tick < longFrom + 20) || (shortFrom >= 0 && tick < shortFrom + 8);
                return drop ? null : hand;
            });
            Assert.That(game.Valley.IsComplete, Is.True); Assert.That(longFrom, Is.GreaterThan(0)); Assert.That(shortFrom, Is.GreaterThan(0));
            var d = recorder.Dropouts;
            Assert.That(d.Puzzle(1).Count, Is.EqualTo(1)); Assert.That(d.Puzzle(1).DisruptiveCount, Is.EqualTo(1));
            Assert.That(d.Puzzle(2).Count, Is.Zero);
            Assert.That(d.Count, Is.EqualTo(1)); Assert.That(d.DisruptiveCount, Is.EqualTo(1));
            var log = d.Log();
            Assert.That(log, Does.StartWith("puzzle,dropouts,disruptive")); Assert.That(log, Does.Contain("\n1,1,1"));
            Assert.That(log.Split('\n').Count(l => l.StartsWith("1,") && l.Contains(",True,")), Is.EqualTo(1));
        }

        [Test] public void AWholeLiveValleyReplaysWithIdenticalDecisions()
        {
            var recording = Record();
            Assert.That(recording.Partial, Is.False, $"{recording.Frames.Count} frames");
            var bytes = GameRecordingCodec.Encode(recording);
            var decoded = GameRecordingCodec.Decode(bytes, "test-build");
            Assert.That(GameRecordingCodec.Encode(decoded), Is.EqualTo(bytes));
            var replay = GameReplay.Run(decoded, TestValley.Chapter1());
            Assert.That(replay.SameDecisions, Is.True); Assert.That(replay.SameFinalState, Is.True);
            Assert.That(replay.Game.Valley.IsComplete, Is.True);
            Assert.That(replay.Game.Pose.Origin, Is.EqualTo(recordedGame.Pose.Origin));
            Assert.That(replay.Game.Valley.Bridges.Keys, Is.EquivalentTo(recordedGame.Valley.Bridges.Keys));
        }

        [Test] public void AChangedFrameChangesTheDecisions()
        {
            var recording = Record();
            // Move the hand 5 cm for two seconds of the first bridge build.
            var frames = recording.Frames.ToArray(); int at = Array.FindIndex(frames, f => f.Left != null && f.Tick > 600);
            Assert.That(at, Is.GreaterThan(0));
            for (int i = at; i < at + 120; i++)
            {
                var f = frames[i];
                frames[i] = new HandFrame(f.Tick, f.TimestampMicroseconds, f.Left == null ? null : Planted.Map(f.Left, j => j.Position + new Vec3(0.05, 0, 0)),
                    f.Right == null ? null : Planted.Map(f.Right, j => j.Position + new Vec3(0.05, 0, 0)), f.HasFocus, f.Command, f.ControllersInHand);
            }
            var changed = new GameRecording(recording.BuildVersion, recording.ContentId, recording.Settings, recording.Initial, recording.Pose, recording.FirstTick, frames,
                false, recording.Digest, recording.FinalSave);
            Assert.That(GameReplay.Run(changed, TestValley.Chapter1()).SameDecisions, Is.False);
        }

        [Test] public void AResumedVrSessionRecordsAndReplays()
        {
            // Open a save mid-valley at a known pose (VR mode), play to the end, replay.
            var first = new ValleySimulation(TestValley.Chapter1()); var player = new TestPlayer();
            Assert.That(player.RunUntil(first, () => first.PuzzleIndex == 3), Is.True);
            var pose = new ValleyPose(new Vec3(0.1, 0.9, 0.3), new Vec3(-1, 0, 1), 1.2);
            var save = new SaveData { Valley = first.Progress, Mode = ValleyMode.VR, Pose = pose, Settings = new PlayerSettings { StillnessToleranceStep = 2 } };
            var settings = new GameSettings();
            var game = PalmfolkGame.Open(TestValley.Chapter1(), save, pose, settings);
            var recorder = new GameRecorder(true, "b", "test-chapter", game, save, pose, settings);
            LiveInputTests.LivePlay.Run(game, recorder, 90, g => g.Valley.IsComplete);
            Assert.That(game.Valley.IsComplete, Is.True); Assert.That(game.Plantings, Is.EqualTo(0));
            var replay = GameReplay.Run(GameRecordingCodec.Decode(GameRecordingCodec.Encode(recorder.Finish()), "b"), TestValley.Chapter1());
            Assert.That(replay.SameDecisions, Is.True, "decisions"); Assert.That(replay.SameFinalState, Is.True, "final state");
            Assert.That(replay.Game.Valley.Settings.StillnessToleranceStep, Is.EqualTo(2));
        }

        [Test] public void APartialRecordingIsDisclosedAndNeverClaimsAMatch()
        {
            var save = new SaveData(); var game = PalmfolkGame.Open(TestValley.Chapter1(), save);
            var recorder = new GameRecorder(true, "b", "c", game, save, null, null, maximumFrames: 100);
            LiveInputTests.LivePlay.Run(game, recorder, 60, g => g.Tick >= 300);
            var recording = recorder.Finish();
            Assert.That(recording.Partial, Is.True); Assert.That(recording.Frames.Count, Is.EqualTo(100)); Assert.That(game.Tick, Is.EqualTo(300));
            var replay = GameReplay.Run(recording, TestValley.Chapter1());
            Assert.That(replay.SameDecisions || replay.SameFinalState, Is.False);
        }

        [Test] public void RecordingNeedsConsentAFreshGameAndTheRightBuild()
        {
            var save = new SaveData(); var game = PalmfolkGame.Open(TestValley.Chapter1(), save);
            Assert.Throws<InvalidOperationException>(() => new GameRecorder(false, "b", "c", game, save, null, null));
            var recorder = new GameRecorder(true, "b", "c", game, save, null, null);
            recorder.Step(Planted.Frame(0));
            Assert.Throws<InvalidOperationException>(() => new GameRecorder(true, "b", "c", PlantedOnce(), save, null, null));
            var bytes = GameRecordingCodec.Encode(recorder.Finish());
            Assert.Throws<InvalidDataException>(() => GameRecordingCodec.Decode(bytes, "other"));
            var copy = (byte[])bytes.Clone(); copy[copy.Length / 2] ^= 4;
            Assert.Throws<InvalidDataException>(() => GameRecordingCodec.Decode(copy, "b"));
            Assert.Throws<InvalidDataException>(() => GameRecordingCodec.Decode(bytes.Take(bytes.Length - 1).ToArray(), "b"));
        }

        private static PalmfolkGame PlantedOnce()
        {
            var game = new PalmfolkGame(TestValley.Chapter1()); var table = new Planted.Placement();
            for (int i = 0; i < 300 && game.Plantings == 0; i++) game.Step(Planted.Frame(game.Tick + 1, table.Hand(game.Tick + 1)));
            Assert.That(game.Plantings, Is.EqualTo(1));
            return game;
        }
    }
}
#endif
