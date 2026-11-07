#if PALMFOLK_DEVELOPMENT
using System;
using System.Collections.Generic;
using Palmfolk.Core;

namespace Palmfolk.Developer
{
    public readonly struct DropoutRecord
    {
        public readonly long StartTick, DurationTicks;
        public readonly uint AffectedJoints;
        public readonly bool Disruptive;
        public readonly HandSide Hand;
        public readonly Vec3 FingerDirection;
        public DropoutRecord(long start, long duration, uint joints, bool disruptive, HandSide hand, Vec3 direction)
        { StartTick = start; DurationTicks = duration; AffectedJoints = joints; Disruptive = disruptive; Hand = hand; FingerDirection = direction; }
    }
    public sealed class DropoutWatcher
    {
        private long start, duration;
        private uint joints;
        private bool disruptive, countedDisruptive;
        private HandSide hand;
        private Vec3 direction;
        private readonly List<DropoutRecord> completed = new List<DropoutRecord>();
        public int Count { get; private set; }
        public int DisruptiveCount { get; private set; }
        public IReadOnlyList<DropoutRecord> Completed => completed.AsReadOnly();
        public void Step(long tick, HandSide owner, bool supportingOrBuilding, uint invalidJoints, bool disrupted, Vec3 fingerDirection)
        {
            if (!supportingOrBuilding || invalidJoints == 0 || (duration > 0 && hand != owner)) EndEpisode();
            if (!supportingOrBuilding || invalidJoints == 0) return;
            if (duration == 0) { start = tick; hand = owner; direction = fingerDirection.Normalized; }
            duration++; joints |= invalidJoints; disruptive |= disrupted;
            if (duration == 15) Count++;
            if (duration >= 15 && disruptive && !countedDisruptive) { DisruptiveCount++; countedDisruptive = true; }
        }
        public void EndEpisode()
        {
            if (duration >= 15) completed.Add(new DropoutRecord(start, duration, joints, disruptive, hand, direction));
            duration = 0; joints = 0; disruptive = countedDisruptive = false;
        }
    }
    public sealed class PlaytestMetrics
    {
        public long? BoardingTick { get; private set; }
        public long? FirstPersistentBridgeTick { get; private set; }
        public int Hints { get; private set; }
        public int Refusals { get; private set; }
        public double FramesPerSecond { get; private set; }
        private double seconds;
        private int frames;
        private int idleTicks;
        public long StuckTicks { get; private set; }
        public void Tick(long tick, bool progressed, bool running)
        {
            if (!running || progressed) { idleTicks = 0; return; }
            idleTicks++; if (idleTicks == 600) StuckTicks += 600; else if (idleTicks > 600) StuckTicks++;
        }
        public void Observe(IReadOnlyList<GameEvent> events)
        {
            foreach (var e in events)
            {
                if (e.Kind == GameEventKind.Boarded && !BoardingTick.HasValue) BoardingTick = e.Tick;
                if (e.Kind == GameEventKind.PersistentBridgeObserved && !FirstPersistentBridgeTick.HasValue) FirstPersistentBridgeTick = e.Tick;
                if (e.Kind == GameEventKind.HintDue) Hints++;
                if (e.Kind == GameEventKind.Refused) Refusals++;
            }
        }
        public void FrameRendered(double elapsedSeconds)
        {
            if (!Vec3.Finite(elapsedSeconds) || elapsedSeconds <= 0) throw new ArgumentOutOfRangeException();
            seconds += elapsedSeconds; frames++;
            if (seconds >= 1) { FramesPerSecond = frames / seconds; frames = 0; seconds = 0; }
        }
    }
    public readonly struct RadiusSummary
    {
        public readonly long Valid, Missing;
        public readonly double Minimum, Maximum, Mean;
        public RadiusSummary(long valid, long missing, double min, double max, double mean) { Valid = valid; Missing = missing; Minimum = min; Maximum = max; Mean = mean; }
    }
    public sealed class RadiusWatcher
    {
        private readonly long[,] valid = new long[2, 26], missing = new long[2, 26];
        private readonly double[,] min = new double[2, 26], max = new double[2, 26], sum = new double[2, 26];
        public void Observe(HandSample hand)
        {
            if (hand == null) return; int side = (int)hand.Side;
            for (int i = 0; i < 26; i++)
            {
                var joint = hand.Joints[i];
                if (!joint.Usable || !joint.HasRadius) { missing[side, i]++; continue; }
                if (valid[side, i] == 0) min[side, i] = max[side, i] = joint.Radius;
                min[side, i] = Math.Min(min[side, i], joint.Radius); max[side, i] = Math.Max(max[side, i], joint.Radius);
                sum[side, i] += joint.Radius; valid[side, i]++;
            }
        }
        public RadiusSummary Summary(HandSide side, JointId joint)
        {
            int h = (int)side, j = (int)joint;
            if (h < 0 || h > 1 || j < 0 || j >= 26) throw new ArgumentOutOfRangeException();
            return new RadiusSummary(valid[h, j], missing[h, j], min[h, j], max[h, j], valid[h, j] == 0 ? 0 : sum[h, j] / valid[h, j]);
        }
    }
}
#endif
