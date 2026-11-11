#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using Palmfolk.Core;
using UnityEngine;

namespace Palmfolk.Views
{
    // Read-only D8 visitors, returning residents and permanent, shape-coded bridge marks.
    public sealed class LivingValleyView : MonoBehaviour, IValleyView
    {
        private Transform content, marks;
        private Material wood, light;
        private readonly List<TravelerRig> figures = new List<TravelerRig>();
        private readonly List<Transform> carts = new List<Transform>();
        private IReadOnlyDictionary<string, int> seenMarks;
        private IReadOnlyDictionary<string, CurveTree> seenBridges;
        public bool ReducedMotion { get; set; }
        public bool HighContrast { get; set; }
        public int VisibleFigures { get; private set; }
        public int VisibleMarks { get; private set; }
        public void Present(ValleyPresentation frame)
        {
            Ensure(); content.gameObject.SetActive(frame.Pose != null); if (frame.Pose == null) return;
            content.SetPositionAndRotation(PalmfolkView.V(frame.Pose.Origin), Quaternion.Euler(0, (float)(frame.Pose.YawRadians * 180 / System.Math.PI), 0));
            content.localScale = Vector3.one * (float)frame.Pose.Scale;
            wood.shader = Resources.Load<Material>("Palmfolk/" + (HighContrast ? "HighContrast" : "StandardOpaque")).shader;
            wood.color = HighContrast ? Color.white : new Color(0.58f, 0.39f, 0.20f);
            int used = 0;
            if (frame.VisitorFigure != null) Figure(frame.VisitorFigure);
            foreach (var resident in frame.Residents) Figure(resident);
            for (int i = used; i < figures.Count; i++) { figures[i].gameObject.SetActive(false); carts[i].gameObject.SetActive(false); }
            VisibleFigures = used;
            if (!ReferenceEquals(seenMarks, frame.BridgeMarks) || !ReferenceEquals(seenBridges, frame.Bridges))
            {
                seenMarks = frame.BridgeMarks; seenBridges = frame.Bridges;
                foreach (Transform child in marks) child.gameObject.SetActive(false);
                VisibleMarks = 0;
                foreach (var entry in frame.BridgeMarks)
                {
                    if (!frame.Bridges.TryGetValue(entry.Key, out var tree)) continue;
                    var route = tree.Curves[tree.Curves.Count - 1];
                    for (int bit = 0; bit < frame.Windows; bit++) if ((entry.Value & (1 << bit)) != 0)
                    {
                        string key = entry.Key + " / " + bit; var mark = marks.Find(key);
                        if (mark == null) { mark = new GameObject(key).transform; mark.SetParent(marks, false); Glyph(mark, bit); }
                        mark.gameObject.SetActive(true); mark.localPosition = PalmfolkView.V(route.Points[route.Points.Count / 2]) + new Vector3((bit - 2) * 0.007f, 0.006f, 0);
                        VisibleMarks++;
                    }
                }
            }
            void Figure(TravelerView traveler)
            {
                int members = traveler.Kind == TravelerKind.Group ? 3 : 1;
                for (int member = 0; member < members; member++)
                {
                    if (used == figures.Count)
                    {
                        var actor = new GameObject("Visitor or resident " + used); actor.transform.SetParent(content, false);
                        var rig = actor.AddComponent<TravelerRig>(); rig.Initialize(wood); figures.Add(rig);
                        var cart = PalmfolkView.Primitive(PrimitiveType.Cube, "Resident cart " + used, wood, content); cart.localScale = new Vector3(0.02f, 0.012f, 0.016f); carts.Add(cart);
                    }
                    var figure = figures[used]; figure.gameObject.SetActive(true); figure.ReducedMotion = ReducedMotion;
                    var position = traveler.Position + new Vec3((member - (members - 1) * 0.5) * 0.006, 0, 0);
                    figure.transform.localPosition = PalmfolkView.V(position); figure.PresentTraveler(position, frame.Tick, traveler.Arrived, frame.Pose, frame.Bridges);
                    carts[used].gameObject.SetActive(traveler.Kind == TravelerKind.Cart);
                    carts[used].localPosition = figure.transform.localPosition + figure.transform.localRotation * Vector3.back * 0.018f; carts[used].localRotation = figure.transform.localRotation;
                    used++;
                }
            }
        }
        private void Ensure()
        {
            if (content != null) return;
            content = new GameObject("Living valley").transform; content.SetParent(transform, false);
            marks = new GameObject("Visitor bridge marks").transform; marks.SetParent(content, false);
            wood = new Material(Resources.Load<Material>("Palmfolk/StandardOpaque"));
            light = new Material(Resources.Load<Material>("Palmfolk/Glow")) { color = new Color(1, 0.85f, 0.45f) };
        }
        private void Glyph(Transform parent, int visitor)
        {
            void Part(PrimitiveType type, Vector3 at, Vector3 size, float yaw = 0)
            { var part = PalmfolkView.Primitive(type, "Visitor mark", light, parent); part.localPosition = at; part.localScale = size; part.localRotation = Quaternion.Euler(0, yaw, 0); }
            switch (visitor)
            {
                case 0: Part(PrimitiveType.Sphere, Vector3.zero, new Vector3(0.003f, 0.001f, 0.007f), 35); break;
                case 1: Part(PrimitiveType.Cube, Vector3.zero, new Vector3(0.005f, 0.002f, 0.004f)); Part(PrimitiveType.Sphere, Vector3.forward * 0.003f, Vector3.one * 0.002f); break;
                case 2: Part(PrimitiveType.Sphere, Vector3.left * 0.002f, Vector3.one * 0.003f); Part(PrimitiveType.Sphere, Vector3.right * 0.002f, Vector3.one * 0.003f); break;
                case 3: Part(PrimitiveType.Cube, Vector3.zero, new Vector3(0.007f, 0.001f, 0.001f)); Part(PrimitiveType.Cube, Vector3.zero, new Vector3(0.001f, 0.001f, 0.007f)); break;
                default: Part(PrimitiveType.Cube, Vector3.zero, new Vector3(0.003f, 0.005f, 0.003f)); Part(PrimitiveType.Cube, Vector3.up * 0.003f, new Vector3(0.005f, 0.001f, 0.005f)); break;
            }
        }
        private void OnDestroy() { if (wood != null) DestroyImmediate(wood); if (light != null) DestroyImmediate(light); }
    }
}
#endif
