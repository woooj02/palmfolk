using System;
using System.Collections.Generic;

namespace Palmfolk.Core
{
    // Building a curve tree (T3 for T4's trees). One curve builds exactly like a BridgeBuild. A fork builds its trunk
    // first, then every branch at once from the frozen junction; the whole takes BuildTicks either way. Any curve
    // pausing pauses the rest; any curve abandoned takes the whole scaffold down.
    public sealed class TreeBuild
    {
        private readonly GameSettings settings;
        private readonly BridgeBuild[] curves;
        private readonly int[] parents;
        private readonly int trunkTicks;
        private CurveTree reference;

        public bool IsFork => curves.Length > 1;
        public int CurveCount => curves.Length;
        public BuildPhase Phase { get; private set; } = BuildPhase.Building;
        public double Progress
        {
            get
            {
                if (!IsFork) return curves[0].Progress;
                double branch = 0; int count = 0;
                for (int i = 1; i < curves.Length; i++) if (curves[i] != null) { branch += curves[i].Progress; count++; }
                double trunkShare = (double)trunkTicks / settings.BuildTicks;
                return curves[0].Progress * trunkShare + (count == 0 ? 0 : branch / count * (1 - trunkShare));
            }
        }
        public int FrozenSections { get { int n = 0; foreach (var c in curves) if (c != null) n += c.FrozenSections; return n; } }
        public double RemovalProgress { get { double r = 0; foreach (var c in curves) if (c != null) r = Math.Max(r, c.RemovalProgress); return r; } }
        // The single curve's build, for code written against BridgeBuild (M1 views, recovery).
        public BridgeBuild Trunk => curves[0];

        public TreeBuild(CurveTree initial, GameSettings settings)
        {
            this.settings = settings?.Copy() ?? throw new ArgumentNullException(nameof(settings)); this.settings.Validate();
            reference = initial ?? throw new ArgumentNullException(nameof(initial));
            curves = new BridgeBuild[initial.Curves.Count]; parents = new int[curves.Length];
            for (int i = 0; i < curves.Length; i++) parents[i] = initial.Parents[i];
            for (int i = 1; i < curves.Length; i++) if (parents[i] != 0) throw new ArgumentException("Only a trunk with branches is supported.");
            if (curves.Length == 1) { curves[0] = new BridgeBuild(initial.Curves[0], settings); trunkTicks = settings.BuildTicks; return; }
            int trunkSegments = initial.Curves[0].Widths.Count, branchSegments = 0;
            for (int i = 1; i < curves.Length; i++) branchSegments = Math.Max(branchSegments, initial.Curves[i].Widths.Count);
            trunkTicks = Math.Max(1, Math.Min(settings.BuildTicks - 1, settings.BuildTicks * trunkSegments / (trunkSegments + branchSegments)));
            curves[0] = new BridgeBuild(initial.Curves[0], Ticks(trunkTicks));
        }

        private GameSettings Ticks(int ticks) { var s = settings.Copy(); s.BuildTicks = ticks; return s; }

        public void Remove() { foreach (var c in curves) c?.Remove(); Phase = BuildPhase.Removing; }

        public void Step(CurveTree live, bool connected, bool still, bool abandon = false)
        {
            if (abandon) Remove();
            if (Phase == BuildPhase.Removing || Phase == BuildPhase.Removed)
            {
                bool done = true;
                foreach (var c in curves) if (c != null) { c.Step(null, false, false); done &= c.Phase == BuildPhase.Removed; }
                if (done) Phase = BuildPhase.Removed;
                return;
            }
            if (Phase == BuildPhase.Complete) return;
            bool topology = live != null && live.Curves.Count == curves.Length;
            if (topology) for (int i = 0; i < curves.Length; i++) topology &= live.Parents[i] == parents[i];
            // While one curve is paused the others hold too (they see "not still").
            bool anyPaused = Phase == BuildPhase.Paused;
            for (int i = 0; i < curves.Length; i++)
            {
                var build = curves[i]; if (build == null || build.Phase == BuildPhase.Complete) continue;
                var route = topology ? live.Curves[i] : null;
                bool holding = anyPaused && build.Phase == BuildPhase.Building;
                build.Step(route, connected, still && !holding);
            }
            if (topology) reference = live;
            // The trunk is done: branches start from its frozen end.
            if (IsFork && curves[0].Phase == BuildPhase.Complete && curves[1] == null)
            {
                var junction = curves[0].FrozenEnd;
                int branchTicks = settings.BuildTicks - trunkTicks;
                for (int i = 1; i < curves.Length; i++)
                {
                    var shape = reference.Curves[i]; var points = shape.CopyPoints(); points[0] = junction;
                    curves[i] = new BridgeBuild(new Route(points, shape.CopyWidths(), shape.Finger, shape.Reversed), Ticks(branchTicks));
                }
            }
            bool removing = false, paused = false, complete = true;
            foreach (var c in curves)
            {
                if (c == null) { complete = false; continue; }
                removing |= c.Phase == BuildPhase.Removing || c.Phase == BuildPhase.Removed;
                paused |= c.Phase == BuildPhase.Paused;
                complete &= c.Phase == BuildPhase.Complete;
            }
            if (removing) { Remove(); return; }
            Phase = complete ? BuildPhase.Complete : paused ? BuildPhase.Paused : BuildPhase.Building;
        }

        // The frozen scaffold so far as a tree (curves with fewer than two frozen points left out), or null.
        public CurveTree FrozenTree()
        {
            var trunk = curves[0].FrozenRoute(); if (trunk == null) return null;
            var list = new List<Route> { trunk }; var parentList = new List<int> { -1 };
            for (int i = 1; i < curves.Length; i++)
            {
                var route = curves[i]?.FrozenRoute(); if (route == null) continue;
                list.Add(route); parentList.Add(0);
            }
            return new CurveTree(list.ToArray(), parentList.ToArray());
        }
        // The first walk through the scaffold (trunk, then branch 0): what a single-route view draws.
        public Route FrozenRoute()
        {
            var tree = FrozenTree();
            if (tree == null || tree.Curves.Count == 1) return tree?.Curves[0];
            return HandRoutes.Spine(tree, 0);
        }
        // Where Pip stands while building: along the trunk, then along branch 0.
        public Vec3 BuilderPosition(CurveTree live)
        {
            bool topology = live != null && live.Curves.Count == curves.Length;
            if (!IsFork || curves[1] == null) return curves[0].BuilderPosition(topology ? live.Curves[0] : null);
            return curves[1].BuilderPosition(topology ? live.Curves[1] : null);
        }
    }
}
