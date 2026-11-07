using System;
using System.IO;
using System.Security.Cryptography;

namespace Palmfolk.Core
{
    public static class SaveCodec
    {
        private const int Magic = 0x50464B31;
        public const int Version = 1;
        public static byte[] Encode(M1Snapshot state)
        {
            using (var payload = new MemoryStream())
            {
                using (var writer = new BinaryWriter(payload, System.Text.Encoding.UTF8, true)) WriteSnapshot(writer, state);
                byte[] body = payload.ToArray();
                using (var result = new MemoryStream()) using (var writer = new BinaryWriter(result))
                {
                    writer.Write(Magic); writer.Write(Version); writer.Write(body.Length); writer.Write(body);
                    using (var sha = SHA256.Create()) writer.Write(sha.ComputeHash(body));
                    return result.ToArray();
                }
            }
        }
        public static M1Snapshot Decode(byte[] data)
        {
            if (data == null || data.Length < 44 || data.Length > 1024 * 1024) throw new InvalidDataException("Invalid save size.");
            try
            {
                using (var stream = new MemoryStream(data, false)) using (var reader = new BinaryReader(stream))
                {
                    if (reader.ReadInt32() != Magic || reader.ReadInt32() != Version) throw new InvalidDataException("Unsupported save header/version.");
                    int length = reader.ReadInt32(); if (length < 0 || length != data.Length - 44) throw new InvalidDataException("Truncated save.");
                    byte[] body = reader.ReadBytes(length), checksum = reader.ReadBytes(32);
                    using (var sha = SHA256.Create())
                    {
                        var expected = sha.ComputeHash(body); int difference = 0;
                        for (int i = 0; i < 32; i++) difference |= expected[i] ^ checksum[i];
                        if (difference != 0) throw new InvalidDataException("Save checksum mismatch.");
                    }
                    using (var input = new MemoryStream(body, false)) using (var content = new BinaryReader(input))
                    {
                        var state = ReadSnapshot(content);
                        if (input.Position != input.Length) throw new InvalidDataException("Trailing save data.");
                        return state;
                    }
                }
            }
            catch (Exception error) when (error is EndOfStreamException || error is ArgumentException || error is OverflowException)
            { throw new InvalidDataException("Invalid save data.", error); }
        }
        public static void WriteVector(BinaryWriter w, Vec3 v) { w.Write(v.X); w.Write(v.Y); w.Write(v.Z); }
        public static Vec3 ReadVector(BinaryReader r)
        {
            var v = new Vec3(r.ReadDouble(), r.ReadDouble(), r.ReadDouble());
            if (!v.IsFinite) throw new InvalidDataException("Non-finite position."); return v;
        }
        public static void WriteRoute(BinaryWriter w, Route route)
        {
            w.Write(route != null); if (route == null) return;
            w.Write(route.Finger); w.Write(route.Reversed); w.Write(route.Points.Count);
            foreach (var point in route.Points) WriteVector(w, point);
            foreach (double width in route.Widths) w.Write(width);
        }
        public static Route ReadRoute(BinaryReader r)
        {
            if (!r.ReadBoolean()) return null;
            int finger = r.ReadInt32(); bool reversed = r.ReadBoolean(); int count = Count(r, 2, 128);
            if (finger < 0 || finger > 4) throw new InvalidDataException("Invalid route finger.");
            var points = new Vec3[count]; var widths = new double[count - 1];
            for (int i = 0; i < count; i++) points[i] = ReadVector(r);
            for (int i = 0; i < widths.Length; i++) widths[i] = r.ReadDouble();
            return new Route(points, widths, finger, reversed);
        }
        private static int Count(BinaryReader r, int min, int max)
        { int count = r.ReadInt32(); if (count < min || count > max) throw new InvalidDataException("Invalid bounded collection length."); return count; }
        private static void WriteSnapshot(BinaryWriter w, M1Snapshot s)
        {
            w.Write(s.Tick); w.Write((int)s.Phase); WriteVector(w, s.Pip); WriteVector(w, s.Bram);
            w.Write(s.PipCheckpoint); w.Write(s.BramCheckpoint); w.Write(s.Owner.HasValue ? (int)s.Owner.Value : -1);
            WriteRoute(w, s.ActiveRoute); WriteCursor(w, s.Cursor); WriteCursor(w, s.CartCursor);
            w.Write(s.TransferTarget.HasValue); if (s.TransferTarget.HasValue) WriteVector(w, s.TransferTarget.Value);
            w.Write(s.TransferCheckpoint); w.Write(s.BridgeWithdrawn); w.Write(s.CartStarted);
            w.Write(s.Recovery != null);
            if (s.Recovery != null)
            {
                var r = s.Recovery; WriteRoute(w, r.Ghost); WriteVector(w, r.Position); WriteVector(w, r.Frozen); WriteVector(w, r.ReturnFrom);
                w.Write((int)r.Phase); w.Write(r.Elapsed); w.Write(r.Aligned); w.Write(r.Blending); w.Write(r.Returning);
            }
            w.Write(s.Build != null);
            if (s.Build != null)
            {
                var b = s.Build; w.Write((int)b.Phase); w.Write(b.ProgressTicks); w.Write(b.PausedTicks); w.Write(b.RemovalTicks); w.Write(b.StableTicks);
                WriteRoute(w, b.Reference); w.Write(b.FrozenPoints.Length); foreach (var point in b.FrozenPoints) WriteVector(w, point);
                foreach (double width in b.FrozenWidths) w.Write(width);
            }
            w.Write(s.Bridge != null);
            if (s.Bridge != null)
            {
                w.Write(s.Bridge.Curves.Count);
                for (int i = 0; i < s.Bridge.Curves.Count; i++) { w.Write(s.Bridge.Parents[i]); WriteRoute(w, s.Bridge.Curves[i]); }
            }
        }
        private static M1Snapshot ReadSnapshot(BinaryReader r)
        {
            var s = new M1Snapshot { Tick = r.ReadInt64(), Phase = (M1Phase)r.ReadInt32(), Pip = ReadVector(r), Bram = ReadVector(r), PipCheckpoint = r.ReadInt32(), BramCheckpoint = r.ReadInt32() };
            int owner = r.ReadInt32(); if (owner < -1 || owner > 1) throw new InvalidDataException("Invalid saved hand owner.");
            s.Owner = owner < 0 ? (HandSide?)null : (HandSide)owner;
            s.ActiveRoute = ReadRoute(r); s.Cursor = ReadCursor(r); s.CartCursor = ReadCursor(r);
            if (r.ReadBoolean()) s.TransferTarget = ReadVector(r);
            s.TransferCheckpoint = r.ReadInt32(); s.BridgeWithdrawn = r.ReadBoolean(); s.CartStarted = r.ReadBoolean();
            if (r.ReadBoolean()) s.Recovery = new RecoverySnapshot { Ghost = ReadRoute(r), Position = ReadVector(r), Frozen = ReadVector(r), ReturnFrom = ReadVector(r),
                Phase = (RecoveryPhase)r.ReadInt32(), Elapsed = r.ReadInt32(), Aligned = r.ReadInt32(), Blending = r.ReadInt32(), Returning = r.ReadInt32() };
            if (r.ReadBoolean())
            {
                var b = new BuildSnapshot { Phase = (BuildPhase)r.ReadInt32(), ProgressTicks = r.ReadInt32(), PausedTicks = r.ReadInt32(), RemovalTicks = r.ReadInt32(), StableTicks = r.ReadInt32(), Reference = ReadRoute(r) };
                int count = Count(r, 1, 128); b.FrozenPoints = new Vec3[count]; b.FrozenWidths = new double[count - 1];
                for (int i = 0; i < count; i++) b.FrozenPoints[i] = ReadVector(r);
                for (int i = 0; i < count - 1; i++) b.FrozenWidths[i] = r.ReadDouble(); s.Build = b;
            }
            if (r.ReadBoolean())
            {
                int count = Count(r, 1, 16); var routes = new Route[count]; var parents = new int[count];
                for (int i = 0; i < count; i++) { parents[i] = r.ReadInt32(); routes[i] = ReadRoute(r); }
                s.Bridge = new CurveTree(routes, parents);
            }
            if (s.Tick < -1 || !Enum.IsDefined(typeof(M1Phase), s.Phase) || s.PipCheckpoint < 0 || s.PipCheckpoint > 1 || s.BramCheckpoint < 0 || s.BramCheckpoint > 1 ||
                (s.Owner.HasValue && s.ActiveRoute == null) || (s.ActiveRoute != null && s.Cursor.Segment >= s.ActiveRoute.Widths.Count)) throw new InvalidDataException("Invalid saved state.");
            return s;
        }
        private static void WriteCursor(BinaryWriter w, RouteCursor c) { w.Write(c.Segment); w.Write(c.Fraction); }
        private static RouteCursor ReadCursor(BinaryReader r) => new RouteCursor(r.ReadInt32(), r.ReadDouble());
    }

    // The platform provides atomic replacement. Gameplay never chooses a path or
    // writes recordings; only save data passes through this port.
    public interface ISaveStorage { byte[] Read(string slot); void WriteAtomic(string slot, byte[] data); }
    public enum SaveLoadStatus { Missing, Loaded, RecoveredBackup, Corrupt }
    public sealed class SaveRepository
    {
        private readonly ISaveStorage storage;
        public SaveRepository(ISaveStorage storage) { this.storage = storage ?? throw new ArgumentNullException(nameof(storage)); }
        private bool Read(string slot, out long generation, out M1Snapshot state, out bool exists)
        {
            generation = -1; state = null; byte[] data = storage.Read(slot); exists = data != null;
            if (!exists || data.Length < 40) return false;
            using (var sha = SHA256.Create())
            {
                byte[] expected = sha.ComputeHash(data, 0, data.Length - 32); int difference = 0;
                for (int i = 0; i < 32; i++) difference |= expected[i] ^ data[data.Length - 32 + i];
                if (difference != 0) return false;
            }
            try
            {
                using (var stream = new MemoryStream(data, false)) using (var reader = new BinaryReader(stream))
                {
                    generation = reader.ReadInt64(); if (generation < 0) return false;
                    state = SaveCodec.Decode(reader.ReadBytes(data.Length - 40)); return true;
                }
            }
            catch (InvalidDataException) { return false; }
        }
        public SaveLoadStatus Load(out M1Snapshot state)
        {
            bool a = Read("save-a.bin", out long ga, out var sa, out bool ea), b = Read("save-b.bin", out long gb, out var sb, out bool eb);
            state = a && (!b || ga >= gb) ? sa : sb;
            if (!a && !b) return ea || eb ? SaveLoadStatus.Corrupt : SaveLoadStatus.Missing;
            return (ea && !a) || (eb && !b) ? SaveLoadStatus.RecoveredBackup : SaveLoadStatus.Loaded;
        }
        public void Save(M1Snapshot state)
        {
            bool a = Read("save-a.bin", out long ga, out _, out bool ea), b = Read("save-b.bin", out long gb, out _, out bool eb);
            if (!a && !b && (ea || eb)) throw new InvalidDataException("Existing saves are corrupt; explicit recovery is required before replacing them.");
            long generation = checked(Math.Max(ga, gb) + 1);
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
            {
                writer.Write(generation); writer.Write(SaveCodec.Encode(state));
                using (var sha = SHA256.Create()) writer.Write(sha.ComputeHash(stream.ToArray()));
                storage.WriteAtomic(!a || (b && ga <= gb) ? "save-a.bin" : "save-b.bin", stream.ToArray());
            }
        }
    }
}
