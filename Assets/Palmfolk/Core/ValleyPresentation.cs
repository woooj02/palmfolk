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
        // S6 contact: the bridge the traveler stands on and where (interpolated), or null off bridges. When set,
        // Position is Support.Evaluate(SupportCursor), so feet planted on the deck stay on it.
        public Route Support { get; }
        public RouteCursor SupportCursor { get; }
        public TravelerView(Traveler traveler, double alpha)
        {
            Kind = traveler.Kind; Destination = traveler.Destination; Arrived = traveler.Arrived;
            Position = Vec3.Lerp(traveler.PreviousPosition, traveler.Position, alpha);
            var support = traveler.Support;
            if (support == null) return;
            // Stepping on this tick: the walk to the deck ended at its start.
            var from = ReferenceEquals(traveler.PreviousSupport, support) ? traveler.PreviousSupportCursor : new RouteCursor(0, 0);
            Support = support; SupportCursor = Between(support, from, traveler.SupportCursor, alpha); Position = support.Evaluate(SupportCursor);
        }
        internal static RouteCursor Between(Route route, RouteCursor a, RouteCursor b, double alpha)
        {
            double x = a.Segment + a.Fraction + (b.Segment + b.Fraction - a.Segment - a.Fraction) * Vec3.Clamp(alpha, 0, 1);
            int last = route.Points.Count - 2, segment = Math.Min(last, Math.Max(0, (int)Math.Floor(x)));
            return new RouteCursor(segment, Vec3.Clamp(x - segment, 0, 1));
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
        // Bram while he walks the finale route (null otherwise), with his support like any traveler.
        public TravelerView BramWalk { get; }
        // E17: the active hand holds every anchor (see EdgeSession.ConnectionLocked).
        public bool ConnectionLocked { get; }
        // While Pip rides a carrying hand (Board): where he stands on FilteredHandRoute. Null otherwise.
        public RouteCursor? PipCursor { get; }
        // T5: the whole hand as last seen before the loss, for the carved ghost (null if unknown). E8: the shape and a
        // passing bridge for the hint hand, shown on HintDue (null when no interaction waits or nothing fits).
        public HandSample GhostHand { get; }
        public SolutionHint Hint { get; }
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
        // R1-R3 (D8). Visitor: who is being helped (null when none). VisitorFigure: that visitor walking in from the
        // sanctuary and waiting at their island (null before the way in is passable and once their travelers set
        // off: those are in Travelers). Residents: settled visitors walking their bridges (Bram is in Bram).
        // Windows: how many sanctuary windows to draw (dark from the finale on); bit i of LitWindows lights window i.
        // BridgeMarks: by edge, bit i = visitor i crossed it.
        public VisitorSpec Visitor { get; }
        public int VisitorIndex { get; } = -1;
        public TravelerView VisitorFigure { get; }
        public IReadOnlyList<TravelerView> Residents { get; } = Array.Empty<TravelerView>();
        public int Windows { get; }
        public int LitWindows { get; }
        public IReadOnlyDictionary<string, int> BridgeMarks { get; } = new Dictionary<string, int>();
        // T18: show the one line "Hand tracking works best in a well-lit room, with your hand in view." while
        // QualityPrompt is set, and the loosened-stillness icon while StillnessLoosened is set.
        public bool QualityPrompt { get; }
        public bool StillnessLoosened { get; }
        // T5: a hand-supported traveler is frozen or blending back (GhostRoute is drawn, Bram gestures).
        public bool Recovering { get; }
        public RecoveryPhase? RecoveryPhase { get; }
        // Planting (D2). Playing and Identity when built from a ValleySimulation alone; Pose is null before the first planting.
        // The wall sculpt: the gust gathering or blowing (null when none), whether a hand shields it this tick,
        // whether the travelers brace, and the score.
        public Vec3? GustOrigin { get; }
        public Vec3? GustTarget { get; }
        public GustPhase? GustPhase { get; }
        public double GustProgress { get; }
        public bool GustShielded { get; }
        public bool Bracing { get; }
        public int GustsBlocked { get; }
        public ValleyStage PlantingStage { get; } = ValleyStage.Playing;
        public ValleyPose Pose { get; } = ValleyPose.Identity;
        public double PlantProgress { get; }
        public HandSide? PlantingHand { get; }

        public ValleyPresentation(ValleySimulation game, double interpolationAlpha)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));
            Tick = game.Tick; Valley = game.Valley; Puzzle = game.Puzzle.Kind; PuzzleIndex = game.PuzzleIndex; Stage = game.Stage; Complete = game.IsComplete;
            Pip = Vec3.Lerp(game.PreviousPip, game.Pip, interpolationAlpha);
            if (game.Bram != null) { BramWalk = new TravelerView(game.Bram, interpolationAlpha); Bram = BramWalk.Position; }
            else Bram = game.BramPosition;
            var travelers = game.Travelers;
            var travelerViews = travelers.Count == 0 ? Array.Empty<TravelerView>() : new TravelerView[travelers.Count];
            for (int i = 0; i < travelerViews.Length; i++) travelerViews[i] = new TravelerView(travelers[i], interpolationAlpha);
            Travelers = travelerViews;
            Bridges = game.BridgesCopy(); FailingEdges = game.FailingEdges;
            LedgeWidth = game.LedgeWidth; WideningProgress = game.WideningProgress;
            Paused = game.IsPaused; WaitingForHands = game.WaitingForHands; ShowStartupText = game.ShowStartupText;
            QualityPrompt = game.QualityPromptShowing; StillnessLoosened = game.StillnessLoosened;
            Visitor = game.Visitor; VisitorIndex = game.VisitorIndex; Windows = game.Valley.Visitors.Count; LitWindows = game.LitWindows;
            VisitorFigure = game.VisitorArrival == null ? null : new TravelerView(game.VisitorArrival, interpolationAlpha);
            int residents = game.ResidentCount;
            var residentViews = residents == 0 ? Array.Empty<TravelerView>() : new TravelerView[residents];
            for (int i = 0; i < residents; i++) residentViews[i] = new TravelerView(game.ResidentAt(i), interpolationAlpha);
            Residents = residentViews;
            BridgeMarks = game.MarksCopy();
            var gust = game.CurrentGust;
            if (gust != null) { GustOrigin = gust.Origin; GustTarget = gust.Target; GustPhase = gust.Phase; GustProgress = gust.Progress; GustShielded = gust.Shielded; }
            Bracing = game.Bracing; GustsBlocked = game.GustsBlocked;
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
            Recovering = session.Recovery != null; RecoveryPhase = session.Recovery?.Phase; GhostHand = session.Recovery?.GhostHand;
            ConnectionLocked = session.ConnectionLocked; PipCursor = session.PipCursor; Hint = session.Hint;
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
