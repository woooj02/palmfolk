using System;
using System.Collections.Generic;

namespace Palmfolk.Core
{
    public enum SessionResult { Continue, Carried, Baked }

    // One hand interaction: Board's carry, a bridge across one gap, or (D4) a fork across several gaps from one
    // island. This is M1's hand loop (T2 connection, T3 build, T4 validation, T5 recovery) with the anchors, the
    // rules and the width cap supplied by the valley. Positions are valley-local.
    //
    // A fork is a trunk from the wrist with one branch per destination. Each branch is judged with the trunk on its
    // own (T4 "per branch"): branches that pass are baked, branches that fail are refused and their edges stay open.
    public sealed class EdgeSession
    {
        private readonly GameSettings settings;
        private readonly IClearanceField clearance;
        private readonly Action<GameEventKind> emit;
        private readonly HandFilter leftFilter = new HandFilter(), rightFilter = new HandFilter();
        private readonly ConnectionWatcher leftContact = new ConnectionWatcher(), rightContact = new ConnectionWatcher();
        private readonly ConnectionWatcher[] destinationContacts;
        private readonly StillnessWatcher stillness;
        private readonly WidthWatcher width = new WidthWatcher();
        private readonly string[] edges;
        private readonly AnchorZone[] sources, destinations;
        private Route activeRoute;
        private CurveTree activeTree;
        private ForkLayout layout, leftLayout, rightLayout;
        private RouteCursor cursor;
        private bool attached, transferring, locked;
        // The owning hand's last readable sample (valley-local), kept for the recovery ghost (T5).
        private readonly HandSample lastLeft = new HandSample(HandSide.Left), lastRight = new HandSample(HandSide.Right);
        private bool haveLast;
        private SolutionHint hint; private bool hintFound;
        private Dictionary<string, CurveTree> results = new Dictionary<string, CurveTree>();

        public string Edge => edges[0];
        public IReadOnlyList<string> Edges => edges;
        public bool Carry { get; }
        public bool IsFork => edges.Length > 1;
        // Where the hand's root must connect. For a fork, the part shared by every edge's source anchor.
        public AnchorZone Source { get; }
        public AnchorZone Destination => destinations[0];
        public IReadOnlyList<AnchorZone> Destinations => destinations;
        public TravelerKind Rule { get; }
        public double WidthCap { get; }
        public HandSide? Owner { get; private set; }
        // T18: on the last step, the owning hand was missing or its route joints were invalid or low-confidence.
        public bool PoorTracking { get; private set; }
        public Vec3 Pip { get; private set; }
        public TreeBuild Build { get; private set; }
        public Recovery Recovery { get; private set; }
        // The live route the hand loop follows (for a fork: the trunk and the first branch).
        public Route ActiveRoute => activeRoute;
        // The live shape: one curve, or the fork's trunk and branches.
        public CurveTree ActiveTree => activeTree;
        public WidthTier RouteTier => width.Tier;
        // Failures from the last validation. Curve indices refer to RefusedTree (for a fork: 0 trunk, k+1 branch k).
        public IReadOnlyList<ValidationFailure> Refusals { get; private set; } = Array.Empty<ValidationFailure>();
        public CurveTree RefusedTree { get; private set; }
        // Edges whose branch was refused in the last validation.
        public IReadOnlyList<string> RefusedEdges { get; private set; } = Array.Empty<string>();
        // Set on the tick Step returns Baked: the new bridge for each edge that passed. Fork bridges share their trunk.
        public IReadOnlyDictionary<string, CurveTree> Results => results;
        public CurveTree Result => results.TryGetValue(Edge, out var tree) ? tree : null;
        // E17: the hand holds every anchor (a carry: its fingertip holds the destination). ConnectionLocked is raised
        // when this turns on.
        public bool ConnectionLocked => locked;
        // While Pip rides the hand in a carry (Board): where he stands on ActiveRoute. Null otherwise.
        public RouteCursor? PipCursor => Carry && attached && !transferring && Recovery == null && activeRoute != null ? cursor : (RouteCursor?)null;
        // E8: a shape that solves this edge under the current rules, found once per interaction. Null if none
        // fits (a ledge still too narrow for the traveler).
        public SolutionHint Hint
        {
            get
            {
                if (!hintFound) { hint = SolutionHints.Find(Carry, Source, sources, destinations, Rule, WidthCap, settings, clearance); hintFound = true; }
                return hint;
            }
        }

        public EdgeSession(string edge, bool carry, AnchorZone source, AnchorZone destination, TravelerKind rule, double widthCap,
            Vec3 pip, GameSettings settings, IClearanceField clearance, Action<GameEventKind> emit)
            : this(new[] { edge }, carry, source, new[] { source }, new[] { destination }, rule, widthCap, pip, settings, clearance, emit) { }

        public EdgeSession(string[] edges, bool carry, AnchorZone source, AnchorZone[] sources, AnchorZone[] destinations, TravelerKind rule,
            double widthCap, Vec3 pip, GameSettings settings, IClearanceField clearance, Action<GameEventKind> emit)
        {
            if (edges == null || sources == null || destinations == null || edges.Length == 0 || edges.Length > 4 || sources.Length != edges.Length ||
                destinations.Length != edges.Length || (carry && edges.Length > 1) || !Enum.IsDefined(typeof(TravelerKind), rule) || !(widthCap > 0) || !pip.IsFinite)
                throw new ArgumentException("Invalid edge session.");
            foreach (var id in edges) if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Invalid edge session.");
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.clearance = clearance ?? throw new ArgumentNullException(nameof(clearance));
            this.emit = emit ?? throw new ArgumentNullException(nameof(emit));
            this.edges = (string[])edges.Clone(); this.sources = (AnchorZone[])sources.Clone(); this.destinations = (AnchorZone[])destinations.Clone();
            Carry = carry; Source = source; Rule = rule; WidthCap = widthCap; Pip = pip;
            destinationContacts = new ConnectionWatcher[edges.Length];
            for (int i = 0; i < edges.Length; i++) destinationContacts[i] = new ConnectionWatcher();
            stillness = new StillnessWatcher(settings.StillnessTicks);
        }

        // Pause, focus loss or replanting: a hand carrying Pip goes into recovery.
        public void Suspend()
        {
            if (Owner.HasValue && Recovery == null) BeginRecovery(false);
            leftFilter.Reset(); rightFilter.Reset(); stillness.Reset();
        }
        // Controllers in hand before anyone owns Pip (E19): nothing connects.
        public void HoldForControllers() { leftContact.Reset(); rightContact.Reset(); }

        // The hand read with the owner's fixed topology (finger and direction, or fork layout).
        private CurveTree Read(HandSample hand, out Route spine)
        {
            spine = null;
            if (hand == null || activeRoute == null) return null;
            if (layout == null)
            {
                spine = HandRoutes.Create(hand, activeRoute.Finger, activeRoute.Reversed, settings);
                return spine == null ? null : CurveTree.Single(spine);
            }
            var tree = HandRoutes.Fork(hand, layout, settings);
            if (tree != null) spine = HandRoutes.Spine(tree);
            return tree;
        }
        private uint RequiredMask()
        {
            if (layout == null) return HandRoutes.RequiredJointMask(activeRoute.Finger);
            uint mask = 0; var branches = layout.Branches;
            for (int b = 0; b < branches.Count; b++) for (int i = 0; i < branches[b].Count; i++) mask |= HandRoutes.RequiredJointMask(branches[b][i]);
            return mask;
        }

        public SessionResult Step(HandFrame frame, double stillnessThreshold)
        {
            var left = leftFilter.Step(frame.Left, settings); var right = rightFilter.Step(frame.Right, settings);
            var hand = Owner == HandSide.Left ? left : right;
            CurveTree liveTree = null; Route live = null; PoorTracking = false;
            if (Owner.HasValue && activeRoute != null) liveTree = Read(hand, out live);
            if (Owner.HasValue)
            {
                // Test raw data too: smoothing must not make a returning hand look aligned for a tick.
                var rawHand = frame.Hand(Owner.Value);
                // D11: a single route's raw check builds nothing; the raw route is made only when recovery needs it.
                Route raw = null; bool rawReadable;
                if (layout == null) rawReadable = activeRoute != null && HandRoutes.CanCreate(rawHand, activeRoute.Finger, activeRoute.Reversed, settings);
                else { Read(rawHand, out raw); rawReadable = raw != null; }
                bool poorTracking = PoorTracking = rawHand == null || (rawHand.InvalidMask & RequiredMask()) != 0;
                if (!rawReadable && Recovery == null) BeginRecovery(poorTracking);
                if (Recovery != null)
                {
                    if (rawReadable && raw == null) Read(rawHand, out raw);
                    var previous = Recovery.Phase;
                    var aligned = live != null && Recovery.Ghost.Difference(live) < settings.ConnectDistance ? raw : null;
                    Vec3 support = live == null ? Pip : (Build != null && !Carry ? Build.BuilderPosition(liveTree) : live.Evaluate(cursor));
                    Recovery.Step(aligned, support); Pip = Recovery.Position;
                    if (Build != null) Build.Step(null, false, false);
                    if (previous != Recovery.Phase && Recovery.Phase == RecoveryPhase.Blending) emit(GameEventKind.RecoveryAligned);
                    if (previous != Recovery.Phase && Recovery.Phase == RecoveryPhase.Returning) { Build?.Remove(); emit(GameEventKind.ReturningToCheckpoint); }
                    if (Recovery.Phase == RecoveryPhase.Recovered) { Recovery = null; attached = true; stillness.Reset(); emit(GameEventKind.RecoveryCompleted); }
                    else if (Recovery.Phase == RecoveryPhase.Returned) { Pip = Source.Centre; transferring = false; ReleaseHand(); Recovery = null; }
                    return SessionResult.Continue;
                }
                if (live != null)
                {
                    activeRoute = live; activeTree = liveTree; width.Step(MinimumWidth(liveTree));
                    Array.Copy(hand.Buffer, (hand.Side == HandSide.Left ? lastLeft : lastRight).Buffer, 26); haveLast = true;
                }
            }
            if (transferring)
            {
                Pip = MoveTowards(Pip, Destination.Centre, settings.TravelerSpeed * GameSettings.Delta);
                if (Vec3.Distance(Pip, Destination.Centre) < 1e-9) { transferring = false; emit(GameEventKind.CheckpointReached); ReleaseHand(); return SessionResult.Carried; }
                return SessionResult.Continue;
            }
            if (Build != null && (Build.Phase == BuildPhase.Removing || Build.Phase == BuildPhase.Removed))
            {
                Build.Step(null, false, false);
                if (Build.Phase == BuildPhase.Removed && !Owner.HasValue) Build = null;
            }
            if (!Owner.HasValue) Acquire(left, right);
            if (!Owner.HasValue || activeRoute == null) return SessionResult.Continue;
            if (frame.Command == InputCommand.CancelBuild || frame.Command == InputCommand.StartElsewhere || frame.Command == InputCommand.Restart)
            {
                Build?.Remove(); BeginRecovery(false); Recovery.ReturnToCheckpoint(); emit(GameEventKind.ScaffoldRemoving); emit(GameEventKind.ReturningToCheckpoint);
                return SessionResult.Continue;
            }
            if (!attached)
            {
                Pip = MoveTowards(Pip, activeRoute.Start, settings.TravelerSpeed * GameSettings.Delta);
                if (Vec3.Distance(Pip, activeRoute.Start) < 1e-9) { attached = true; emit(GameEventKind.Boarded); }
                return SessionResult.Continue;
            }
            return Carry ? StepCarry() : StepBuild(stillnessThreshold);
        }

        private static double MinimumWidth(CurveTree tree)
        {
            double w = double.PositiveInfinity; for (int i = 0; i < tree.Curves.Count; i++) w = Math.Min(w, tree.Curves[i].MinimumWidth); return w;
        }

        private void Acquire(HandSample left, HandSample right)
        {
            CurveTree ta, tb; Route a, b;
            if (IsFork)
            {
                var ends = new Vec3[destinations.Length]; for (int i = 0; i < ends.Length; i++) ends[i] = destinations[i].Centre;
                ta = HandRoutes.BestFork(left, Source.Centre, ends, settings, out leftLayout); tb = HandRoutes.BestFork(right, Source.Centre, ends, settings, out rightLayout);
                a = ta == null ? null : HandRoutes.Spine(ta); b = tb == null ? null : HandRoutes.Spine(tb);
            }
            else
            {
                // D11: each hand's best route is chosen from its end points; only the hand that connects builds it.
                bool ha = HandRoutes.BestChoice(left, Source.Centre, Destination.Centre, settings, out int fa, out bool ra, out var sa);
                bool hb = HandRoutes.BestChoice(right, Source.Centre, Destination.Centre, settings, out int fb, out bool rb, out var sb);
                double sda = ha ? Source.ConnectionDistance(sa, settings) : double.PositiveInfinity;
                double sdb = hb ? Source.ConnectionDistance(sb, settings) : double.PositiveInfinity;
                leftContact.Step(sda, ha, settings); rightContact.Step(sdb, hb, settings);
                if (!leftContact.Connected && !rightContact.Connected) return;
                bool left1 = leftContact.Connected && (!rightContact.Connected || sda <= sdb);
                var route = left1 ? HandRoutes.Create(left, fa, ra, settings) : HandRoutes.Create(right, fb, rb, settings);
                if (route == null) return;
                Take(left1, route, CurveTree.Single(route), null);
                return;
            }
            double da = a == null ? double.PositiveInfinity : Source.ConnectionDistance(a.Start, settings);
            double db = b == null ? double.PositiveInfinity : Source.ConnectionDistance(b.Start, settings);
            leftContact.Step(da, a != null, settings); rightContact.Step(db, b != null, settings);
            if (!leftContact.Connected && !rightContact.Connected) return;
            bool chooseLeft = leftContact.Connected && (!rightContact.Connected || da <= db);
            Take(chooseLeft, chooseLeft ? a : b, chooseLeft ? ta : tb, chooseLeft ? leftLayout : rightLayout);
        }

        private void Take(bool chooseLeft, Route route, CurveTree tree, ForkLayout forkLayout)
        {
            Owner = chooseLeft ? HandSide.Left : HandSide.Right; activeRoute = route; activeTree = tree;
            layout = IsFork ? forkLayout : null;
            width.Step(MinimumWidth(activeTree));
            cursor = new RouteCursor(0, 0); attached = false; locked = false; haveLast = false; stillness.Reset(); foreach (var c in destinationContacts) c.Reset();
        }

        private SessionResult StepCarry()
        {
            double distance = Destination.ConnectionDistance(activeRoute.End, settings);
            destinationContacts[0].Step(distance, true, settings);
            Lock(destinationContacts[0].Connected);
            cursor = activeRoute.Advance(cursor, settings.TravelerSpeed * GameSettings.Delta);
            Pip = activeRoute.Evaluate(cursor);
            if (activeRoute.AtEnd(cursor) && destinationContacts[0].Connected && distance < settings.ConnectDistance) transferring = true;
            return SessionResult.Continue;
        }

        private SessionResult StepBuild(double stillnessThreshold)
        {
            // Stillness is read at the fingertip end (a fork: the first branch's tip).
            bool still = stillness.Step(IsFork ? activeRoute.End : activeRoute.Reversed ? activeRoute.End : activeRoute.Start, stillnessThreshold);
            var sourceContact = Owner == HandSide.Left ? leftContact : rightContact;
            sourceContact.Step(Source.ConnectionDistance(activeTree.Curves[0].Start, settings), true, settings);
            bool connected = sourceContact.Connected;
            for (int i = 0; i < destinations.Length; i++)
            {
                var end = IsFork ? activeTree.Curves[i + 1].End : activeRoute.End;
                destinationContacts[i].Step(destinations[i].ConnectionDistance(end, settings), true, settings);
                connected &= destinationContacts[i].Connected;
            }
            Lock(connected);
            if (Build == null)
            {
                // Pip rides the fingertip he boarded at until the build starts, so a hand that settles a little
                // before going still doesn't make him pop to the build front (D7).
                Pip = activeRoute.Start;
                if (!connected || !still) return SessionResult.Continue;
                Build = new TreeBuild(activeTree, settings); Refusals = Array.Empty<ValidationFailure>(); RefusedTree = null; RefusedEdges = Array.Empty<string>();
                emit(GameEventKind.BuildStarted);
            }
            var phase = Build.Phase; int sections = Build.FrozenSections;
            Build.Step(activeTree, connected, still);
            if (Build.Phase == BuildPhase.Paused && phase != BuildPhase.Paused) emit(GameEventKind.BuildPaused);
            if (Build.FrozenSections > sections) emit(GameEventKind.SectionCommitted);
            if (Build.Phase == BuildPhase.Removing)
            {
                BeginRecovery(false); Recovery.ReturnToCheckpoint(); emit(GameEventKind.ScaffoldRemoving); emit(GameEventKind.ReturningToCheckpoint);
                return SessionResult.Continue;
            }
            if (Build.Phase == BuildPhase.Building || Build.Phase == BuildPhase.Complete)
            {
                Pip = Build.BuilderPosition(activeTree);
                double parameter = Build.Progress * (activeRoute.Points.Count - 1); int segment = Math.Min(activeRoute.Points.Count - 2, (int)parameter);
                cursor = new RouteCursor(segment, parameter - segment);
            }
            if (Build.Phase != BuildPhase.Complete) return SessionResult.Continue;
            return Judge(Cap(Build.FrozenTree(), WidthCap));
        }

        // T4: validate each branch with the trunk, against its own anchors.
        private SessionResult Judge(CurveTree tree)
        {
            var failures = new List<ValidationFailure>(); var refused = new List<string>(); results = new Dictionary<string, CurveTree>();
            for (int k = 0; k < edges.Length; k++)
            {
                var walk = IsFork ? new CurveTree(new[] { tree.Curves[0], tree.Curves[k + 1] }, new[] { -1, 0 }) : tree;
                var found = BridgeValidator.Validate(walk, sources[k], new[] { destinations[k] }, Rule, settings, clearance);
                if (found.Count == 0) { results[edges[k]] = walk; continue; }
                refused.Add(edges[k]);
                foreach (var f in found) failures.Add(new ValidationFailure(f.Reason, IsFork && f.Curve == 1 ? k + 1 : f.Curve, f.Segment));
            }
            Refusals = failures.AsReadOnly(); RefusedEdges = refused.AsReadOnly(); RefusedTree = refused.Count > 0 ? tree : null;
            if (results.Count == 0)
            {
                Build.Remove(); BeginRecovery(false); Recovery.ReturnToCheckpoint(); emit(GameEventKind.Refused); emit(GameEventKind.ReturningToCheckpoint);
                return SessionResult.Continue;
            }
            // Some branches passed: they stay; a refused branch is taken down by the views from RefusedTree.
            if (refused.Count > 0) emit(GameEventKind.Refused);
            Build = null; emit(GameEventKind.BridgeBaked); ReleaseHand();
            return SessionResult.Baked;
        }

        // V2: the deck can be no wider than the ledge it leaves from.
        public static Route Cap(Route route, double widthCap)
        {
            if (double.IsPositiveInfinity(widthCap)) return route;
            var widths = route.CopyWidths(); bool changed = false;
            for (int i = 0; i < widths.Length; i++) if (widths[i] > widthCap) { widths[i] = widthCap; changed = true; }
            return changed ? new Route(route.CopyPoints(), widths, route.Finger, route.Reversed) : route;
        }
        public static CurveTree Cap(CurveTree tree, double widthCap)
        {
            if (double.IsPositiveInfinity(widthCap)) return tree;
            var curves = new Route[tree.Curves.Count]; var parents = new int[curves.Length];
            for (int i = 0; i < curves.Length; i++) { curves[i] = Cap(tree.Curves[i], widthCap); parents[i] = tree.Parents[i]; }
            return new CurveTree(curves, parents);
        }

        private void Lock(bool connected)
        {
            if (connected && !locked) emit(GameEventKind.ConnectionLocked);
            locked = connected;
        }

        private void BeginRecovery(bool trackingLost)
        {
            if (activeRoute == null) return;
            Recovery = new Recovery(activeRoute, Pip, Source.Centre, settings);
            if (haveLast && Owner.HasValue) Recovery.GhostHand = (Owner == HandSide.Left ? lastLeft : lastRight).Copy();
            locked = false;
            leftFilter.Reset(); rightFilter.Reset(); stillness.Reset();
            if (trackingLost) emit(GameEventKind.TrackingLost);
        }
        private void ReleaseHand()
        {
            Owner = null; attached = false; locked = false; haveLast = false; activeRoute = null; activeTree = null; layout = null;
            leftContact.Reset(); rightContact.Reset(); foreach (var c in destinationContacts) c.Reset(); stillness.Reset();
            emit(GameEventKind.HandReleased);
        }
        internal static Vec3 MoveTowards(Vec3 from, Vec3 to, double distance) => Vec3.Distance(from, to) <= distance ? to : from + (to - from).Normalized * distance;
    }
}
