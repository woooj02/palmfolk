using System;
using System.Collections.Generic;

namespace Palmfolk.Core
{
    public enum TravelerKind { Walker, Group, Cart }
    public enum Refusal { Unanchored, TooNarrow, TooSteep, LowClearance, SelfCrossing, TooLong }
    public readonly struct ValidationFailure
    {
        public readonly Refusal Reason;
        public readonly int Curve, Segment;
        public ValidationFailure(Refusal reason, int curve, int segment) { Reason = reason; Curve = curve; Segment = segment; }
    }
    public sealed class CurveTree
    {
        public IReadOnlyList<Route> Curves { get; }
        public IReadOnlyList<int> Parents { get; }
        public CurveTree(Route[] curves, int[] parents)
        {
            if (curves == null || parents == null || curves.Length == 0 || curves.Length > 16 || parents.Length != curves.Length || parents[0] != -1) throw new ArgumentException("Invalid curve tree.");
            for (int i = 0; i < curves.Length; i++)
            {
                if (curves[i] == null || (i > 0 && (parents[i] < 0 || parents[i] >= i))) throw new ArgumentException("Invalid curve parent.");
                if (i > 0 && Vec3.Distance(curves[i].Start, curves[parents[i]].End) > 1e-6) throw new ArgumentException("Disconnected curve junction.");
            }
            Curves = Array.AsReadOnly((Route[])curves.Clone()); Parents = Array.AsReadOnly((int[])parents.Clone());
        }
        public static CurveTree Single(Route route) => new CurveTree(new[] { route }, new[] { -1 });
    }
    public readonly struct Obstacle
    {
        public readonly Vec3 Min, Max;
        public Obstacle(Vec3 min, Vec3 max)
        {
            if (!min.IsFinite || !max.IsFinite || min.X >= max.X || min.Y >= max.Y || min.Z >= max.Z) throw new ArgumentException("Invalid obstacle bounds.");
            Min = min; Max = max;
        }
    }
    public interface IClearanceField { double MinimumHeadroom(Vec3 from, Vec3 to, double deckWidth); }
    // Free headroom above a deck segment, from axis-aligned obstacle boxes.
    public sealed class ObstacleField : IClearanceField
    {
        public IReadOnlyList<Obstacle> Obstacles { get; }
        public ObstacleField(params Obstacle[] obstacles)
        {
            if (obstacles == null) throw new ArgumentException("Invalid obstacles.");
            Obstacles = Array.AsReadOnly((Obstacle[])obstacles.Clone());
        }
        public double MinimumHeadroom(Vec3 from, Vec3 to, double deckWidth)
        {
            double result = double.PositiveInfinity;
            var tangent = new Vec3(to.X - from.X, 0, to.Z - from.Z).Normalized;
            var sideways = new Vec3(-tangent.Z, 0, tangent.X) * (deckWidth / 2);
            foreach (var box in Obstacles)
            {
                // Clip in (segment parameter, sideways parameter) space. This
                // preserves the correlation between x/z at diagonal deck ends.
                var polygon = new List<Vec3> { new Vec3(0, 0, -1), new Vec3(1, 0, -1), new Vec3(1, 0, 1), new Vec3(0, 0, 1) };
                polygon = Clip(polygon, to.X - from.X, sideways.X, box.Min.X - from.X);
                polygon = Clip(polygon, from.X - to.X, -sideways.X, from.X - box.Max.X);
                polygon = Clip(polygon, to.Z - from.Z, sideways.Z, box.Min.Z - from.Z);
                polygon = Clip(polygon, from.Z - to.Z, -sideways.Z, from.Z - box.Max.Z);
                if (polygon.Count == 0) continue;
                double a = 1, b = 0;
                foreach (var point in polygon) { a = Math.Min(a, point.X); b = Math.Max(b, point.X); }
                double y0 = from.Y + (to.Y - from.Y) * a, y1 = from.Y + (to.Y - from.Y) * b;
                if (Math.Min(y0, y1) >= box.Max.Y) continue;
                result = Math.Min(result, Math.Max(0, box.Min.Y - Math.Max(y0, y1)));
            }
            return result;
        }
        private static List<Vec3> Clip(List<Vec3> polygon, double along, double sideways, double limit)
        {
            var result = new List<Vec3>(); if (polygon.Count == 0) return result;
            var previous = polygon[polygon.Count - 1]; double before = along * previous.X + sideways * previous.Z - limit;
            foreach (var current in polygon)
            {
                double after = along * current.X + sideways * current.Z - limit;
                if ((before >= 0) != (after >= 0)) result.Add(Vec3.Lerp(previous, current, before / (before - after)));
                if (after >= 0) result.Add(current);
                previous = current; before = after;
            }
            return result;
        }
    }
    public sealed class LevelGeometry : IClearanceField
    {
        private readonly ObstacleField field;
        public Vec3 Start { get; }
        public Vec3 End { get; }
        public IReadOnlyList<Obstacle> Obstacles => field.Obstacles;
        public LevelGeometry(Vec3 start, Vec3 end, params Obstacle[] obstacles)
        {
            if (!start.IsFinite || !end.IsFinite || Vec3.Distance(start, end) < 0.01 || obstacles == null) throw new ArgumentException("Invalid level anchors.");
            Start = start; End = end; field = new ObstacleField(obstacles);
        }
        public double MinimumHeadroom(Vec3 from, Vec3 to, double deckWidth) => field.MinimumHeadroom(from, to, deckWidth);
    }
    public static class BridgeValidator
    {
        public static IReadOnlyList<ValidationFailure> Validate(CurveTree tree, Vec3 start, Vec3 end, TravelerKind kind, GameSettings settings, IClearanceField clearance)
        {
            if (settings == null) throw new ArgumentException("Invalid validation input.");
            return Validate(tree, new AnchorZone(start, settings.ConnectDistance), new[] { new AnchorZone(end, settings.ConnectDistance) }, kind, settings, clearance);
        }
        // Zone anchors: the root curve starts in `start`; every leaf ends in one of `ends`, and every end zone is reached.
        public static IReadOnlyList<ValidationFailure> Validate(CurveTree tree, AnchorZone start, IReadOnlyList<AnchorZone> ends, TravelerKind kind, GameSettings settings, IClearanceField clearance)
        {
            if (tree == null || clearance == null || settings == null || ends == null || ends.Count == 0 || !Enum.IsDefined(typeof(TravelerKind), kind)) throw new ArgumentException("Invalid validation input.");
            var failures = new List<ValidationFailure>();
            var reached = new bool[ends.Count];
            double minWidth = MinimumWidth(kind);
            double maxSlope = kind == TravelerKind.Walker ? 35 : 25;
            double minHeight = kind == TravelerKind.Cart ? 0.05 : 0.035;
            for (int c = 0; c < tree.Curves.Count; c++)
            {
                var route = tree.Curves[c];
                bool leaf = true; for (int i = c + 1; i < tree.Parents.Count; i++) if (tree.Parents[i] == c) leaf = false;
                bool endAnchored = false;
                if (leaf) for (int e = 0; e < ends.Count; e++) if (ends[e].Contains(route.End)) { reached[e] = true; endAnchored = true; }
                if ((c == 0 && !start.Contains(route.Start)) || (leaf && !endAnchored)) failures.Add(new ValidationFailure(Refusal.Unanchored, c, 0));
                if (route.Length > settings.MaximumRouteLength) failures.Add(new ValidationFailure(Refusal.TooLong, c, 0));
                for (int i = 0; i < route.Widths.Count; i++)
                {
                    Vec3 a = route.Points[i], b = route.Points[i + 1], delta = b - a;
                    if (route.Widths[i] < minWidth) failures.Add(new ValidationFailure(Refusal.TooNarrow, c, i));
                    double slope = Math.Atan2(Math.Abs(delta.Y), Math.Sqrt(delta.X * delta.X + delta.Z * delta.Z)) * 180 / Math.PI;
                    if (slope > maxSlope) failures.Add(new ValidationFailure(Refusal.TooSteep, c, i));
                    if (clearance.MinimumHeadroom(a, b, route.Widths[i]) < minHeight) failures.Add(new ValidationFailure(Refusal.LowClearance, c, i));
                    for (int j = i + 2; j < route.Widths.Count; j++)
                        if (SegmentDistance(a, b, route.Points[j], route.Points[j + 1]) < 0.001) failures.Add(new ValidationFailure(Refusal.SelfCrossing, c, i));
                }
            }
            // An end zone no branch reaches (a missing Fork branch) is reported on the root curve.
            for (int e = 0; e < ends.Count; e++)
                if (!reached[e] && !failures.Exists(f => f.Reason == Refusal.Unanchored)) failures.Add(new ValidationFailure(Refusal.Unanchored, 0, 0));
            return failures.AsReadOnly();
        }
        public static double MinimumWidth(TravelerKind kind) => kind == TravelerKind.Walker ? 0.01 : 0.025;
        private static double SegmentDistance(Vec3 p, Vec3 q, Vec3 r, Vec3 s)
        {
            Vec3 u = q - p, v = s - r, w = p - r;
            double a = Vec3.Dot(u, u), b = Vec3.Dot(u, v), c = Vec3.Dot(v, v), d = Vec3.Dot(u, w), e = Vec3.Dot(v, w);
            double denominator = a * c - b * b;
            if (denominator > 1e-20)
            {
                double x = (b * e - c * d) / denominator, y = (a * e - b * d) / denominator;
                if (x >= 0 && x <= 1 && y >= 0 && y <= 1) return Vec3.Distance(p + u * x, r + v * y);
            }
            return Math.Min(Math.Min(PointDistance(p, r, s), PointDistance(q, r, s)), Math.Min(PointDistance(r, p, q), PointDistance(s, p, q)));
        }
        public static double PointDistance(Vec3 p, Vec3 a, Vec3 b)
        { var d = b - a; double length = Vec3.Dot(d, d); return Vec3.Distance(p, a + d * (length > 1e-20 ? Vec3.Clamp(Vec3.Dot(p - a, d) / length, 0, 1) : 0)); }
    }
}
