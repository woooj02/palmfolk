using System;
using System.Collections.Generic;

namespace Palmfolk.Core
{
    public static class HandRoutes
    {
        private static readonly int[][] Chains =
        {
            new[] { 5, 4, 3, 2, 1 }, new[] { 10, 9, 8, 7, 6, 1 },
            new[] { 15, 14, 13, 12, 11, 1 }, new[] { 20, 19, 18, 17, 16, 1 }, new[] { 25, 24, 23, 22, 21, 1 }
        };
        public static uint RequiredJointMask(int finger)
        {
            if (finger < 0 || finger >= Chains.Length) throw new ArgumentOutOfRangeException(nameof(finger));
            uint mask = (1u << 1) | (1u << 6) | (1u << 11) | (1u << 16) | (1u << 21);
            foreach (int id in Chains[finger]) mask |= 1u << id; return mask;
        }
        public static Vec3[] PalmPolygon(HandSample hand)
        {
            if (hand == null) return Array.Empty<Vec3>();
            int[] ids = { 1, 6, 11, 16, 21 };
            var polygon = new Vec3[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                if (!hand.Joints[ids[i]].Usable) return Array.Empty<Vec3>();
                polygon[i] = hand.Joints[ids[i]].Position;
            }
            return polygon;
        }
        public static Route Create(HandSample hand, int finger, bool reversed, GameSettings settings)
        {
            if (hand == null || finger < 0 || finger >= Chains.Length) return null;
            var polygon = PalmPolygon(hand);
            if (polygon.Length == 0) return null;
            Vec3 normal = Vec3.Cross(polygon[1] - polygon[0], polygon[4] - polygon[0]).Normalized;
            if (hand.Side == HandSide.Right) normal = normal * -1;
            if (Vec3.Dot(normal, Vec3.Up) <= 0) return null;
            var chain = Chains[finger];
            foreach (int id in chain) if (!hand.Joints[id].Usable) return null;

            // Merge neighboring surfaces only when their gaps stay under 1 cm
            // for a majority of five longitudinal samples (excluding the shared wrist).
            int first = finger, last = finger;
            while (first > 0 && Merged(hand, first - 1, first, settings)) first--;
            while (last < 4 && Merged(hand, last, last + 1, settings)) last++;
            bool radii = settings.UseJointRadii;
            for (int f = first; f <= last; f++) foreach (int id in Chains[f]) radii &= hand.Joints[id].HasRadius;
            double width = last == first ? 0.012 : (last - first + 1) * 0.02;
            if (radii)
            {
                width = double.PositiveInfinity;
                for (int sample = 0; sample < 5; sample++)
                {
                    double span = 0;
                    for (int f = first; f <= last; f++)
                    {
                        SampleFinger(hand, f, sample / 4.0, true, out var center, out double radius);
                        span += radius * 2;
                        if (f > first)
                        {
                            SampleFinger(hand, f - 1, sample / 4.0, true, out var previous, out double otherRadius);
                            span += Math.Max(0, Vec3.Distance(center, previous) - radius - otherRadius);
                        }
                    }
                    width = Math.Min(width, span);
                }
                // The narrowest finger-to-palm tier controls the entire route.
                width = Math.Min(width, hand[JointId.Wrist].Radius * 2);
            }
            var points = new Vec3[chain.Length]; var widths = new double[chain.Length - 1];
            for (int i = 0; i < chain.Length; i++)
            {
                var joint = hand.Joints[chain[i]];
                points[i] = joint.Position + normal * (radii ? joint.Radius : 0.006);
                if (i > 0) widths[i - 1] = width;
            }
            try { var route = new Route(points, widths, finger); return reversed ? route.Reverse() : route; }
            catch (ArgumentException) { return null; }
        }
        private static bool Merged(HandSample hand, int a, int b, GameSettings settings)
        {
            foreach (int id in Chains[a]) if (!hand.Joints[id].Usable) return false;
            foreach (int id in Chains[b]) if (!hand.Joints[id].Usable) return false;
            int touching = 0;
            for (int sample = 0; sample < 5; sample++)
            {
                SampleFinger(hand, a, sample / 4.0, settings.UseJointRadii, out var x, out double rx);
                SampleFinger(hand, b, sample / 4.0, settings.UseJointRadii, out var y, out double ry);
                if (Vec3.Distance(x, y) - rx - ry < 0.01) touching++;
            }
            return touching >= 3;
        }
        private static void SampleFinger(HandSample hand, int finger, double t, bool radii, out Vec3 point, out double radius)
        {
            var chain = Chains[finger]; int count = chain.Length - 1;
            double position = t * (count - 1); int i = Math.Min(count - 2, (int)position); double u = position - i;
            var a = hand.Joints[chain[i]]; var b = hand.Joints[chain[i + 1]];
            point = Vec3.Lerp(a.Position, b.Position, u);
            radius = radii && a.HasRadius && b.HasRadius ? a.Radius + (b.Radius - a.Radius) * u : 0.006;
        }
        public static Route Best(HandSample hand, Vec3 start, Vec3 end, GameSettings settings)
        {
            Route best = null; double score = double.PositiveInfinity;
            for (int finger = 0; finger < 5; finger++)
            {
                var route = Create(hand, finger, false, settings);
                if (route == null) continue;
                for (int direction = 0; direction < 2; direction++)
                {
                    double candidate = Vec3.Distance(route.Start, start) + Vec3.Distance(route.End, end);
                    if (candidate < score) { score = candidate; best = route; }
                    route = route.Reverse();
                }
            }
            return best;
        }
    }
}
