using System;
using System.Collections.Generic;

namespace Palmfolk.Core
{
    // Which fingers make each fork branch, by end. Fixed when the hand is first read, like a route's finger.
    public sealed class ForkLayout
    {
        public IReadOnlyList<IReadOnlyList<int>> Branches { get; }
        public ForkLayout(int[][] branches)
        {
            if (branches == null || branches.Length < 2) throw new ArgumentException("A fork has at least two branches.");
            var list = new IReadOnlyList<int>[branches.Length];
            for (int i = 0; i < branches.Length; i++)
            {
                if (branches[i] == null || branches[i].Length == 0) throw new ArgumentException("Empty fork branch.");
                list[i] = Array.AsReadOnly((int[])branches[i].Clone());
            }
            Branches = Array.AsReadOnly(list);
        }
    }
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
            double width = Width(hand, first, last, settings, out bool radii);
            // The narrowest finger-to-palm tier controls the entire route.
            if (radii) width = Math.Min(width, hand[JointId.Wrist].Radius * 2);
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
        // Width of the surface made by fingers first..last (merged), from joint radii when every one has them.
        private static double Width(HandSample hand, int first, int last, GameSettings settings, out bool radii)
        {
            radii = settings.UseJointRadii;
            for (int f = first; f <= last; f++) foreach (int id in Chains[f]) radii &= hand.Joints[id].HasRadius;
            if (!radii) return last == first ? 0.012 : (last - first + 1) * 0.02;
            double width = double.PositiveInfinity;
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
            return width;
        }

        // Palm normal facing up, or null when the palm can't carry anything.
        private static Vec3? UpNormal(HandSample hand)
        {
            var normal = PalmNormal(hand);
            return normal.HasValue && Vec3.Dot(normal.Value, Vec3.Up) > 0 ? normal : null;
        }

        // The direction the palm faces (out of the palm), or null when the palm joints aren't usable.
        public static Vec3? PalmNormal(HandSample hand)
        {
            var polygon = PalmPolygon(hand);
            if (polygon.Length == 0) return null;
            Vec3 normal = Vec3.Cross(polygon[1] - polygon[0], polygon[4] - polygon[0]).Normalized;
            return hand.Side == HandSide.Right ? normal * -1 : normal;
        }

        // Fingers (index to little) grouped into surfaces: neighbours with a gap under 1 cm merge (Definitions).
        public static List<int[]> Clusters(HandSample hand, GameSettings settings)
        {
            if (hand == null || settings == null) return null;
            var clusters = new List<int[]>();
            int start = 1;
            for (int f = 1; f <= 4; f++)
            {
                foreach (int id in Chains[f]) if (!hand.Joints[id].Usable) return null;
                if (f == 4 || !Merged(hand, f, f + 1, settings))
                {
                    var c = new int[f - start + 1]; for (int i = 0; i < c.Length; i++) c[i] = start + i;
                    clusters.Add(c); start = f + 1;
                }
            }
            return clusters;
        }

        // Fork (T4, I1): a trunk from the wrist to a junction at the metacarpal joints, then one branch per finger
        // surface, branch k aimed at end k. Fingers not aimed at an end are not built. Null if the surfaces changed.
        public static CurveTree Fork(HandSample hand, ForkLayout layout, GameSettings settings)
        {
            if (hand == null || layout == null || settings == null) return null;
            var normal = UpNormal(hand); if (!normal.HasValue) return null;
            var clusters = Clusters(hand, settings); if (clusters == null) return null;
            foreach (var branch in layout.Branches)
            {
                bool found = false;
                foreach (var c in clusters) if (c.Length == branch.Count && c[0] == branch[0]) found = true;
                if (!found) return null;
            }
            var n = normal.Value; bool radii = settings.UseJointRadii;
            Vec3 Lift(JointSample j) => j.Position + n * (radii && j.HasRadius ? j.Radius : 0.006);
            Vec3 Average(IReadOnlyList<int> fingers, int bone)
            {
                var sum = Vec3.Zero; foreach (int f in fingers) sum += Lift(hand.Joints[Chains[f][bone]]);
                return sum * (1.0 / fingers.Count);
            }
            var junction = Vec3.Zero; int count = 0;
            foreach (var branch in layout.Branches) foreach (int f in branch) { junction += Lift(hand.Joints[Chains[f][4]]); count++; }
            junction = junction * (1.0 / count);
            var curves = new Route[layout.Branches.Count + 1]; var parents = new int[curves.Length];
            double trunkWidth = 0;
            try
            {
                for (int b = 0; b < layout.Branches.Count; b++)
                {
                    var fingers = layout.Branches[b];
                    double width = Width(hand, fingers[0], fingers[fingers.Count - 1], settings, out _);
                    trunkWidth += width;
                    var points = new[] { junction, Average(fingers, 3), Average(fingers, 2), Average(fingers, 1), Average(fingers, 0) };
                    curves[b + 1] = new Route(points, new[] { width, width, width, width }, fingers[0]);
                    parents[b + 1] = 0;
                }
                curves[0] = new Route(new[] { Lift(hand.Joints[1]), junction }, new[] { trunkWidth }, layout.Branches[0][0]);
                parents[0] = -1;
                return new CurveTree(curves, parents);
            }
            catch (ArgumentException) { return null; }
        }

        // The fork that best spans start (at the wrist) to the ends (at the fingertips), or null if the hand has too
        // few separate surfaces. Each end gets its own surface.
        public static CurveTree BestFork(HandSample hand, Vec3 start, IReadOnlyList<Vec3> ends, GameSettings settings, out ForkLayout layout)
        {
            layout = null;
            if (hand == null || ends == null || ends.Count < 2 || ends.Count > 4 || !UpNormal(hand).HasValue || !hand.Joints[1].Usable) return null;
            var clusters = Clusters(hand, settings);
            if (clusters == null || clusters.Count < ends.Count) return null;
            int[] best = null; double score = double.PositiveInfinity;
            var choice = new int[ends.Count];
            void Search(int k, int used, double sum)
            {
                if (sum >= score) return;
                if (k == ends.Count) { score = sum; best = (int[])choice.Clone(); return; }
                for (int c = 0; c < clusters.Count; c++)
                {
                    if ((used & (1 << c)) != 0) continue;
                    var tip = Vec3.Zero; foreach (int f in clusters[c]) tip += hand.Joints[Chains[f][0]].Position;
                    tip = tip * (1.0 / clusters[c].Length);
                    choice[k] = c; Search(k + 1, used | (1 << c), sum + Vec3.Distance(tip, ends[k]));
                }
            }
            Search(0, 0, Vec3.Distance(hand.Joints[1].Position, start));
            if (best == null) return null;
            var branches = new int[ends.Count][];
            for (int k = 0; k < ends.Count; k++) branches[k] = clusters[best[k]];
            layout = new ForkLayout(branches);
            return Fork(hand, layout, settings);
        }

        // One walk through a fork: the trunk, then branch k. The hand loop (contact, stillness, ghost) follows it.
        public static Route Spine(CurveTree fork, int branch = 0)
        {
            var trunk = fork.Curves[0]; var tip = fork.Curves[branch + 1];
            var points = new List<Vec3>(trunk.Points); var widths = new List<double>(trunk.Widths);
            for (int p = 1; p < tip.Points.Count; p++) { points.Add(tip.Points[p]); widths.Add(tip.Widths[p - 1]); }
            return new Route(points.ToArray(), widths.ToArray(), tip.Finger);
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
