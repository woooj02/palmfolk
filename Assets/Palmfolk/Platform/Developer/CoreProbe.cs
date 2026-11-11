#if PALMFOLK_DEVELOPMENT
using Palmfolk.Core;
using UnityEngine;

namespace Palmfolk.Platform.Developer
{
    // Developer builds only: the D6 headset check before views are bound. Shows the planted valley origin (a flat
    // disc at the pose's scale), a sphere that grows with planting progress at the resting palm, and Pip; logs every
    // Core event to the device log (adb logcat -s Unity).
    public sealed class CoreProbe : MonoBehaviour
    {
        [SerializeField] private PalmfolkRunner runner;
        private Transform origin, pip, plant;
        public PalmfolkRunner Runner { get => runner; set { if (runner != null) { runner.Ticked -= Log; runner.Presented -= Draw; } runner = value; OnEnable(); } }
        public int Logged { get; private set; }

        private void OnEnable()
        {
            if (runner == null) return;
            runner.Ticked -= Log; runner.Ticked += Log; runner.Presented -= Draw; runner.Presented += Draw;
        }
        private void OnDisable() { if (runner != null) { runner.Ticked -= Log; runner.Presented -= Draw; } }

        private static Transform Marker(PrimitiveType type, string name, Color color, Transform parent)
        {
            var t = GameObject.CreatePrimitive(type).transform; t.name = name; t.SetParent(parent, false);
            var collider = t.GetComponent<Collider>();
            if (collider != null) { if (Application.isPlaying) Destroy(collider); else DestroyImmediate(collider); }
            var renderer = t.GetComponent<Renderer>(); var material = new Material(renderer.sharedMaterial) { color = color }; renderer.sharedMaterial = material;
            return t;
        }

        private void Log(System.Collections.Generic.IReadOnlyList<GameEvent> events)
        {
            foreach (var e in events) { Debug.Log($"Palmfolk event: {e.Kind} at tick {e.Tick}"); Logged++; }
        }

        private GazeSource shownSource = (GazeSource)(-1);
        private void Draw(PalmfolkRunner r)
        {
            // GZ3: which gaze source is active, logged when it changes.
            if (r.GazeSource != shownSource) { shownSource = r.GazeSource; Debug.Log($"Palmfolk gaze source: {shownSource}"); }
            if (origin == null)
            {
                origin = Marker(PrimitiveType.Cylinder, "Probe valley origin", new Color(0.3f, 0.8f, 0.5f), transform);
                pip = Marker(PrimitiveType.Sphere, "Probe Pip", new Color(1f, 0.7f, 0.2f), transform);
                plant = Marker(PrimitiveType.Sphere, "Probe planting", new Color(0.4f, 0.6f, 1f), transform);
            }
            var game = r.Game; var pose = game?.Pose;
            origin.gameObject.SetActive(pose != null);
            if (pose != null)
            {
                PalmfolkRunner.Apply(origin, pose); origin.localScale = new Vector3(0.06f, 0.001f, 0.06f) * (float)pose.Scale;
            }
            var local = game?.Valley != null ? game.Valley.Pip : game?.Puzzle?.Pip;
            pip.gameObject.SetActive(pose != null && local.HasValue);
            if (pose != null && local.HasValue) { var w = pose.ToWorld(local.Value); pip.position = new Vector3((float)w.X, (float)w.Y, (float)w.Z); pip.localScale = Vector3.one * 0.012f * (float)pose.Scale; }
            var palm = game?.Planting.Hand is HandSide side ? (side == HandSide.Left ? r.Source.Left : r.Source.Right) : null;
            bool planting = game != null && game.Stage == ValleyStage.Planting && palm != null;
            plant.gameObject.SetActive(planting);
            if (planting)
            {
                var p = palm[JointId.Palm].Position; plant.position = new Vector3((float)p.X, (float)p.Y + 0.03f, (float)p.Z);
                plant.localScale = Vector3.one * (0.005f + 0.02f * (float)game.Planting.Progress);
            }
        }
    }
}
#endif
