#if UNITY_5_3_OR_NEWER
using System;
using Palmfolk.Core;

namespace Palmfolk.Views
{
    // Presentation contacts only. Stance feet retain route coordinates as tracked geometry moves.
    public sealed class FootPlanting
    {
        private bool initialized, routeMode;
        private int finger, points, nextFoot, swing = -1;
        private bool reversed;
        private double previousParameter, travel, direction = 1, startParameter, targetParameter;
        private long swingStart;
        private Vec3 previousCentre, startPlane, targetPlane;
        private readonly double[] planted = new double[2];
        private readonly Vec3[] plane = new Vec3[2];
        public Vec3 Left { get; private set; }
        public Vec3 Right { get; private set; }
        public Vec3 Forward { get; private set; } = new Vec3(0, 0, 1);
        public event Action<Vec3> Landed;
        public void Update(Route route, Vec3 centre, Vec3 facing, long tick, bool canStep)
        {
            double parameter = 0;
            if (route != null && Closest(route, centre, out parameter) > 0.015) route = null;
            bool onRoute = route != null;
            bool reset = !initialized || routeMode != onRoute || (onRoute && (finger != route.Finger || points != route.Points.Count || reversed != route.Reversed));
            if (reset)
            {
                initialized = true; routeMode = onRoute; previousParameter = parameter; previousCentre = centre; travel = 0; swing = -1;
                if (onRoute) { finger = route.Finger; points = route.Points.Count; reversed = route.Reversed; planted[0] = planted[1] = parameter; }
                var side = Vec3.Cross(Vec3.Up, facing).Normalized;
                plane[0] = centre - side * 0.003; plane[1] = centre + side * 0.003;
            }
            if (onRoute)
            {
                double moved = Arc(route, parameter) - Arc(route, previousParameter);
                if (Math.Abs(moved) > 1e-8) direction = Math.Sign(moved);
                travel += Math.Abs(moved); previousParameter = parameter;
                Forward = Tangent(route, parameter) * direction;
            }
            else
            {
                Vec3 moved = centre - previousCentre;
                if (moved.Length > 1e-8) Forward = moved.Normalized;
                travel += moved.Length;
            }
            previousCentre = centre;
            if (swing < 0 && canStep && travel >= 0.008)
            {
                swing = nextFoot; nextFoot = 1 - nextFoot; swingStart = tick; travel %= 0.008;
                startParameter = planted[swing]; targetParameter = parameter;
                startPlane = plane[swing];
                var side = Vec3.Cross(Vec3.Up, Forward).Normalized;
                targetPlane = centre + side * (swing == 0 ? -0.003 : 0.003);
            }
            double u = swing < 0 ? 0 : Vec3.Clamp((tick - swingStart) / 12.0, 0, 1);
            Vec3 Position(int foot)
            {
                double p = foot == swing ? startParameter + (targetParameter - startParameter) * u : planted[foot];
                Vec3 basePoint = onRoute ? Evaluate(route, p) + Vec3.Cross(Vec3.Up, Tangent(route, p) * direction).Normalized * (foot == 0 ? -0.003 : 0.003) :
                    foot == swing ? Vec3.Lerp(startPlane, targetPlane, u) : plane[foot];
                return basePoint + Vec3.Up * (foot == swing ? Math.Sin(Math.PI * u) * 0.003 : 0);
            }
            Left = Position(0); Right = Position(1);
            if (swing >= 0 && u >= 1)
            {
                planted[swing] = targetParameter; plane[swing] = targetPlane;
                Vec3 contact = swing == 0 ? Left : Right; swing = -1; Landed?.Invoke(contact);
            }
        }
        public static double Closest(Route route, Vec3 point, out double parameter)
        {
            double best = double.PositiveInfinity; parameter = 0;
            for (int i = 0; i + 1 < route.Points.Count; i++)
            {
                Vec3 d = route.Points[i + 1] - route.Points[i];
                double squaredLength = Vec3.Dot(d, d); if (squaredLength < 1e-12) continue;
                double u = Vec3.Clamp(Vec3.Dot(point - route.Points[i], d) / squaredLength, 0, 1);
                double distance = Vec3.Distance(point, Vec3.Lerp(route.Points[i], route.Points[i + 1], u));
                if (distance < best) { best = distance; parameter = i + u; }
            }
            return best;
        }
        private static Vec3 Evaluate(Route route, double p)
        { int i = Math.Min(route.Points.Count - 2, Math.Max(0, (int)p)); return Vec3.Lerp(route.Points[i], route.Points[i + 1], p - i); }
        private static Vec3 Tangent(Route route, double p)
        { int i = Math.Min(route.Points.Count - 2, Math.Max(0, (int)p)); return (route.Points[i + 1] - route.Points[i]).Normalized; }
        private static double Arc(Route route, double p)
        {
            int segment = Math.Min(route.Points.Count - 2, Math.Max(0, (int)p)); double length = 0;
            for (int i = 0; i < segment; i++) length += Vec3.Distance(route.Points[i], route.Points[i + 1]);
            return length + Vec3.Distance(route.Points[segment], route.Points[segment + 1]) * (p - segment);
        }
    }
}
#endif
