using System;
using System.Collections.Generic;

namespace Palmfolk.Core
{
    // D10 (GZ1 to GZ3). Gaze only changes what is drawn and which waystone button a pinch chooses; it is never an
    // input to the valley, so every puzzle plays the same with gaze off, and recordings don't carry it.

    // GZ3: where the gaze pose came from. Head: synthesized from the head pose (Quest 2, 3 and 3S).
    public enum GazeSource { None, Head, Eyes }

    // A world-space gaze ray.
    public sealed class GazeSample
    {
        public Vec3 Origin { get; }
        public Vec3 Direction { get; }
        public GazeSource Source { get; }
        public GazeSample(Vec3 origin, Vec3 direction, GazeSource source)
        {
            if (!origin.IsFinite || !direction.IsFinite || direction.Length < 1e-9 || source == GazeSource.None || !Enum.IsDefined(typeof(GazeSource), source))
                throw new ArgumentException("Invalid gaze sample.");
            Origin = origin; Direction = direction.Normalized; Source = source;
        }

        // Degrees between the ray and the direction to a point.
        public double DegreesTo(Vec3 point)
        {
            var to = (point - Origin).Normalized; if (to.Length == 0) return 0;
            return Math.Acos(Math.Max(-1, Math.Min(1, Vec3.Dot(Direction, to)))) * 180 / Math.PI;
        }
    }

    // GZ1: what a waiting traveler needs, shown as a carved sign by shape and motion (A7).
    public enum NeedSign { None, Rope, Plank, Arch, Branches }

    // Something that can be looked at: id, world position, world radius, and the sign it holds up.
    public sealed class GazeTarget
    {
        public string Id { get; }
        public Vec3 Position { get; }
        public double Radius { get; }
        public NeedSign Need { get; }
        public GazeTarget(string id, Vec3 position, double radius, NeedSign need)
        {
            if (string.IsNullOrEmpty(id) || !position.IsFinite || !(radius > 0)) throw new ArgumentException("Invalid gaze target.");
            Id = id; Position = position; Radius = radius; Need = need;
        }
    }

    // GZ1: who can be looked at in a valley, and what they need.
    public static class NeedSigns
    {
        // A waiting figure is about 3 cm across at world scale 1; their head is 2 cm above the island.
        public const double FigureRadius = 0.03, FigureHeight = 0.02;

        // The traveler (or visitor) waiting for the current puzzle's bridges, while something is still to build.
        public static IReadOnlyList<GazeTarget> Targets(ValleySimulation game, ValleyPose pose)
        {
            var list = new List<GazeTarget>();
            if (game == null || pose == null) return list;
            var need = Need(game);
            if (need == NeedSign.None) return list;
            Vec3 local;
            if (game.Visitor != null)
            {
                if (game.VisitorArrival == null) return list; // not yet on the way in
                local = game.VisitorArrival.Position;
            }
            else if (game.Puzzle.Kind == PuzzleKind.Finale) local = game.Valley.Island(game.Valley.Camp).Top;
            else local = game.Valley.Island(game.Puzzle.TravelerFrom).Top;
            list.Add(new GazeTarget(game.Visitor?.Id ?? "Puzzle" + game.PuzzleIndex, pose.ToWorld(local + new Vec3(0, FigureHeight, 0)), FigureRadius * pose.Scale, need));
            return list;
        }

        // What the waiting traveler needs now: None once nothing is left to build, and on the Board (Pip rides the hand).
        public static NeedSign Need(ValleySimulation game)
        {
            if (game == null || game.PendingEdges.Count == 0) return NeedSign.None;
            var stage = game.Stage;
            if (stage != DirectorStage.Walking && stage != DirectorStage.Interacting && stage != DirectorStage.Widening) return NeedSign.None;
            var arch = game.Valley.Puzzles[4].Edges;
            var visitor = game.Visitor;
            if (visitor != null)
            {
                if (visitor.Edges.Count > 1 && Count(game.PendingEdges, visitor.Edges) > 1) return NeedSign.Branches;
                foreach (var id in game.PendingEdges) if (Contains(arch, id) && visitor.Traveler != TravelerKind.Walker) return NeedSign.Arch;
                return visitor.Traveler == TravelerKind.Walker ? NeedSign.Rope : NeedSign.Plank;
            }
            switch (game.Puzzle.Kind)
            {
                case PuzzleKind.FirstBridge: return NeedSign.Rope;
                case PuzzleKind.Width: return NeedSign.Plank;
                case PuzzleKind.Fork: return NeedSign.Branches;
                case PuzzleKind.Arch: return NeedSign.Arch;
                case PuzzleKind.Finale: return NeedSign.Plank;
                default: return NeedSign.None;
            }
        }

        private static bool Contains(IReadOnlyList<string> list, string id) { foreach (var x in list) if (x == id) return true; return false; }
        private static int Count(IReadOnlyList<string> pending, IReadOnlyList<string> of) { int n = 0; foreach (var id in pending) if (Contains(of, id)) n++; return n; }
    }

    // GZ1: a look of DwellSeconds at a target raises its sign; looking away for ReleaseSeconds puts it down.
    // Head direction is coarse, so a target is hit within MinimumDegrees even when it looks smaller than that.
    public sealed class LookWatcher
    {
        public const double DwellSeconds = 0.3, ReleaseSeconds = 0.2, MinimumDegrees = 3;
        private string candidate; private double dwell, away;

        // The target whose sign is up, or null.
        public GazeTarget Shown { get; private set; }
        public string Looking => candidate;

        // One rendered frame. Returns true when the shown sign changed (raised, put down, or another one).
        public bool Step(double seconds, GazeSample gaze, IReadOnlyList<GazeTarget> targets)
        {
            if (!(seconds >= 0)) seconds = 0;
            var before = Shown;
            var hit = gaze == null ? null : Hit(gaze, targets);
            // A shown target that is gone (built, solved, walked off) puts its sign down at once; one whose need
            // changed shows the new need.
            if (Shown != null)
            {
                GazeTarget current = null;
                if (targets != null) foreach (var t in targets) if (t.Id == Shown.Id) current = t;
                Shown = current;
            }
            if (hit?.Id != candidate) { candidate = hit?.Id; dwell = 0; }
            if (hit != null) dwell += seconds;
            if (Shown != null)
            {
                if (hit != null && hit.Id == Shown.Id) { away = 0; Shown = hit; }
                else if ((away += seconds) >= ReleaseSeconds - 1e-9) { Shown = null; away = 0; }
            }
            if (hit != null && dwell >= DwellSeconds - 1e-9 && (Shown == null || Shown.Id != hit.Id)) { Shown = hit; away = 0; }
            return !ReferenceEquals(before, Shown) && (before == null || Shown == null || before.Id != Shown.Id || before.Need != Shown.Need);
        }

        public void Reset() { Shown = null; candidate = null; dwell = away = 0; }

        public static GazeTarget Hit(GazeSample gaze, IReadOnlyList<GazeTarget> targets)
        {
            GazeTarget best = null; double bestRatio = double.PositiveInfinity;
            if (gaze == null || targets == null) return null;
            foreach (var t in targets)
            {
                double distance = Vec3.Distance(gaze.Origin, t.Position);
                double half = distance <= t.Radius ? 180 : Math.Max(MinimumDegrees, Math.Asin(t.Radius / distance) * 180 / Math.PI);
                double ratio = gaze.DegreesTo(t.Position) / half;
                if (ratio <= 1 && ratio < bestRatio) { best = t; bestRatio = ratio; }
            }
            return best;
        }
    }

    // GZ2: a waystone button for look and pinch: id, world centre, and its width in degrees as seen from the head.
    public sealed class GazeButton
    {
        public const double MinimumWidthDegrees = 6, MinimumGapDegrees = 1;
        public string Id { get; }
        public Vec3 Centre { get; }
        public double WidthDegrees { get; }
        public GazeButton(string id, Vec3 centre, double widthDegrees)
        {
            if (string.IsNullOrEmpty(id) || !centre.IsFinite || !(widthDegrees > 0) || widthDegrees > 90) throw new ArgumentException("Invalid gaze button.");
            Id = id; Centre = centre; WidthDegrees = widthDegrees;
        }
    }

    // GZ2 and E9: look at a waystone button and pinch with either hand, wherever it rests. A pinch counts once, when
    // thumb and index tips close (PinchOn) and opens again (PinchOff) before the next. A pinch made with the palm
    // facing the head is the system gesture's, never ours.
    public sealed class LookAndPinch
    {
        public const double PinchOn = 0.015, PinchOff = 0.03, FacingHead = 0.5;
        private readonly bool[] pinched = new bool[2];

        public string Hovered { get; private set; }
        // The last pinch refused because the palm faced the head (for tests and the developer panel).
        public int SystemGesturePinches { get; private set; }

        // Returns the button chosen this frame, or null. Hovered is the button under the gaze (null with no gaze).
        public string Step(GazeSample gaze, HandSample left, HandSample right, IReadOnlyList<GazeButton> buttons)
        {
            Hovered = gaze == null ? null : Hover(gaze, buttons);
            string chosen = null;
            for (int s = 0; s < 2; s++)
            {
                var hand = s == 0 ? left : right;
                double? gap = Gap(hand);
                if (!gap.HasValue) { pinched[s] = false; continue; }
                if (pinched[s]) { if (gap.Value > PinchOff) pinched[s] = false; continue; }
                if (gap.Value >= PinchOn) continue;
                pinched[s] = true;
                if (gaze != null && FacesHead(hand, gaze.Origin)) { SystemGesturePinches++; continue; }
                if (chosen == null && Hovered != null) chosen = Hovered;
            }
            return chosen;
        }

        public void Reset() { pinched[0] = pinched[1] = false; Hovered = null; }

        public static string Hover(GazeSample gaze, IReadOnlyList<GazeButton> buttons)
        {
            string best = null; double bestRatio = double.PositiveInfinity;
            if (buttons == null) return null;
            foreach (var b in buttons)
            {
                double ratio = gaze.DegreesTo(b.Centre) / (b.WidthDegrees / 2);
                if (ratio <= 1 && ratio < bestRatio) { best = b.Id; bestRatio = ratio; }
            }
            return best;
        }

        // Thumb tip to index tip, or null when either isn't usable.
        public static double? Gap(HandSample hand)
        {
            if (hand == null) return null;
            var thumb = hand[JointId.ThumbTip]; var index = hand[JointId.IndexTip];
            if (!thumb.Usable || !index.Usable) return null;
            return Vec3.Distance(thumb.Position, index.Position);
        }

        // E9: the palm faces the head (within 60 degrees).
        public static bool FacesHead(HandSample hand, Vec3 head)
        {
            var normal = HandRoutes.PalmNormal(hand); if (!normal.HasValue || !hand[JointId.Palm].Usable) return false;
            return Vec3.Dot(normal.Value, (head - hand[JointId.Palm].Position).Normalized) > FacingHead;
        }
    }
}
