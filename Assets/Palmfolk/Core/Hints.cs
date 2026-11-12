using System;
using System.Collections.Generic;

namespace Palmfolk.Core
{
    // E8 hints (HintDue): which hand shape solves the edge in front of the player, and a bridge of that shape that passes
    // the current rules. The views pose their hand rig along Tree; Core never invents joints. Valley-local.
    public enum HintShape
    {
        CarryFinger, // Board: one finger from Pip's island to the next
        OneFinger,   // a rope for walkers
        FlatHand,    // a plank for groups and carts
        Arch,        // a raised plank over an obstacle
        Spread       // a fork: wrist on the shared island, fingers apart toward each end
    }

    public sealed class SolutionHint
    {
        public HintShape Shape { get; }
        // Root curve first; a fork's trunk is curve 0 and branch k is curve k + 1, ending at Destinations[k].
        public CurveTree Tree { get; }
        // How high above the straight line between the anchors the deck rises, at most (metres).
        public double Rise { get; }
        public SolutionHint(HintShape shape, CurveTree tree, double rise)
        {
            if (!Enum.IsDefined(typeof(HintShape), shape) || tree == null || !Vec3.Finite(rise) || rise < 0) throw new ArgumentException("Invalid hint.");
            Shape = shape; Tree = tree; Rise = rise;
        }
    }

    internal static class SolutionHints
    {
        private const int Points = 11;
        private const double RiseStep = 0.0025, MaximumRise = 0.08;

        // Searches rises from flat upward, a smooth arch then a ramped deck at each, and keeps the first that passes
        // validation exactly as the bake would judge it. Null when nothing within reach passes (a ledge too narrow).
        public static SolutionHint Find(bool carry, AnchorZone source, AnchorZone[] sources, AnchorZone[] destinations, TravelerKind rule,
            double widthCap, GameSettings settings, IClearanceField clearance)
        {
            var capacity = settings.Rule(rule);
            double width = Math.Min(widthCap, Math.Max(capacity.MinimumWidth * 1.25, rule == TravelerKind.Walker ? 0.012 : 0.03));
            if (carry) return new SolutionHint(HintShape.CarryFinger, CurveTree.Single(Line(source.Centre, destinations[0].Centre, width, 0, false)), 0);
            bool fork = destinations.Length > 1;
            for (double rise = 0; rise <= MaximumRise + 1e-12; rise += RiseStep)
                for (int profile = 0; profile < (rise == 0 ? 1 : 2); profile++)
                {
                    var tree = fork ? Fork(source.Centre, destinations, width, rise, profile == 1) : CurveTree.Single(Line(source.Centre, destinations[0].Centre, width, rise, profile == 1));
                    if (!Passes(tree, fork, sources, destinations, rule, settings, clearance)) continue;
                    var shape = fork ? HintShape.Spread : rise > 0 ? HintShape.Arch : rule == TravelerKind.Walker ? HintShape.OneFinger : HintShape.FlatHand;
                    return new SolutionHint(shape, tree, rise);
                }
            return null;
        }

        private static bool Passes(CurveTree tree, bool fork, AnchorZone[] sources, AnchorZone[] destinations, TravelerKind rule, GameSettings settings, IClearanceField clearance)
        {
            for (int k = 0; k < destinations.Length; k++)
            {
                var walk = fork ? new CurveTree(new[] { tree.Curves[0], tree.Curves[k + 1] }, new[] { -1, 0 }) : tree;
                if (BridgeValidator.Validate(walk, sources[k], new[] { destinations[k] }, rule, settings, clearance).Count > 0) return false;
            }
            return true;
        }

        // A deck from `from` to `to`, raised by `rise`: a half sine, or (ramped) up over the first two fifths, level,
        // and down over the last two fifths, which keeps a cart's slope low for the same height.
        private static Route Line(Vec3 from, Vec3 to, double width, double rise, bool ramped)
        {
            var points = new Vec3[Points]; var widths = new double[Points - 1];
            for (int i = 0; i < Points; i++)
            {
                double t = (double)i / (Points - 1);
                double lift = ramped ? rise * Math.Min(1, Math.Min(t, 1 - t) / 0.4) : rise * Math.Sin(Math.PI * t);
                points[i] = Vec3.Lerp(from, to, t) + new Vec3(0, lift, 0);
            }
            for (int i = 0; i < widths.Length; i++) widths[i] = width;
            return new Route(points, widths, 1, false, true);
        }

        // A trunk a quarter of the way toward the ends' middle, then one deck to each end.
        private static CurveTree Fork(Vec3 root, AnchorZone[] ends, double width, double rise, bool ramped)
        {
            var middle = Vec3.Zero; for (int k = 0; k < ends.Length; k++) middle = middle + ends[k].Centre;
            middle = middle * (1.0 / ends.Length);
            var split = Vec3.Lerp(root, middle, 0.25);
            var curves = new List<Route> { Line(root, split, width, 0, false) }; var parents = new List<int> { -1 };
            for (int k = 0; k < ends.Length; k++) { curves.Add(Line(split, ends[k].Centre, width, rise, ramped)); parents.Add(0); }
            return new CurveTree(curves.ToArray(), parents.ToArray());
        }
    }
}
