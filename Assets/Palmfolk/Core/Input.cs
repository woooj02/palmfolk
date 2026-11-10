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
    // Restart (D5): take the current scaffold down and return Pip to his island; bridges stay. RingBell: call the next visitor (D8).
    public enum InputCommand { None, Pause, Resume, CancelBuild, StartElsewhere, Replant, Restart, RingBell }
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

    // What a traveler needs from a deck (the capacity table). Lengths at default world scale.
    public readonly struct CapacityRule
    {
        public readonly double MinimumWidth, MaximumSlopeDegrees, MinimumClearance;
        public CapacityRule(double minimumWidth, double maximumSlopeDegrees, double minimumClearance)
        { MinimumWidth = minimumWidth; MaximumSlopeDegrees = maximumSlopeDegrees; MinimumClearance = minimumClearance; }
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

        // Planting (E21, build plan default 5). Palm speed uses StillnessThreshold(), so the tolerance setting applies. Lengths are world metres.
        public int PlantTicks { get; set; } = 60;
        public double PlantMaximumTiltDegrees { get; set; } = 20;
        public double FingerStraightness { get; set; } = 0.9;
        public double PalmHalfThickness { get; set; } = 0.015;
        // Hand-size calibration (T6) and the player's world-scale setting (A4).
        public double ReferenceHandLength { get; set; } = 0.18;
        public double MinimumHandScale { get; set; } = 0.75;
        public double MaximumHandScale { get; set; } = 1.3;
        public double WorldScaleAdjustment { get; set; } = 1;

        // The capacity table (Definitions). H1 may change these after the Arch playtest; the rock, span and anchor
        // heights are valley content. EndRampLength > 0 lets Pip add end ramps: deck within that distance (along the
        // route) of either anchored end is not slope-checked.
        public CapacityRule WalkerRule { get; set; } = new CapacityRule(0.01, 35, 0.035);
        public CapacityRule GroupRule { get; set; } = new CapacityRule(0.025, 25, 0.035);
        public CapacityRule CartRule { get; set; } = new CapacityRule(0.025, 25, 0.05);
        public double EndRampLength { get; set; }
        public CapacityRule Rule(TravelerKind kind) => kind == TravelerKind.Walker ? WalkerRule : kind == TravelerKind.Group ? GroupRule : kind == TravelerKind.Cart ? CartRule : throw new ArgumentException("Unknown traveler.");

        public GameSettings Copy() => (GameSettings)MemberwiseClone();
        public void Validate()
        {
            double[] values = { ConnectDistance, BreakDistance, StillnessSpeed, TremorDistance, TravelerSpeed, MaximumRouteLength, FilterCutoff,
                PalmHalfThickness, ReferenceHandLength, MinimumHandScale, MaximumHandScale };
            foreach (double v in values) if (!Vec3.Finite(v) || v <= 0) throw new ArgumentException("Invalid gameplay setting.");
            if (!Vec3.Finite(FilterBeta) || FilterBeta < 0 || ConnectDistance >= BreakDistance || StillnessToleranceStep < 0 || StillnessToleranceStep > 2) throw new ArgumentException("Invalid filter, tolerance or connection settings.");
            if (!Vec3.Finite(PlantMaximumTiltDegrees) || PlantMaximumTiltDegrees <= 0 || PlantMaximumTiltDegrees >= 90 ||
                !Vec3.Finite(FingerStraightness) || FingerStraightness <= 0 || FingerStraightness > 1 || MinimumHandScale > MaximumHandScale ||
                !Vec3.Finite(WorldScaleAdjustment) || WorldScaleAdjustment < 0.5 || WorldScaleAdjustment > 2) throw new ArgumentException("Invalid planting or scale settings.");
            foreach (var rule in new[] { WalkerRule, GroupRule, CartRule })
                if (!Vec3.Finite(rule.MinimumWidth) || rule.MinimumWidth <= 0 || !Vec3.Finite(rule.MaximumSlopeDegrees) || rule.MaximumSlopeDegrees <= 0 ||
                    rule.MaximumSlopeDegrees >= 90 || !Vec3.Finite(rule.MinimumClearance) || rule.MinimumClearance < 0) throw new ArgumentException("Invalid capacity rule.");
            if (!Vec3.Finite(EndRampLength) || EndRampLength < 0 || EndRampLength > 0.05) throw new ArgumentException("Invalid end ramp length.");
            int[] ticks = { ConnectTicks, StillnessTicks, BuildTicks, ScaffoldTicks, RemovalTicks, AlignmentTicks, BlendTicks, RecoveryTicks, ReturnTicks, PlantTicks };
            foreach (int v in ticks) if (v < 1 || v > 36000) throw new ArgumentException("Invalid tick setting.");
        }
    }
}
