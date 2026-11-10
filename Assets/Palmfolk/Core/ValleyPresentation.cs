using System;
using System.Collections.Generic;
using System.Linq;

namespace Palmfolk.Core
{
    public sealed class TravelerView
    {
        public TravelerKind Kind { get; }
        public string Destination { get; }
        public Vec3 Position { get; }
        public bool Arrived { get; }
        public TravelerView(Traveler traveler, double alpha)
        {
            Kind = traveler.Kind; Destination = traveler.Destination; Arrived = traveler.Arrived;
            Position = Vec3.Lerp(traveler.PreviousPosition, traveler.Position, alpha);
        }
    }

    // What the views draw for the whole valley (D3). Positions are valley-local; draw them through Pose.
    public sealed class ValleyPresentation
    {
        public long Tick { get; }
        public ValleySpec Valley { get; }
        public PuzzleKind Puzzle { get; }
        public int PuzzleIndex { get; }
        public DirectorStage Stage { get; }
        public bool Complete { get; }
        public Vec3 Pip { get; }
        public Vec3 Bram { get; }
        public IReadOnlyList<TravelerView> Travelers { get; }
        // Finished bridges by edge id.
        public IReadOnlyDictionary<string, CurveTree> Bridges { get; }
        // V3: edges that glow because they must be rebuilt for the finale.
        public IReadOnlyList<string> FailingEdges { get; }
        // The edge being carried or built, its anchor zones and the rules it must meet. Null between interactions.
        public string ActiveEdge { get; }
        public AnchorZone? Source { get; }
        public AnchorZone? Destination { get; }
        public TravelerKind? Rule { get; }
        public double LedgeWidth { get; }
        public double WideningProgress { get; }
        public HandSide? HandOwner { get; }
        public Route FilteredHandRoute { get; }
        public Route GhostRoute { get; }
        public Route Scaffold { get; }
        // D4: the whole scaffold and live shape as trees (a fork has a trunk and branches; Scaffold and
        // FilteredHandRoute show its first branch). ActiveEdges lists every edge of a fork, by branch.
        public CurveTree ScaffoldTree { get; }
        public CurveTree HandTree { get; }
        public IReadOnlyList<string> ActiveEdges { get; } = Array.Empty<string>();
        // A refused fork branch whose siblings were baked; Refusals index into it while it is shown.
        public CurveTree RefusedScaffold { get; }
        public BuildPhase? BuildPhase { get; }
        public double BuildProgress { get; }
        public double ScaffoldRemovalProgress { get; }
        public WidthTier RouteTier { get; }
        public IReadOnlyList<ValidationFailure> Refusals { get; }
        public bool Paused { get; }
        public bool WaitingForHands { get; }
        public bool PutControllersDown { get; }
        public bool ShowStartupText { get; }
        // Planting (D2). Playing and Identity when built from a ValleySimulation alone; Pose is null before the first planting.
        public ValleyStage PlantingStage { get; } = ValleyStage.Playing;
        public ValleyPose Pose { get; } = ValleyPose.Identity;
        public double PlantProgress { get; }
        public HandSide? PlantingHand { get; }

        public ValleyPresentation(ValleySimulation game, double interpolationAlpha)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));
            Tick = game.Tick; Valley = game.Valley; Puzzle = game.Puzzle.Kind; PuzzleIndex = game.PuzzleIndex; Stage = game.Stage; Complete = game.IsComplete;
            Pip = Vec3.Lerp(game.PreviousPip, game.Pip, interpolationAlpha);
            Bram = game.Bram == null ? game.BramPosition : Vec3.Lerp(game.Bram.PreviousPosition, game.Bram.Position, interpolationAlpha);
            Travelers = game.Travelers.Select(t => new TravelerView(t, interpolationAlpha)).ToList().AsReadOnly();
            Bridges = new Dictionary<string, CurveTree>(game.Progress.Bridges); FailingEdges = game.FailingEdges;
            LedgeWidth = game.LedgeWidth; WideningProgress = game.WideningProgress;
            Paused = game.IsPaused; WaitingForHands = game.WaitingForHands; ShowStartupText = game.ShowStartupText;
            var session = game.Session;
            PutControllersDown = game.PuzzleIndex == 0 && session != null && !session.Owner.HasValue && game.ControllersInHand;
            Refusals = session?.Refusals ?? game.RefusedFailures;
            RefusedScaffold = session?.RefusedTree ?? game.RefusedScaffold;
            if (session == null) return;
            ActiveEdges = session.Edges; HandTree = session.ActiveTree;
            ScaffoldTree = session.Build?.Phase == Core.BuildPhase.Removed ? null : session.Build?.FrozenTree();
            ActiveEdge = session.Edge; Source = session.Source; Destination = session.Destination; Rule = session.Rule;
            HandOwner = session.Owner; FilteredHandRoute = session.ActiveRoute; GhostRoute = session.Recovery?.Ghost; RouteTier = session.RouteTier;
            Scaffold = session.Build?.Phase == Core.BuildPhase.Removed ? null : session.Build?.FrozenRoute();
            BuildPhase = session.Build?.Phase; BuildProgress = session.Build?.Progress ?? 0; ScaffoldRemovalProgress = session.Build?.RemovalProgress ?? 0;
        }

        public ValleyPresentation(PalmfolkGame game, double interpolationAlpha)
            : this(game?.Valley ?? throw new ArgumentException("Use WorldPresentation for the M1 slice."), interpolationAlpha)
        {
            PlantingStage = game.Stage; Pose = game.Pose;
            if (game.Stage != ValleyStage.Planting) return;
            Paused = game.IsPaused; PlantProgress = game.Planting.Progress; PlantingHand = game.Planting.Hand;
            WaitingForHands = game.Planting.WaitingForHands; ShowStartupText = game.Planting.ShowStartupText;
            PutControllersDown = WaitingForHands && game.Valley.ControllersInHand;
        }
    }
}
