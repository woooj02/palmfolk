#if PALMFOLK_DEVELOPMENT
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Palmfolk.Core;

namespace Palmfolk.Developer
{
    public readonly struct CalibrationRecord
    {
        public readonly double WorldScale, LeftBoneLength, RightBoneLength;
        public CalibrationRecord(double worldScale, double leftBoneLength = 0, double rightBoneLength = 0)
        {
            if (!Vec3.Finite(worldScale) || worldScale <= 0 || !Vec3.Finite(leftBoneLength) || leftBoneLength < 0 || !Vec3.Finite(rightBoneLength) || rightBoneLength < 0) throw new ArgumentException("Invalid calibration record.");
            WorldScale = worldScale; LeftBoneLength = leftBoneLength; RightBoneLength = rightBoneLength;
        }
    }
    public sealed class RecordedSession
    {
        public string BuildVersion { get; }
        public GameSettings Settings => settings.Copy();
        private readonly GameSettings settings;
        public LevelGeometry Level { get; }
        public CalibrationRecord Calibration { get; }
        public M1Snapshot InitialState => SaveCodec.Decode(initialState);
        private readonly byte[] initialState;
        public IReadOnlyList<HandFrame> Frames { get; }
        public bool Partial { get; }
        public RecordedSession(string buildVersion, GameSettings settings, LevelGeometry level, CalibrationRecord calibration, M1Snapshot initial, HandFrame[] frames, bool partial = false)
        {
            if (string.IsNullOrWhiteSpace(buildVersion) || buildVersion.Length > 128 || frames == null || frames.Length > 36000) throw new ArgumentException("Invalid recording header or duration.");
            this.settings = settings.Copy(); this.settings.Validate(); BuildVersion = buildVersion;
            Level = level ?? throw new ArgumentNullException(nameof(level)); Calibration = calibration;
            initialState = SaveCodec.Encode(initial); Frames = Array.AsReadOnly((HandFrame[])frames.Clone()); Partial = partial;
            long tick = initial.Tick + 1, timestamp = -1;
            foreach (var frame in Frames)
            {
                if (frame == null || frame.Tick != tick++ || frame.TimestampMicroseconds < timestamp) throw new ArgumentException("Nonsequential recording.");
                timestamp = frame.TimestampMicroseconds;
            }
        }
    }
    // Both the live recording run and its replay start from the same restored
    // save. No hidden pre-recording filter history influences the first tick.
    public sealed class RecordedRun : ITickSimulation
    {
        private readonly List<HandFrame> frames = new List<HandFrame>();
        private readonly RecordedSession header;
        private readonly M1Simulation game;
        private readonly int maximumFrames;
        private long lastTimestamp = -1;
        public bool RecordingLimitReached { get; private set; }
        public M1Snapshot State => game.Capture();
        public long Tick => game.Tick;
        public WorldPresentation Presentation(double interpolationAlpha) => new WorldPresentation(game, interpolationAlpha);
        public IReadOnlyList<GameEvent> Events => game.Events;
        public PlaytestMetrics Metrics { get; } = new PlaytestMetrics();
        public DropoutWatcher Dropouts { get; } = new DropoutWatcher();
        public RadiusWatcher Radii { get; } = new RadiusWatcher();
        public RecordedRun(bool testerConsented, string buildVersion, LevelGeometry level, GameSettings settings, M1Snapshot initial, int maximumFrames = 36000)
        {
            if (!testerConsented) throw new InvalidOperationException("Verbal tester consent is required before local recording.");
            if (maximumFrames < 1 || maximumFrames > 36000) throw new ArgumentOutOfRangeException(nameof(maximumFrames));
            this.maximumFrames = maximumFrames;
            header = new RecordedSession(buildVersion, settings, level, new CalibrationRecord(1), initial, Array.Empty<HandFrame>());
            game = M1Simulation.Restore(header.InitialState, level, settings);
        }
        public void Step(HandFrame frame)
        {
            if (frame == null || frame.TimestampMicroseconds < lastTimestamp) throw new ArgumentException("Missing frame or timestamp moved backwards.");
            var owner = game.Owner; var route = game.ActiveRoute;
            game.Step(frame); lastTimestamp = frame.TimestampMicroseconds;
            // Live hands may be refilled every frame (D11): the recording keeps its own copy.
            if (frames.Count < maximumFrames) frames.Add(frame.Copy()); else RecordingLimitReached = true;
            Metrics.Observe(game.Events);
            bool progressed = false;
            foreach (var e in game.Events) progressed |= e.Kind == GameEventKind.Boarded || e.Kind == GameEventKind.CheckpointReached || e.Kind == GameEventKind.SectionCommitted ||
                e.Kind == GameEventKind.BridgeBaked || e.Kind == GameEventKind.BramFinished || e.Kind == GameEventKind.RecoveryCompleted;
            Metrics.Tick(frame.Tick, progressed, frame.HasFocus && !game.IsPaused && game.Phase != M1Phase.Complete);
            Radii.Observe(frame.Left); Radii.Observe(frame.Right);
            var active = owner ?? game.Owner; route = route ?? game.ActiveRoute;
            if (active.HasValue && route != null)
            {
                var hand = frame.Hand(active.Value);
                uint invalid = hand == null ? 0x3ffffffu : hand.InvalidMask;
                Dropouts.Step(frame.Tick, active.Value, frame.HasFocus && !game.IsPaused, invalid, game.Recovery != null || game.Build?.Phase == BuildPhase.Paused,
                    route.Reversed ? route.End - route.Start : route.Start - route.End);
            }
            else Dropouts.EndEpisode();
        }
        public RecordedSession Finish() => new RecordedSession(header.BuildVersion, header.Settings, header.Level, header.Calibration, header.InitialState, frames.ToArray(), RecordingLimitReached);
    }
    public sealed class ReplayFrameSource : IHandFrameSource
    {
        private readonly RecordedSession session;
        private int index;
        public ReplayFrameSource(RecordedSession session) { this.session = session ?? throw new ArgumentNullException(nameof(session)); }
        public bool TryRead(long tick, out HandFrame frame)
        {
            frame = null;
            if (index >= session.Frames.Count || session.Frames[index].Tick != tick) return false;
            frame = session.Frames[index++]; return true;
        }
    }
    public static class RecordingCodec
    {
        public static byte[] Encode(RecordedSession session)
        {
            using (var stream = new MemoryStream()) using (var w = new BinaryWriter(stream))
            {
                w.Write(0x50465231); w.Write(1); w.Write(GameSettings.TickRate); w.Write(session.BuildVersion);
                WriteSettings(w, session.Settings); SaveCodec.WriteVector(w, session.Level.Start); SaveCodec.WriteVector(w, session.Level.End);
                w.Write(session.Level.Obstacles.Count);
                foreach (var obstacle in session.Level.Obstacles) { SaveCodec.WriteVector(w, obstacle.Min); SaveCodec.WriteVector(w, obstacle.Max); }
                w.Write(session.Calibration.WorldScale); w.Write(session.Calibration.LeftBoneLength); w.Write(session.Calibration.RightBoneLength); w.Write(session.Partial);
                var save = SaveCodec.Encode(session.InitialState); w.Write(save.Length); w.Write(save);
                w.Write(session.Frames.Count);
                foreach (var f in session.Frames)
                {
                    w.Write(f.Tick); w.Write(f.TimestampMicroseconds); w.Write(f.HasFocus); w.Write((int)f.Command); w.Write(f.ControllersInHand);
                    WriteHand(w, f.Left); WriteHand(w, f.Right);
                }
                using (var sha = SHA256.Create()) w.Write(sha.ComputeHash(stream.ToArray()));
                return stream.ToArray();
            }
        }
        public static RecordedSession Decode(byte[] data, string expectedBuildVersion)
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
                    if (r.ReadInt32() != 0x50465231 || r.ReadInt32() != 1 || r.ReadInt32() != 60) throw new InvalidDataException("Unsupported recording header.");
                    string build = r.ReadString(); if (build != expectedBuildVersion) throw new InvalidDataException("Replay requires the same build version.");
                    var settings = ReadSettings(r); var start = SaveCodec.ReadVector(r); var end = SaveCodec.ReadVector(r);
                    var obstacles = new Obstacle[Count(r, 256)];
                    for (int i = 0; i < obstacles.Length; i++) obstacles[i] = new Obstacle(SaveCodec.ReadVector(r), SaveCodec.ReadVector(r));
                    var calibration = new CalibrationRecord(r.ReadDouble(), r.ReadDouble(), r.ReadDouble()); bool partial = r.ReadBoolean();
                    var initial = SaveCodec.Decode(r.ReadBytes(Count(r, 1024 * 1024)));
                    var frames = new HandFrame[Count(r, 36000)];
                    for (int i = 0; i < frames.Length; i++)
                    {
                        long tick = r.ReadInt64(), timestamp = r.ReadInt64(); bool focus = r.ReadBoolean(); var command = (InputCommand)r.ReadInt32(); bool controllers = r.ReadBoolean();
                        frames[i] = new HandFrame(tick, timestamp, ReadHand(r, HandSide.Left), ReadHand(r, HandSide.Right), focus, command, controllers);
                    }
                    if (stream.Position != stream.Length) throw new InvalidDataException("Trailing recording data.");
                    return new RecordedSession(build, settings, new LevelGeometry(start, end, obstacles), calibration, initial, frames, partial);
                }
            }
            catch (Exception e) when (e is EndOfStreamException || e is ArgumentException || e is OverflowException) { throw new InvalidDataException("Invalid recording data.", e); }
        }
        private static int Count(BinaryReader r, int max) { int n = r.ReadInt32(); if (n < 0 || n > max) throw new InvalidDataException("Recording collection limit exceeded."); return n; }
        internal static void WriteHand(BinaryWriter w, HandSample hand)
        {
            w.Write(hand != null); if (hand == null) return;
            foreach (var j in hand.Joints)
            {
                SaveCodec.WriteVector(w, j.Position); w.Write(j.Rotation.X); w.Write(j.Rotation.Y); w.Write(j.Rotation.Z); w.Write(j.Rotation.W); w.Write(j.Radius); w.Write((int)j.Flags);
            }
        }
        internal static HandSample ReadHand(BinaryReader r, HandSide side)
        {
            if (!r.ReadBoolean()) return null;
            var joints = new JointSample[26];
            for (int i = 0; i < 26; i++) joints[i] = new JointSample(SaveCodec.ReadVector(r), new Rotation(r.ReadDouble(), r.ReadDouble(), r.ReadDouble(), r.ReadDouble()), r.ReadDouble(), (JointFlags)r.ReadInt32());
            return new HandSample(side, joints);
        }
        private static void WriteSettings(BinaryWriter w, GameSettings s)
        {
            w.Write(s.ConnectDistance); w.Write(s.BreakDistance); w.Write(s.ConnectTicks); w.Write(s.StillnessTicks); w.Write(s.StillnessSpeed); w.Write(s.TremorDistance);
            w.Write(s.BuildTicks); w.Write(s.ScaffoldTicks); w.Write(s.RemovalTicks); w.Write(s.AlignmentTicks); w.Write(s.BlendTicks); w.Write(s.RecoveryTicks); w.Write(s.ReturnTicks);
            w.Write(s.TravelerSpeed); w.Write(s.MaximumRouteLength); w.Write(s.FilterCutoff); w.Write(s.FilterBeta); w.Write(s.UseJointRadii); w.Write(s.StillnessToleranceStep);
        }
        private static GameSettings ReadSettings(BinaryReader r)
        {
            var s = new GameSettings { ConnectDistance = r.ReadDouble(), BreakDistance = r.ReadDouble(), ConnectTicks = r.ReadInt32(), StillnessTicks = r.ReadInt32(), StillnessSpeed = r.ReadDouble(), TremorDistance = r.ReadDouble(),
                BuildTicks = r.ReadInt32(), ScaffoldTicks = r.ReadInt32(), RemovalTicks = r.ReadInt32(), AlignmentTicks = r.ReadInt32(), BlendTicks = r.ReadInt32(), RecoveryTicks = r.ReadInt32(), ReturnTicks = r.ReadInt32(),
                TravelerSpeed = r.ReadDouble(), MaximumRouteLength = r.ReadDouble(), FilterCutoff = r.ReadDouble(), FilterBeta = r.ReadDouble(), UseJointRadii = r.ReadBoolean(), StillnessToleranceStep = r.ReadInt32() };
            s.Validate(); return s;
        }
    }
}
#endif
