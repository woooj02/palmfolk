using System;
using System.Collections.Generic;
using System.Linq;

namespace Palmfolk.Core
{
    public enum DirectorStage { Walking, Interacting, Widening, AwaitingWithdrawal, TravelersCrossing, BramCrossing, Complete }

    // What the valley keeps between puzzles: what Save v2 (D5) stores. Bridges are valley-local.
    public sealed class ValleyProgress
    {
        public int CompletedPuzzles;
        public string PipIsland;
        public bool LedgeWidened;
        public Dictionary<string, CurveTree> Bridges = new Dictionary<string, CurveTree>();
        public ValleyProgress Copy() => new ValleyProgress { CompletedPuzzles = CompletedPuzzles, PipIsland = PipIsland, LedgeWidened = LedgeWidened, Bridges = new Dictionary<string, CurveTree>(Bridges) };
    }

    // A traveler walking a fixed path: bridge curves joined by straight walks across islands.
    public sealed class Traveler
    {
        private readonly List<Route> legs;
        private readonly Vec3 end;
        private int leg;
        private bool onLeg;
        private RouteCursor cursor;
        public TravelerKind Kind { get; }
        public string Destination { get; }
        public Vec3 Position { get; private set; }
        public Vec3 PreviousPosition { get; private set; }
        public bool Arrived { get; private set; }
        // How many bridges this traveler has finished crossing.
        public int Crossed => leg;
        internal Traveler(TravelerKind kind, string destination, Vec3 start, List<Route> legs, Vec3 end)
        {
            Kind = kind; Destination = destination; Position = PreviousPosition = start; this.legs = legs; this.end = end;
        }
        // Returns true on the tick a bridge crossing is finished.
        internal bool Step(double distance)
        {
            PreviousPosition = Position;
            if (Arrived) return false;
            if (leg < legs.Count)
            {
                var route = legs[leg];
                if (!onLeg)
                {
                    Position = EdgeSession.MoveTowards(Position, route.Start, distance);
                    if (Vec3.Distance(Position, route.Start) < 1e-9) { onLeg = true; cursor = new RouteCursor(0, 0); }
                    return false;
                }
                cursor = route.Advance(cursor, distance); Position = route.Evaluate(cursor);
                if (!route.AtEnd(cursor)) return false;
                leg++; onLeg = false; return true;
            }
            Position = EdgeSession.MoveTowards(Position, end, distance);
            if (Vec3.Distance(Position, end) < 1e-9) Arrived = true;
            return false;
        }
        internal void Settle() => PreviousPosition = Position;
    }

    // D3: the valley and its six puzzles. Feed valley-local frames (PalmfolkGame does the conversion).
    public sealed class ValleySimulation : ITickSimulation
    {
        private readonly GameSettings settings;
        private readonly List<GameEvent> events = new List<GameEvent>();
        private readonly HintWatcher hints = new HintWatcher();
        private readonly ValleyProgress progress;
        private readonly List<string> pending = new List<string>();
        private readonly List<string> built = new List<string>();
        private readonly List<Traveler> travelers = new List<Traveler>();
        private Traveler pipWalk;
        private string pipTarget, sourceIsland;
        private bool paused, hasFocus = true, withdrawnObserved, announce = true;
        private int startupWaitTicks, wideningTicks;

        public ValleySpec Valley { get; }
        public long Tick { get; private set; } = -1;
        public IReadOnlyList<GameEvent> Events => events.AsReadOnly();
        public GameSettings Settings => settings.Copy();
        public int PuzzleIndex => Math.Min(progress.CompletedPuzzles, 5);
        public PuzzleSpec Puzzle => Valley.Puzzles[PuzzleIndex];
        public DirectorStage Stage { get; private set; }
        public EdgeSession Session { get; private set; }
        public Vec3 Pip { get; private set; }
        public Vec3 PreviousPip { get; private set; }
        public string PipIsland => progress.PipIsland;
        public Traveler Bram { get; private set; }
        public Vec3 BramPosition => Bram?.Position ?? Valley.Island(Valley.Camp).Top;
        public IReadOnlyList<Traveler> Travelers => travelers.AsReadOnly();
        public IReadOnlyDictionary<string, CurveTree> Bridges => progress.Bridges;
        // V3: edges on the needed route that fail their traveler's rules and must be rebuilt.
        public IReadOnlyList<string> FailingEdges { get; private set; } = Array.Empty<string>();
        public IReadOnlyList<string> PendingEdges => pending.AsReadOnly();
        // A fork branch refused while its siblings were baked (D4): drawn glowing and taken down, until the next interaction.
        public CurveTree RefusedScaffold { get; private set; }
        public IReadOnlyList<ValidationFailure> RefusedFailures { get; private set; } = Array.Empty<ValidationFailure>();
        public double LedgeWidth => Valley.Ledge == null ? double.PositiveInfinity : progress.LedgeWidened ? Valley.Ledge.WideWidth : Valley.Ledge.NarrowWidth;
        public double WideningProgress => Valley.Ledge == null ? 1 : progress.LedgeWidened ? 1 : (double)wideningTicks / Valley.Ledge.WideningTicks;
        public bool IsPaused => paused || !hasFocus;
        public bool ControllersInHand { get; private set; }
        public bool WaitingForHands { get; private set; }
        public bool ShowStartupText => WaitingForHands && startupWaitTicks >= 600;
        public bool IsComplete => Stage == DirectorStage.Complete;
        public ValleyProgress Progress => progress.Copy();

        public ValleySimulation(ValleySpec valley, GameSettings settings = null, ValleyProgress resume = null)
        {
            Valley = valley ?? throw new ArgumentNullException(nameof(valley));
            this.settings = (settings ?? new GameSettings()).Copy(); this.settings.Validate();
            progress = resume?.Copy() ?? new ValleyProgress { PipIsland = valley.Puzzles[0].TravelerFrom };
            if (progress.CompletedPuzzles < 0 || progress.CompletedPuzzles > 6 || progress.Bridges == null) throw new ArgumentException("Invalid valley progress.");
            valley.Island(progress.PipIsland);
            foreach (var pair in progress.Bridges) { valley.Edge(pair.Key); if (pair.Value == null) throw new ArgumentException("Missing bridge for " + pair.Key); }
            var board = valley.Edge(valley.Puzzles[0].Edges[0]);
            Pip = PreviousPip = progress.CompletedPuzzles == 0 ? board.AnchorAt(valley.Puzzles[0].TravelerFrom).Centre : valley.Island(progress.PipIsland).Top;
            BeginPuzzle();
        }

        public void Step(HandFrame frame)
        {
            if (frame == null || frame.Tick != Tick + 1) throw new ArgumentException("Feed exactly one sequential frame per 60 Hz tick.");
            Tick = frame.Tick; events.Clear(); PreviousPip = Pip;
            if (announce) { announce = false; if (Stage != DirectorStage.Complete) Emit(GameEventKind.PuzzleStarted); }
            foreach (var t in travelers) t.Settle(); Bram?.Settle();
            hasFocus = frame.HasFocus; ControllersInHand = frame.ControllersInHand;
            if (frame.Command == InputCommand.Pause) paused = true;
            if (frame.Command == InputCommand.Resume) paused = false;
            if (IsPaused) { Session?.Suspend(); return; }

            bool usable = Tracked(frame.Left) || Tracked(frame.Right);
            bool boarding = PuzzleIndex == 0 && Stage == DirectorStage.Interacting && Session != null && !Session.Owner.HasValue;
            WaitingForHands = boarding && (frame.ControllersInHand || !usable);
            startupWaitTicks = WaitingForHands ? startupWaitTicks + 1 : 0;
            if (startupWaitTicks == 600) Emit(GameEventKind.StartupPromptDue);

            double step = settings.TravelerSpeed * GameSettings.Delta;
            switch (Stage)
            {
                case DirectorStage.Walking:
                    pipWalk.Step(step); Pip = pipWalk.Position;
                    if (pipWalk.Arrived) { progress.PipIsland = pipTarget; pipWalk = null; ArrivedAtSource(); }
                    break;
                case DirectorStage.Interacting:
                    if (boarding && frame.ControllersInHand) { Session.HoldForControllers(); break; }
                    var result = Session.Step(frame, settings.StillnessThreshold(hints.StillnessAdjustment));
                    Pip = Session.Pip;
                    if (result == SessionResult.Carried) { progress.PipIsland = Valley.Edge(Session.Edge).Other(sourceIsland); Session = null; CompletePuzzle(); }
                    else if (result == SessionResult.Baked) Baked(frame);
                    break;
                case DirectorStage.Widening:
                    if (++wideningTicks >= Valley.Ledge.WideningTicks) { progress.LedgeWidened = true; Emit(GameEventKind.LedgeWidened); CheckRoute(); }
                    break;
                case DirectorStage.AwaitingWithdrawal:
                    if (!HandNear(frame.Left) && !HandNear(frame.Right))
                    {
                        if (!withdrawnObserved) { withdrawnObserved = true; Emit(GameEventKind.PersistentBridgeObserved); }
                        if (PuzzleIndex == 5) StartBram(); else StartTravelers();
                    }
                    break;
                case DirectorStage.TravelersCrossing:
                    foreach (var t in travelers) if (!t.Arrived) { t.Step(step); if (t.Arrived) Emit(GameEventKind.TravelerArrived); }
                    if (travelers.All(t => t.Arrived)) CompletePuzzle();
                    break;
                case DirectorStage.BramCrossing:
                    if (Bram.Step(step)) Emit(GameEventKind.CrossingCompleted);
                    if (Bram.Arrived) { Emit(GameEventKind.BramFinished); CompletePuzzle(); }
                    break;
            }
            bool poor = (frame.Left == null || frame.Left.InvalidMask != 0) && (frame.Right == null || frame.Right.InvalidMask != 0);
            bool waitingOnPlayer = Stage == DirectorStage.Interacting || Stage == DirectorStage.AwaitingWithdrawal;
            hints.Step(!waitingOnPlayer || events.Count > 0, waitingOnPlayer && poor);
            if (waitingOnPlayer) { if (hints.HintDue) Emit(GameEventKind.HintDue); if (hints.QualityPromptDue) Emit(GameEventKind.QualityPromptDue); }
        }

        // V3: which edges of a route fail the traveler's rules (missing, narrow, steep, blocked, unanchored).
        public IReadOnlyList<string> Check(IEnumerable<string> route, TravelerKind kind) =>
            route.Where(id => !Passes(id, kind)).ToList().AsReadOnly();

        public bool Passes(string edgeId, TravelerKind kind)
        {
            var edge = Valley.Edge(edgeId);
            if (!progress.Bridges.TryGetValue(edgeId, out var tree)) return false;
            // A bridge may have been built from either end.
            bool forward = edge.FromAnchor.Contains(tree.Curves[0].Start);
            var start = forward ? edge.FromAnchor : edge.ToAnchor; var end = forward ? edge.ToAnchor : edge.FromAnchor;
            return BridgeValidator.Validate(tree, start, new[] { end }, kind, settings, Valley.Clearance).Count == 0;
        }

        private void BeginPuzzle()
        {
            pending.Clear(); built.Clear(); travelers.Clear(); Session = null; withdrawnObserved = false; FailingEdges = Array.Empty<string>();
            RefusedScaffold = null; RefusedFailures = Array.Empty<ValidationFailure>();
            if (progress.CompletedPuzzles >= 6) { Stage = DirectorStage.Complete; return; }
            var puzzle = Puzzle;
            if (!announce) Emit(GameEventKind.PuzzleStarted);
            if (puzzle.Kind == PuzzleKind.Board) { pending.Add(puzzle.Edges[0]); NextEdge(); return; }
            if (puzzle.Kind == PuzzleKind.Finale)
            {
                if (Valley.Ledge != null && !progress.LedgeWidened)
                {
                    // Pip walks to the ledge and hammers it wider (V2).
                    wideningTicks = 0; WalkTo(Valley.Ledge.Island, DirectorStage.Widening); return;
                }
                CheckRoute(); return;
            }
            pending.AddRange(puzzle.Edges.Where(id => !Passes(id, puzzle.Rule)));
            built.AddRange(puzzle.Edges);
            if (pending.Count == 0) { Stage = DirectorStage.AwaitingWithdrawal; return; }
            NextEdge();
        }

        private void CheckRoute()
        {
            var route = Valley.Puzzles[5].Edges;
            FailingEdges = Check(route, TravelerKind.Cart);
            foreach (var id in FailingEdges) Emit(GameEventKind.RebuildNeeded);
            pending.Clear(); pending.AddRange(FailingEdges); built.Clear(); built.AddRange(route);
            if (pending.Count == 0) { Stage = DirectorStage.AwaitingWithdrawal; return; }
            NextEdge();
        }

        private void NextEdge()
        {
            var edge = Valley.Edge(pending[0]);
            // Board starts where Pip stands. A bridge is built from the edge's From island when Pip can walk
            // there, otherwise from its To island (a saved bridge may be missing).
            string from = Puzzle.Kind == PuzzleKind.Board ? Puzzle.TravelerFrom
                : Reachable(progress.PipIsland, edge.From) ? edge.From : Reachable(progress.PipIsland, edge.To) ? edge.To : edge.From;
            sourceIsland = from;
            if (progress.PipIsland == from && Vec3.Distance(Pip, edge.AnchorAt(from).Centre) < 1e-9) { ArrivedAtSource(); return; }
            WalkTo(from, DirectorStage.Interacting, edge.AnchorAt(from).Centre);
        }

        private void WalkTo(string island, DirectorStage then, Vec3? point = null)
        {
            pipTarget = island; afterWalk = then;
            pipWalk = new Traveler(TravelerKind.Walker, island, Pip, Path(progress.PipIsland, island), point ?? Valley.Island(island).Top);
            Stage = DirectorStage.Walking;
        }
        private DirectorStage afterWalk;

        private void ArrivedAtSource()
        {
            if (afterWalk == DirectorStage.Widening && Stage == DirectorStage.Walking && pending.Count == 0)
            { Stage = DirectorStage.Widening; Emit(GameEventKind.LedgeWidening); return; }
            var edge = Valley.Edge(pending[0]); var puzzle = Puzzle;
            var source = edge.AnchorAt(sourceIsland); var destination = edge.AnchorAt(edge.Other(sourceIsland));
            bool carry = puzzle.Kind == PuzzleKind.Board;
            double cap = Valley.Ledge != null && Valley.Ledge.Edge == edge.Id ? LedgeWidth : double.PositiveInfinity;
            RefusedScaffold = null; RefusedFailures = Array.Empty<ValidationFailure>();
            Session = ForkSession(cap) ?? new EdgeSession(edge.Id, carry, source, destination, puzzle.Rule, cap, Pip, settings, Valley.Clearance, Emit);
            Stage = DirectorStage.Interacting;
        }

        // D4: in the Fork puzzle, every open edge from Pip's island is built with one spread hand, when their
        // source anchors overlap (the root must sit in all of them). Otherwise the edges are built one at a time.
        private EdgeSession ForkSession(double cap)
        {
            if (Puzzle.Kind != PuzzleKind.Fork) return null;
            var ids = pending.Where(id => Valley.Edge(id).Touches(sourceIsland)).ToArray();
            if (ids.Length < 2) return null;
            var sources = ids.Select(id => Valley.Edge(id).AnchorAt(sourceIsland)).ToArray();
            var destinations = ids.Select(id => { var e = Valley.Edge(id); return e.AnchorAt(e.Other(sourceIsland)); }).ToArray();
            var centre = Vec3.Zero; foreach (var a in sources) centre += a.Centre; centre = centre * (1.0 / sources.Length);
            double radius = sources.Min(a => a.Radius - Vec3.Distance(a.Centre, centre));
            if (radius < 0.005) return null;
            return new EdgeSession(ids, false, new AnchorZone(centre, radius), sources, destinations, Puzzle.Rule, cap, Pip, settings, Valley.Clearance, Emit);
        }

        private void Baked(HandFrame frame)
        {
            // R4: the old bridge is replaced only now, after the new one passed. A fork stores one tree per edge
            // (the shared trunk and that edge's branch); refused branches leave their edges open.
            foreach (var pair in Session.Results) { progress.Bridges[pair.Key] = pair.Value; pending.Remove(pair.Key); }
            RefusedScaffold = Session.RefusedTree; RefusedFailures = Session.Refusals;
            // Pip stands at the end of the first branch; if that branch was refused he walks back from there.
            progress.PipIsland = Session.Results.ContainsKey(Session.Edge) ? Valley.Edge(Session.Edge).Other(sourceIsland) : sourceIsland;
            Session = null;
            if (Puzzle.Kind == PuzzleKind.Finale) FailingEdges = FailingEdges.Where(pending.Contains).ToList().AsReadOnly();
            if (pending.Count > 0) { NextEdge(); return; }
            Stage = DirectorStage.AwaitingWithdrawal;
        }

        private void StartTravelers()
        {
            var puzzle = Puzzle;
            foreach (var id in built)
            {
                var edge = Valley.Edge(id); string to = edge.Other(puzzle.TravelerFrom);
                travelers.Add(new Traveler(puzzle.Traveler, to, Valley.Island(puzzle.TravelerFrom).Top,
                    Legs(id, puzzle.TravelerFrom, to), Valley.Island(to).Top));
            }
            Stage = DirectorStage.TravelersCrossing; Emit(GameEventKind.TravelersStarted);
        }

        private void StartBram()
        {
            var legs = new List<Route>();
            foreach (var (edge, from, to) in Valley.FinaleRoute()) legs.AddRange(Legs(edge.Id, from, to));
            Bram = new Traveler(TravelerKind.Cart, Valley.Sanctuary, Valley.Island(Valley.Camp).Top, legs, Valley.Island(Valley.Sanctuary).Top);
            Stage = DirectorStage.BramCrossing; Emit(GameEventKind.BramStarted);
        }

        private void CompletePuzzle()
        {
            Emit(GameEventKind.PuzzleCompleted);
            progress.CompletedPuzzles++;
            BeginPuzzle();
        }

        // The bridge's curves oriented from one island to the other. A tree (D4) walks root to the leaf that lands there.
        private List<Route> Legs(string edgeId, string from, string to)
        {
            var edge = Valley.Edge(edgeId); var tree = progress.Bridges[edgeId];
            var target = edge.AnchorAt(to).Centre; var origin = edge.AnchorAt(from).Centre;
            bool rootAtFrom = Vec3.Distance(tree.Curves[0].Start, origin) <= Vec3.Distance(tree.Curves[0].Start, target);
            var far = rootAtFrom ? target : origin;
            int leaf = 0; double best = double.PositiveInfinity;
            for (int c = 0; c < tree.Curves.Count; c++)
            {
                bool isLeaf = true; for (int i = c + 1; i < tree.Parents.Count; i++) if (tree.Parents[i] == c) isLeaf = false;
                double d = Vec3.Distance(tree.Curves[c].End, far);
                if (isLeaf && d < best) { best = d; leaf = c; }
            }
            var chain = new List<Route>();
            for (int c = leaf; c >= 0; c = tree.Parents[c]) chain.Insert(0, tree.Curves[c]);
            if (rootAtFrom) return chain;
            chain.Reverse(); return chain.Select(r => r.Reverse()).ToList();
        }

        // Pip walks over existing bridges. If none connects (only after save corruption), he walks straight there.
        private List<Route> Path(string from, string to)
        {
            var previous = new Dictionary<string, (string Island, string Edge)> { [from] = (null, null) };
            var queue = new Queue<string>(); queue.Enqueue(from);
            while (queue.Count > 0)
            {
                var at = queue.Dequeue(); if (at == to) break;
                foreach (var edge in Valley.Edges)
                    if (edge.Touches(at) && progress.Bridges.ContainsKey(edge.Id) && !previous.ContainsKey(edge.Other(at)))
                    { previous[edge.Other(at)] = (at, edge.Id); queue.Enqueue(edge.Other(at)); }
            }
            var legs = new List<Route>();
            if (!previous.ContainsKey(to)) return legs;
            for (string at = to; previous[at].Island != null; at = previous[at].Island) legs.InsertRange(0, Legs(previous[at].Edge, previous[at].Island, at));
            return legs;
        }
        private bool Reachable(string from, string to) => from == to || Path(from, to).Count > 0;

        private bool HandNear(HandSample hand)
        {
            if (hand == null) return false;
            foreach (var id in built)
            {
                if (!progress.Bridges.TryGetValue(id, out var tree)) continue;
                foreach (var joint in hand.Joints)
                    if (joint.Usable) foreach (var curve in tree.Curves) for (int i = 0; i < curve.Widths.Count; i++)
                        if (BridgeValidator.PointDistance(joint.Position, curve.Points[i], curve.Points[i + 1]) < settings.BreakDistance) return true;
            }
            return false;
        }
        private static bool Tracked(HandSample hand) => hand != null && hand[JointId.Wrist].Usable && hand[JointId.Palm].Usable;
        private void Emit(GameEventKind kind) => events.Add(new GameEvent(Tick, kind));
    }
}
