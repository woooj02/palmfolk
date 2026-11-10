#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using Palmfolk.Core;
using UnityEngine;

namespace Palmfolk.Views
{
    // Visual input port only. The puzzle director supplies counts and actual saved bridge trees.
    public sealed class NightView : MonoBehaviour, IPalmfolkView, IValleyView
    {
        private Transform valley, environment, windows;
        private Material ground, darkWindow, litWindow, lightRun;
        private readonly List<LineRenderer> bridgeLights = new List<LineRenderer>();
        private CurveTree[] bridges = Array.Empty<CurveTree>();
        private int solved = -1, lit;
        private bool finale;
        private float start = 1, target = 1, elapsed = 1;
        private float verifiedLimit;
        public bool ReducedMotion { get; set; }
        public bool TableMode { get; set; }
        public float Intensity { get; private set; } = 1;
        public int LitWindows => lit;
        public event Action<float> TableDimmingRequested;
        // Zero until an owner-run table-edge visibility test supplies a safe limit.
        public float VerifiedTableDimmingLimit
        {
            get => verifiedLimit;
            set { if (float.IsNaN(value) || value < 0 || value > 1) throw new ArgumentOutOfRangeException(); verifiedLimit = value; }
        }
        public void Configure(Vec3 sanctuary)
        {
            if (valley != null) return;
            valley = new GameObject("Night presentation").transform; valley.SetParent(transform, false);
            ground = new Material(Shader.Find("Standard"));
            darkWindow = new Material(Shader.Find("Standard")) { color = new Color(0.06f, 0.04f, 0.02f) };
            litWindow = new Material(Shader.Find("Standard")) { color = new Color(1, 0.8f, 0.3f) };
            litWindow.EnableKeyword("_EMISSION"); litWindow.SetColor("_EmissionColor", new Color(1, 0.5f, 0.1f));
            lightRun = new Material(Shader.Find("Sprites/Default")) { color = new Color(1, 0.75f, 0.25f) };
            environment = new GameObject("VR valley floor").transform; environment.SetParent(valley, false);
            var floor = PalmfolkView.Primitive(PrimitiveType.Cube, "Valley base", ground, environment);
            floor.localPosition = Vector3.down * 0.06f; floor.localScale = new Vector3(0.6f, 0.02f, 0.45f);
            windows = new GameObject("Five sanctuary windows").transform; windows.SetParent(valley, false);
            windows.localPosition = PalmfolkView.V(sanctuary);
            for (int i = 0; i < 5; i++)
            {
                var window = PalmfolkView.Primitive(PrimitiveType.Cube, "Window " + (i + 1), darkWindow, windows);
                window.localPosition = new Vector3((i - 2) * 0.007f, 0.02f, 0); window.localScale = new Vector3(0.005f, 0.01f, 0.002f);
            }
        }
        public void SetState(int solvedPuzzles, int litWindows, bool showFinale, IReadOnlyList<CurveTree> actualBridges)
        {
            if (solvedPuzzles < 0 || solvedPuzzles > 6 || litWindows < 0 || litWindows > 5) throw new ArgumentOutOfRangeException();
            if (solved != solvedPuzzles || finale != showFinale)
            { start = Intensity; target = showFinale ? 1 : 1 - solvedPuzzles / 6f * 0.75f; elapsed = 0; solved = solvedPuzzles; }
            lit = litWindows; finale = showFinale;
            if (actualBridges == null) bridges = Array.Empty<CurveTree>();
            else { bridges = new CurveTree[actualBridges.Count]; for (int i = 0; i < bridges.Length; i++) bridges[i] = actualBridges[i] ?? throw new ArgumentException("Null bridge."); }
        }
        public void Advance(float seconds)
        {
            if (float.IsNaN(seconds) || seconds < 0 || float.IsInfinity(seconds)) throw new ArgumentOutOfRangeException();
            elapsed = Mathf.Min(1, elapsed + seconds);
            float pulse = ReducedMotion ? 0 : Mathf.Sin(elapsed * Mathf.PI * 4) * (1 - elapsed) * 0.08f;
            Intensity = Mathf.Clamp01(Mathf.Lerp(start, target, Mathf.SmoothStep(0, 1, elapsed)) + pulse);
            if (TableMode) TableDimmingRequested?.Invoke((1 - Intensity) * verifiedLimit);
        }
        private void Update() { Advance(Time.unscaledDeltaTime); }
        public void Present(WorldPresentation frame)
            => Draw(frame.Pose, frame.Tick);
        public void Present(ValleyPresentation frame)
        {
            SetState(frame.Complete ? 6 : frame.PuzzleIndex, lit, frame.Complete || frame.Stage == DirectorStage.BramCrossing, new List<CurveTree>(frame.Bridges.Values));
            Draw(frame.Pose, frame.Tick);
        }
        private void Draw(ValleyPose pose, long tick)
        {
            if (valley == null) throw new InvalidOperationException("Configure with the actual sanctuary position first.");
            valley.gameObject.SetActive(pose != null); if (pose == null) return;
            valley.SetPositionAndRotation(PalmfolkView.V(pose.Origin), new Quaternion((float)pose.Rotation.X, (float)pose.Rotation.Y, (float)pose.Rotation.Z, (float)pose.Rotation.W));
            valley.localScale = Vector3.one * (float)pose.Scale;
            environment.gameObject.SetActive(!TableMode);
            ground.color = new Color(0.15f, 0.18f, 0.24f) * Intensity;
            windows.gameObject.SetActive(solved >= 5 || lit > 0);
            for (int i = 0; i < 5; i++) windows.GetChild(i).GetComponent<Renderer>().sharedMaterial = i < lit ? litWindow : darkWindow;
            int index = 0;
            if (finale) foreach (var tree in bridges) foreach (var curve in tree.Curves)
            {
                if (index == bridgeLights.Count)
                {
                    var line = new GameObject("Finale along saved bridge").AddComponent<LineRenderer>(); line.transform.SetParent(valley, false);
                    line.useWorldSpace = false; line.sharedMaterial = lightRun; line.startWidth = line.endWidth = 0.002f; bridgeLights.Add(line);
                }
                var renderer = bridgeLights[index++]; renderer.gameObject.SetActive(true); renderer.positionCount = curve.Points.Count;
                for (int p = 0; p < curve.Points.Count; p++) renderer.SetPosition(p, PalmfolkView.V(curve.Points[p]) + Vector3.up * 0.003f);
                float phase = ReducedMotion ? 1 : Mathf.Repeat(tick / 120f, 1);
                var gradient = new Gradient();
                gradient.SetKeys(new[] { new GradientColorKey(new Color(1, 0.6f, 0.15f), 0), new GradientColorKey(Color.white, phase), new GradientColorKey(new Color(1, 0.6f, 0.15f), 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
                renderer.colorGradient = gradient;
            }
            for (int i = index; i < bridgeLights.Count; i++) bridgeLights[i].gameObject.SetActive(false);
        }
        private void OnDestroy()
        { if (ground != null) DestroyImmediate(ground); if (darkWindow != null) DestroyImmediate(darkWindow); if (litWindow != null) DestroyImmediate(litWindow); if (lightRun != null) DestroyImmediate(lightRun); }
    }
}
#endif
