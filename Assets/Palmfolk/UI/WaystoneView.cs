#if UNITY_5_3_OR_NEWER
using System;
using Oculus.Interaction;
using Oculus.Interaction.Surfaces;
using Palmfolk.Core;
using Palmfolk.Views;
using UnityEngine;

namespace Palmfolk.UI
{
    public enum WaystoneAction { Pause, Resume, Restart, Replant, Mode, Stillness, Scale, ReducedMotion, Captions, HighContrast, XRay, Bell, Quit }
    public enum WaystonePage { Main, Journey, More, Settings, Access }
    public readonly struct WaystoneRequest
    {
        public readonly WaystoneAction Action;
        public readonly double Value;
        public readonly HandSide? Hand;
        public readonly bool RestingPinch;
        public WaystoneRequest(WaystoneAction action, double value, HandSide? hand, bool restingPinch)
        { Action = action; Value = value; Hand = hand; RestingPinch = restingPinch; }
    }

    // Input port: publishes requests; never changes a simulation, save, mode or application lifecycle.
    public sealed class WaystoneView : MonoBehaviour
    {
        private readonly Transform[] targets = new Transform[4];
        private readonly TextMesh[] labels = new TextMesh[4];
        private readonly CircleSurface[] surfaces = new CircleSurface[4];
        private readonly Vector2[] spans = new Vector2[4];
        private Material wood;
        private GameSettings settings = new GameSettings();
        private bool paused, reduced, captions = true, contrast, xray, table;
        public WaystonePage Page { get; private set; }
        public event Action<WaystoneRequest> Requested;
        public void Reflect(GameSettings actual, bool isPaused, bool reducedMotion, bool captionsOn, bool highContrast, bool xrayOn, bool tableMode)
        { settings = actual.Copy(); paused = isPaused; reduced = reducedMotion; captions = captionsOn; contrast = highContrast; xray = xrayOn; table = tableMode; Ensure(); Refresh(); }
        public void Open(WaystonePage page) { Page = page; Ensure(); Refresh(); }
        public Vector2 TargetSpan(int slot) => spans[slot];
        public void LayoutForViewer(Vector3 viewerWorld)
        {
            var forward = transform.position - viewerWorld;
            if (forward.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.LookRotation(forward, Mathf.Abs(Vector3.Dot(forward.normalized, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up);
            Layout(forward.magnitude / Mathf.Max(0.0001f, transform.lossyScale.x));
        }
        public void Layout(float localViewingDistance)
        {
            if (localViewingDistance <= 0 || float.IsNaN(localViewingDistance) || float.IsInfinity(localViewingDistance)) throw new ArgumentOutOfRangeException();
            Ensure();
            for (int i = 0; i < 4; i++)
            {
                float centreDegrees = (i - 1.5f) * 7;
                float left = localViewingDistance * Mathf.Tan((centreDegrees - 3) * Mathf.Deg2Rad);
                float right = localViewingDistance * Mathf.Tan((centreDegrees + 3) * Mathf.Deg2Rad);
                spans[i] = new Vector2(left, right); float width = right - left;
                targets[i].localPosition = new Vector3((left + right) / 2, 0, 0);
                var face = targets[i].Find("Carved target"); face.localScale = new Vector3(width, 0.002f, width);
                surfaces[i].InjectOptionalRadius(width / 2);
                labels[i].characterSize = width / 22;
            }
        }
        public bool RequestSlot(int slot, HandSide? hand = null, bool restingPinch = false, bool palmFacesHeadset = false)
        {
            if (slot < 0 || slot >= 4 || (restingPinch && palmFacesHeadset)) return false;
            WaystoneAction action; double value = 0;
            switch (Page)
            {
                case WaystonePage.Main:
                    if (slot == 2) { Open(WaystonePage.Journey); return true; }
                    if (slot == 3) { Open(WaystonePage.Settings); return true; }
                    action = slot == 0 ? (paused ? WaystoneAction.Resume : WaystoneAction.Pause) : WaystoneAction.Replant; break;
                case WaystonePage.Journey:
                    if (slot == 2) { Open(WaystonePage.More); return true; }
                    if (slot == 3) { Open(WaystonePage.Main); return true; }
                    action = slot == 0 ? WaystoneAction.Restart : WaystoneAction.Bell; break;
                case WaystonePage.More:
                    if (slot == 3) { Open(WaystonePage.Main); return true; }
                    action = slot == 0 ? WaystoneAction.Mode : slot == 1 ? WaystoneAction.XRay : WaystoneAction.Quit;
                    value = slot == 0 ? (table ? 0 : 1) : slot == 1 ? (xray ? 0 : 1) : 0; break;
                case WaystonePage.Settings:
                    if (slot == 2) { Open(WaystonePage.Access); return true; }
                    if (slot == 3) { Open(WaystonePage.Main); return true; }
                    action = slot == 0 ? WaystoneAction.Stillness : WaystoneAction.Scale;
                    value = slot == 0 ? (settings.StillnessToleranceStep + 1) % 3 : settings.WorldScaleAdjustment >= 2 ? 0.5 : Math.Min(2, settings.WorldScaleAdjustment + 0.25); break;
                default:
                    if (slot == 3) { Open(WaystonePage.Settings); return true; }
                    action = slot == 0 ? WaystoneAction.ReducedMotion : slot == 1 ? WaystoneAction.Captions : WaystoneAction.HighContrast;
                    value = (slot == 0 ? reduced : slot == 1 ? captions : contrast) ? 0 : 1; break;
            }
            if (Requested == null) return false;
            Requested(new WaystoneRequest(action, value, hand, restingPinch)); return true;
        }
        private void Ensure()
        {
            if (wood != null) return;
            wood = new Material(Shader.Find("Standard")) { color = new Color(0.45f, 0.29f, 0.15f) };
            var post = PalmfolkView.Primitive(PrimitiveType.Cube, "Waystone post", wood, transform); post.localPosition = Vector3.down * 0.04f; post.localScale = new Vector3(0.02f, 0.08f, 0.02f);
            for (int i = 0; i < 4; i++)
            {
                int slot = i; var target = new GameObject("Waystone target " + i); target.transform.SetParent(transform, false); targets[i] = target.transform;
                var face = PalmfolkView.Primitive(PrimitiveType.Cylinder, "Carved target", wood, target.transform); face.localRotation = Quaternion.Euler(90, 0, 0);
                var plane = target.AddComponent<PlaneSurface>(); plane.InjectAllPlaneSurface(PlaneSurface.NormalFacing.Backward, false);
                surfaces[i] = target.AddComponent<CircleSurface>(); surfaces[i].InjectAllCircleSurface(plane);
                var poke = target.AddComponent<PokeInteractable>(); poke.InjectAllPokeInteractable(surfaces[i]);
                poke.WhenPointerEventRaised += pointer => { if (pointer.Type == PointerEventType.Select) RequestSlot(slot); };
                labels[i] = new GameObject("Carved label").AddComponent<TextMesh>(); labels[i].transform.SetParent(target.transform, false);
                labels[i].transform.localPosition = new Vector3(0, 0, -0.005f); labels[i].anchor = TextAnchor.MiddleCenter; labels[i].fontSize = 32;
            }
            Layout(0.4f); Refresh();
        }
        private void Refresh()
        {
            string[] words;
            switch (Page)
            {
                case WaystonePage.Main: words = new[] { paused ? "Resume" : "Pause", "Replant", "Journey", "Settings" }; break;
                case WaystonePage.Journey: words = new[] { "Restart", "Bell", "More", "Back" }; break;
                case WaystonePage.More: words = new[] { table ? "VR mode" : "Table mode", "X-ray", "Quit", "Back" }; break;
                case WaystonePage.Settings: words = new[] { "Tolerance\n" + (settings.StillnessToleranceStep + 1) + "/3", "Scale\n" + settings.WorldScaleAdjustment.ToString("0.##") + "x", "Access", "Back" }; break;
                default: words = new[] { "Reduced\nmotion", "Captions", "High\ncontrast", "Back" }; break;
            }
            for (int i = 0; i < 4; i++) labels[i].text = words[i];
        }
        private void OnDestroy() { if (wood != null) DestroyImmediate(wood); }
    }
}
#endif
