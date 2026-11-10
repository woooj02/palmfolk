using System;
using System.Collections.Generic;

namespace Palmfolk.Core
{
    public enum BuildPhase { Building, Paused, Removing, Removed, Complete }
    public sealed class BuildSnapshot
    {
        public BuildPhase Phase;
        public int ProgressTicks, PausedTicks, RemovalTicks, StableTicks;
        public Route Reference;
        public Vec3[] FrozenPoints;
        public double[] FrozenWidths;
    }
    public sealed class BridgeBuild
    {
        private readonly GameSettings settings;
        private readonly List<Vec3> points = new List<Vec3>();
        private readonly List<double> widths = new List<double>();
        private Route reference;
        private int progress, paused, removing, stable;
        public BuildPhase Phase { get; private set; }
        public double Progress => (double)progress / settings.BuildTicks;
        public int FrozenSections => widths.Count;
        public double RemovalProgress => Vec3.Clamp((double)removing / settings.RemovalTicks, 0, 1);
        public Vec3 FrozenEnd => points[points.Count - 1];
        public BridgeBuild(Route initial, GameSettings settings)
        {
            this.settings = settings.Copy(); this.settings.Validate(); reference = initial ?? throw new ArgumentNullException(nameof(initial));
            points.Add(initial.Start); Phase = BuildPhase.Building;
        }
        public void Remove() { if (Phase != BuildPhase.Removed && Phase != BuildPhase.Removing) { Phase = BuildPhase.Removing; removing = 0; } }
        public void Step(Route live, bool connected, bool still, bool abandon = false)
        {
            if (abandon) Remove();
            if (Phase == BuildPhase.Removing) { if (++removing >= settings.RemovalTicks) Phase = BuildPhase.Removed; return; }
            if (Phase == BuildPhase.Removed || Phase == BuildPhase.Complete) return;
            bool topology = live != null && live.Points.Count == reference.Points.Count && live.Finger == reference.Finger && live.Reversed == reference.Reversed;
            int join = Math.Min(widths.Count, reference.Points.Count - 1);
            bool joined = topology && Vec3.Distance(live.Points[join], FrozenEnd) < settings.ConnectDistance;
            if (Phase == BuildPhase.Paused)
            {
                paused++;
                stable = connected && joined && still ? stable + 1 : 0;
                if (stable >= settings.StillnessTicks) { reference = live; Phase = BuildPhase.Building; paused = stable = 0; }
                else { if (paused >= settings.ScaffoldTicks) Remove(); return; }
            }
            bool changed = !topology;
            if (topology) changed |= (live.MinimumWidth >= 0.025) != (reference.MinimumWidth >= 0.025);
            if (topology) for (int i = join; i < live.Points.Count; i++) changed |= Vec3.Distance(live.Points[i], reference.Points[i]) > settings.TremorDistance;
            bool harmless = topology && Vec3.Distance(live.Points[join], FrozenEnd) <= settings.TremorDistance;
            if (!connected || !still || changed || !harmless) { Phase = BuildPhase.Paused; stable = paused = 0; return; }
            progress++;
            int due = Math.Min(reference.Points.Count - 1, progress * (reference.Points.Count - 1) / settings.BuildTicks);
            while (widths.Count < due)
            {
                int segment = widths.Count;
                points.Add(live.Points[segment + 1]); widths.Add(live.Widths[segment]);
            }
            if (progress >= settings.BuildTicks) Phase = BuildPhase.Complete;
        }
        public Route FrozenRoute() => points.Count >= 2 ? new Route(points.ToArray(), widths.ToArray(), reference.Finger, reference.Reversed) : null;
        public Vec3 BuilderPosition(Route live)
        {
            double position = Progress * (reference.Points.Count - 1);
            int segment = Math.Min(reference.Points.Count - 2, (int)position);
            Vec3 start = segment < points.Count ? points[segment] : reference.Points[segment];
            Vec3 end = segment + 1 < points.Count ? points[segment + 1] : (live ?? reference).Points[segment + 1];
            return Vec3.Lerp(start, end, position - segment);
        }
        public BuildSnapshot Capture() => new BuildSnapshot { Phase = Phase, ProgressTicks = progress, PausedTicks = paused, RemovalTicks = removing, StableTicks = stable,
            Reference = reference, FrozenPoints = points.ToArray(), FrozenWidths = widths.ToArray() };
        public static BridgeBuild Restore(BuildSnapshot snapshot, GameSettings settings)
        {
            if (snapshot == null || snapshot.Reference == null || snapshot.FrozenPoints == null || snapshot.FrozenWidths == null ||
                snapshot.FrozenPoints.Length != snapshot.FrozenWidths.Length + 1 || snapshot.FrozenPoints.Length > snapshot.Reference.Points.Count ||
                snapshot.ProgressTicks < 0 || snapshot.ProgressTicks > settings.BuildTicks || !Enum.IsDefined(typeof(BuildPhase), snapshot.Phase)) throw new ArgumentException("Invalid scaffold snapshot.");
            var build = new BridgeBuild(snapshot.Reference, settings);
            build.points.Clear(); build.points.AddRange(snapshot.FrozenPoints); build.widths.AddRange(snapshot.FrozenWidths);
            foreach (var p in build.points) if (!p.IsFinite) throw new ArgumentException("Invalid frozen point.");
            foreach (double w in build.widths) if (!Vec3.Finite(w) || w <= 0) throw new ArgumentException("Invalid frozen width.");
            build.progress = snapshot.ProgressTicks; build.paused = Math.Max(0, snapshot.PausedTicks);
            build.removing = Math.Max(0, snapshot.RemovalTicks); build.stable = 0;
            build.Phase = snapshot.Phase == BuildPhase.Building ? BuildPhase.Paused : snapshot.Phase;
            return build;
        }
    }
}
