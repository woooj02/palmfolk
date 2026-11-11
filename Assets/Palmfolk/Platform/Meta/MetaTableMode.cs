using System;
using System.Collections.Generic;
using Palmfolk.Core;
using UnityEngine;

namespace Palmfolk.Platform.Meta
{
    // D9 and D10: the Meta XR SDK behind the runner's interfaces. Add MetaTableMode next to the runner
    // (Palmfolk > Platform > 3. Enable Table Mode); it hands the runner passthrough, anchors and gaze in Awake,
    // before the runner opens the game in Start.
    [DefaultExecutionOrder(-100)]
    public sealed class MetaTableMode : MonoBehaviour
    {
        [SerializeField] private PalmfolkRunner runner;

        public MetaPassthrough Passthrough { get; private set; }
        public MetaSpatialAnchors Anchors { get; private set; }
        public MetaGaze Gaze { get; private set; }

        private void Awake()
        {
            if (runner == null) runner = GetComponent<PalmfolkRunner>();
            if (runner == null) runner = FindAnyObjectByType<PalmfolkRunner>();
            if (runner == null) { Debug.LogWarning("MetaTableMode: no PalmfolkRunner in the scene."); return; }
            var rig = FindAnyObjectByType<OVRCameraRig>();
            var layer = rig == null ? null : rig.GetComponent<OVRPassthroughLayer>();
            // OVRPassthroughLayer composites as an underlay by itself.
            if (rig != null && layer == null) layer = rig.gameObject.AddComponent<OVRPassthroughLayer>();
            if (layer != null) layer.hidden = true;
            var eye = rig != null && rig.centerEyeAnchor != null ? rig.centerEyeAnchor.GetComponent<Camera>() : Camera.main;
            Passthrough = new MetaPassthrough(layer, eye);
            Anchors = new MetaSpatialAnchors();
            Gaze = new MetaGaze(FindAnyObjectByType<Oculus.Interaction.Input.EyeGaze>(), eye != null ? eye.transform : null);
            runner.Passthrough = Passthrough; runner.Anchors = Anchors; runner.Gaze = Gaze;
        }

        private void OnDestroy() => Anchors?.Release();
    }

    // Passthrough through OVRManager and an underlay OVRPassthroughLayer. While visible, the eye camera clears to
    // transparent so the room shows behind the valley; hidden, the camera's own background (the night valley) is back.
    public sealed class MetaPassthrough : IPassthrough
    {
        private readonly OVRPassthroughLayer layer;
        private readonly Camera camera;
        private readonly CameraClearFlags flags;
        private readonly Color background;

        public MetaPassthrough(OVRPassthroughLayer layer, Camera camera)
        {
            this.layer = layer; this.camera = camera;
            if (camera != null) { flags = camera.clearFlags; background = camera.backgroundColor; }
        }

        public bool Available => layer != null && OVRManager.instance != null && OVRManager.IsInsightPassthroughSupported() && !OVRManager.HasInsightPassthroughInitFailed();
        public bool Visible { get; private set; }

        public void SetVisible(bool visible)
        {
            Visible = visible && layer != null;
            if (OVRManager.instance != null) OVRManager.instance.isInsightPassthroughEnabled = Visible;
            if (layer != null) layer.hidden = !Visible;
            if (camera == null) return;
            camera.clearFlags = Visible ? CameraClearFlags.SolidColor : flags;
            camera.backgroundColor = Visible ? new Color(0, 0, 0, 0) : background;
        }
    }

    // GZ3: the Interaction SDK's EyeGaze when the scene has one (its pose says whether it is eye-tracked or
    // synthesized from the camera), otherwise the head camera's forward, reported as Head.
    public sealed class MetaGaze : IGazeReader
    {
        private readonly Oculus.Interaction.Input.EyeGaze eye;
        private readonly Transform head;
        public MetaGaze(Oculus.Interaction.Input.EyeGaze eye, Transform head) { this.eye = eye; this.head = head; }

        public GazeSample Read()
        {
            if (eye != null && eye.isActiveAndEnabled && eye.TryGetWorldPose(out var pose))
                return new GazeSample(V(pose.position), V(pose.forward), eye.IsSyntheticPose ? GazeSource.Head : GazeSource.Eyes);
            return head == null ? null : new GazeSample(V(head.position), V(head.forward), GazeSource.Head);
        }

        private static Vec3 V(Vector3 v) => new Vec3(v.x, v.y, v.z);
    }

    // Local spatial anchors (OVRSpatialAnchor): saved on the headset, found by UUID. The anchor marks the valley
    // origin and faces the valley's forward.
    public sealed class MetaSpatialAnchors : ISpatialAnchors
    {
        private readonly Dictionary<string, GameObject> held = new Dictionary<string, GameObject>();

        public void Save(Vec3 position, Vec3 forward, Action<string> done)
        {
            var go = new GameObject("Valley Anchor");
            go.transform.SetPositionAndRotation(ToUnity(position), Facing(forward));
            var anchor = go.AddComponent<OVRSpatialAnchor>();
            anchor.WhenLocalizedAsync().ContinueWith(localized =>
            {
                if (!localized || anchor == null) { UnityEngine.Object.Destroy(go); done(null); return; }
                anchor.SaveAnchorAsync().ContinueWith(result =>
                {
                    if (!result.Success || anchor == null) { UnityEngine.Object.Destroy(go); done(null); return; }
                    var id = anchor.Uuid.ToString("D"); held[id] = go; done(id);
                });
            });
        }

        public void Find(string id, Action<bool, Vec3, Vec3> done)
        {
            if (!Guid.TryParse(id, out var uuid)) { done(false, Vec3.Zero, Vec3.Zero); return; }
            var unbound = new List<OVRSpatialAnchor.UnboundAnchor>();
            OVRSpatialAnchor.LoadUnboundAnchorsAsync(new[] { uuid }, unbound).ContinueWith(result =>
            {
                if (!result.Success || unbound.Count == 0) { done(false, Vec3.Zero, Vec3.Zero); return; }
                var found = unbound[0];
                found.LocalizeAsync().ContinueWith(localized =>
                {
                    if (!localized || !found.TryGetPose(out var pose)) { done(false, Vec3.Zero, Vec3.Zero); return; }
                    var go = new GameObject("Valley Anchor");
                    go.transform.SetPositionAndRotation(pose.position, pose.rotation);
                    found.BindTo(go.AddComponent<OVRSpatialAnchor>()); held[id] = go;
                    var f = pose.rotation * Vector3.forward;
                    done(true, new Vec3(pose.position.x, pose.position.y, pose.position.z), new Vec3(f.x, 0, f.z));
                });
            });
        }

        public void Erase(string id)
        {
            if (!Guid.TryParse(id, out var uuid)) return;
            if (held.TryGetValue(id, out var go)) { held.Remove(id); if (go != null) UnityEngine.Object.Destroy(go); }
            OVRSpatialAnchor.EraseAnchorsAsync(Array.Empty<OVRSpatialAnchor>(), new[] { uuid });
        }

        // Destroys the anchor objects (the saved anchors stay on the headset).
        public void Release()
        {
            foreach (var go in held.Values) if (go != null) UnityEngine.Object.Destroy(go);
            held.Clear();
        }

        private static Vector3 ToUnity(Vec3 v) => new Vector3((float)v.X, (float)v.Y, (float)v.Z);
        private static Quaternion Facing(Vec3 forward) => Quaternion.Euler(0, (float)(Math.Atan2(forward.X, forward.Z) * 180 / Math.PI), 0);
    }
}
