using System.Collections.Generic;

namespace Palmfolk.Core
{
    // Unity implements this port. No SDK object enters the simulation.
    public interface IPalmfolkView { void Present(WorldPresentation frame); }
    public sealed class WorldPresentation
    {
        public long Tick { get; }
        public M1Phase Phase { get; }
        public Vec3 Pip { get; }
        public Vec3 Bram { get; }
        public HandSide? HandOwner { get; }
        public Route FilteredHandRoute { get; }
        public Route GhostRoute { get; }
        public Route Scaffold { get; }
        public CurveTree Bridge { get; }
        public WidthTier RouteTier { get; }
        public double BuildProgress { get; }
        public BuildPhase? BuildPhase { get; }
        public double ScaffoldRemovalProgress { get; }
        public IReadOnlyList<ValidationFailure> Refusals { get; }
        public bool Paused { get; }
        public bool WaitingForHands { get; }
        public bool PutControllersDown { get; }
        public bool ShowStartupText { get; }
        public bool CartCrossing { get; }
        // Every position above is valley-local; draw it through Pose.ToWorld. Pose is null before the first planting.
        public ValleyStage Stage { get; } = ValleyStage.Playing;
        public ValleyPose Pose { get; } = ValleyPose.Identity;
        public double PlantProgress { get; }
        public HandSide? PlantingHand { get; }
        public WorldPresentation(PalmfolkGame game, double interpolationAlpha) : this(game.Puzzle, interpolationAlpha)
        {
            Stage = game.Stage; Pose = game.Pose;
            if (game.Stage != ValleyStage.Planting) return;
            // While planting, the startup safeguards (E19) come from the planting detector, not the paused puzzle.
            Paused = game.IsPaused; PlantProgress = game.Planting.Progress; PlantingHand = game.Planting.Hand;
            WaitingForHands = game.Planting.WaitingForHands; ShowStartupText = game.Planting.ShowStartupText;
            PutControllersDown = WaitingForHands && game.Puzzle.ControllersInHand;
        }
        public WorldPresentation(M1Simulation game, double interpolationAlpha)
        {
            Tick = game.Tick; Phase = game.Phase; Pip = Vec3.Lerp(game.PreviousPip, game.Pip, interpolationAlpha);
            Paused = game.IsPaused; WaitingForHands = game.WaitingForHands; PutControllersDown = game.Phase == M1Phase.Board && !game.Owner.HasValue && game.ControllersInHand;
            ShowStartupText = game.ShowStartupText; CartCrossing = game.CartCrossing;
            Bram = Vec3.Lerp(game.PreviousBram, game.Bram, interpolationAlpha); HandOwner = game.Owner;
            FilteredHandRoute = Interpolate(game.PreviousRoute, game.ActiveRoute, interpolationAlpha); GhostRoute = game.Recovery?.Ghost;
            Scaffold = game.Build?.Phase == Core.BuildPhase.Removed ? null : game.Build?.FrozenRoute();
            BuildPhase = game.Build?.Phase; ScaffoldRemovalProgress = game.Build?.RemovalProgress ?? 0;
            Bridge = game.Bridge; RouteTier = game.RouteTier; BuildProgress = game.Build?.Progress ?? 0; Refusals = game.Refusals;
        }
        private static Route Interpolate(Route previous, Route current, double alpha)
        {
            if (previous == null || current == null || previous.Finger != current.Finger || previous.Reversed != current.Reversed || previous.Points.Count != current.Points.Count) return current;
            var points = current.CopyPoints(); var widths = current.CopyWidths();
            for (int i = 0; i < points.Length; i++) points[i] = Vec3.Lerp(previous.Points[i], points[i], alpha);
            try { return new Route(points, widths, current.Finger, current.Reversed); }
            catch (System.ArgumentException) { return current; }
        }
    }
}
