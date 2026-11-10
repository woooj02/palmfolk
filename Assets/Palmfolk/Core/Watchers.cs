using System;

namespace Palmfolk.Core
{
    public sealed class ConnectionWatcher
    {
        public bool Connected { get; private set; }
        public int HeldTicks { get; private set; }
        public bool Step(double distance, bool usable, GameSettings settings)
        {
            bool before = Connected;
            if (!usable || !Vec3.Finite(distance)) { Connected = false; HeldTicks = 0; }
            else if (Connected) { if (distance > settings.BreakDistance) { Connected = false; HeldTicks = 0; } }
            else if (distance < settings.ConnectDistance) { if (++HeldTicks >= settings.ConnectTicks) Connected = true; }
            else HeldTicks = 0;
            return before != Connected;
        }
        public void Reset() { Connected = false; HeldTicks = 0; }
    }

    public sealed class OneEuroFilter
    {
        private bool initialized;
        private Vec3 raw, filtered, derivative;
        private static double Alpha(double cutoff) => 1 / (1 + 1 / (2 * Math.PI * cutoff * GameSettings.Delta));
        public Vec3 Step(Vec3 value, GameSettings settings)
        {
            if (!value.IsFinite) throw new ArgumentException("Non-finite filter input.");
            if (!initialized) { initialized = true; raw = filtered = value; derivative = Vec3.Zero; return value; }
            derivative = Vec3.Lerp(derivative, (value - raw) / GameSettings.Delta, Alpha(1));
            filtered = Vec3.Lerp(filtered, value, Alpha(settings.FilterCutoff + settings.FilterBeta * derivative.Length));
            raw = value; return filtered;
        }
        public void Reset() { initialized = false; }
    }

    public sealed class HandFilter
    {
        private readonly OneEuroFilter[] filters = new OneEuroFilter[26];
        public HandFilter() { for (int i = 0; i < 26; i++) filters[i] = new OneEuroFilter(); }
        public HandSample Step(HandSample hand, GameSettings settings)
        {
            if (hand == null) { Reset(); return null; }
            var joints = new JointSample[26];
            for (int i = 0; i < 26; i++)
            {
                joints[i] = hand.Joints[i];
                if (joints[i].Usable) joints[i] = joints[i].WithPosition(filters[i].Step(joints[i].Position, settings));
                else filters[i].Reset();
            }
            return new HandSample(hand.Side, joints);
        }
        public void Reset() { foreach (var filter in filters) filter.Reset(); }
    }

    public sealed class StillnessWatcher
    {
        private readonly double[] speeds;
        private int count, index;
        private double sum;
        private Vec3 previous;
        private bool hasPrevious;
        public StillnessWatcher(int windowTicks) { if (windowTicks < 1) throw new ArgumentOutOfRangeException(); speeds = new double[windowTicks]; }
        public bool Step(Vec3 point, double threshold)
        {
            if (!hasPrevious) { previous = point; hasPrevious = true; return false; }
            double speed = Vec3.Distance(previous, point) / GameSettings.Delta; previous = point;
            if (count == speeds.Length) sum -= speeds[index]; else count++;
            speeds[index] = speed; sum += speed; index = (index + 1) % speeds.Length;
            return count == speeds.Length && sum / count < threshold;
        }
        public void Reset() { count = index = 0; sum = 0; hasPrevious = false; Array.Clear(speeds, 0, speeds.Length); }
    }

    public enum WidthTier { Rope, Plank }
    public sealed class WidthWatcher
    {
        public WidthTier Tier { get; private set; }
        public WidthTier Step(double width)
        {
            // Conservative hysteresis: a cart never passes on a width below 2.5 cm.
            if (Tier == WidthTier.Rope && width >= 0.027) Tier = WidthTier.Plank;
            else if (width < 0.025) Tier = WidthTier.Rope;
            return Tier;
        }
    }

    public sealed class HintWatcher
    {
        private int idleTicks, poorTicks;
        private bool qualityPrompted;
        public bool HintDue { get; private set; }
        public bool QualityPromptDue { get; private set; }
        public int StillnessAdjustment { get; private set; }
        public void Step(bool progressed, bool poorTracking)
        {
            HintDue = QualityPromptDue = false;
            idleTicks = progressed ? 0 : idleTicks + 1;
            if (idleTicks >= 600) { HintDue = true; idleTicks = 0; }
            poorTicks = poorTracking ? poorTicks + 1 : 0;
            if (poorTicks >= 1200 && !qualityPrompted)
            {
                qualityPrompted = true; StillnessAdjustment = 1; QualityPromptDue = true;
            }
        }
    }
}
