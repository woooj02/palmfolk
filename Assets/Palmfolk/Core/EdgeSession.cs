using System;
using System.Collections.Generic;

namespace Palmfolk.Core
{
    public enum SessionResult { Continue, Carried, Baked }

    // One hand interaction across one gap: Board's carry, or building a bridge. This is M1's hand loop
    // (T2 connection, T3 build, T4 validation, T5 recovery) with the anchors, the rules and the width cap
    // supplied by the valley instead of fixed. Positions are valley-local.
    public sealed class EdgeSession
    {
        private readonly GameSettings settings;
        private readonly IClearanceField clearance;
        private readonly Action<GameEventKind> emit;
        private readonly HandFilter leftFilter = new HandFilter(), rightFilter = new HandFilter();
        private readonly ConnectionWatcher leftContact = new ConnectionWatcher(), rightContact = new ConnectionWatcher(), destinationContact = new ConnectionWatcher();
        private readonly StillnessWatcher stillness;
        private readonly WidthWatcher width = new WidthWatcher();
        private Route activeRoute;
        private RouteCursor cursor;
        private bool attached, transferring;

        public string Edge { get; }
        public bool Carry { get; }
        public AnchorZone Source { get; }
        public AnchorZone Destination { get; }
        public TravelerKind Rule { get; }
        public double WidthCap { get; }
        public HandSide? Owner { get; private set; }
        public Vec3 Pip { get; private set; }
        public BridgeBuild Build { get; private set; }
        public Recovery Recovery { get; private set; }
        public Route ActiveRoute => activeRoute;
        public WidthTier RouteTier => width.Tier;
        public IReadOnlyList<ValidationFailure> Refusals { get; private set; } = Array.Empty<ValidationFailure>();
        // The baked bridge, set on the tick Step returns Baked.
        public CurveTree Result { get; private set; }

        public EdgeSession(string edge, bool carry, AnchorZone source, AnchorZone destination, TravelerKind rule, double widthCap,
            Vec3 pip, GameSettings settings, IClearanceField clearance, Action<GameEventKind> emit)
        {
            if (string.IsNullOrWhiteSpace(edge) || !Enum.IsDefined(typeof(TravelerKind), rule) || !(widthCap > 0) || !pip.IsFinite) throw new ArgumentException("Invalid edge session.");
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.clearance = clearance ?? throw new ArgumentNullException(nameof(clearance));
            this.emit = emit ?? throw new ArgumentNullException(nameof(emit));
            Edge = edge; Carry = carry; Source = source; Destination = destination; Rule = rule; WidthCap = widthCap; Pip = pip;
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

        public SessionResult Step(HandFrame frame, double stillnessThreshold)
        {
            var left = leftFilter.Step(frame.Left, settings); var right = rightFilter.Step(frame.Right, settings);
            var hand = Owner == HandSide.Left ? left : right;
            Route live = Owner.HasValue && activeRoute != null ? HandRoutes.Create(hand, activeRoute.Finger, activeRoute.Reversed, settings) : null;
            if (Owner.HasValue)
            {
                // Test raw data too: smoothing must not make a returning hand look aligned for a tick.
                var rawHand = frame.Hand(Owner.Value);
                var raw = HandRoutes.Create(rawHand, activeRoute.Finger, activeRoute.Reversed, settings);
                bool poorTracking = rawHand == null || (rawHand.InvalidMask & HandRoutes.RequiredJointMask(activeRoute.Finger)) != 0;
                if (raw == null && Recovery == null) BeginRecovery(poorTracking);
                if (Recovery != null)
                {
                    var previous = Recovery.Phase;
                    var aligned = live != null && Recovery.Ghost.Difference(live) < settings.ConnectDistance ? raw : null;
                    Vec3 support = live == null ? Pip : (Build != null && !Carry ? Build.BuilderPosition(live) : live.Evaluate(cursor));
                    Recovery.Step(aligned, support); Pip = Recovery.Position;
                    if (Build != null) Build.Step(null, false, false);
                    if (previous != Recovery.Phase && Recovery.Phase == RecoveryPhase.Blending) emit(GameEventKind.RecoveryAligned);
                    if (previous != Recovery.Phase && Recovery.Phase == RecoveryPhase.Returning) { Build?.Remove(); emit(GameEventKind.ReturningToCheckpoint); }
                    if (Recovery.Phase == RecoveryPhase.Recovered) { Recovery = null; attached = true; stillness.Reset(); emit(GameEventKind.RecoveryCompleted); }
                    else if (Recovery.Phase == RecoveryPhase.Returned) { Pip = Source.Centre; transferring = false; ReleaseHand(); Recovery = null; }
                    return SessionResult.Continue;
                }
                if (live != null) { activeRoute = live; width.Step(live.MinimumWidth); }
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
            if (frame.Command == InputCommand.CancelBuild || frame.Command == InputCommand.StartElsewhere)
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

        private void Acquire(HandSample left, HandSample right)
        {
            var a = HandRoutes.Best(left, Source.Centre, Destination.Centre, settings); var b = HandRoutes.Best(right, Source.Centre, Destination.Centre, settings);
            double da = a == null ? double.PositiveInfinity : Source.ConnectionDistance(a.Start, settings);
            double db = b == null ? double.PositiveInfinity : Source.ConnectionDistance(b.Start, settings);
            leftContact.Step(da, a != null, settings); rightContact.Step(db, b != null, settings);
            if (!leftContact.Connected && !rightContact.Connected) return;
            bool chooseLeft = leftContact.Connected && (!rightContact.Connected || da <= db);
            Owner = chooseLeft ? HandSide.Left : HandSide.Right; activeRoute = chooseLeft ? a : b;
            width.Step(activeRoute.MinimumWidth);
            cursor = new RouteCursor(0, 0); attached = false; stillness.Reset(); destinationContact.Reset();
        }

        private SessionResult StepCarry()
        {
            double distance = Destination.ConnectionDistance(activeRoute.End, settings);
            destinationContact.Step(distance, true, settings);
            cursor = activeRoute.Advance(cursor, settings.TravelerSpeed * GameSettings.Delta);
            Pip = activeRoute.Evaluate(cursor);
            if (activeRoute.AtEnd(cursor) && destinationContact.Connected && distance < settings.ConnectDistance) transferring = true;
            return SessionResult.Continue;
        }

        private SessionResult StepBuild(double stillnessThreshold)
        {
            bool still = stillness.Step(activeRoute.Reversed ? activeRoute.End : activeRoute.Start, stillnessThreshold);
            var sourceContact = Owner == HandSide.Left ? leftContact : rightContact;
            sourceContact.Step(Source.ConnectionDistance(activeRoute.Start, settings), true, settings);
            destinationContact.Step(Destination.ConnectionDistance(activeRoute.End, settings), true, settings);
            bool connected = sourceContact.Connected && destinationContact.Connected;
            if (Build == null)
            {
                if (!connected || !still) return SessionResult.Continue;
                Build = new BridgeBuild(activeRoute, settings); Refusals = Array.Empty<ValidationFailure>(); emit(GameEventKind.BuildStarted);
            }
            var phase = Build.Phase; int sections = Build.FrozenSections;
            Build.Step(activeRoute, connected, still);
            if (Build.Phase == BuildPhase.Paused && phase != BuildPhase.Paused) emit(GameEventKind.BuildPaused);
            if (Build.FrozenSections > sections) emit(GameEventKind.SectionCommitted);
            if (Build.Phase == BuildPhase.Removing)
            {
                BeginRecovery(false); Recovery.ReturnToCheckpoint(); emit(GameEventKind.ScaffoldRemoving); emit(GameEventKind.ReturningToCheckpoint);
                return SessionResult.Continue;
            }
            if (Build.Phase == BuildPhase.Building || Build.Phase == BuildPhase.Complete)
            {
                Pip = Build.BuilderPosition(activeRoute);
                double parameter = Build.Progress * (activeRoute.Points.Count - 1); int segment = Math.Min(activeRoute.Points.Count - 2, (int)parameter);
                cursor = new RouteCursor(segment, parameter - segment);
            }
            if (Build.Phase != BuildPhase.Complete) return SessionResult.Continue;
            var tree = CurveTree.Single(Cap(Build.FrozenRoute(), WidthCap));
            Refusals = BridgeValidator.Validate(tree, Source, new[] { Destination }, Rule, settings, clearance);
            if (Refusals.Count > 0)
            {
                Build.Remove(); BeginRecovery(false); Recovery.ReturnToCheckpoint(); emit(GameEventKind.Refused); emit(GameEventKind.ReturningToCheckpoint);
                return SessionResult.Continue;
            }
            Result = tree; Build = null; emit(GameEventKind.BridgeBaked); ReleaseHand();
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

        private void BeginRecovery(bool trackingLost)
        {
            if (activeRoute == null) return;
            Recovery = new Recovery(activeRoute, Pip, Source.Centre, settings); leftFilter.Reset(); rightFilter.Reset(); stillness.Reset();
            if (trackingLost) emit(GameEventKind.TrackingLost);
        }
        private void ReleaseHand()
        {
            Owner = null; attached = false; activeRoute = null; leftContact.Reset(); rightContact.Reset(); destinationContact.Reset(); stillness.Reset();
            emit(GameEventKind.HandReleased);
        }
        internal static Vec3 MoveTowards(Vec3 from, Vec3 to, double distance) => Vec3.Distance(from, to) <= distance ? to : from + (to - from).Normalized * distance;
    }
}
