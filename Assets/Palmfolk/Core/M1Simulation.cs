using System;
using System.Collections.Generic;

namespace Palmfolk.Core
{
    public enum M1Phase { Board, Build, Cart, Complete }
    public enum GameEventKind
    {
        Boarded, CheckpointReached, HandReleased, BuildStarted, BuildPaused, SectionCommitted,
        ScaffoldRemoving, BridgeBaked, Refused, BramStarted, BramFinished, TrackingLost,
        RecoveryAligned, RecoveryCompleted, ReturningToCheckpoint, HintDue, QualityPromptDue, PersistentBridgeObserved, StartupPromptDue,
        Planted, ReplantStarted,
        PuzzleStarted, PuzzleCompleted, LedgeWidening, LedgeWidened, RebuildNeeded, TravelersStarted, TravelerArrived, CrossingCompleted
    }
    public readonly struct GameEvent
    {
        public readonly long Tick;
        public readonly GameEventKind Kind;
        public GameEvent(long tick, GameEventKind kind) { Tick = tick; Kind = kind; }
    }
    public sealed class M1Snapshot
    {
        public long Tick = -1;
        public M1Phase Phase;
        public Vec3 Pip, Bram;
        public int PipCheckpoint, BramCheckpoint;
        public HandSide? Owner;
        public Route ActiveRoute;
        public RouteCursor Cursor, CartCursor;
        public BuildSnapshot Build;
        public CurveTree Bridge;
        public Vec3? TransferTarget;
        public int TransferCheckpoint;
        public bool BridgeWithdrawn;
        public RecoverySnapshot Recovery;
        public bool CartStarted;
    }
    public sealed class M1Simulation : ITickSimulation
    {
        private readonly GameSettings settings;
        private readonly HandFilter leftFilter = new HandFilter(), rightFilter = new HandFilter();
        private readonly ConnectionWatcher leftContact = new ConnectionWatcher(), rightContact = new ConnectionWatcher(), destinationContact = new ConnectionWatcher();
        private readonly StillnessWatcher stillness;
        private readonly HintWatcher hints = new HintWatcher();
        private readonly WidthWatcher width = new WidthWatcher();
        private readonly List<GameEvent> events = new List<GameEvent>();
        private bool paused, attached, cartStarted, cartRefused, bridgeWithdrawn;
        private Route activeRoute;
        private RouteCursor cursor, cartCursor;
        private Vec3? transferTarget;
        private int transferCheckpoint;
        private int startupWaitTicks;
        private bool hasFocus = true;
        public long Tick { get; private set; } = -1;
        public M1Phase Phase { get; private set; }
        public LevelGeometry Level { get; }
        public HandSide? Owner { get; private set; }
        public Vec3 Pip { get; private set; }
        public Vec3 Bram { get; private set; }
        public Vec3 PreviousPip { get; private set; }
        public Vec3 PreviousBram { get; private set; }
        public int PipCheckpoint { get; private set; }
        public int BramCheckpoint { get; private set; }
        public BridgeBuild Build { get; private set; }
        public CurveTree Bridge { get; private set; }
        public Recovery Recovery { get; private set; }
        public Route ActiveRoute => activeRoute;
        public Route PreviousRoute { get; private set; }
        public WidthTier RouteTier => width.Tier;
        public GameSettings Settings => settings.Copy();
        public void SetStillnessTolerance(int step)
        {
            if (step < 0 || step > 2) throw new ArgumentOutOfRangeException(nameof(step));
            settings.StillnessToleranceStep = step;
        }
        public IReadOnlyList<GameEvent> Events => events.AsReadOnly();
        public IReadOnlyList<ValidationFailure> Refusals { get; private set; } = Array.Empty<ValidationFailure>();
        public bool IsPaused => paused || !hasFocus;
        public bool ControllersInHand { get; private set; }
        public bool WaitingForHands { get; private set; }
        public bool ShowStartupText => WaitingForHands && startupWaitTicks >= 600;
        public bool CartCrossing => cartStarted && Phase == M1Phase.Cart;
        public M1Simulation(LevelGeometry level, GameSettings settings = null)
        {
            Level = level ?? throw new ArgumentNullException(nameof(level));
            this.settings = (settings ?? new GameSettings()).Copy(); this.settings.Validate();
            stillness = new StillnessWatcher(this.settings.StillnessTicks);
            Pip = PreviousPip = Bram = PreviousBram = level.Start;
        }
        private void Emit(GameEventKind kind) => events.Add(new GameEvent(Tick, kind));
        private Vec3 Checkpoint => PipCheckpoint == 0 ? Level.Start : Level.End;
        public void Step(HandFrame frame)
        {
            if (frame == null || frame.Tick != Tick + 1) throw new ArgumentException("Feed exactly one sequential frame per 60 Hz tick.");
            Tick = frame.Tick; events.Clear(); PreviousPip = Pip; PreviousBram = Bram; PreviousRoute = activeRoute;
            hasFocus = frame.HasFocus; ControllersInHand = frame.ControllersInHand;
            if (frame.Command == InputCommand.Pause) paused = true;
            if (frame.Command == InputCommand.Resume) paused = false;
            if (!frame.HasFocus || paused)
            {
                if (Owner.HasValue && Recovery == null) BeginRecovery(false);
                leftFilter.Reset(); rightFilter.Reset(); stillness.Reset(); return;
            }
            bool usable = (frame.Left != null && frame.Left[JointId.Wrist].Usable && frame.Left[JointId.Palm].Usable) ||
                (frame.Right != null && frame.Right[JointId.Wrist].Usable && frame.Right[JointId.Palm].Usable);
            WaitingForHands = Phase == M1Phase.Board && !Owner.HasValue && (frame.ControllersInHand || !usable);
            startupWaitTicks = WaitingForHands ? startupWaitTicks + 1 : 0;
            if (startupWaitTicks == 600) Emit(GameEventKind.StartupPromptDue);
            if (Phase == M1Phase.Board && !Owner.HasValue && frame.ControllersInHand)
            { leftContact.Reset(); rightContact.Reset(); return; }
            var left = leftFilter.Step(frame.Left, settings); var right = rightFilter.Step(frame.Right, settings);
            var hand = Owner == HandSide.Left ? left : right;
            Route live = Owner.HasValue && activeRoute != null ? HandRoutes.Create(hand, activeRoute.Finger, activeRoute.Reversed, settings) : null;
            if (Owner.HasValue)
            {
                // Test raw data too: smoothing must not make a newly appearing hand
                // look aligned for a tick when it actually returned 20 cm away.
                var raw = HandRoutes.Create(frame.Hand(Owner.Value), activeRoute.Finger, activeRoute.Reversed, settings);
                var rawHand = frame.Hand(Owner.Value);
                bool poorTracking = rawHand == null || (rawHand.InvalidMask & HandRoutes.RequiredJointMask(activeRoute.Finger)) != 0;
                if (raw == null && Recovery == null) BeginRecovery(poorTracking);
                if (Recovery != null)
                {
                    var previous = Recovery.Phase;
                    var aligned = live != null && Recovery.Ghost.Difference(live) < settings.ConnectDistance ? raw : null;
                    Vec3 support = live == null ? Pip : (Phase == M1Phase.Build && Build != null ? Build.BuilderPosition(live) : live.Evaluate(cursor));
                    Recovery.Step(aligned, support); Pip = Recovery.Position;
                    if (Build != null) Build.Step(null, false, false);
                    if (previous != Recovery.Phase && Recovery.Phase == RecoveryPhase.Blending) Emit(GameEventKind.RecoveryAligned);
                    if (previous != Recovery.Phase && Recovery.Phase == RecoveryPhase.Returning)
                    { Build?.Remove(); Emit(GameEventKind.ReturningToCheckpoint); }
                    if (Recovery.Phase == RecoveryPhase.Recovered)
                    { Recovery = null; attached = true; stillness.Reset(); Emit(GameEventKind.RecoveryCompleted); }
                    else if (Recovery.Phase == RecoveryPhase.Returned)
                    {
                        Pip = Checkpoint; transferTarget = null; ReleaseHand(); Recovery = null;
                    }
                    hints.Step(false, poorTracking); EmitHints(); return;
                }
                if (live != null) { activeRoute = live; width.Step(live.MinimumWidth); }
            }
            if (transferTarget.HasValue)
            {
                Pip = MoveTowards(Pip, transferTarget.Value, settings.TravelerSpeed * GameSettings.Delta);
                if (Vec3.Distance(Pip, transferTarget.Value) < 1e-9)
                {
                    PipCheckpoint = transferCheckpoint; transferTarget = null; Emit(GameEventKind.CheckpointReached);
                    if (Phase == M1Phase.Board) { Phase = M1Phase.Build; ReleaseHand(); }
                }
                return;
            }
            if (Build != null && (Build.Phase == BuildPhase.Removing || Build.Phase == BuildPhase.Removed))
            {
                Build.Step(null, false, false);
                if (Build.Phase == BuildPhase.Removed && !Owner.HasValue) Build = null;
            }
            if ((Phase == M1Phase.Board || Phase == M1Phase.Build) && !Owner.HasValue)
                Acquire(left, right);
            if (Owner.HasValue && activeRoute != null)
            {
                if (frame.Command == InputCommand.CancelBuild || frame.Command == InputCommand.StartElsewhere)
                {
                    Build?.Remove(); BeginRecovery(false); Recovery.ReturnToCheckpoint(); Emit(GameEventKind.ScaffoldRemoving); Emit(GameEventKind.ReturningToCheckpoint); return;
                }
                if (!attached)
                {
                    Pip = MoveTowards(Pip, activeRoute.Start, settings.TravelerSpeed * GameSettings.Delta);
                    if (Vec3.Distance(Pip, activeRoute.Start) < 1e-9) { attached = true; Emit(GameEventKind.Boarded); }
                }
                else if (Phase == M1Phase.Board) StepBoard();
                else if (Phase == M1Phase.Build) StepBuild();
            }
            else if (Phase == M1Phase.Cart) StepCart();
            if (Bridge != null && !bridgeWithdrawn)
            {
                // A persistent bridge is observed separately from the bake event (E20).
                if (!HandNearBridge(left) && !HandNearBridge(right))
                { bridgeWithdrawn = true; Emit(GameEventKind.PersistentBridgeObserved); }
            }
            hints.Step(Phase == M1Phase.Complete || events.Count > 0, (left == null || left.InvalidMask != 0) && (right == null || right.InvalidMask != 0));
            if (Phase != M1Phase.Complete) EmitHints();
        }
        private bool HandNearBridge(HandSample hand)
        {
            if (hand == null) return false;
            foreach (var joint in hand.Joints)
                if (joint.Usable) foreach (var curve in Bridge.Curves) for (int i = 0; i < curve.Widths.Count; i++)
                    if (BridgeValidator.PointDistance(joint.Position, curve.Points[i], curve.Points[i + 1]) < settings.BreakDistance) return true;
            return false;
        }
        private void Acquire(HandSample left, HandSample right)
        {
            Vec3 start = Phase == M1Phase.Board ? Level.Start : Level.End, end = Phase == M1Phase.Board ? Level.End : Level.Start;
            var a = HandRoutes.Best(left, start, end, settings); var b = HandRoutes.Best(right, start, end, settings);
            leftContact.Step(a == null ? double.PositiveInfinity : Vec3.Distance(a.Start, start), a != null, settings);
            rightContact.Step(b == null ? double.PositiveInfinity : Vec3.Distance(b.Start, start), b != null, settings);
            if (!leftContact.Connected && !rightContact.Connected) return;
            bool chooseLeft = leftContact.Connected && (!rightContact.Connected || Vec3.Distance(a.Start, start) <= Vec3.Distance(b.Start, start));
            Owner = chooseLeft ? HandSide.Left : HandSide.Right; activeRoute = chooseLeft ? a : b;
            width.Step(activeRoute.MinimumWidth);
            cursor = new RouteCursor(0, 0); attached = false; stillness.Reset(); destinationContact.Reset();
        }
        private void StepBoard()
        {
            destinationContact.Step(Vec3.Distance(activeRoute.End, Level.End), true, settings);
            cursor = activeRoute.Advance(cursor, settings.TravelerSpeed * GameSettings.Delta);
            Pip = activeRoute.Evaluate(cursor);
            if (activeRoute.AtEnd(cursor) && destinationContact.Connected && Vec3.Distance(activeRoute.End, Level.End) < settings.ConnectDistance)
            { transferTarget = Level.End; transferCheckpoint = 1; }
        }
        private void StepBuild()
        {
            bool still = stillness.Step(activeRoute.Reversed ? activeRoute.End : activeRoute.Start, settings.StillnessThreshold(hints.StillnessAdjustment));
            var sourceContact = Owner == HandSide.Left ? leftContact : rightContact;
            sourceContact.Step(Vec3.Distance(activeRoute.Start, Level.End), true, settings);
            bool a = sourceContact.Connected;
            destinationContact.Step(Vec3.Distance(activeRoute.End, Level.Start), true, settings);
            if (Build == null)
            {
                if (!a || !destinationContact.Connected || !still) return;
                Build = new BridgeBuild(activeRoute, settings); Refusals = Array.Empty<ValidationFailure>(); Emit(GameEventKind.BuildStarted);
            }
            var phase = Build.Phase; int sections = Build.FrozenSections;
            Build.Step(activeRoute, a && destinationContact.Connected, still);
            if (Build.Phase == BuildPhase.Paused && phase != BuildPhase.Paused) Emit(GameEventKind.BuildPaused);
            if (Build.FrozenSections > sections) Emit(GameEventKind.SectionCommitted);
            if (Build.Phase == BuildPhase.Removing)
            { BeginRecovery(false); Recovery.ReturnToCheckpoint(); Emit(GameEventKind.ScaffoldRemoving); Emit(GameEventKind.ReturningToCheckpoint); return; }
            if (Build.Phase == BuildPhase.Building || Build.Phase == BuildPhase.Complete)
            {
                Pip = Build.BuilderPosition(activeRoute);
                double parameter = Build.Progress * (activeRoute.Points.Count - 1); int segment = Math.Min(activeRoute.Points.Count - 2, (int)parameter);
                cursor = new RouteCursor(segment, parameter - segment);
            }
            if (Build.Phase != BuildPhase.Complete) return;
            var tree = CurveTree.Single(Build.FrozenRoute());
            Refusals = BridgeValidator.Validate(tree, Level.End, Level.Start, TravelerKind.Walker, settings, Level);
            if (Refusals.Count > 0) { Build.Remove(); BeginRecovery(false); Recovery.ReturnToCheckpoint(); Emit(GameEventKind.Refused); Emit(GameEventKind.ReturningToCheckpoint); return; }
            Bridge = tree; bridgeWithdrawn = false; Build = null; cartRefused = cartStarted = false;
            Phase = M1Phase.Cart; transferTarget = Level.Start; transferCheckpoint = 0; cursor = new RouteCursor(0, 0);
            Emit(GameEventKind.BridgeBaked); ReleaseHand();
        }
        private void StepCart()
        {
            if (Bridge == null) { Phase = M1Phase.Build; return; }
            if (!cartStarted)
            {
                Refusals = BridgeValidator.Validate(Bridge, Level.End, Level.Start, TravelerKind.Cart, settings, Level);
                if (Refusals.Count > 0)
                {
                    if (!cartRefused) { Emit(GameEventKind.Refused); cartRefused = true; }
                    // Keep the rope visible; Pip waits on A for a wider replacement.
                    var returnRoute = Bridge.Curves[0].Reverse();
                    if (cursor.Segment == 0 && cursor.Fraction == 0 && Vec3.Distance(Pip, returnRoute.Start) > settings.TravelerSpeed * GameSettings.Delta)
                    { Pip = MoveTowards(Pip, returnRoute.Start, settings.TravelerSpeed * GameSettings.Delta); return; }
                    if (!returnRoute.AtEnd(cursor)) { cursor = returnRoute.Advance(cursor, settings.TravelerSpeed * GameSettings.Delta); Pip = returnRoute.Evaluate(cursor); }
                    else Pip = MoveTowards(Pip, Level.End, settings.TravelerSpeed * GameSettings.Delta);
                    if (Vec3.Distance(Pip, Level.End) < 1e-9) { PipCheckpoint = 1; Phase = M1Phase.Build; }
                    return;
                }
                cartStarted = true; cartCursor = new RouteCursor(0, 0); Emit(GameEventKind.BramStarted);
            }
            var route = Bridge.Curves[0].Reverse();
            if (route.AtEnd(cartCursor))
            {
                Bram = MoveTowards(Bram, Level.End, settings.TravelerSpeed * GameSettings.Delta);
                if (Vec3.Distance(Bram, Level.End) < 1e-9) { BramCheckpoint = 1; Phase = M1Phase.Complete; Emit(GameEventKind.BramFinished); }
                return;
            }
            var target = route.Evaluate(cartCursor);
            if (Vec3.Distance(Bram, target) > settings.TravelerSpeed * GameSettings.Delta)
            { Bram = MoveTowards(Bram, target, settings.TravelerSpeed * GameSettings.Delta); return; }
            cartCursor = route.Advance(cartCursor, settings.TravelerSpeed * GameSettings.Delta); Bram = route.Evaluate(cartCursor);
        }
        private void BeginRecovery(bool trackingLost = true)
        {
            if (activeRoute == null) return;
            Recovery = new Recovery(activeRoute, Pip, Checkpoint, settings); leftFilter.Reset(); rightFilter.Reset(); stillness.Reset(); if (trackingLost) Emit(GameEventKind.TrackingLost);
        }
        private void ReleaseHand()
        {
            Owner = null; attached = false; activeRoute = null; leftContact.Reset(); rightContact.Reset(); destinationContact.Reset(); stillness.Reset(); Emit(GameEventKind.HandReleased);
        }
        private void EmitHints() { if (hints.HintDue) Emit(GameEventKind.HintDue); if (hints.QualityPromptDue) Emit(GameEventKind.QualityPromptDue); }
        private static Vec3 MoveTowards(Vec3 from, Vec3 to, double distance) => Vec3.Distance(from, to) <= distance ? to : from + (to - from).Normalized * distance;
        public M1Snapshot Capture() => new M1Snapshot { Tick = Tick, Phase = Phase, Pip = Pip, Bram = Bram, PipCheckpoint = PipCheckpoint, BramCheckpoint = BramCheckpoint,
            Owner = Owner, ActiveRoute = activeRoute, Cursor = cursor, CartCursor = cartCursor, Build = Build?.Capture(), Bridge = Bridge,
            TransferTarget = transferTarget, TransferCheckpoint = transferCheckpoint, BridgeWithdrawn = bridgeWithdrawn, Recovery = Recovery?.Capture(), CartStarted = cartStarted };
        public static M1Simulation Restore(M1Snapshot state, LevelGeometry level, GameSettings settings)
        {
            if (state == null || state.Tick < -1 || !Enum.IsDefined(typeof(M1Phase), state.Phase) || !state.Pip.IsFinite || !state.Bram.IsFinite ||
                state.PipCheckpoint < 0 || state.PipCheckpoint > 1 || state.BramCheckpoint < 0 || state.BramCheckpoint > 1 ||
                (state.Owner.HasValue && (state.ActiveRoute == null || !Enum.IsDefined(typeof(HandSide), state.Owner.Value))) ||
                (state.TransferTarget.HasValue && (!state.TransferTarget.Value.IsFinite || state.TransferCheckpoint < 0 || state.TransferCheckpoint > 1))) throw new ArgumentException("Invalid M1 save state.");
            var game = new M1Simulation(level, settings) { Tick = state.Tick, Phase = state.Phase, Pip = state.Pip, Bram = state.Bram, PreviousPip = state.Pip, PreviousBram = state.Bram,
                PipCheckpoint = state.PipCheckpoint, BramCheckpoint = state.BramCheckpoint, Owner = state.Owner, activeRoute = state.ActiveRoute, cursor = state.Cursor, cartCursor = state.CartCursor,
                Bridge = state.Bridge, transferTarget = state.TransferTarget, transferCheckpoint = state.TransferCheckpoint, bridgeWithdrawn = state.BridgeWithdrawn, PreviousRoute = state.ActiveRoute, cartStarted = state.CartStarted };
            game.Build = state.Build == null ? null : BridgeBuild.Restore(state.Build, settings);
            if (state.Bridge != null && state.CartCursor.Segment >= state.Bridge.Curves[0].Widths.Count) throw new ArgumentException("Invalid saved cart cursor.");
            if (game.Owner.HasValue)
            {
                if (state.Cursor.Segment >= state.ActiveRoute.Widths.Count) throw new ArgumentException("Invalid saved hand cursor.");
                if (state.Recovery != null) game.Recovery = Core.Recovery.Restore(state.Recovery, game.Checkpoint, settings);
                else game.BeginRecovery(false);
            }
            if ((game.Phase == M1Phase.Cart || game.Phase == M1Phase.Complete) && (game.Bridge == null ||
                BridgeValidator.Validate(game.Bridge, level.End, level.Start, game.Phase == M1Phase.Complete ? TravelerKind.Cart : TravelerKind.Walker, settings, level).Count > 0))
            {
                game.Phase = M1Phase.Build; game.Bridge = null; game.Pip = game.PreviousPip = level.End; game.PipCheckpoint = 1;
                game.Bram = game.PreviousBram = level.Start; game.BramCheckpoint = 0; game.transferTarget = null;
                game.Owner = null; game.activeRoute = null; game.Recovery = null; game.Build = null; game.cartStarted = game.cartRefused = false;
            }
            return game;
        }
    }
}
