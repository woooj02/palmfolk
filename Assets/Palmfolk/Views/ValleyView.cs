#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using Palmfolk.Core;
using UnityEngine;

namespace Palmfolk.Views
{
    public interface IValleyView { void Present(ValleyPresentation frame); }
    public sealed class ValleyView : MonoBehaviour, IValleyView
    {
        private Transform valley, islands, pip, bram, cart, ledge, capacity;
        private TravelerRig pipRig, bramRig;
        private PalmfolkView.ShapePool live, preview, scaffold, ghost, refusals, refusedScaffold, capacityGlyph, connectionDust;
        private long? connectionTick;
        private readonly List<Vec3> lockedAnchors = new List<Vec3>();
        public bool ShowingConnectionDust { get; private set; }
        private readonly Dictionary<string, PalmfolkView.ShapePool> bridges = new Dictionary<string, PalmfolkView.ShapePool>();
        private readonly List<Transform> travelers = new List<Transform>();
        private readonly List<Transform> travelerCarts = new List<Transform>();
        private readonly List<Material> materials = new List<Material>();
        private Material wood, cord, skin, transparent, carved, warning;
        private ValleySpec spec;
        private Route previousRoute, currentRoute;
        private long routeTick = -2;
        private long? planted;
        private CurveTree lastRefused, previousTree, currentTree;
        private long refusedSince;
        private readonly HashSet<Route> drawn = new HashSet<Route>(RouteGeometryComparer.Instance);
        private GameSettings rules = new GameSettings();
        public void Configure(GameSettings actual) { rules = (actual ?? new GameSettings()).Copy(); }
        private Material islandMaterial, handMaterial, backdropMaterial;
        private Transform backdrop;
        private bool? contrastApplied;
        public bool ReducedMotion { get; set; }
        public bool HighContrast { get; set; }
        public bool TableMode { get; set; }
        public Transform ValleyRoot => valley;
        public int RenderedBridgeCount => bridges.Count;
        public int RenderedUniqueCurveCount { get; private set; }
        public TravelerKind? PreviewCapacity { get; private set; }
        // The one-line instruction floating over the valley (Quest 2 finding, Nov 13: without it nobody knew what to do).
        public string Prompt { get; private set; } = "";
        private PalmfolkView.ShapePool rings;
        private Material ringIdle, ringReady, pipMaterial, gustMaterial;
        private TextMesh prompt;
        private string transient; private long transientUntil = -1, refusedAt = -1;
        // The bake moment: the bridge that just solidified, when, and how many gusts the player has shielded.
        private readonly HashSet<string> seenBridges = new HashSet<string>();
        private string bakedKey; private long bakedAt = -1; private int shieldedCount; private bool bakePrimed;
        private const int BakeTicks = 70;
        public void Consume(IReadOnlyList<GameEvent> events)
        {
            if (pipRig != null) { pipRig.Consume(events); bramRig.Consume(events); }
            foreach (var e in events)
            {
                if (e.Kind == GameEventKind.Planted) planted = e.Tick;
                if (e.Kind == GameEventKind.ConnectionLocked) connectionTick = e.Tick;
                if (e.Kind == GameEventKind.BridgeBaked) Say("Bridge built! Lift your hand away; it stays", e.Tick, 180);
                if (e.Kind == GameEventKind.TrackingLost) Say("Keep your hand where the headset can see it", e.Tick, 150);
                if (e.Kind == GameEventKind.PuzzleCompleted) Say("Well done!", e.Tick, 120);
                if (e.Kind == GameEventKind.Refused) refusedAt = e.Tick;
                if (e.Kind == GameEventKind.GustBlocked) { shieldedCount++; Say(shieldedCount == 1 ? "Shielded! Your hand held back the wind" : "Shielded! " + shieldedCount + " gusts held back", e.Tick, 150); }
                if (e.Kind == GameEventKind.GustHit) Say("Whoosh! They're bracing.\nStand your hand up like a wall to shield them", e.Tick, 180);
                if (e.Kind == GameEventKind.ReplantStarted) { seenBridges.Clear(); bakedKey = null; bakePrimed = false; }
                if (e.Kind == GameEventKind.ReplantStarted || e.Kind == GameEventKind.PuzzleStarted || e.Kind == GameEventKind.VisitorCalled) { planted = e.Kind == GameEventKind.ReplantStarted ? null : planted; connectionTick = null; }
            }
        }
        public void Present(ValleyPresentation frame) { Present(frame, 1); }
        public void Present(ValleyPresentation frame, float handInterpolation)
        {
            if (frame == null) return;
            Ensure(frame.Valley); ApplyContrast(); valley.gameObject.SetActive(frame.Pose != null); if (frame.Pose == null) return;
            var pose = frame.Pose;
            valley.SetPositionAndRotation(PalmfolkView.V(pose.Origin), new Quaternion((float)pose.Rotation.X, (float)pose.Rotation.Y, (float)pose.Rotation.Z, (float)pose.Rotation.W));
            float growth = planted.HasValue && !ReducedMotion ? Mathf.Max(0.001f, Mathf.SmoothStep(0, 1, (frame.Tick - planted.Value) / 30f)) : 1;
            valley.localScale = Vector3.one * (float)pose.Scale * growth;
            DrawConnection(frame); DrawGuide(frame);
            pip.localPosition = PalmfolkView.V(frame.Pip); bram.localPosition = PalmfolkView.V(frame.Bram);
            pipRig.ReducedMotion = bramRig.ReducedMotion = ReducedMotion; pipRig.Present(frame); bramRig.Present(frame);
            cart.localPosition = bram.localPosition + bram.localRotation * Vector3.back * 0.016f; cart.localRotation = bram.localRotation;
            int usedTraveler = 0;
            foreach (var traveler in frame.Travelers)
                for (int member = 0; member < (traveler.Kind == TravelerKind.Group ? 3 : 1); member++)
                {
                    if (usedTraveler == travelers.Count) { travelers.Add(Actor("Traveler " + usedTraveler)); travelerCarts.Add(Cart("Traveler cart " + usedTraveler)); }
                    int index = usedTraveler++; var actor = travelers[index]; actor.gameObject.SetActive(true);
                    var rig = actor.GetComponent<TravelerRig>(); rig.ReducedMotion = ReducedMotion;
                    rig.PresentTraveler(traveler, frame.Tick, frame.Pose, traveler.Kind == TravelerKind.Group ? (member - 1) * 0.006 : 0);
                    travelerCarts[index].gameObject.SetActive(traveler.Kind == TravelerKind.Cart);
                    travelerCarts[index].localPosition = actor.localPosition + actor.localRotation * Vector3.back * 0.018f; travelerCarts[index].localRotation = actor.localRotation;
                }
            for (int i = usedTraveler; i < travelers.Count; i++) { travelers[i].gameObject.SetActive(false); travelerCarts[i].gameObject.SetActive(false); }
            if (spec.Ledge != null) ledge.localScale = new Vector3((float)frame.LedgeWidth, 0.002f, 0.01f);
            live.Begin(); preview.Begin(); scaffold.Begin(); ghost.Begin(); refusals.Begin(); refusedScaffold.Begin(); capacityGlyph.Begin(); drawn.Clear(); PreviewCapacity = null;
            foreach (var pool in bridges.Values) pool.Begin();
            if (frame.Tick != routeTick) { previousRoute = currentRoute; currentRoute = frame.FilteredHandRoute; previousTree = currentTree; currentTree = frame.HandTree; routeTick = frame.Tick; }
            var displayed = Blend(previousRoute, currentRoute, handInterpolation);
            var displayedTree = BlendTree(previousTree, currentTree, handInterpolation);
            if (displayedTree != null) DrawTree(live, displayedTree, false, handMaterial); else Draw(live, displayed, false, handMaterial);
            if (frame.ScaffoldTree != null) DrawTree(scaffold, frame.ScaffoldTree, true, wood, (float)(1 - frame.ScaffoldRemovalProgress));
            else Draw(scaffold, frame.Scaffold, true, wood, (float)(1 - frame.ScaffoldRemovalProgress));
            Draw(ghost, frame.GhostRoute, false, carved);
            bool inZones = displayed != null && frame.Source.HasValue && frame.Destination.HasValue && frame.Source.Value.Contains(displayed.Start) && frame.Destination.Value.Contains(displayed.End);
            capacity.gameObject.SetActive(inZones);
            if (inZones)
            {
                var effective = displayed;
                if (frame.ActiveEdge == spec.Ledge?.Edge)
                {
                    var widths = displayed.CopyWidths(); for (int i = 0; i < widths.Length; i++) widths[i] = Math.Min(widths[i], frame.LedgeWidth);
                    effective = new Route(displayed.CopyPoints(), widths, displayed.Finger, displayed.Reversed);
                }
                var tree = displayedTree ?? CurveTree.Single(effective);
                if (frame.ActiveEdge == spec.Ledge?.Edge) tree = EdgeSession.Cap(tree, frame.LedgeWidth);
                DrawTree(preview, tree, true, transparent);
                var destinations = new List<AnchorZone>();
                foreach (string id in frame.ActiveEdges)
                {
                    var edge = spec.Edge(id);
                    bool from = Vec3.Distance(edge.FromAnchor.Centre, frame.Source.Value.Centre) <= Vec3.Distance(edge.ToAnchor.Centre, frame.Source.Value.Centre);
                    destinations.Add(from ? edge.ToAnchor : edge.FromAnchor);
                }
                if (destinations.Count == 0) destinations.Add(frame.Destination.Value);
                bool Fits(TravelerKind kind) => BridgeValidator.Validate(tree, frame.Source.Value, destinations, kind, rules, spec.Clearance).Count == 0;
                PreviewCapacity = Fits(TravelerKind.Cart) ? TravelerKind.Cart : Fits(TravelerKind.Group) ? TravelerKind.Group : Fits(TravelerKind.Walker) ? TravelerKind.Walker : (TravelerKind?)null;
                capacity.localPosition = PalmfolkView.V(effective.Points[effective.Points.Count / 2]) + Vector3.up * 0.025f;
                PalmfolkView.DrawCapacity(capacityGlyph, PreviewCapacity, wood, warning);
            }
            foreach (var saved in frame.Bridges)
            {
                if (!bridges.TryGetValue(saved.Key, out var pool)) { pool = new PalmfolkView.ShapePool(valley, "Bridge " + saved.Key); bridges.Add(saved.Key, pool); }
                for (int c = 0; c < saved.Value.Curves.Count; c++)
                {
                    var curve = saved.Value.Curves[c]; if (!drawn.Add(curve)) continue;
                    bool leaf = true; for (int p = c + 1; p < saved.Value.Parents.Count; p++) if (saved.Value.Parents[p] == c) leaf = false;
                    Draw(pool, curve, true, null, 1, leaf, saved.Value.Curves.Count > 1 ? curve.Points.Count - 1 : (int?)null);
                }
            }
            RenderedUniqueCurveCount = drawn.Count;
            if (!ReferenceEquals(lastRefused, frame.RefusedScaffold)) { lastRefused = frame.RefusedScaffold; refusedSince = frame.Tick; }
            if (frame.RefusedScaffold != null) DrawTree(refusedScaffold, frame.RefusedScaffold, true, carved, 1 - Mathf.Clamp01((frame.Tick - refusedSince) / (float)rules.RemovalTicks));
            foreach (string failing in frame.FailingEdges)
            {
                var edge = spec.Edge(failing); Cross(PalmfolkView.V(Vec3.Lerp(edge.FromAnchor.Centre, edge.ToAnchor.Centre, 0.5)));
            }
            foreach (var failure in frame.Refusals)
            {
                var failedTree = frame.RefusedScaffold ?? frame.HandTree;
                var failedRoute = failedTree != null && failure.Curve >= 0 && failure.Curve < failedTree.Curves.Count ? failedTree.Curves[failure.Curve] : displayed;
                if (failedRoute != null && failure.Segment >= 0 && failure.Segment + 1 < failedRoute.Points.Count)
                    Cross(PalmfolkView.V(Vec3.Lerp(failedRoute.Points[failure.Segment], failedRoute.Points[failure.Segment + 1], 0.5)));
            }
            live.End(); preview.End(); scaffold.End(); ghost.End(); refusals.End(); refusedScaffold.End(); capacityGlyph.End(); foreach (var pool in bridges.Values) pool.End();
            void Cross(Vector3 center)
            {
                float width = ReducedMotion ? 0.014f : 0.014f + Mathf.Sin(frame.Tick / 5f) * 0.002f;
                center += Vector3.up * 0.015f;
                refusals.Box(center, new Vector3(width, 0.003f, 0.003f), Quaternion.Euler(0, 0, 45), warning);
                refusals.Box(center, new Vector3(width, 0.003f, 0.003f), Quaternion.Euler(0, 0, -45), warning);
            }
        }
        private void Ensure(ValleySpec valleySpec)
        {
            if (valley != null) { if (!ReferenceEquals(spec, valleySpec)) throw new InvalidOperationException("Use a new view for a different valley spec."); return; }
            spec = valleySpec; valley = new GameObject("Full valley presentation").transform; valley.SetParent(transform, false);
            wood = Material(new Color(0.48f, 0.29f, 0.13f)); cord = Material(new Color(0.22f, 0.17f, 0.10f)); skin = Material(new Color(0.65f, 0.51f, 0.31f));
            transparent = Material(new Color(0.7f, 0.8f, 0.9f, 0.25f)); carved = Material(new Color(0.8f, 0.8f, 0.75f, 0.45f)); warning = Material(new Color(1, 0.65f, 0.15f));
            islandMaterial = Material(new Color(0.48f, 0.29f, 0.13f)); handMaterial = Material(new Color(0.65f, 0.51f, 0.31f)); backdropMaterial = Material(new Color(0.02f, 0.02f, 0.02f));
            backdrop = PalmfolkView.Primitive(PrimitiveType.Cube, "High contrast backdrop", backdropMaterial, valley);
            backdrop.localPosition = new Vector3(0, 0.001f, -0.015f); backdrop.localScale = new Vector3(0.60f, 0.001f, 0.45f); backdrop.gameObject.SetActive(false);
            islands = new GameObject("Islands and obstacles").transform; islands.SetParent(valley, false);
            foreach (var island in spec.Islands)
            {
                var top = PalmfolkView.Primitive(PrimitiveType.Cylinder, "Island " + island.Id, islandMaterial, islands);
                top.localPosition = PalmfolkView.V(island.Top) + Vector3.down * 0.012f; top.localScale = new Vector3((float)island.Radius * 2, 0.012f, (float)island.Radius * 2);
            }
            foreach (var obstacle in spec.Clearance.Obstacles)
            {
                var rock = PalmfolkView.Primitive(PrimitiveType.Cube, "Configured rock", cord, islands);
                rock.localPosition = PalmfolkView.V((obstacle.Min + obstacle.Max) / 2); rock.localScale = PalmfolkView.V(obstacle.Max - obstacle.Min);
            }
            pipMaterial = Material(new Color(1f, 0.72f, 0.08f));
            pip = Actor("Pip", pipMaterial); pip.localScale = Vector3.one * 1.8f; bram = Actor("Bram"); pipRig = pip.GetComponent<TravelerRig>(); bramRig = bram.GetComponent<TravelerRig>(); bramRig.Role = TravelerRole.Bram;
            cart = Cart("Lantern cart");
            capacity = new GameObject("Capacity icon").transform; capacity.SetParent(valley, false); capacityGlyph = new PalmfolkView.ShapePool(capacity, "Capacity glyph");
            ledge = PalmfolkView.Primitive(PrimitiveType.Cube, "Widening ledge", wood, valley);
            ledge.gameObject.SetActive(spec.Ledge != null);
            if (spec.Ledge != null)
            {
                var edge = spec.Edge(spec.Ledge.Edge); ledge.localPosition = PalmfolkView.V(edge.AnchorAt(spec.Ledge.Island).Centre) - Vector3.up * 0.001f;
                ledge.localRotation = Quaternion.LookRotation(PalmfolkView.V(edge.ToAnchor.Centre - edge.FromAnchor.Centre), Vector3.up);
            }
            live = new PalmfolkView.ShapePool(valley, "Displayed route"); preview = new PalmfolkView.ShapePool(valley, "Preview");
            scaffold = new PalmfolkView.ShapePool(valley, "Frozen scaffold"); ghost = new PalmfolkView.ShapePool(valley, "Recovery ghost"); refusals = new PalmfolkView.ShapePool(valley, "Refusal crosses");
            refusedScaffold = new PalmfolkView.ShapePool(valley, "Refused branch removal");
            connectionDust = new PalmfolkView.ShapePool(valley, "Connection lock dust");
            rings = new PalmfolkView.ShapePool(valley, "Target rings");
            ringIdle = Material(new Color(1f, 0.86f, 0.25f)); ringReady = Material(new Color(0.3f, 1f, 0.5f)); gustMaterial = Material(new Color(0.75f, 0.9f, 1f));
            prompt = new GameObject("Instruction").AddComponent<TextMesh>(); prompt.transform.SetParent(transform, false);
            prompt.characterSize = 0.0028f; prompt.fontSize = 48; prompt.anchor = TextAnchor.MiddleCenter; prompt.alignment = TextAlignment.Center;
        }
        private void Say(string text, long tick, int ticks) { transient = text; transientUntil = tick + ticks; }

        // Glowing rings on every anchor the hand must reach (gold, green once held), and the instruction line.
        private void DrawGuide(ValleyPresentation frame)
        {
            rings.Begin();
            var targets = new List<AnchorZone>();
            if (frame.Source.HasValue && frame.Stage == DirectorStage.Interacting)
            {
                targets.Add(frame.Source.Value);
                foreach (string id in frame.ActiveEdges)
                {
                    var edge = spec.Edge(id); bool from = Vec3.Distance(edge.FromAnchor.Centre, frame.Source.Value.Centre) <= Vec3.Distance(edge.ToAnchor.Centre, frame.Source.Value.Centre);
                    targets.Add(from ? edge.ToAnchor : edge.FromAnchor);
                }
                if (frame.ActiveEdges.Count == 0 && frame.Destination.HasValue) targets.Add(frame.Destination.Value);
            }
            float pulse = ReducedMotion ? 1 : 0.85f + 0.15f * Mathf.Sin(frame.Tick / 8f);
            for (int t = 0; t < targets.Count; t++)
            {
                bool held = frame.ConnectionLocked || (t == 0 && frame.HandOwner.HasValue);
                float radius = Mathf.Max(0.012f, (float)Math.Min(targets[t].Radius, rules.ConnectDistance)) * (held ? 1 : pulse);
                var centre = PalmfolkView.V(targets[t].Centre) + Vector3.up * 0.002f;
                for (int i = 0; i < 16; i++)
                {
                    float angle = i * Mathf.PI / 8;
                    rings.Sphere(centre + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius), 0.0035f, held ? ringReady : ringIdle);
                }
                if (!held && !ReducedMotion) rings.Sphere(centre + Vector3.up * (0.03f + 0.006f * Mathf.Sin(frame.Tick / 6f)), 0.006f, ringIdle);
            }
            DrawGust(frame); DrawBake(frame);
            rings.End();
            Prompt = PromptFor(frame);
            prompt.text = Prompt; prompt.gameObject.SetActive(Prompt.Length > 0);
            if (Prompt.Length > 0)
            {
                prompt.transform.position = valley.TransformPoint(new Vector3(0, 0.11f, 0));
                var viewer = Camera.main != null ? Camera.main.transform : null;
                if (viewer != null) prompt.transform.rotation = Quaternion.LookRotation(prompt.transform.position - viewer.position, Vector3.up);
            }
        }

        // The bake moment: when a bridge solidifies, a wave of golden sparks runs along it from end to end and lifts
        // off, so the shape your hand held visibly becomes part of the world.
        private void DrawBake(ValleyPresentation frame)
        {
            foreach (var key in frame.Bridges.Keys)
                if (seenBridges.Add(key) && bakePrimed) { bakedKey = key; bakedAt = frame.Tick; }
            bakePrimed = true;
            if (bakedKey == null || !frame.Bridges.TryGetValue(bakedKey, out var tree) || frame.Tick - bakedAt > BakeTicks) return;
            float u = (frame.Tick - bakedAt) / (float)BakeTicks;
            foreach (var curve in tree.Curves)
            {
                int n = curve.Points.Count; if (n < 2) continue;
                for (int i = 0; i < n; i++)
                {
                    float along = i / (float)(n - 1);
                    float local = Mathf.Clamp01(u * 2f - along);
                    if (local <= 0 || local >= 1) continue;
                    var point = PalmfolkView.V(curve.Points[i]);
                    float lift = ReducedMotion ? 0.004f : 0.004f + local * 0.035f;
                    float size = 0.006f * (1 - local) + 0.0015f;
                    rings.Sphere(point + Vector3.up * lift, size, ringIdle);
                    if (!ReducedMotion && i % 2 == 0) rings.Sphere(point + new Vector3(0.006f, lift * 0.6f, -0.004f), size * 0.6f, ringReady);
                }
            }
        }

        // The gust: a swirl where it gathers and a dotted path to the traveler; while it blows, streaks run along the
        // path and stop short (green) when a hand shields it.
        private void DrawGust(ValleyPresentation frame)
        {
            if (!frame.GustOrigin.HasValue || !frame.GustTarget.HasValue) return;
            var from = PalmfolkView.V(frame.GustOrigin.Value); var to = PalmfolkView.V(frame.GustTarget.Value) + Vector3.up * 0.01f;
            bool blowing = frame.GustPhase == GustPhase.Blowing;
            var wind = frame.GustShielded ? ringReady : gustMaterial;
            float swirl = 0.012f + 0.02f * (float)(blowing ? 1 : frame.GustProgress);
            for (int i = 0; i < 10; i++)
            {
                float angle = i * Mathf.PI / 5 + (ReducedMotion ? 0 : frame.Tick / 5f);
                rings.Sphere(from + new Vector3(Mathf.Cos(angle) * swirl, (i % 3) * 0.006f, Mathf.Sin(angle) * swirl), 0.004f, wind);
            }
            for (int i = 1; i < 12; i++) rings.Sphere(Vector3.Lerp(from, to, i / 12f), 0.0018f, wind);
            if (!blowing) return;
            float reach = frame.GustShielded ? 0.55f : 1f;
            for (int i = 0; i < 6; i++)
            {
                float t = ReducedMotion ? i / 6f : Mathf.Repeat(frame.Tick / 20f + i / 6f, 1f);
                rings.Box(Vector3.Lerp(from, to, t * reach), new Vector3(0.012f, 0.002f, 0.002f), Quaternion.LookRotation(to - from) * Quaternion.Euler(0, 90, 0), wind);
            }
        }

        private string PromptFor(ValleyPresentation frame)
        {
            if (frame.Tick < transientUntil && transient != null) return transient;
            if (frame.GustPhase == GustPhase.Gathering) return "A gust is coming!\nStand your hand up like a wall between it and them";
            if (frame.GustPhase == GustPhase.Blowing) return frame.GustShielded ? "Hold it\u2026 shielding!" : "Block the wind with an upright hand!";
            if (frame.Bracing) return "They're bracing against the wind\u2026";
            if (refusedAt >= 0 && frame.Tick - refusedAt < 240 && frame.Refusals.Count > 0)
            {
                switch (frame.Refusals[0].Reason)
                {
                    case Refusal.TooNarrow: return "Too narrow for them. Put your fingers together, side by side";
                    case Refusal.TooSteep: return "Too steep. Keep your hand flatter";
                    case Refusal.LowClearance: return "It hits the rock. Arch your hand over it";
                    case Refusal.SelfCrossing: return "The path crosses itself. Straighten your fingers";
                    case Refusal.TooLong: return "Too long. Try a shorter path";
                    default: return "Both ends must touch the glowing rings";
                }
            }
            if (frame.Stage == DirectorStage.AwaitingWithdrawal) return "Lift your hand away; the bridge stays";
            if (frame.Stage != DirectorStage.Interacting || !frame.Source.HasValue) return "";
            if (frame.Recovering) return "Bring your hand back to where it was";
            if (frame.BuildPhase == BuildPhase.Building) return "Hold still\u2026 Pip is building " + Mathf.RoundToInt((float)frame.BuildProgress * 100) + "%";
            if (frame.BuildPhase == BuildPhase.Paused) return "Hold still to keep building";
            if (frame.ConnectionLocked) return "Hold still";
            if (frame.HandOwner.HasValue) return "Stretch your fingers to the other glowing ring";
            switch (frame.Puzzle)
            {
                case PuzzleKind.Width: return "The family needs a wide bridge:\nlay your fingers together, ring to ring";
                case PuzzleKind.Fork: return "Spread your fingers to reach every glowing ring";
                case PuzzleKind.Arch: return "Arch your hand over the rock, ring to ring";
                case PuzzleKind.Finale: return "Bram's cart needs wide, gentle bridges";
                default: return "Lay your hand flat from one glowing ring to the other";
            }
        }
        private void DrawConnection(ValleyPresentation frame)
        {
            connectionDust.Begin(); ShowingConnectionDust = connectionTick.HasValue && frame.ConnectionLocked && frame.Tick >= connectionTick.Value && frame.Tick - connectionTick.Value < 30;
            if (ShowingConnectionDust)
            {
                lockedAnchors.Clear();
                if (frame.Source.HasValue) lockedAnchors.Add(frame.Source.Value.Centre);
                foreach (string id in frame.ActiveEdges)
                {
                    var edge = spec.Edge(id); bool from = !frame.Source.HasValue || Vec3.Distance(edge.FromAnchor.Centre, frame.Source.Value.Centre) <= Vec3.Distance(edge.ToAnchor.Centre, frame.Source.Value.Centre);
                    lockedAnchors.Add(from ? edge.ToAnchor.Centre : edge.FromAnchor.Centre);
                }
                float age = (frame.Tick - connectionTick.Value) / 30f;
                foreach (var anchor in lockedAnchors)
                    for (int i = 0; i < 6; i++)
                    {
                        float angle = i * Mathf.PI / 3, radius = ReducedMotion ? 0.005f : 0.003f + age * 0.007f;
                        var offset = new Vector3(Mathf.Cos(angle) * radius, ReducedMotion ? 0.003f : Mathf.Sin(age * Mathf.PI) * 0.006f, Mathf.Sin(angle) * radius);
                        connectionDust.Sphere(PalmfolkView.V(anchor) + offset, 0.002f * (1 - age), warning);
                    }
            }
            connectionDust.End();
        }
        private Material Material(Color color) { var value = PalmfolkView.CreateMaterial(color); materials.Add(value); return value; }
        private void ApplyContrast()
        {
            backdrop.gameObject.SetActive(HighContrast && TableMode);
            if (contrastApplied == HighContrast) return; contrastApplied = HighContrast;
            var shader = Resources.Load<Material>("Palmfolk/" + (HighContrast ? "HighContrast" : "StandardOpaque")).shader;
            foreach (var material in new[] { wood, cord, skin, handMaterial, islandMaterial, warning, backdropMaterial, pipMaterial }) material.shader = shader;
            wood.color = HighContrast ? Color.white : new Color(0.48f, 0.29f, 0.13f);
            cord.color = HighContrast ? Color.white : new Color(0.22f, 0.17f, 0.10f);
            skin.color = HighContrast ? Color.white : new Color(0.65f, 0.51f, 0.31f);
            handMaterial.color = HighContrast ? Color.gray : new Color(0.65f, 0.51f, 0.31f);
            islandMaterial.color = HighContrast ? new Color(0.06f, 0.06f, 0.06f) : new Color(0.48f, 0.29f, 0.13f);
            warning.color = HighContrast ? Color.white : new Color(1, 0.65f, 0.15f);
        }
        private Transform Actor(string name, Material material = null)
        {
            var root = new GameObject(name).transform; root.SetParent(valley, false); root.gameObject.AddComponent<TravelerRig>().Initialize(material ?? skin); return root;
        }
        private Transform Cart(string name)
        {
            var root = new GameObject(name).transform; root.SetParent(valley, false);
            var body = PalmfolkView.Primitive(PrimitiveType.Cube, "Cart body", wood, root); body.localPosition = Vector3.up * 0.01f; body.localScale = new Vector3(0.02f, 0.012f, 0.016f);
            for (int side = -1; side <= 1; side += 2)
            { var wheel = PalmfolkView.Primitive(PrimitiveType.Cylinder, "Cart wheel", cord, root); wheel.localPosition = new Vector3(side * 0.011f, 0.005f, 0); wheel.localScale = new Vector3(0.008f, 0.001f, 0.008f); wheel.localRotation = Quaternion.Euler(0, 0, 90); }
            return root;
        }
        private void Draw(PalmfolkView.ShapePool pool, Route route, bool built, Material material, float visibility = 1, bool markTip = true, int? tip = null) => PalmfolkView.DrawGeometry(pool, route, built, material, wood, cord, visibility, markTip, tip);
        private void DrawTree(PalmfolkView.ShapePool pool, CurveTree tree, bool built, Material material, float visibility = 1)
        {
            for (int c = 0; c < tree.Curves.Count; c++)
            {
                bool leaf = true; for (int p = c + 1; p < tree.Parents.Count; p++) if (tree.Parents[p] == c) leaf = false;
                Draw(pool, tree.Curves[c], built, material, visibility, leaf, tree.Curves.Count > 1 ? tree.Curves[c].Points.Count - 1 : (int?)null);
            }
        }
        private static CurveTree BlendTree(CurveTree previous, CurveTree current, float alpha)
        {
            if (previous == null || current == null || previous.Curves.Count != current.Curves.Count) return current;
            var curves = new Route[current.Curves.Count]; var parents = new int[curves.Length];
            for (int i = 0; i < curves.Length; i++) { if (previous.Parents[i] != current.Parents[i]) return current; curves[i] = Blend(previous.Curves[i], current.Curves[i], alpha); parents[i] = current.Parents[i]; }
            try { return new CurveTree(curves, parents); } catch (ArgumentException) { return current; }
        }
        private static Route Blend(Route previous, Route current, float alpha)
        {
            if (previous == null || current == null || previous.Finger != current.Finger || previous.Reversed != current.Reversed || previous.Points.Count != current.Points.Count) return current;
            var points = current.CopyPoints(); for (int i = 0; i < points.Length; i++) points[i] = Vec3.Lerp(previous.Points[i], points[i], alpha);
            try { return new Route(points, current.CopyWidths(), current.Finger, current.Reversed); } catch (ArgumentException) { return current; }
        }
        private void OnDestroy() { foreach (var material in materials) DestroyImmediate(material); }
    }
}
#endif
