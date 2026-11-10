#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using Palmfolk.Core;
using UnityEngine;

namespace Palmfolk.Views
{
    // Ships independently of developer stats/recording. Derived visual routes are not new Core edges.
    public sealed class XRayView : MonoBehaviour, IPalmfolkView, IValleyView
    {
        private readonly List<LineRenderer> lines = new List<LineRenderer>();
        private Material material;
        private Transform content;
        private TextMesh progress;
        private LevelGeometry level;
        private int used;
        private readonly HashSet<Route> drawn = new HashSet<Route>(RouteGeometryComparer.Instance);
        public bool Visible { get; private set; }
        public double ConnectDistance { get; set; } = 0.02;
        public double BreakDistance { get; set; } = 0.04;
        public void Configure(LevelGeometry geometry, GameSettings settings = null)
        { level = geometry; if (settings != null) { ConnectDistance = settings.ConnectDistance; BreakDistance = settings.BreakDistance; } }
        public void SetVisible(bool visible) { Visible = visible; if (content != null) content.gameObject.SetActive(visible); }
        public void Present(WorldPresentation frame)
        {
            Ensure();
            content.gameObject.SetActive(Visible && frame.Pose != null);
            if (!Visible || frame.Pose == null) return;
            ApplyPose(frame.Pose); used = 0;
            Route(frame.FilteredHandRoute, Color.white, 0.001f);
            Route(frame.Scaffold, Color.yellow, 0.003f);
            if (frame.Bridge != null) foreach (var curve in frame.Bridge.Curves) Route(curve, Color.cyan, 0.002f);
            Route(frame.GhostRoute, Color.gray, 0.0015f);
            if (level != null)
                foreach (var anchor in new[] { level.Start, level.End })
                { Ring(anchor, ConnectDistance, false); Ring(anchor, BreakDistance, true); }
            Finish(frame.BuildProgress, "M1");
        }
        public void Present(ValleyPresentation frame)
        {
            Ensure(); content.gameObject.SetActive(Visible && frame.Pose != null);
            if (!Visible || frame.Pose == null) return;
            ApplyPose(frame.Pose); used = 0;
            foreach (var edge in frame.Valley.Edges)
            {
                var line = Next(Color.gray, 0.0005f); line.positionCount = 2;
                line.SetPosition(0, PalmfolkView.V(edge.FromAnchor.Centre)); line.SetPosition(1, PalmfolkView.V(edge.ToAnchor.Centre));
            }
            if (frame.HandTree != null) foreach (var curve in frame.HandTree.Curves) Route(curve, Color.white, 0.001f); else Route(frame.FilteredHandRoute, Color.white, 0.001f);
            if (frame.ScaffoldTree != null) foreach (var curve in frame.ScaffoldTree.Curves) Route(curve, Color.yellow, 0.003f); else Route(frame.Scaffold, Color.yellow, 0.003f);
            Route(frame.GhostRoute, Color.gray, 0.0015f); drawn.Clear();
            foreach (var bridge in frame.Bridges.Values) foreach (var curve in bridge.Curves) if (drawn.Add(curve)) Route(curve, Color.cyan, 0.002f);
            if (frame.Source.HasValue) { Ring(frame.Source.Value.Centre, frame.Source.Value.Radius, false); Ring(frame.Source.Value.Centre, frame.Source.Value.Radius + BreakDistance - ConnectDistance, true); }
            if (frame.Destination.HasValue) { Ring(frame.Destination.Value.Centre, frame.Destination.Value.Radius, false); Ring(frame.Destination.Value.Centre, frame.Destination.Value.Radius + BreakDistance - ConnectDistance, true); }
            Finish(frame.BuildProgress, frame.ActiveEdge ?? frame.Puzzle.ToString());
        }
        private void Ensure()
        {
            if (content == null)
            {
                content = new GameObject("X-ray routes").transform; content.SetParent(transform, false);
                material = new Material(Shader.Find("Sprites/Default"));
                progress = new GameObject("Progress sign").AddComponent<TextMesh>(); progress.transform.SetParent(content, false);
                progress.characterSize = 0.003f; progress.fontSize = 32; progress.anchor = TextAnchor.MiddleCenter;
            }
        }
        private void ApplyPose(ValleyPose pose)
        {
            content.SetPositionAndRotation(PalmfolkView.V(pose.Origin), new Quaternion((float)pose.Rotation.X, (float)pose.Rotation.Y, (float)pose.Rotation.Z, (float)pose.Rotation.W));
            content.localScale = Vector3.one * (float)pose.Scale;
        }
        private void Finish(double buildProgress, string context)
        {
            for (int i = used; i < lines.Count; i++) lines[i].gameObject.SetActive(false);
            progress.text = context + " / BUILD " + Mathf.RoundToInt((float)buildProgress * 100) + "%\nsolid: frozen  /  thin: live\nconnect " + (ConnectDistance * 100).ToString("0.#") + " cm / break " + (BreakDistance * 100).ToString("0.#") + " cm";
            progress.transform.localPosition = Vector3.up * 0.08f;
        }
        private LineRenderer Next(Color color, float width)
        {
            if (used == lines.Count)
            {
                var line = new GameObject("Diagnostic line").AddComponent<LineRenderer>(); line.transform.SetParent(content, false);
                line.useWorldSpace = false; line.sharedMaterial = material; line.numCapVertices = 0; lines.Add(line);
            }
            var result = lines[used++]; result.gameObject.SetActive(true); result.startColor = result.endColor = color;
            result.startWidth = result.endWidth = width; result.loop = false; return result;
        }
        private void Route(Route route, Color color, float width)
        {
            if (route == null) return;
            var line = Next(color, width); line.positionCount = route.Points.Count;
            for (int i = 0; i < route.Points.Count; i++) line.SetPosition(i, PalmfolkView.V(route.Points[i]) + Vector3.up * 0.001f);
            // Joint markers make frozen/live differences visible in greyscale too.
            if (width >= 0.002f) foreach (var point in route.Points)
            {
                var post = Next(color, width); post.positionCount = 2;
                post.SetPosition(0, PalmfolkView.V(point)); post.SetPosition(1, PalmfolkView.V(point) + Vector3.up * 0.008f);
            }
        }
        private void Ring(Vec3 center, double radius, bool dashed)
        {
            for (int i = 0; i < 32; i++)
            {
                if (dashed && i % 2 == 0) continue;
                var line = Next(Color.white, 0.0006f); line.positionCount = 2;
                for (int end = 0; end < 2; end++)
                {
                    float angle = (i + end) * Mathf.PI / 16;
                    line.SetPosition(end, PalmfolkView.V(center) + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * (float)radius);
                }
            }
        }
        private void OnDestroy() { if (material != null) DestroyImmediate(material); }
    }
}
#endif
