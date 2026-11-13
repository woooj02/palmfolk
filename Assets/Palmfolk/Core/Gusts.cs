using System;

namespace Palmfolk.Core
{
    public enum GustPhase { Gathering, Blowing }

    // The wall sculpt (concept change, Nov 13): while travelers cross, gusts gather at the valley's edge and blow at
    // them. An upright hand standing between the gust and the traveler shields them; a gust that gets through makes
    // the travelers brace and wait. Positions are valley-local. Deterministic: gusts come on a fixed schedule and
    // alternate sides.
    public sealed class Gust
    {
        public Vec3 Origin { get; }
        public Vec3 Target { get; internal set; }
        public GustPhase Phase { get; internal set; }
        // 0 to 1 through the current phase.
        public double Progress { get; internal set; }
        // The hand is shielding this tick.
        public bool Shielded { get; internal set; }
        internal int Ticks, ShieldedTicks;
        internal Gust(Vec3 origin, Vec3 target) { Origin = origin; Target = target; }
    }

    public sealed class GustField
    {
        public const int FirstDelay = 90, Interval = 420, GatherTicks = 150, BlowTicks = 60;
        // How far out the gust gathers, and how far from the hand's centre the wind may cross its plane.
        public const double Reach = 0.16, HandRadius = 0.085, UprightLimit = 0.6;
        private int untilNext = FirstDelay, count;
        public Gust Current { get; private set; }
        public int Blocked { get; private set; }
        public int Hits { get; private set; }

        public void Reset() { untilNext = FirstDelay; Current = null; }

        // One tick while travelers walk. `target` is the traveler to protect, `heading` the way they walk.
        // Returns the event of this tick, if any.
        public GameEventKind? Step(HandFrame frame, Vec3 target, Vec3 heading)
        {
            if (Current == null)
            {
                if (--untilNext > 0) return null;
                var flat = new Vec3(heading.X, 0, heading.Z); flat = flat.Length < 1e-9 ? new Vec3(1, 0, 0) : flat.Normalized;
                var side = Vec3.Cross(Vec3.Up, flat).Normalized * ((count++ & 1) == 0 ? 1 : -1);
                Current = new Gust(target + side * Reach + Vec3.Up * 0.01, target);
                return GameEventKind.GustGathering;
            }
            var gust = Current; gust.Target = target; gust.Ticks++;
            gust.Shielded = frame != null && (Shields(frame.Left, gust.Origin, target) || Shields(frame.Right, gust.Origin, target));
            if (gust.Phase == GustPhase.Gathering)
            {
                gust.Progress = gust.Ticks / (double)GatherTicks;
                if (gust.Ticks >= GatherTicks) { gust.Phase = GustPhase.Blowing; gust.Ticks = 0; gust.Progress = 0; }
                return null;
            }
            if (gust.Shielded) gust.ShieldedTicks++;
            gust.Progress = gust.Ticks / (double)BlowTicks;
            if (gust.Ticks < BlowTicks) return null;
            Current = null; untilNext = Interval;
            if (gust.ShieldedTicks * 2 >= BlowTicks) { Blocked++; return GameEventKind.GustBlocked; }
            Hits++; return GameEventKind.GustHit;
        }

        // An upright hand whose palm plane cuts the wind's path close to the hand.
        public static bool Shields(HandSample hand, Vec3 origin, Vec3 target)
        {
            if (hand == null || !hand[JointId.Palm].Usable || !hand[JointId.MiddleTip].Usable || !hand[JointId.Wrist].Usable) return false;
            var normal = HandRoutes.PalmNormal(hand);
            if (!normal.HasValue || Math.Abs(normal.Value.Y) > UprightLimit) return false;
            var n = normal.Value; var palm = hand[JointId.Palm].Position;
            double d0 = Vec3.Dot(origin - palm, n), d1 = Vec3.Dot(target - palm, n);
            if (d0 * d1 >= 0) return false;
            var crossing = origin + (target - origin) * (d0 / (d0 - d1));
            var centre = Vec3.Lerp(palm, hand[JointId.MiddleTip].Position, 0.4);
            return Vec3.Distance(crossing, centre) <= HandRadius;
        }
    }
}
