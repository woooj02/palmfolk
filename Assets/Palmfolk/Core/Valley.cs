using System;

namespace Palmfolk.Core
{
    // Where the valley stands in the world (E21, T6, A4). Gameplay never sees world space: the valley's
    // local frame has x = valley right, y = up, z = valley forward, measured in valley units
    // (world metres divided by Scale). The valley only turns about world up, so "up" never changes.
    public sealed class ValleyPose
    {
        public Vec3 Origin { get; }
        public Vec3 Forward { get; }
        public double Scale { get; }
        public Vec3 Right => new Vec3(Forward.Z, 0, -Forward.X);
        public double YawRadians => Math.Atan2(Forward.X, Forward.Z);
        // World-from-valley rotation; Unity's Quaternion.Euler(0, yawDegrees, 0).
        public Rotation Rotation => Rotation.AboutUp(YawRadians);
        public static ValleyPose Identity => new ValleyPose(Vec3.Zero, new Vec3(0, 0, 1), 1);

        public ValleyPose(Vec3 origin, Vec3 forward, double scale)
        {
            var level = new Vec3(forward.X, 0, forward.Z);
            if (!origin.IsFinite || !forward.IsFinite || level.Length < 1e-6 || !Vec3.Finite(scale) || scale < 0.1 || scale > 10)
                throw new ArgumentException("Invalid valley pose.");
            // A forward that is already level and unit length is kept bit for bit, so a pose read back from a save or
            // a recording maps hands exactly as the original did.
            Origin = origin; Forward = level.Equals(forward) && Math.Abs(level.Length - 1) < 1e-12 ? level : level.Normalized; Scale = scale;
        }

        public Vec3 ToValley(Vec3 world)
        {
            var d = world - Origin; var right = Right;
            return new Vec3(Vec3.Dot(d, right), d.Y, Vec3.Dot(d, Forward)) / Scale;
        }
        public Vec3 ToWorld(Vec3 local)
        {
            var right = Right;
            return Origin + new Vec3(right.X * local.X + Forward.X * local.Z, local.Y, right.Z * local.X + Forward.Z * local.Z) * Scale;
        }
        public double ToValleyLength(double world) => world / Scale;
        public double ToWorldLength(double local) => local * Scale;
        public Rotation ToValley(Rotation world) => Rotation.Inverse * world;
        public Rotation ToWorld(Rotation local) => Rotation * local;

        public JointSample ToValley(JointSample joint) => new JointSample(ToValley(joint.Position), ToValley(joint.Rotation), joint.Radius / Scale, joint.Flags);
        public JointSample ToWorld(JointSample joint) => new JointSample(ToWorld(joint.Position), ToWorld(joint.Rotation), joint.Radius * Scale, joint.Flags);
        public HandSample ToValley(HandSample hand) => Map(hand, true);
        public HandSample ToWorld(HandSample hand) => Map(hand, false);
        public HandFrame ToValley(HandFrame frame) => Map(frame, true);
        public HandFrame ToWorld(HandFrame frame) => Map(frame, false);

        private HandSample Map(HandSample hand, bool toValley)
        {
            if (hand == null) return null;
            var joints = new JointSample[26];
            for (int i = 0; i < 26; i++) joints[i] = toValley ? ToValley(hand.Joints[i]) : ToWorld(hand.Joints[i]);
            return new HandSample(hand.Side, joints);
        }
        // D11: the valley-local frame in buffers the caller keeps and reuses (PalmfolkGame, every tick).
        internal HandFrame ToValley(HandFrame frame, HandFrame into, ref HandSample left, ref HandSample right)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            into.Set(frame.Tick, frame.TimestampMicroseconds, MapInto(frame.Left, ref left), MapInto(frame.Right, ref right), frame.HasFocus, frame.Command, frame.ControllersInHand);
            return into;
        }
        private HandSample MapInto(HandSample hand, ref HandSample buffer)
        {
            if (hand == null) return null;
            if (buffer == null || buffer.Side != hand.Side) buffer = new HandSample(hand.Side);
            var joints = buffer.Buffer;
            for (int i = 0; i < 26; i++) joints[i] = ToValley(hand[(JointId)i]);
            return buffer;
        }
        private HandFrame Map(HandFrame frame, bool toValley)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            return new HandFrame(frame.Tick, frame.TimestampMicroseconds, Map(frame.Left, toValley), Map(frame.Right, toValley), frame.HasFocus, frame.Command, frame.ControllersInHand);
        }
    }

    // Hand-size calibration (T6): the bone lengths of the middle finger chain set the world scale.
    public static class HandMeasure
    {
        private static readonly JointId[] Chain = { JointId.Wrist, JointId.MiddleMetacarpal, JointId.MiddleProximal, JointId.MiddleIntermediate, JointId.MiddleDistal, JointId.MiddleTip };

        // Wrist to middle fingertip along the bones, in world metres; null if any joint on the chain is untracked.
        public static double? Length(HandSample hand)
        {
            if (hand == null) return null;
            double length = 0;
            for (int i = 0; i < Chain.Length; i++)
            {
                if (!hand[Chain[i]].Usable) return null;
                if (i > 0) length += Vec3.Distance(hand[Chain[i - 1]].Position, hand[Chain[i]].Position);
            }
            return length;
        }

        // Hand-size scale, clamped, then the player's world-scale setting (A4).
        public static double Scale(double handLength, GameSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (!Vec3.Finite(handLength) || handLength <= 0) throw new ArgumentOutOfRangeException(nameof(handLength));
            return Vec3.Clamp(handLength / settings.ReferenceHandLength, settings.MinimumHandScale, settings.MaximumHandScale) * settings.WorldScaleAdjustment;
        }
    }

    public sealed class PlantingResult
    {
        public ValleyPose Pose { get; }
        public HandSide Hand { get; }
        public double HandLength { get; }
        public PlantingResult(ValleyPose pose, HandSide hand, double handLength)
        {
            Pose = pose ?? throw new ArgumentNullException(nameof(pose)); Hand = hand; HandLength = handLength;
        }
    }

    // Build plan default 5: palm normal within 20 degrees of straight down or up, fingers extended,
    // palm speed under 3 cm/s for 1 s. Works on world-space frames, before any valley exists.
    public sealed class PlantingDetector
    {
        private readonly GameSettings settings;
        private readonly HandFilter leftFilter = new HandFilter(), rightFilter = new HandFilter();
        private readonly StillnessWatcher stillness;
        private int restTicks, waitTicks;
        private Vec3 lastPalm;
        private double lengthSum;
        private int lengthCount;
        public HandSide? Hand { get; private set; }
        public bool WaitingForHands { get; private set; }
        public bool ShowStartupText => WaitingForHands && waitTicks >= 600;
        // True only on the tick the startup text becomes due (E19).
        public bool StartupPromptDue { get; private set; }
        // 0 to 1 over the resting second (PlantTicks speed samples need PlantTicks + 1 positions).
        public double Progress => Math.Min(1, restTicks / (double)(settings.PlantTicks + 1));

        // A4: the player's world-scale setting, used from the next planting.
        public void SetWorldScale(double adjustment)
        {
            if (!Vec3.Finite(adjustment) || adjustment < 0.5 || adjustment > 2) throw new ArgumentOutOfRangeException(nameof(adjustment));
            settings.WorldScaleAdjustment = adjustment;
        }
        public double WorldScale => settings.WorldScaleAdjustment;
        public PlantingDetector(GameSettings settings = null)
        {
            this.settings = (settings ?? new GameSettings()).Copy(); this.settings.Validate();
            stillness = new StillnessWatcher(this.settings.PlantTicks);
        }

        public void Reset() { leftFilter.Reset(); rightFilter.Reset(); ResetRest(); waitTicks = 0; WaitingForHands = false; StartupPromptDue = false; }
        private void ResetRest() { stillness.Reset(); restTicks = 0; lengthSum = 0; lengthCount = 0; Hand = null; }

        // Returns the pose on the tick the valley is planted, otherwise null.
        public PlantingResult Step(HandFrame frame)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            StartupPromptDue = false;
            if (!frame.HasFocus) { leftFilter.Reset(); rightFilter.Reset(); ResetRest(); return null; }
            bool usable = Tracked(frame.Left) || Tracked(frame.Right);
            WaitingForHands = frame.ControllersInHand || !usable;
            waitTicks = WaitingForHands ? waitTicks + 1 : 0;
            if (waitTicks == 600) StartupPromptDue = true;
            // An untracked hand restarts its filter, so a hand that reappears elsewhere does not slide in.
            var left = leftFilter.Step(Tracked(frame.Left) ? frame.Left : null, settings); var right = rightFilter.Step(Tracked(frame.Right) ? frame.Right : null, settings);
            if (frame.ControllersInHand) { ResetRest(); return null; }

            // Keep the hand that is already resting; otherwise prefer the left hand.
            HandSide? candidate = Hand.HasValue && IsFlatPalm(Hand == HandSide.Left ? left : right, settings) ? Hand
                : IsFlatPalm(left, settings) ? HandSide.Left : IsFlatPalm(right, settings) ? HandSide.Right : (HandSide?)null;
            if (candidate != Hand) ResetRest();
            if (!candidate.HasValue) return null;
            var hand = candidate == HandSide.Left ? left : right;
            var palm = hand[JointId.Palm].Position;
            double threshold = settings.StillnessThreshold();
            // A clearly moving palm restarts the second; tremor only raises the average speed.
            if (restTicks > 0 && Vec3.Distance(palm, lastPalm) / GameSettings.Delta > 3 * threshold) ResetRest();
            Hand = candidate; lastPalm = palm; restTicks++;
            bool still = stillness.Step(palm, threshold);
            var length = HandMeasure.Length(candidate == HandSide.Left ? frame.Left : frame.Right);
            if (length.HasValue) { lengthSum += length.Value; lengthCount++; }
            if (!still || lengthCount == 0) return null;

            var result = new PlantingResult(PoseFrom(hand, lengthSum / lengthCount), candidate.Value, lengthSum / lengthCount);
            leftFilter.Reset(); rightFilter.Reset(); ResetRest(); waitTicks = 0;
            return result;
        }

        private ValleyPose PoseFrom(HandSample hand, double handLength)
        {
            // The valley grows from under the palm. The palm joint radius is a width, not a thickness, so a
            // fixed half thickness is used (to be checked on the headset, H2).
            var origin = hand[JointId.Palm].Position - Vec3.Up * settings.PalmHalfThickness;
            // Facing follows the fingers: wrist to middle knuckle, projected onto the floor plane.
            var forward = hand[JointId.MiddleProximal].Position - hand[JointId.Wrist].Position;
            return new ValleyPose(origin, forward, HandMeasure.Scale(handLength, settings));
        }

        private static bool Tracked(HandSample hand) => hand != null && hand[JointId.Wrist].Usable && hand[JointId.Palm].Usable;

        private static readonly JointId[][] Fingers =
        {
            new[] { JointId.IndexProximal, JointId.IndexIntermediate, JointId.IndexDistal, JointId.IndexTip },
            new[] { JointId.MiddleProximal, JointId.MiddleIntermediate, JointId.MiddleDistal, JointId.MiddleTip },
            new[] { JointId.RingProximal, JointId.RingIntermediate, JointId.RingDistal, JointId.RingTip },
            new[] { JointId.LittleProximal, JointId.LittleIntermediate, JointId.LittleDistal, JointId.LittleTip },
        };

        // Flat: the palm plane within the tilt limit of level (palm up or down), and the four fingers straight.
        public static bool IsFlatPalm(HandSample hand, GameSettings settings)
        {
            if (hand == null || settings == null || !Tracked(hand) || !hand[JointId.IndexProximal].Usable || !hand[JointId.LittleProximal].Usable) return false;
            var wrist = hand[JointId.Wrist].Position;
            var normal = Vec3.Cross(hand[JointId.IndexProximal].Position - wrist, hand[JointId.LittleProximal].Position - wrist);
            if (normal.Length < 1e-8) return false;
            if (Math.Abs(normal.Y) / normal.Length < Math.Cos(settings.PlantMaximumTiltDegrees * Math.PI / 180)) return false;
            foreach (var finger in Fingers)
            {
                double along = 0;
                for (int i = 0; i < finger.Length; i++)
                {
                    if (!hand[finger[i]].Usable) return false;
                    if (i > 0) along += Vec3.Distance(hand[finger[i - 1]].Position, hand[finger[i]].Position);
                }
                if (along < 1e-6 || Vec3.Distance(hand[finger[0]].Position, hand[finger[finger.Length - 1]].Position) / along < settings.FingerStraightness) return false;
            }
            return true;
        }
    }
}
