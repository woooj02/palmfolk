using System;
using System.Collections.Generic;

namespace Palmfolk.Core
{
    public readonly struct Vec3 : IEquatable<Vec3>
    {
        public readonly double X, Y, Z;
        public Vec3(double x, double y, double z) { X = x; Y = y; Z = z; }
        public static Vec3 Zero => new Vec3(0, 0, 0);
        public static Vec3 Up => new Vec3(0, 1, 0);
        public double Length => Math.Sqrt(Dot(this, this));
        public bool IsFinite => Finite(X) && Finite(Y) && Finite(Z);
        public Vec3 Normalized => Length > 1e-10 ? this / Length : Zero;
        public static bool Finite(double x) => !double.IsNaN(x) && !double.IsInfinity(x);
        public static double Clamp(double x, double a, double b) => Math.Max(a, Math.Min(b, x));
        public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vec3 operator *(Vec3 a, double b) => new Vec3(a.X * b, a.Y * b, a.Z * b);
        public static Vec3 operator /(Vec3 a, double b) => a * (1 / b);
        public static double Dot(Vec3 a, Vec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        public static Vec3 Cross(Vec3 a, Vec3 b) => new Vec3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
        public static double Distance(Vec3 a, Vec3 b) => (a - b).Length;
        public static Vec3 Lerp(Vec3 a, Vec3 b, double t) => a + (b - a) * Clamp(t, 0, 1);
        public bool Equals(Vec3 b) => X.Equals(b.X) && Y.Equals(b.Y) && Z.Equals(b.Z);
        public override bool Equals(object obj) => obj is Vec3 b && Equals(b);
        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
        public override string ToString() => $"({X:R},{Y:R},{Z:R})";
    }

    public readonly struct Rotation
    {
        public readonly double X, Y, Z, W;
        public Rotation(double x, double y, double z, double w)
        {
            if (!Vec3.Finite(x) || !Vec3.Finite(y) || !Vec3.Finite(z) || !Vec3.Finite(w)) throw new ArgumentException("Non-finite rotation.");
            X = x; Y = y; Z = z; W = w;
        }
        public static Rotation Identity => new Rotation(0, 0, 0, 1);
        // Rotation by `radians` about world up (+Y); Unity's Quaternion.Euler(0, degrees, 0).
        public static Rotation AboutUp(double radians) => new Rotation(0, Math.Sin(radians / 2), 0, Math.Cos(radians / 2));
        public Rotation Inverse => new Rotation(-X, -Y, -Z, W);
        // Hamilton product: apply b, then a (same order as Unity's a * b).
        public static Rotation operator *(Rotation a, Rotation b) => new Rotation(
            a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
            a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X,
            a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W,
            a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z);
    }

    public readonly struct RouteCursor
    {
        public readonly int Segment;
        public readonly double Fraction;
        public RouteCursor(int segment, double fraction)
        {
            if (segment < 0 || !Vec3.Finite(fraction) || fraction < 0 || fraction > 1) throw new ArgumentOutOfRangeException();
            Segment = segment; Fraction = fraction;
        }
    }

    // Immutable world-space deck. A cursor remains on its joint segment when the hand moves.
    public sealed class Route
    {
        private readonly Vec3[] points;
        private readonly double[] widths;
        public IReadOnlyList<Vec3> Points { get; }
        public IReadOnlyList<double> Widths { get; }
        public int Finger { get; }
        public bool Reversed { get; }
        public double Length { get; }
        public double MinimumWidth { get; }
        public Vec3 Start => points[0];
        public Vec3 End => points[points.Length - 1];
        public Route(Vec3[] points, double[] widths, int finger = 1, bool reversed = false) : this(points, widths, finger, reversed, false) { }
        // D11: owned takes the arrays as they are (the caller made them and drops them), instead of copying.
        internal Route(Vec3[] points, double[] widths, int finger, bool reversed, bool owned)
        {
            if (points == null || widths == null || points.Length < 2 || points.Length > 128 || widths.Length != points.Length - 1) throw new ArgumentException("Invalid route shape.");
            this.points = owned ? points : (Vec3[])points.Clone(); this.widths = owned ? widths : (double[])widths.Clone();
            double length = 0, width = double.PositiveInfinity;
            for (int i = 0; i < points.Length; i++)
            {
                if (!points[i].IsFinite) throw new ArgumentException("Non-finite route point.");
                if (i == 0) continue;
                if (!Vec3.Finite(widths[i - 1]) || widths[i - 1] <= 0) throw new ArgumentException("Invalid route width.");
                length += Vec3.Distance(points[i - 1], points[i]); width = Math.Min(width, widths[i - 1]);
            }
            if (length < 1e-8) throw new ArgumentException("Zero-length route.");
            Finger = finger; Reversed = reversed; Length = length; MinimumWidth = width;
            Points = Array.AsReadOnly(this.points); Widths = Array.AsReadOnly(this.widths);
        }
        public Route Reverse()
        {
            var p = (Vec3[])points.Clone(); var w = (double[])widths.Clone(); Array.Reverse(p); Array.Reverse(w);
            return new Route(p, w, Finger, !Reversed, true);
        }
        public Vec3 Evaluate(RouteCursor cursor)
        {
            if (cursor.Segment >= points.Length - 1) throw new ArgumentOutOfRangeException(nameof(cursor));
            return Vec3.Lerp(points[cursor.Segment], points[cursor.Segment + 1], cursor.Fraction);
        }
        public RouteCursor Advance(RouteCursor cursor, double distance)
        {
            if (!Vec3.Finite(distance) || distance < 0 || cursor.Segment >= points.Length - 1) throw new ArgumentOutOfRangeException();
            int segment = cursor.Segment; double fraction = cursor.Fraction;
            while (segment < points.Length - 1)
            {
                double length = Vec3.Distance(points[segment], points[segment + 1]);
                double remaining = length * (1 - fraction);
                if (distance < remaining && length > 1e-10) return new RouteCursor(segment, fraction + distance / length);
                distance -= remaining;
                if (segment == points.Length - 2) return new RouteCursor(segment, 1);
                segment++; fraction = 0;
            }
            return cursor;
        }
        public bool AtEnd(RouteCursor cursor) => cursor.Segment == points.Length - 2 && cursor.Fraction >= 1;
        public double Difference(Route other)
        {
            if (other == null || other.Points.Count != points.Length || Finger != other.Finger || Reversed != other.Reversed) return double.PositiveInfinity;
            double max = 0;
            for (int i = 0; i < points.Length; i++) max = Math.Max(max, Vec3.Distance(points[i], other.points[i]));
            return max;
        }
        public Vec3[] CopyPoints() => (Vec3[])points.Clone();
        public double[] CopyWidths() => (double[])widths.Clone();
    }
}
