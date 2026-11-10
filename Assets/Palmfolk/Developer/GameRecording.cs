#if PALMFOLK_DEVELOPMENT
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Palmfolk.Core;

namespace Palmfolk.Developer
{
    // A running fingerprint of everything the game decided (T17a): each tick's events, stage, puzzle, director stage
    // and Pip's position. Two runs with equal digests made the same decisions at the same ticks.
    public sealed class DecisionDigest
    {
        private const ulong Offset = 14695981039346656037UL, Prime = 1099511628211UL;
        public ulong Value { get; private set; } = Offset;
        public long Ticks { get; private set; }
        public long EventCount { get; private set; }
        private void Mix(long v) { for (int i = 0; i < 8; i++) { Value ^= (byte)(v >> (8 * i)); Value *= Prime; } }
        private void Mix(double v) => Mix(BitConverter.DoubleToInt64Bits(v));
        public void Observe(PalmfolkGame game)
        {
            Mix(game.Tick); Mix((int)game.Stage); Mix(game.Plantings); Mix(game.IsPaused ? 1 : 0);
            foreach (var e in game.Events) { Mix(e.Tick); Mix((int)e.Kind); EventCount++; }
            var valley = game.Valley;
            if (valley != null) { Mix((int)valley.Stage); Mix(valley.PuzzleIndex); Mix(valley.Pip.X); Mix(valley.Pip.Y); Mix(valley.Pip.Z); }
            var puzzle = game.Puzzle;
            if (puzzle != null) { Mix((int)puzzle.Phase); Mix(puzzle.Pip.X); Mix(puzzle.Pip.Y); Mix(puzzle.Pip.Z); }
            if (game.Pose != null) { Mix(game.Pose.Origin.X); Mix(game.Pose.Origin.Z); Mix(game.Pose.Scale); }
            Ticks++;
        }
    }

    // A recording of the whole game (format 2): world-space frames fed to PalmfolkGame from an opened save. Holds
    // the live run's digest and final save so a replay proves it made the same decisions.
    public sealed class GameRecording
    {
        public string BuildVersion { get; }
        public string ContentId { get; }
        public GameSettings Settings => settings.Copy();
        private readonly GameSettings settings;
        private readonly byte[] initial;
        public SaveData Initial => SaveCodec.DecodeSave(initial);
        public ValleyPose Pose { get; }
        public long FirstTick { get; }
        public IReadOnlyList<HandFrame> Frames { get; }
        public bool Partial { get; }
        public ulong Digest { get; }
        private readonly byte[] final;
        public byte[] FinalSave => (byte[])final.Clone();

        public GameRecording(string buildVersion, string contentId, GameSettings settings, SaveData initial, ValleyPose pose, long firstTick, HandFrame[] frames, bool partial, ulong digest, byte[] finalSave)
            : this(buildVersion, contentId, settings, SaveCodec.EncodeSave(initial ?? throw new ArgumentNullException(nameof(initial))), pose, firstTick, frames, partial, digest, finalSave) { }

        internal GameRecording(string buildVersion, string contentId, GameSettings settings, byte[] initialSave, ValleyPose pose, long firstTick, HandFrame[] frames, bool partial, ulong digest, byte[] finalSave)
        {
            if (string.IsNullOrWhiteSpace(buildVersion) || buildVersion.Length > 128 || string.IsNullOrWhiteSpace(contentId) || contentId.Length > 128)
                throw new ArgumentException("Invalid recording header.");
            if (frames == null || frames.Length > 36000 || finalSave == null) throw new ArgumentException("Invalid recording duration.");
            this.settings = settings?.Copy() ?? throw new ArgumentNullException(nameof(settings)); this.settings.Validate();
            BuildVersion = buildVersion; ContentId = contentId; initial = (byte[])initialSave.Clone(); SaveCodec.DecodeSave(initial);
            Pose = pose; FirstTick = firstTick; Partial = partial; Digest = digest; final = (byte[])finalSave.Clone();
            long tick = firstTick + 1, timestamp = -1;
            foreach (var frame in frames)
            {
                if (frame == null || frame.Tick != tick++ || frame.TimestampMicroseconds < timestamp) throw new ArgumentException("Nonsequential recording.");
                timestamp = frame.TimestampMicroseconds;
            }
            Frames = Array.AsReadOnly((HandFrame[])frames.Clone());
        }
    }

    // Wraps the live game: steps it, keeps the frames (up to the limit) and the decision digest. Must start from a
    // game just opened with `PalmfolkGame.Open(valley, initial, pose, settings)`, before its first tick.
    public sealed class GameRecorder : ITickSimulation
    {
        private readonly List<HandFrame> frames = new List<HandFrame>();
        private readonly string buildVersion, contentId;
        private readonly GameSettings settings;
        private readonly byte[] initial;
        private readonly ValleyPose pose;
        private readonly long firstTick;
        private readonly int maximumFrames;
        private readonly PlayerSettings player;
        public PalmfolkGame Game { get; }
        public DecisionDigest Digest { get; } = new DecisionDigest();
        public RadiusWatcher Radii { get; } = new RadiusWatcher();
        public ValleyDropouts Dropouts { get; } = new ValleyDropouts();
        public bool RecordingLimitReached { get; private set; }
        public long Tick => Game.Tick;
        public IReadOnlyList<GameEvent> Events => Game.Events;
        public int FrameCount => frames.Count;

        public GameRecorder(bool testerConsented, string buildVersion, string contentId, PalmfolkGame game, SaveData openedFrom, ValleyPose pose, GameSettings settings, int maximumFrames = 36000)
        {
            if (!testerConsented) throw new InvalidOperationException("Verbal tester consent is required before local recording.");
            if (maximumFrames < 1 || maximumFrames > 36000) throw new ArgumentOutOfRangeException(nameof(maximumFrames));
            Game = game ?? throw new ArgumentNullException(nameof(game));
            if (game.Plantings != 0 || game.Events.Count != 0) throw new InvalidOperationException("Start recording before the game's first tick.");
            this.buildVersion = buildVersion; this.contentId = contentId; this.pose = pose; this.maximumFrames = maximumFrames;
            this.settings = (settings ?? new GameSettings()).Copy();
            var save = openedFrom ?? new SaveData();
            initial = SaveCodec.EncodeSave(save); player = (save.Settings ?? new PlayerSettings()).Copy();
            firstTick = game.Tick;
        }

        public void Step(HandFrame frame)
        {
            Game.Step(frame);
            if (frames.Count < maximumFrames) frames.Add(frame); else RecordingLimitReached = true;
            Digest.Observe(Game); Radii.Observe(frame.Left); Radii.Observe(frame.Right); Dropouts.Observe(Game, frame);
        }

        public GameRecording Finish()
        {
            var opened = SaveCodec.DecodeSave(initial);
            return new GameRecording(buildVersion, contentId, settings, initial, pose, firstTick, frames.ToArray(), RecordingLimitReached,
                Digest.Value, SaveCodec.EncodeSave(Game.Capture(player, opened)));
        }
    }

    public sealed class ReplayResult
    {
        public ulong Digest { get; }
        public byte[] FinalSave { get; }
        public bool SameDecisions { get; }
        public bool SameFinalState { get; }
        public PalmfolkGame Game { get; }
        public ReplayResult(ulong digest, byte[] finalSave, bool sameDecisions, bool sameFinalState, PalmfolkGame game)
        { Digest = digest; FinalSave = finalSave; SameDecisions = sameDecisions; SameFinalState = sameFinalState; Game = game; }
    }

    public static class GameReplay
    {
        // Reopens the recorded save with the same content and settings, feeds every frame, and compares.
        // A partial recording (limit reached) can't be compared: its decisions after the limit weren't recorded.
        public static ReplayResult Run(GameRecording recording, ValleySpec valley)
        {
            if (recording == null || valley == null) throw new ArgumentNullException();
            var opened = recording.Initial;
            var game = PalmfolkGame.Open(valley, opened, recording.Pose, recording.Settings);
            if (game.Tick != recording.FirstTick) throw new InvalidDataException("The recording starts at a different tick.");
            var digest = new DecisionDigest();
            foreach (var frame in recording.Frames) { game.Step(frame); digest.Observe(game); }
            var final = SaveCodec.EncodeSave(game.Capture(opened.Settings, opened));
            bool sameState = final.Length == recording.FinalSave.Length;
            var expected = recording.FinalSave;
            for (int i = 0; sameState && i < final.Length; i++) sameState = final[i] == expected[i];
            return new ReplayResult(digest.Value, final, !recording.Partial && digest.Value == recording.Digest, !recording.Partial && sameState, game);
        }
    }

    public static class GameRecordingCodec
    {
        private const int Magic = 0x50465232, Version = 2;

        public static byte[] Encode(GameRecording recording)
        {
            using (var stream = new MemoryStream()) using (var w = new BinaryWriter(stream))
            {
                w.Write(Magic); w.Write(Version); w.Write(GameSettings.TickRate); w.Write(recording.BuildVersion); w.Write(recording.ContentId);
                WriteSettings(w, recording.Settings);
                var initial = SaveCodec.EncodeSave(recording.Initial); w.Write(initial.Length); w.Write(initial);
                w.Write(recording.Pose != null);
                if (recording.Pose != null) { SaveCodec.WriteVector(w, recording.Pose.Origin); SaveCodec.WriteVector(w, recording.Pose.Forward); w.Write(recording.Pose.Scale); }
                w.Write(recording.FirstTick); w.Write(recording.Partial); w.Write(recording.Digest);
                var final = recording.FinalSave; w.Write(final.Length); w.Write(final);
                w.Write(recording.Frames.Count);
                foreach (var f in recording.Frames)
                {
                    w.Write(f.Tick); w.Write(f.TimestampMicroseconds); w.Write(f.HasFocus); w.Write((int)f.Command); w.Write(f.ControllersInHand);
                    RecordingCodec.WriteHand(w, f.Left); RecordingCodec.WriteHand(w, f.Right);
                }
                using (var sha = SHA256.Create()) w.Write(sha.ComputeHash(stream.ToArray()));
                return stream.ToArray();
            }
        }

        public static GameRecording Decode(byte[] data, string expectedBuildVersion)
        {
            if (data == null || data.Length < 44 || data.Length > 256 * 1024 * 1024) throw new InvalidDataException("Invalid recording size.");
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(data, 0, data.Length - 32); int difference = 0;
                for (int i = 0; i < 32; i++) difference |= hash[i] ^ data[data.Length - 32 + i];
                if (difference != 0) throw new InvalidDataException("Recording checksum mismatch.");
            }
            try
            {
                using (var stream = new MemoryStream(data, 0, data.Length - 32, false)) using (var r = new BinaryReader(stream))
                {
                    if (r.ReadInt32() != Magic || r.ReadInt32() != Version || r.ReadInt32() != GameSettings.TickRate) throw new InvalidDataException("Unsupported recording header.");
                    string build = r.ReadString(); if (build != expectedBuildVersion) throw new InvalidDataException("Replay requires the same build version.");
                    string content = r.ReadString();
                    var settings = ReadSettings(r);
                    var initial = r.ReadBytes(Count(r, 1024 * 1024));
                    ValleyPose pose = null;
                    if (r.ReadBoolean()) pose = new ValleyPose(SaveCodec.ReadVector(r), SaveCodec.ReadVector(r), r.ReadDouble());
                    long firstTick = r.ReadInt64(); bool partial = r.ReadBoolean(); ulong digest = r.ReadUInt64();
                    var final = r.ReadBytes(Count(r, 1024 * 1024));
                    var frames = new HandFrame[Count(r, 36000)];
                    for (int i = 0; i < frames.Length; i++)
                    {
                        long tick = r.ReadInt64(), timestamp = r.ReadInt64(); bool focus = r.ReadBoolean(); int command = r.ReadInt32(); bool controllers = r.ReadBoolean();
                        if (!Enum.IsDefined(typeof(InputCommand), command)) throw new InvalidDataException("Unknown command.");
                        frames[i] = new HandFrame(tick, timestamp, RecordingCodec.ReadHand(r, HandSide.Left), RecordingCodec.ReadHand(r, HandSide.Right), focus, (InputCommand)command, controllers);
                    }
                    if (stream.Position != stream.Length) throw new InvalidDataException("Trailing recording data.");
                    return new GameRecording(build, content, settings, initial, pose, firstTick, frames, partial, digest, final);
                }
            }
            catch (Exception e) when (e is EndOfStreamException || e is ArgumentException || e is OverflowException) { throw new InvalidDataException("Invalid recording data.", e); }
        }

        private static int Count(BinaryReader r, int max) { int n = r.ReadInt32(); if (n < 0 || n > max) throw new InvalidDataException("Recording collection limit exceeded."); return n; }

        // Every GameSettings value, so a replay runs with exactly the recorded rules.
        private static void WriteSettings(BinaryWriter w, GameSettings s)
        {
            w.Write(s.ConnectDistance); w.Write(s.BreakDistance); w.Write(s.ConnectTicks); w.Write(s.StillnessTicks); w.Write(s.StillnessSpeed); w.Write(s.TremorDistance);
            w.Write(s.BuildTicks); w.Write(s.ScaffoldTicks); w.Write(s.RemovalTicks); w.Write(s.AlignmentTicks); w.Write(s.BlendTicks); w.Write(s.RecoveryTicks); w.Write(s.ReturnTicks);
            w.Write(s.TravelerSpeed); w.Write(s.MaximumRouteLength); w.Write(s.FilterCutoff); w.Write(s.FilterBeta); w.Write(s.UseJointRadii); w.Write(s.StillnessToleranceStep);
            w.Write(s.PlantTicks); w.Write(s.PlantMaximumTiltDegrees); w.Write(s.FingerStraightness); w.Write(s.PalmHalfThickness);
            w.Write(s.ReferenceHandLength); w.Write(s.MinimumHandScale); w.Write(s.MaximumHandScale); w.Write(s.WorldScaleAdjustment);
            foreach (var rule in new[] { s.WalkerRule, s.GroupRule, s.CartRule }) { w.Write(rule.MinimumWidth); w.Write(rule.MaximumSlopeDegrees); w.Write(rule.MinimumClearance); }
            w.Write(s.EndRampLength);
        }
        private static GameSettings ReadSettings(BinaryReader r)
        {
            var s = new GameSettings { ConnectDistance = r.ReadDouble(), BreakDistance = r.ReadDouble(), ConnectTicks = r.ReadInt32(), StillnessTicks = r.ReadInt32(), StillnessSpeed = r.ReadDouble(), TremorDistance = r.ReadDouble(),
                BuildTicks = r.ReadInt32(), ScaffoldTicks = r.ReadInt32(), RemovalTicks = r.ReadInt32(), AlignmentTicks = r.ReadInt32(), BlendTicks = r.ReadInt32(), RecoveryTicks = r.ReadInt32(), ReturnTicks = r.ReadInt32(),
                TravelerSpeed = r.ReadDouble(), MaximumRouteLength = r.ReadDouble(), FilterCutoff = r.ReadDouble(), FilterBeta = r.ReadDouble(), UseJointRadii = r.ReadBoolean(), StillnessToleranceStep = r.ReadInt32(),
                PlantTicks = r.ReadInt32(), PlantMaximumTiltDegrees = r.ReadDouble(), FingerStraightness = r.ReadDouble(), PalmHalfThickness = r.ReadDouble(),
                ReferenceHandLength = r.ReadDouble(), MinimumHandScale = r.ReadDouble(), MaximumHandScale = r.ReadDouble(), WorldScaleAdjustment = r.ReadDouble() };
            s.WalkerRule = new CapacityRule(r.ReadDouble(), r.ReadDouble(), r.ReadDouble());
            s.GroupRule = new CapacityRule(r.ReadDouble(), r.ReadDouble(), r.ReadDouble());
            s.CartRule = new CapacityRule(r.ReadDouble(), r.ReadDouble(), r.ReadDouble());
            s.EndRampLength = r.ReadDouble();
            s.Validate(); return s;
        }
    }
}
#endif
