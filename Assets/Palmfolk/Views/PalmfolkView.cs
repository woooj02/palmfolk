#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using Palmfolk.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Palmfolk.Views
{
    // A presentation-only adapter. The platform/clock supplies interpolated Core frames.
    public sealed class PalmfolkView : MonoBehaviour, IPalmfolkView
    {
        private Transform valley, pip, bram, cart, apprentice, capacity;
        private ShapePool hand, preview, scaffold, bridge, ghost, hint, refusal;
        private Material wood, cord, skin, translucent, ghostMaterial, hintMaterial, warning;
        private LevelGeometry level;
        private TravelerRig pipRig, bramRig, apprenticeRig;
        private long? growthStart;
        private Material islandMaterial, handMaterial, backdropMaterial;
        private Transform backdrop;
        private bool? contrastApplied;
        public bool HighContrast { get; set; }
        public bool TableMode { get; set; }
        private readonly List<Material> materials = new List<Material>();
        public Transform ValleyRoot { get { Ensure(); return valley; } }
        public Route HintRoute { get; set; }
        public bool ReducedMotion { get; set; }
        public WorldPresentation LastFrame { get; private set; }

        public void Configure(LevelGeometry geometry) { level = geometry; Ensure(); }
        public void Consume(IReadOnlyList<GameEvent> events)
        {
            Ensure(); pipRig.Consume(events); bramRig.Consume(events);
            foreach (var e in events)
            {
                if (e.Kind == GameEventKind.Planted) growthStart = e.Tick;
                if (e.Kind == GameEventKind.ReplantStarted) growthStart = null;
            }
        }

        public void Present(WorldPresentation frame)
        {
            if (frame == null) return;
            Ensure(); LastFrame = frame;
            ApplyContrast();
            valley.gameObject.SetActive(frame.Pose != null);
            if (frame.Pose == null) return;
            var pose = frame.Pose;
            valley.SetPositionAndRotation(V(pose.Origin), new Quaternion((float)pose.Rotation.X, (float)pose.Rotation.Y, (float)pose.Rotation.Z, (float)pose.Rotation.W));
            float growth = growthStart.HasValue && !ReducedMotion ? Mathf.Max(0.001f, Mathf.SmoothStep(0, 1, (frame.Tick - growthStart.Value) / 30f)) : 1;
            valley.localScale = Vector3.one * (float)pose.Scale * growth;
            pip.localPosition = V(frame.Pip); bram.localPosition = V(frame.Bram);
            pipRig.ReducedMotion = bramRig.ReducedMotion = apprenticeRig.ReducedMotion = ReducedMotion;
            pipRig.Present(frame); bramRig.Present(frame); apprenticeRig.Present(frame);
            cart.localRotation = bram.localRotation;
            cart.localPosition = V(frame.Bram) + bram.localRotation * Vector3.back * 0.016f;
            apprentice.localPosition = level == null ? Vector3.zero : V(level.End) + new Vector3(0, 0, 0.025f);
            hand.Begin(); preview.Begin(); scaffold.Begin(); bridge.Begin(); ghost.Begin(); hint.Begin(); refusal.Begin();
            DrawRoute(hand, frame.FilteredHandRoute, false, handMaterial);
            bool connected = frame.FilteredHandRoute != null && level != null &&
                ((Vec3.Distance(frame.FilteredHandRoute.Start, level.Start) < 0.02 && Vec3.Distance(frame.FilteredHandRoute.End, level.End) < 0.02) ||
                (Vec3.Distance(frame.FilteredHandRoute.End, level.Start) < 0.02 && Vec3.Distance(frame.FilteredHandRoute.Start, level.End) < 0.02));
            if (connected && frame.Bridge == null) DrawRoute(preview, frame.FilteredHandRoute, true, translucent);
            capacity.gameObject.SetActive(connected && frame.Bridge == null);
            if (connected)
            {
                var route = frame.FilteredHandRoute;
                bool wide = BridgeValidator.Validate(CurveTree.Single(route), route.Start, route.End, TravelerKind.Cart, new GameSettings(), level).Count == 0;
                capacity.localPosition = V(route.Points[route.Points.Count / 2]) + Vector3.up * 0.025f;
                capacity.localScale = wide ? new Vector3(0.014f, 0.006f, 0.014f) : new Vector3(0.004f, 0.015f, 0.004f);
            }
            DrawRoute(scaffold, frame.Scaffold, true, wood, (float)(1 - frame.ScaffoldRemovalProgress));
            if (frame.Bridge != null) foreach (var curve in frame.Bridge.Curves) DrawRoute(bridge, curve, true, null);
            DrawRoute(ghost, frame.GhostRoute, false, ghostMaterial);
            // Recovery ghost is still, solid-jointed and carved. Hint has separated moving beads.
            if (HintRoute != null)
                for (int i = 0; i < HintRoute.Points.Count; i++)
                {
                    float bob = ReducedMotion ? 0 : Mathf.Sin((float)frame.Tick / 12 + i) * 0.003f;
                    hint.Sphere(V(HintRoute.Points[i]) + Vector3.up * bob, 0.008f, hintMaterial);
                }
            foreach (var fail in frame.Refusals)
            {
                var route = frame.Bridge != null && fail.Curve < frame.Bridge.Curves.Count ? frame.Bridge.Curves[fail.Curve] : frame.FilteredHandRoute;
                if (route == null || fail.Segment < 0 || fail.Segment + 1 >= route.Points.Count) continue;
                Vector3 center = V(Vec3.Lerp(route.Points[fail.Segment], route.Points[fail.Segment + 1], 0.5)) + Vector3.up * 0.012f;
                float size = ReducedMotion ? 0.013f : 0.013f + Mathf.Sin((float)frame.Tick / 5) * 0.002f;
                // Crossed bars are readable without colour, including a static reduced-motion state.
                refusal.Box(center, new Vector3(size, 0.003f, 0.003f), Quaternion.Euler(0, 0, 45), warning);
                refusal.Box(center, new Vector3(size, 0.003f, 0.003f), Quaternion.Euler(0, 0, -45), warning);
            }
            hand.End(); preview.End(); scaffold.End(); bridge.End(); ghost.End(); hint.End(); refusal.End();
        }

        private void Ensure()
        {
            if (valley != null) return;
            valley = new GameObject("Valley presentation").transform; valley.SetParent(transform, false);
            wood = Make(new Color(0.48f, 0.29f, 0.13f)); cord = Make(new Color(0.22f, 0.17f, 0.10f));
            skin = Make(new Color(0.65f, 0.51f, 0.31f)); translucent = Make(new Color(0.7f, 0.8f, 0.9f, 0.25f));
            ghostMaterial = Make(new Color(0.8f, 0.8f, 0.75f, 0.45f)); hintMaterial = Make(new Color(0.9f, 0.95f, 1, 0.3f));
            warning = Make(new Color(1, 0.65f, 0.15f));
            islandMaterial = Make(new Color(0.48f, 0.29f, 0.13f)); handMaterial = Make(new Color(0.65f, 0.51f, 0.31f)); backdropMaterial = Make(new Color(0.02f, 0.02f, 0.02f));
            backdrop = Primitive(PrimitiveType.Cube, "High contrast backdrop", backdropMaterial, valley);
            backdrop.localPosition = Vector3.down * 0.001f; backdrop.localScale = new Vector3(0.34f, 0.001f, 0.20f); backdrop.gameObject.SetActive(false);
            hand = new ShapePool(valley, "Displayed route"); preview = new ShapePool(valley, "Preview");
            scaffold = new ShapePool(valley, "Frozen scaffold"); bridge = new ShapePool(valley, "Persistent bridge");
            ghost = new ShapePool(valley, "Recovery ghost"); hint = new ShapePool(valley, "Moving hint"); refusal = new ShapePool(valley, "Refusal crosses");
            pip = Actor("Pip", skin); bram = Actor("Bram", wood); apprentice = Actor("Apprentice", skin);
            pipRig = pip.GetComponent<TravelerRig>(); pipRig.Role = TravelerRole.Pip;
            bramRig = bram.GetComponent<TravelerRig>(); bramRig.Role = TravelerRole.Bram;
            apprenticeRig = apprentice.GetComponent<TravelerRig>(); apprenticeRig.Role = TravelerRole.Apprentice;
            cart = Primitive(PrimitiveType.Cube, "Lantern cart", wood, valley);
            cart.localScale = new Vector3(0.02f, 0.014f, 0.016f);
            capacity = Primitive(PrimitiveType.Cube, "Capacity shape", wood, valley);
            if (level != null)
            {
                Island("S", level.Start, -0.045f); Island("A", level.End, 0.045f);
            }
        }

        private void Island(string name, Vec3 anchor, float direction)
        {
            var island = Primitive(PrimitiveType.Cube, "Island " + name, islandMaterial, valley);
            island.localPosition = V(anchor) + new Vector3(direction, -0.012f, 0);
            island.localScale = new Vector3(0.09f, 0.024f, 0.10f);
        }
        private Transform Actor(string name, Material material)
        {
            var root = new GameObject(name).transform; root.SetParent(valley, false);
            var body = Primitive(PrimitiveType.Capsule, "Body", material, root);
            body.localPosition = Vector3.up * 0.012f; body.localScale = new Vector3(0.012f, 0.009f, 0.009f);
            var head = Primitive(PrimitiveType.Sphere, "Head", material, root);
            head.localPosition = Vector3.up * 0.026f; head.localScale = Vector3.one * 0.008f;
            root.gameObject.AddComponent<TravelerRig>().Initialize(material);
            return root;
        }
        private void DrawRoute(ShapePool pool, Route route, bool built, Material material, float visible = 1)
            => DrawGeometry(pool, route, built, material, wood, cord, visible);
        internal static void DrawGeometry(ShapePool pool, Route route, bool built, Material material, Material wood, Material cord, float visible = 1)
        {
            if (route == null || visible <= 0) return;
            for (int i = 0; i + 1 < route.Points.Count; i++)
            {
                Vector3 a = V(route.Points[i]), b = V(route.Points[i + 1]);
                float length = Vector3.Distance(a, b), width = (float)route.Widths[i];
                Quaternion rotation = Quaternion.LookRotation((b - a).normalized, Vector3.up);
                bool plank = built && width >= 0.025f;
                if (plank)
                {
                    int count = Mathf.Max(1, Mathf.CeilToInt(length / 0.008f));
                    for (int p = 0; p < count; p++) pool.Box(Vector3.Lerp(a, b, (p + 0.5f) / count), new Vector3(width, 0.002f * visible, length / count * 0.8f), rotation, material != null ? material : wood);
                }
                else pool.Box((a + b) / 2, new Vector3(built ? 0.003f : width, 0.002f * visible, length), rotation, material != null ? material : cord);
                if (built)
                {
                    Vector3 side = rotation * Vector3.right * Mathf.Max(0.004f, width / 2);
                    pool.Box((a + b) / 2 + side + Vector3.up * 0.006f, new Vector3(0.001f, 0.001f, length), rotation, material != null ? material : cord);
                    pool.Box((a + b) / 2 - side + Vector3.up * 0.006f, new Vector3(0.001f, 0.001f, length), rotation, material != null ? material : cord);
                }
            }
            for (int i = 0; i < route.Points.Count; i++)
                if (built) pool.Box(V(route.Points[i]) + Vector3.up * 0.005f, new Vector3(0.003f, 0.01f * visible, 0.003f), Quaternion.identity, material != null ? material : wood);
                else pool.Sphere(V(route.Points[i]), 0.007f, material);
            int tip = route.Reversed ? route.Points.Count - 1 : 0;
            if (built) pool.Sphere(V(route.Points[tip]), 0.006f * visible, material != null ? material : cord);
        }

        private Material Make(Color color)
        {
            var result = CreateMaterial(color); materials.Add(result); return result;
        }
        internal static Material CreateMaterial(Color color)
        {
            var template = Resources.Load<Material>("Palmfolk/" + (color.a < 1 ? "StandardTransparent" : "StandardOpaque"));
            if (template == null) throw new System.InvalidOperationException("Missing committed presentation material template.");
            var result = new Material(template) { color = color };
            if (color.a < 1)
            {
                result.SetFloat("_Mode", 3); result.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                result.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha); result.SetInt("_ZWrite", 0);
                result.EnableKeyword("_ALPHABLEND_ON"); result.renderQueue = 3000;
            }
            return result;
        }
        private void ApplyContrast()
        {
            backdrop.gameObject.SetActive(HighContrast && TableMode);
            if (contrastApplied == HighContrast) return; contrastApplied = HighContrast;
            var shader = Resources.Load<Material>("Palmfolk/" + (HighContrast ? "HighContrast" : "StandardOpaque")).shader;
            foreach (var material in new[] { wood, cord, skin, handMaterial, islandMaterial, warning, backdropMaterial }) material.shader = shader;
            wood.color = HighContrast ? Color.white : new Color(0.48f, 0.29f, 0.13f);
            cord.color = HighContrast ? Color.white : new Color(0.22f, 0.17f, 0.10f);
            skin.color = HighContrast ? Color.white : new Color(0.65f, 0.51f, 0.31f);
            handMaterial.color = HighContrast ? Color.gray : new Color(0.65f, 0.51f, 0.31f);
            islandMaterial.color = HighContrast ? new Color(0.06f, 0.06f, 0.06f) : new Color(0.48f, 0.29f, 0.13f);
            warning.color = HighContrast ? Color.white : new Color(1, 0.65f, 0.15f);
        }
        private void OnDestroy() { foreach (var material in materials) DestroyImmediate(material); }
        public static Vector3 V(Vec3 p) => new Vector3((float)p.X, (float)p.Y, (float)p.Z);
        public static Transform Primitive(PrimitiveType type, string name, Material material, Transform parent)
        {
            var result = GameObject.CreatePrimitive(type); result.name = name;
            DestroyImmediate(result.GetComponent<Collider>()); result.transform.SetParent(parent, false);
            result.GetComponent<Renderer>().sharedMaterial = material; return result.transform;
        }

        internal sealed class ShapePool
        {
            private readonly Transform root;
            private readonly List<Transform> boxes = new List<Transform>(), spheres = new List<Transform>();
            private int box, sphere;
            public ShapePool(Transform parent, string name) { root = new GameObject(name).transform; root.SetParent(parent, false); }
            public void Begin() { box = sphere = 0; }
            public void Box(Vector3 p, Vector3 size, Quaternion q, Material material) => Shape(boxes, ref box, PrimitiveType.Cube, p, size, q, material);
            public void Sphere(Vector3 p, float size, Material material) => Shape(spheres, ref sphere, PrimitiveType.Sphere, p, Vector3.one * size, Quaternion.identity, material);
            private void Shape(List<Transform> items, ref int index, PrimitiveType type, Vector3 p, Vector3 size, Quaternion q, Material material)
            {
                if (index == items.Count) items.Add(Primitive(type, "Part " + index, material, root));
                var part = items[index++]; part.gameObject.SetActive(true); part.localPosition = p;
                part.localRotation = q; part.localScale = size; part.GetComponent<Renderer>().sharedMaterial = material;
            }
            public void End()
            {
                for (int i = box; i < boxes.Count; i++) boxes[i].gameObject.SetActive(false);
                for (int i = sphere; i < spheres.Count; i++) spheres[i].gameObject.SetActive(false);
            }
        }
    }
}
#endif
