using System;
using System.Collections.Generic;

namespace Palmfolk.Core
{
    public enum HandSide { Left, Right }
    public enum JointId
    {
        Palm, Wrist, ThumbMetacarpal, ThumbProximal, ThumbDistal, ThumbTip,
        IndexMetacarpal, IndexProximal, IndexIntermediate, IndexDistal, IndexTip,
        MiddleMetacarpal, MiddleProximal, MiddleIntermediate, MiddleDistal, MiddleTip,
        RingMetacarpal, RingProximal, RingIntermediate, RingDistal, RingTip,
        LittleMetacarpal, LittleProximal, LittleIntermediate, LittleDistal, LittleTip
    }
    [Flags] public enum JointFlags { None = 0, PositionValid = 1, RotationValid = 2, HighConfidence = 4, RadiusValid = 8 }
    public readonly struct JointSample
    {
        public readonly Vec3 Position;
        public readonly Rotation Rotation;
        public readonly double Radius;
        public readonly JointFlags Flags;
        public bool Usable => (Flags & (JointFlags.PositionValid | JointFlags.HighConfidence)) == (JointFlags.PositionValid | JointFlags.HighConfidence);
        public bool HasRadius => (Flags & JointFlags.RadiusValid) != 0 && Radius > 0 && Radius < 0.1;
        public JointSample(Vec3 position, Rotation rotation, double radius, JointFlags flags)
        {
            if (!position.IsFinite || !Vec3.Finite(radius) || radius < 0 || ((int)flags & ~15) != 0) throw new ArgumentException("Invalid joint sample.");
            Position = position; Rotation = rotation; Radius = radius; Flags = flags;
        }
        public JointSample WithPosition(Vec3 position) => new JointSample(position, Rotation, Radius, Flags);
    }
    public sealed class HandSample
    {
        private readonly JointSample[] joints;
        public HandSide Side { get; }
        public IReadOnlyList<JointSample> Joints { get; }
        public JointSample this[JointId id] => joints[(int)id];
        public HandSample(HandSide side, JointSample[] joints)
        {
            if ((side != HandSide.Left && side != HandSide.Right) || joints == null || joints.Length != 26) throw new ArgumentException("Expected 26 OpenXR joints.");
            Side = side; this.joints = (JointSample[])joints.Clone(); Joints = Array.AsReadOnly(this.joints);
        }
        public uint InvalidMask
        {
            get { uint mask = 0; for (int i = 0; i < 26; i++) if (!joints[i].Usable) mask |= 1u << i; return mask; }
        }
    }
    public enum InputCommand { None, Pause, Resume, CancelBuild, StartElsewhere }
    public sealed class HandFrame
    {
        public long Tick { get; }
        public long TimestampMicroseconds { get; }
        public HandSample Left { get; }
        public HandSample Right { get; }
        public bool HasFocus { get; }
        public InputCommand Command { get; }
        public bool ControllersInHand { get; }
        public HandFrame(long tick, long timestampMicroseconds, HandSample left, HandSample right, bool hasFocus = true, InputCommand command = InputCommand.None, bool controllersInHand = false)
        {
            if (tick < 0 || timestampMicroseconds < 0 || (left != null && left.Side != HandSide.Left) || (right != null && right.Side != HandSide.Right) || !Enum.IsDefined(typeof(InputCommand), command)) throw new ArgumentException("Invalid input frame.");
            Tick = tick; TimestampMicroseconds = timestampMicroseconds; Left = left; Right = right; HasFocus = hasFocus; Command = command; ControllersInHand = controllersInHand;
        }
        public HandSample Hand(HandSide side) => side == HandSide.Left ? Left : Right;
    }
    public interface IHandFrameSource { bool TryRead(long tick, out HandFrame frame); }
    public interface ITickSimulation
    {
        long Tick { get; }
        IReadOnlyList<GameEvent> Events { get; }
        void Step(HandFrame frame);
    }

    public sealed class GameSettings
    {
        public const int TickRate = 60;
        public const double Delta = 1.0 / TickRate;
        public double ConnectDistance { get; set; } = 0.02;
        public double BreakDistance { get; set; } = 0.04;
        public int ConnectTicks { get; set; } = 9;
        public int StillnessTicks { get; set; } = 30;
        public double StillnessSpeed { get; set; } = 0.03;
        public double TremorDistance { get; set; } = 0.015;
        public int BuildTicks { get; set; } = 150;
        public int ScaffoldTicks { get; set; } = 600;
        public int RemovalTicks { get; set; } = 15;
        public int AlignmentTicks { get; set; } = 18;
        public int BlendTicks { get; set; } = 12;
        public int RecoveryTicks { get; set; } = 480;
        public int ReturnTicks { get; set; } = 60;
        public double TravelerSpeed { get; set; } = 0.03;
        public double MaximumRouteLength { get; set; } = 0.35;
        public double FilterCutoff { get; set; } = 1.0;
        public double FilterBeta { get; set; } = 0.3;
        public bool UseJointRadii { get; set; }
        public int StillnessToleranceStep { get; set; } = 1;
        public double StillnessThreshold(int adjustment = 0) => StillnessSpeed * (Math.Min(2, StillnessToleranceStep + adjustment) == 0 ? 0.75 : Math.Min(2, StillnessToleranceStep + adjustment) == 1 ? 1 : 1.5);
        public GameSettings Copy() => (GameSettings)MemberwiseClone();
        public void Validate()
        {
            double[] values = { ConnectDistance, BreakDistance, StillnessSpeed, TremorDistance, TravelerSpeed, MaximumRouteLength, FilterCutoff };
            foreach (double v in values) if (!Vec3.Finite(v) || v <= 0) throw new ArgumentException("Invalid gameplay setting.");
            if (!Vec3.Finite(FilterBeta) || FilterBeta < 0 || ConnectDistance >= BreakDistance || StillnessToleranceStep < 0 || StillnessToleranceStep > 2) throw new ArgumentException("Invalid filter, tolerance or connection settings.");
            int[] ticks = { ConnectTicks, StillnessTicks, BuildTicks, ScaffoldTicks, RemovalTicks, AlignmentTicks, BlendTicks, RecoveryTicks, ReturnTicks };
            foreach (int v in ticks) if (v < 1 || v > 36000) throw new ArgumentException("Invalid tick setting.");
        }
    }
}
