using System;
using System.Collections.Generic;
using Palmfolk.Core;

namespace Palmfolk.Fixtures
{
    public enum HandShape { OneFinger, Flat, Spread, Curved }
    // Synthetic scale factors, not measured population percentiles.
    public enum HandSize { Small, Medium, Large }
    public enum TrackingFault { MissingHand, InvalidPosition, LowConfidence, MissingRadius }

    public sealed class TrackingWindow
    {
        public long Start { get; }
        public long End { get; }
        public TrackingFault Fault { get; }
        public uint Joints { get; }
        public TrackingWindow(long start, long end, TrackingFault fault, uint joints = (1u << 26) - 1)
        {
            if (start < 0 || end <= start || !Enum.IsDefined(typeof(TrackingFault), fault) || (joints >> 26) != 0)
                throw new ArgumentOutOfRangeException();
            Start = start; End = end; Fault = fault; Joints = joints;
        }
    }

    // Stateless sampling: a seed and tick produce the same data, even out of order.
    // Test fixtures only; this is not a live tracking adapter or a headset measurement.
    public sealed class HandSequence : IHandFrameSource
    {
        private readonly HandShape shape;
        private readonly HandSize size;
        private readonly HandSide side;
        private readonly Vec3 origin, withdrawal;
        private readonly double yaw, tremor;
        private readonly bool palmDown;
        private readonly uint seed;
        private readonly long withdrawStart, withdrawEnd, ticks;
        private readonly TrackingWindow[] faults;

        public HandSequence(HandShape shape = HandShape.Flat, HandSize size = HandSize.Medium,
            HandSide side = HandSide.Left, uint seed = 1, double tremorMetres = 0,
            Vec3 origin = default, double yawDegrees = 0, bool palmDown = false,
            long ticks = 1800, long withdrawStart = 0, long withdrawEnd = 0,
            Vec3 withdrawal = default, params TrackingWindow[] faults)
        {
            if (!Enum.IsDefined(typeof(HandShape), shape) || !Enum.IsDefined(typeof(HandSize), size) ||
                !Enum.IsDefined(typeof(HandSide), side) || !Vec3.Finite(tremorMetres) || tremorMetres < 0 ||
                !origin.IsFinite || !withdrawal.IsFinite || !Vec3.Finite(yawDegrees) || ticks < 1 ||
                ticks > 36000 || withdrawStart < 0 || withdrawEnd < withdrawStart ||
                (withdrawal.Length > 0 && withdrawEnd == withdrawStart)) throw new ArgumentException("Invalid fixture.");
            this.shape = shape; this.size = size; this.side = side; this.seed = seed;
            tremor = tremorMetres; this.origin = origin; yaw = yawDegrees * Math.PI / 180;
            this.palmDown = palmDown; this.ticks = ticks; this.withdrawStart = withdrawStart;
            this.withdrawEnd = withdrawEnd; this.withdrawal = withdrawal;
            this.faults = faults == null ? Array.Empty<TrackingWindow>() : (TrackingWindow[])faults.Clone();
            foreach (var fault in this.faults) if (fault == null) throw new ArgumentException("Null tracking window.");
        }

        public bool TryRead(long tick, out HandFrame frame)
        {
            if (tick < 0 || tick >= ticks) { frame = null; return false; }
            var hand = Sample(tick);
            frame = new HandFrame(tick, tick * 1000000 / GameSettings.TickRate,
                side == HandSide.Left ? hand : null, side == HandSide.Right ? hand : null);
            return true;
        }

        public IEnumerable<HandFrame> Frames()
        {
            for (long tick = 0; tick < ticks; tick++) { TryRead(tick, out var frame); yield return frame; }
        }

        public HandSample Sample(long tick)
        {
            if (tick < 0 || tick >= ticks) throw new ArgumentOutOfRangeException(nameof(tick));
            foreach (var fault in faults)
                if (tick >= fault.Start && tick < fault.End && fault.Fault == TrackingFault.MissingHand) return null;
            double scale = size == HandSize.Small ? 0.8 : size == HandSize.Large ? 1.2 : 1;
            double withdraw = withdrawEnd > withdrawStart ? Vec3.Clamp((double)(tick - withdrawStart) / (withdrawEnd - withdrawStart), 0, 1) : 0;
            Vec3 motion = origin + withdrawal * withdraw + Noise(tick) * tremor;
            var joints = new JointSample[26];
            joints[0] = Joint(new Vec3(0.035, 0, 0.025), 0.03, 0);
            joints[1] = Joint(new Vec3(0.06, 0, 0), 0.03, 1);
            for (int bone = 0; bone < 4; bone++)
                joints[2 + bone] = Joint(new Vec3(0.04 - bone * 0.02, 0, -0.06), 0.006, 2 + bone);
            double[] x = { 0.03, 0.005, -0.015, -0.04, -0.06 };
            for (int finger = 0; finger < 4; finger++)
                for (int bone = 0; bone < 5; bone++)
                {
                    double z = finger * (shape == HandShape.Spread ? 0.018 + 0.03 * bone / 4 : 0.018);
                    double y = shape == HandShape.Curved && bone > 0 ? 0.016 * Math.Sin(Math.PI * (x[bone] + 0.06) / 0.12) : 0;
                    double px = x[bone];
                    if (shape == HandShape.OneFinger && finger > 0 && bone > 0)
                    { px = 0.025 + bone * 0.002; y = -0.018 - bone * 0.003; }
                    int id = 6 + finger * 5 + bone;
                    joints[id] = Joint(new Vec3(px, y, z), 0.006, id);
                }
            return new HandSample(side, joints);

            JointSample Joint(Vec3 point, double radius, int id)
            {
                double z = side == HandSide.Right ? -point.Z : point.Z;
                double y = point.Y;
                if (palmDown) { z = -z; y = -y; }
                Vec3 transformed = new Vec3(point.X * Math.Cos(yaw) + z * Math.Sin(yaw), y,
                    -point.X * Math.Sin(yaw) + z * Math.Cos(yaw)) * scale + motion;
                var flags = JointFlags.PositionValid | JointFlags.RotationValid | JointFlags.HighConfidence | JointFlags.RadiusValid;
                foreach (var fault in faults)
                    if (tick >= fault.Start && tick < fault.End && (fault.Joints & (1u << id)) != 0)
                    {
                        if (fault.Fault == TrackingFault.InvalidPosition) flags &= ~JointFlags.PositionValid;
                        if (fault.Fault == TrackingFault.LowConfidence) flags &= ~JointFlags.HighConfidence;
                        if (fault.Fault == TrackingFault.MissingRadius) flags &= ~JointFlags.RadiusValid;
                    }
                double sy = Math.Sin(yaw / 2), cy = Math.Cos(yaw / 2);
                var rotation = palmDown ? new Rotation(cy, 0, -sy, 0) : new Rotation(0, sy, 0, cy);
                return new JointSample(transformed, rotation, radius * scale, flags);
            }
        }

        private Vec3 Noise(long tick)
        {
            uint value = unchecked(seed ^ ((uint)tick * 747796405u));
            double Next()
            {
                value = unchecked(value + 0x9e3779b9u);
                uint hash = value;
                hash = unchecked((hash ^ (hash >> 16)) * 0x85ebca6bu);
                hash = unchecked((hash ^ (hash >> 13)) * 0xc2b2ae35u);
                hash ^= hash >> 16;
                return (hash / (double)uint.MaxValue * 2 - 1) / Math.Sqrt(3);
            }
            return new Vec3(Next(), Next(), Next());
        }
    }
}
