#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using Palmfolk.Core;
using Palmfolk.Views;
using UnityEngine;
using UnityEngine.Rendering;

namespace Palmfolk.UI
{
    public sealed class StartupView : MonoBehaviour, IPalmfolkView
    {
        private Transform startup, seed, palm, controllers;
        private TextMesh startupText, qualityText;
        private Material wood, glow, ghost;
        private ValleyStage? previous;
        private long demoStart;
        private bool quality;
        private TravelerRig invitationRig;
        public bool ReducedMotion { get; set; }
        public bool ShowingControllers => controllers != null && controllers.gameObject.activeSelf;
        public string Prompt => startupText == null ? "" : startupText.text;
        public void Consume(IReadOnlyList<GameEvent> events)
        {
            foreach (var e in events)
            {
                if (e.Kind == GameEventKind.QualityPromptDue) quality = true;
                if (e.Kind == GameEventKind.RecoveryCompleted || e.Kind == GameEventKind.Planted) quality = false;
            }
        }
        public void Present(WorldPresentation frame)
        {
            Ensure();
            bool planting = frame.Stage == ValleyStage.Planting;
            if (previous != frame.Stage) demoStart = frame.Tick;
            previous = frame.Stage;
            startup.gameObject.SetActive(planting);
            if (planting)
            {
                invitationRig.ReducedMotion = ReducedMotion; invitationRig.Show(BodyPose.Wave, frame.Tick);
                float t = Mathf.Clamp01((frame.Tick - demoStart) / 90f);
                palm.localPosition = new Vector3(0, ReducedMotion ? -0.045f : Mathf.Lerp(0.025f, -0.045f, t), 0.025f);
                seed.localScale = Vector3.one * (1 + (float)frame.PlantProgress * 0.3f);
                controllers.gameObject.SetActive(frame.PutControllersDown);
                float lowering = ReducedMotion ? 1 : Mathf.Clamp01(Mathf.Repeat((frame.Tick - demoStart) / 60f, 2));
                controllers.localPosition = Vector3.down * lowering * 0.06f;
                startupText.text = frame.ShowStartupText ? (frame.PutControllersDown ? "Set down your controllers to use your hands." : "Bring a hand into view.") : "";
                startupText.gameObject.SetActive(frame.ShowStartupText);
            }
            else { startupText.text = ""; controllers.gameObject.SetActive(false); }
            qualityText.text = quality ? "Hand tracking works best in a well-lit room, with your hand in view." : "";
            qualityText.gameObject.SetActive(quality);
        }
        private void Ensure()
        {
            if (startup != null) return;
            wood = new Material(Shader.Find("Standard")) { color = new Color(0.5f, 0.3f, 0.14f) };
            glow = new Material(Shader.Find("Standard")) { color = new Color(1, 0.78f, 0.3f) };
            glow.EnableKeyword("_EMISSION"); glow.SetColor("_EmissionColor", new Color(0.4f, 0.2f, 0.03f));
            ghost = new Material(Shader.Find("Standard")) { color = new Color(0.8f, 0.85f, 0.9f, 0.3f) };
            ghost.SetFloat("_Mode", 3); ghost.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha); ghost.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            ghost.SetInt("_ZWrite", 0); ghost.EnableKeyword("_ALPHABLEND_ON"); ghost.renderQueue = 3000;
            startup = new GameObject("Planting invitation").transform; startup.SetParent(transform, false);
            seed = new GameObject("Seed lantern").transform; seed.SetParent(startup, false);
            Part(seed, PrimitiveType.Sphere, "Seed", Vector3.zero, new Vector3(0.025f, 0.03f, 0.025f), glow);
            Part(seed, PrimitiveType.Cube, "Lantern cap", Vector3.up * 0.017f, new Vector3(0.023f, 0.004f, 0.023f), wood);
            var leaf = Part(seed, PrimitiveType.Cube, "Leaf", new Vector3(0.012f, 0.023f, 0), new Vector3(0.018f, 0.003f, 0.009f), wood);
            leaf.localRotation = Quaternion.Euler(0, 0, 30);
            var scout = new GameObject("Waving Pip").transform; scout.SetParent(seed, false); scout.localPosition = new Vector3(-0.035f, -0.01f, 0);
            invitationRig = scout.gameObject.AddComponent<TravelerRig>(); invitationRig.Initialize(wood); invitationRig.Show(BodyPose.Wave, 0);
            palm = new GameObject("One flat-palm demonstration").transform; palm.SetParent(startup, false);
            Part(palm, PrimitiveType.Cube, "Palm", Vector3.zero, new Vector3(0.055f, 0.005f, 0.065f), ghost);
            for (int i = 0; i < 4; i++) Part(palm, PrimitiveType.Cube, "Finger", new Vector3((i - 1.5f) * 0.014f, 0, 0.057f), new Vector3(0.011f, 0.005f, 0.05f), ghost);
            var thumb = Part(palm, PrimitiveType.Cube, "Thumb", new Vector3(-0.037f, 0, 0.008f), new Vector3(0.013f, 0.005f, 0.035f), ghost); thumb.localRotation = Quaternion.Euler(0, -40, 0);
            controllers = new GameObject("Controller put-down demonstration").transform; controllers.SetParent(startup, false);
            for (int side = -1; side <= 1; side += 2)
            {
                Part(controllers, PrimitiveType.Capsule, "Controller handle", new Vector3(side * 0.06f, 0.04f, -0.015f), new Vector3(0.018f, 0.022f, 0.018f), wood);
                Part(controllers, PrimitiveType.Sphere, "Controller head", new Vector3(side * 0.06f, 0.062f, -0.015f), new Vector3(0.03f, 0.008f, 0.035f), wood);
            }
            startupText = Text("Startup text", new Vector3(0, -0.095f, 0), startup);
            qualityText = Text("Tracking-quality prompt", new Vector3(0, -0.12f, 0), transform);
        }
        private static Transform Part(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 size, Material material)
        { var p = PalmfolkView.Primitive(type, name, material, parent); p.localPosition = position; p.localScale = size; return p; }
        private static TextMesh Text(string name, Vector3 position, Transform parent)
        {
            var text = new GameObject(name).AddComponent<TextMesh>(); text.transform.SetParent(parent, false); text.transform.localPosition = position;
            text.anchor = TextAnchor.MiddleCenter; text.characterSize = 0.002f; text.fontSize = 32; return text;
        }
        private void OnDestroy()
        {
            if (wood != null) DestroyImmediate(wood); if (glow != null) DestroyImmediate(glow); if (ghost != null) DestroyImmediate(ghost);
        }
    }
}
#endif
