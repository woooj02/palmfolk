using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Palmfolk.Core
{
    public enum ValleyMode { Table, VR }

    // The player's settings (A1-A8). The waystone (S4) changes them; Core applies the two that affect play.
    public sealed class PlayerSettings
    {
        // T18/A5 stillness tolerance: 0 strict, 1 default, 2 loosest.
        public int StillnessToleranceStep { get; set; } = 1;
        // A4 world scale on top of the hand calibration, 0.5 to 2.
        public double WorldScale { get; set; } = 1;
        public bool ReducedMotion { get; set; }
        public bool Captions { get; set; } = true;
        public bool HighContrast { get; set; }
        public bool XRay { get; set; }
        public PlayerSettings Copy() => (PlayerSettings)MemberwiseClone();
        public void Validate()
        {
            if (StillnessToleranceStep < 0 || StillnessToleranceStep > 2 || !Vec3.Finite(WorldScale) || WorldScale < 0.5 || WorldScale > 2)
                throw new ArgumentException("Invalid player settings.");
        }
        // The gameplay settings with the player's choices applied.
        public GameSettings Apply(GameSettings settings)
        {
            Validate();
            var result = (settings ?? new GameSettings()).Copy();
            result.StillnessToleranceStep = StillnessToleranceStep; result.WorldScaleAdjustment = WorldScale;
            result.Validate(); return result;
        }
    }

    // Save v2 (T9, MR3): everything that outlives a session. Bridges are valley-local curve trees, so a save
    // opens in either mode and after replanting anywhere.
    public sealed class SaveData
    {
        // Null before the valley is first played.
        public ValleyProgress Valley { get; set; }
        public ValleyMode Mode { get; set; }
        // Table mode's spatial anchor (MR3); null when none was saved.
        public string AnchorId { get; set; }
        // The last planted pose (world space). Used when no anchor is found in VR mode; Table mode replants instead.
        public ValleyPose Pose { get; set; }
        public PlayerSettings Settings { get; set; } = new PlayerSettings();
        // The M1 slice's state: a v1 save migrates here.
        public M1Snapshot M1 { get; set; }
        // Which format this was read from (1 or 2); written saves are always the current version.
        public int LoadedVersion { get; internal set; } = SaveCodec.CurrentVersion;
    }

    public static partial class SaveCodec
    {
        public const int CurrentVersion = 2;
        private const int MaxEdges = 64, MaxId = 64;

        public static byte[] EncodeSave(SaveData save)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            Check(save);
            using (var payload = new MemoryStream())
            {
                using (var w = new BinaryWriter(payload, System.Text.Encoding.UTF8, true))
                {
                    w.Write((int)save.Mode);
                    WriteString(w, save.AnchorId);
                    w.Write(save.Pose != null);
                    if (save.Pose != null) { WriteVector(w, save.Pose.Origin); WriteVector(w, save.Pose.Forward); w.Write(save.Pose.Scale); }
                    var s = save.Settings ?? new PlayerSettings();
                    w.Write(s.StillnessToleranceStep); w.Write(s.WorldScale); w.Write(s.ReducedMotion); w.Write(s.Captions); w.Write(s.HighContrast); w.Write(s.XRay);
                    w.Write(save.Valley != null);
                    if (save.Valley != null) WriteProgress(w, save.Valley);
                    w.Write(save.M1 != null);
                    if (save.M1 != null) { var m1 = Encode(save.M1); w.Write(m1.Length); w.Write(m1); }
                }
                return Frame(CurrentVersion, payload.ToArray());
            }
        }

        // Reads a v2 save, or migrates a v1 (M1 slice) save: its state becomes SaveData.M1 and the valley starts fresh.
        public static SaveData DecodeSave(byte[] data)
        {
            int version = PeekVersion(data);
            if (version == 1) return new SaveData { M1 = Decode(data), LoadedVersion = 1 };
            byte[] body = Unframe(data, CurrentVersion);
            try
            {
                using (var input = new MemoryStream(body, false)) using (var r = new BinaryReader(input))
                {
                    var save = new SaveData { LoadedVersion = 2 };
                    int mode = r.ReadInt32(); if (!Enum.IsDefined(typeof(ValleyMode), mode)) throw new InvalidDataException("Invalid mode.");
                    save.Mode = (ValleyMode)mode;
                    save.AnchorId = ReadString(r);
                    if (r.ReadBoolean()) save.Pose = new ValleyPose(ReadVector(r), ReadVector(r), r.ReadDouble());
                    save.Settings = new PlayerSettings
                    {
                        StillnessToleranceStep = r.ReadInt32(), WorldScale = r.ReadDouble(), ReducedMotion = r.ReadBoolean(),
                        Captions = r.ReadBoolean(), HighContrast = r.ReadBoolean(), XRay = r.ReadBoolean()
                    };
                    if (r.ReadBoolean()) save.Valley = ReadProgress(r);
                    if (r.ReadBoolean()) { int length = r.ReadInt32(); if (length < 44 || length > body.Length) throw new InvalidDataException("Invalid M1 block."); save.M1 = Decode(r.ReadBytes(length)); }
                    if (input.Position != input.Length) throw new InvalidDataException("Trailing save data.");
                    Check(save);
                    return save;
                }
            }
            catch (Exception error) when (error is EndOfStreamException || error is ArgumentException || error is OverflowException)
            { throw new InvalidDataException("Invalid save data.", error); }
        }

        private static void Check(SaveData save)
        {
            if (!Enum.IsDefined(typeof(ValleyMode), save.Mode)) throw new InvalidDataException("Invalid mode.");
            if (save.AnchorId != null && (save.AnchorId.Length == 0 || save.AnchorId.Length > 128 || save.AnchorId.Any(char.IsControl))) throw new InvalidDataException("Invalid anchor id.");
            try { (save.Settings ?? new PlayerSettings()).Validate(); } catch (ArgumentException e) { throw new InvalidDataException(e.Message); }
            var v = save.Valley; if (v == null) return;
            if (v.CompletedPuzzles < 0 || v.CompletedPuzzles > 6 || v.Bridges == null || v.Bridges.Count > MaxEdges || v.BridgeMarks == null || v.BridgeMarks.Count > MaxEdges ||
                v.VisitorsHelped < 0 || v.VisitorsHelped > 31 || v.LitWindows < 0 || v.LitWindows > 31 || v.CurrentVisitor < -1 || v.CurrentVisitor > 4 ||
                string.IsNullOrEmpty(v.PipIsland) || v.PipIsland.Length > MaxId || v.Bridges.Any(b => b.Value == null || string.IsNullOrEmpty(b.Key) || b.Key.Length > MaxId) ||
                v.BridgeMarks.Any(m => string.IsNullOrEmpty(m.Key) || m.Key.Length > MaxId || m.Value < 0 || m.Value > 31))
                throw new InvalidDataException("Invalid valley progress.");
        }

        private static void WriteProgress(BinaryWriter w, ValleyProgress v)
        {
            w.Write(v.CompletedPuzzles); WriteString(w, v.PipIsland); w.Write(v.LedgeWidened);
            w.Write(v.Bridges.Count);
            foreach (var pair in v.Bridges.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                WriteString(w, pair.Key); w.Write(pair.Value.Curves.Count);
                for (int i = 0; i < pair.Value.Curves.Count; i++) { w.Write(pair.Value.Parents[i]); WriteRoute(w, pair.Value.Curves[i]); }
            }
            w.Write(v.CurrentVisitor); w.Write(v.VisitorsHelped); w.Write(v.LitWindows);
            w.Write(v.BridgeMarks.Count);
            foreach (var pair in v.BridgeMarks.OrderBy(p => p.Key, StringComparer.Ordinal)) { WriteString(w, pair.Key); w.Write(pair.Value); }
        }
        private static ValleyProgress ReadProgress(BinaryReader r)
        {
            var v = new ValleyProgress { CompletedPuzzles = r.ReadInt32(), PipIsland = ReadString(r), LedgeWidened = r.ReadBoolean() };
            int bridges = Bounded(r, 0, MaxEdges);
            for (int b = 0; b < bridges; b++)
            {
                string id = ReadString(r); int count = Bounded(r, 1, 16);
                var routes = new Route[count]; var parents = new int[count];
                for (int i = 0; i < count; i++) { parents[i] = r.ReadInt32(); routes[i] = ReadRoute(r) ?? throw new InvalidDataException("Missing curve."); }
                if (id == null || v.Bridges.ContainsKey(id)) throw new InvalidDataException("Duplicate bridge.");
                v.Bridges.Add(id, new CurveTree(routes, parents));
            }
            v.CurrentVisitor = r.ReadInt32(); v.VisitorsHelped = r.ReadInt32(); v.LitWindows = r.ReadInt32();
            int marks = Bounded(r, 0, MaxEdges);
            for (int m = 0; m < marks; m++)
            {
                string id = ReadString(r); int value = r.ReadInt32();
                if (id == null || v.BridgeMarks.ContainsKey(id)) throw new InvalidDataException("Duplicate mark.");
                v.BridgeMarks.Add(id, value);
            }
            return v;
        }

        private static void WriteString(BinaryWriter w, string s) { w.Write(s != null); if (s != null) w.Write(s); }
        private static string ReadString(BinaryReader r)
        {
            if (!r.ReadBoolean()) return null;
            var s = r.ReadString(); if (s.Length > 128) throw new InvalidDataException("String too long."); return s;
        }
        private static int Bounded(BinaryReader r, int min, int max)
        { int n = r.ReadInt32(); if (n < min || n > max) throw new InvalidDataException("Invalid bounded collection length."); return n; }

        private static byte[] Frame(int version, byte[] body)
        {
            using (var result = new MemoryStream()) using (var writer = new BinaryWriter(result))
            {
                writer.Write(Magic); writer.Write(version); writer.Write(body.Length); writer.Write(body);
                using (var sha = SHA256.Create()) writer.Write(sha.ComputeHash(body));
                return result.ToArray();
            }
        }
        private static int PeekVersion(byte[] data)
        {
            if (data == null || data.Length < 44 || data.Length > 1024 * 1024) throw new InvalidDataException("Invalid save size.");
            if (BitConverter.ToInt32(data, 0) != Magic) throw new InvalidDataException("Unsupported save header.");
            int version = BitConverter.ToInt32(data, 4);
            if (version != 1 && version != CurrentVersion) throw new InvalidDataException("Unsupported save version.");
            return version;
        }
        private static byte[] Unframe(byte[] data, int version)
        {
            using (var stream = new MemoryStream(data, false)) using (var reader = new BinaryReader(stream))
            {
                if (reader.ReadInt32() != Magic || reader.ReadInt32() != version) throw new InvalidDataException("Unsupported save header/version.");
                int length = reader.ReadInt32(); if (length < 0 || length != data.Length - 44) throw new InvalidDataException("Truncated save.");
                byte[] body = reader.ReadBytes(length), checksum = reader.ReadBytes(32);
                using (var sha = SHA256.Create())
                {
                    var expected = sha.ComputeHash(body); int difference = 0;
                    for (int i = 0; i < 32; i++) difference |= expected[i] ^ checksum[i];
                    if (difference != 0) throw new InvalidDataException("Save checksum mismatch.");
                }
                return body;
            }
        }
    }
}
