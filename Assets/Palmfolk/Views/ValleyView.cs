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
        private PalmfolkView.ShapePool live, preview, scaffold, ghost, refusals;
        private readonly Dictionary<string, PalmfolkView.ShapePool> bridges = new Dictionary<string, PalmfolkView.ShapePool>();
        private readonly List<Transform> travelers = new List<Transform>();
        private readonly List<Material> materials = new List<Material>();
        private Material wood, cord, skin, transparent, carved, warning;
        private ValleySpec spec;
        private Route previousRoute, currentRoute;
        private long routeTick = -2;
        private long? planted;
        public bool ReducedMotion { get; set; }
        public Transform ValleyRoot => valley;
        public int RenderedBridgeCount => bridges.Count;
        public void Consume(IReadOnlyList<GameEvent> events)
        {
            if (pipRig != null) { pipRig.Consume(events); bramRig.Consume(events); }
            foreach (var e in events) { if (e.Kind == GameEventKind.Planted) planted = e.Tick; if (e.Kind == GameEventKind.ReplantStarted) planted = null; }
        }
        public void Present(ValleyPresentation frame) { Present(frame, 1); }
        public void Present(ValleyPresentation frame, float handInterpolation)
        {
            if (frame == null) return;
            Ensure(frame.Valley); valley.gameObject.SetActive(frame.Pose != null); if (frame.Pose == null) return;
            var pose = frame.Pose;
            valley.SetPositionAndRotation(PalmfolkView.V(pose.Origin), new Quaternion((float)pose.Rotation.X, (float)pose.Rotation.Y, (float)pose.Rotation.Z, (float)pose.Rotation.W));
            float growth = planted.HasValue && !ReducedMotion ? Mathf.Max(0.001f, Mathf.SmoothStep(0, 1, (frame.Tick - planted.Value) / 30f)) : 1;
            valley.localScale = Vector3.one * (float)pose.Scale * growth;
            pip.localPosition = PalmfolkView.V(frame.Pip); bram.localPosition = PalmfolkView.V(frame.Bram);
            pipRig.ReducedMotion = bramRig.ReducedMotion = ReducedMotion; pipRig.Present(frame); bramRig.Present(frame);
            cart.localPosition = bram.localPosition + bram.localRotation * Vector3.back * 0.016f; cart.localRotation = bram.localRotation;
            int usedTraveler = 0;
            foreach (var traveler in frame.Travelers)
                for (int member = 0; member < (traveler.Kind == TravelerKind.Group ? 3 : 1); member++)
                {
                    if (usedTraveler == travelers.Count) travelers.Add(Actor("Traveler " + usedTraveler));
                    var actor = travelers[usedTraveler++]; actor.gameObject.SetActive(true);
                    Vec3 position = traveler.Position + new Vec3(traveler.Kind == TravelerKind.Group ? (member - 1) * 0.006 : 0, 0, 0);
                    actor.localPosition = PalmfolkView.V(position);
                    var rig = actor.GetComponent<TravelerRig>(); rig.ReducedMotion = ReducedMotion; rig.PresentAt(position, frame.Tick, null, null, traveler.Arrived);
                }
            for (int i = usedTraveler; i < travelers.Count; i++) travelers[i].gameObject.SetActive(false);
            if (spec.Ledge != null) ledge.localScale = new Vector3((float)frame.LedgeWidth, 0.002f, 0.01f);
            live.Begin(); preview.Begin(); scaffold.Begin(); ghost.Begin(); refusals.Begin();
            foreach (var pool in bridges.Values) pool.Begin();
            if (frame.Tick != routeTick) { previousRoute = currentRoute; currentRoute = frame.FilteredHandRoute; routeTick = frame.Tick; }
            var displayed = Blend(previousRoute, currentRoute, handInterpolation);
            Draw(live, displayed, false, skin); Draw(scaffold, frame.Scaffold, true, wood, (float)(1 - frame.ScaffoldRemovalProgress));
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
                Draw(preview, effective, true, transparent);
                bool cartFits = BridgeValidator.Validate(CurveTree.Single(effective), frame.Source.Value, new[] { frame.Destination.Value }, TravelerKind.Cart, new GameSettings(), spec.Clearance).Count == 0;
                capacity.localPosition = PalmfolkView.V(effective.Points[effective.Points.Count / 2]) + Vector3.up * 0.025f;
                capacity.localScale = cartFits ? new Vector3(0.014f, 0.006f, 0.014f) : new Vector3(0.004f, 0.015f, 0.004f);
            }
            foreach (var saved in frame.Bridges)
            {
                if (!bridges.TryGetValue(saved.Key, out var pool)) { pool = new PalmfolkView.ShapePool(valley, "Bridge " + saved.Key); bridges.Add(saved.Key, pool); }
                foreach (var curve in saved.Value.Curves) Draw(pool, curve, true, null);
            }
            foreach (string failing in frame.FailingEdges)
            {
                var edge = spec.Edge(failing); Cross(PalmfolkView.V(Vec3.Lerp(edge.FromAnchor.Centre, edge.ToAnchor.Centre, 0.5)));
            }
            foreach (var failure in frame.Refusals)
                if (displayed != null && failure.Segment >= 0 && failure.Segment + 1 < displayed.Points.Count)
                    Cross(PalmfolkView.V(Vec3.Lerp(displayed.Points[failure.Segment], displayed.Points[failure.Segment + 1], 0.5)));
            live.End(); preview.End(); scaffold.End(); ghost.End(); refusals.End(); foreach (var pool in bridges.Values) pool.End();
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
            islands = new GameObject("Islands and obstacles").transform; islands.SetParent(valley, false);
            foreach (var island in spec.Islands)
            {
                var top = PalmfolkView.Primitive(PrimitiveType.Cylinder, "Island " + island.Id, wood, islands);
                top.localPosition = PalmfolkView.V(island.Top) + Vector3.down * 0.012f; top.localScale = new Vector3((float)island.Radius * 2, 0.012f, (float)island.Radius * 2);
            }
            foreach (var obstacle in spec.Clearance.Obstacles)
            {
                var rock = PalmfolkView.Primitive(PrimitiveType.Cube, "Configured rock", cord, islands);
                rock.localPosition = PalmfolkView.V((obstacle.Min + obstacle.Max) / 2); rock.localScale = PalmfolkView.V(obstacle.Max - obstacle.Min);
            }
            pip = Actor("Pip"); bram = Actor("Bram"); pipRig = pip.GetComponent<TravelerRig>(); bramRig = bram.GetComponent<TravelerRig>(); bramRig.Role = TravelerRole.Bram;
            cart = PalmfolkView.Primitive(PrimitiveType.Cube, "Lantern cart", wood, valley); cart.localScale = new Vector3(0.02f, 0.014f, 0.016f);
            capacity = PalmfolkView.Primitive(PrimitiveType.Cube, "Capacity shape", wood, valley);
            ledge = PalmfolkView.Primitive(PrimitiveType.Cube, "Widening ledge", wood, valley);
            ledge.gameObject.SetActive(spec.Ledge != null);
            if (spec.Ledge != null)
            {
                var edge = spec.Edge(spec.Ledge.Edge); ledge.localPosition = PalmfolkView.V(edge.AnchorAt(spec.Ledge.Island).Centre) - Vector3.up * 0.001f;
                ledge.localRotation = Quaternion.LookRotation(PalmfolkView.V(edge.ToAnchor.Centre - edge.FromAnchor.Centre), Vector3.up);
            }
            live = new PalmfolkView.ShapePool(valley, "Displayed route"); preview = new PalmfolkView.ShapePool(valley, "Preview");
            scaffold = new PalmfolkView.ShapePool(valley, "Frozen scaffold"); ghost = new PalmfolkView.ShapePool(valley, "Recovery ghost"); refusals = new PalmfolkView.ShapePool(valley, "Refusal crosses");
        }
        private Material Material(Color color) { var value = PalmfolkView.CreateMaterial(color); materials.Add(value); return value; }
        private Transform Actor(string name)
        {
            var root = new GameObject(name).transform; root.SetParent(valley, false); root.gameObject.AddComponent<TravelerRig>().Initialize(skin); return root;
        }
        private void Draw(PalmfolkView.ShapePool pool, Route route, bool built, Material material, float visibility = 1) => PalmfolkView.DrawGeometry(pool, route, built, material, wood, cord, visibility);
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
