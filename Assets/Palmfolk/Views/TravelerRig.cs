#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using Palmfolk.Core;
using UnityEngine;

namespace Palmfolk.Views
{
    public enum BodyPose { Wave, Hesitate, TestPlank, Grip, Hammer, Celebrate, LookBack, Refuse, Walk }
    public enum TravelerRole { Pip, Bram, Apprentice }

    public sealed class TravelerRig : MonoBehaviour
    {
        private Transform body, head, leftArm, rightArm, leftFoot, rightFoot;
        private Material shadowMaterial;
        private BodyPose? transient;
        private long until;
        private Vec3 previous;
        private bool hasPrevious;
        public TravelerRole Role { get; set; }
        public BodyPose CurrentPose { get; private set; }
        public bool ReducedMotion { get; set; }

        public void Initialize(Material material)
        {
            if (body != null) return;
            body = transform.Find("Body"); head = transform.Find("Head");
            if (body == null) body = PalmfolkView.Primitive(PrimitiveType.Capsule, "Body", material, transform);
            if (head == null) head = PalmfolkView.Primitive(PrimitiveType.Sphere, "Head", material, transform);
            body.localScale = new Vector3(0.012f, 0.009f, 0.009f); head.localScale = Vector3.one * 0.008f;
            leftArm = Limb("Left arm", -0.007f); rightArm = Limb("Right arm", 0.007f);
            leftFoot = Foot("Left foot", -0.003f); rightFoot = Foot("Right foot", 0.003f);
            shadowMaterial = new Material(Shader.Find("Sprites/Default")) { color = new Color(0, 0, 0, 0.25f) };
            foreach (var foot in new[] { leftFoot, rightFoot })
            {
                var shadow = PalmfolkView.Primitive(PrimitiveType.Sphere, "Contact shadow", shadowMaterial, transform);
                shadow.localPosition = new Vector3(foot.localPosition.x, 0.0001f, 0);
                shadow.localScale = new Vector3(0.009f, 0.0002f, 0.01f);
            }
            Show(BodyPose.Wave, 0);

            Transform Limb(string name, float x)
            {
                var pivot = new GameObject(name).transform; pivot.SetParent(transform, false); pivot.localPosition = new Vector3(x, 0.018f, 0);
                var arm = PalmfolkView.Primitive(PrimitiveType.Cube, "Arm", material, pivot);
                arm.localPosition = Vector3.down * 0.0045f; arm.localScale = new Vector3(0.003f, 0.009f, 0.003f); return pivot;
            }
            Transform Foot(string name, float x)
            {
                var foot = PalmfolkView.Primitive(PrimitiveType.Cube, name, material, transform);
                foot.localPosition = new Vector3(x, 0.001f, 0); foot.localScale = new Vector3(0.005f, 0.002f, 0.006f); return foot;
            }
        }

        public void Consume(IReadOnlyList<GameEvent> events)
        {
            foreach (var e in events)
            {
                if (Role == TravelerRole.Pip)
                {
                    if (e.Kind == GameEventKind.Boarded) Set(BodyPose.TestPlank, e.Tick, 20);
                    if (e.Kind == GameEventKind.CheckpointReached) Set(BodyPose.LookBack, e.Tick, 30);
                    if (e.Kind == GameEventKind.BridgeBaked) Set(BodyPose.Celebrate, e.Tick, 45);
                    if (e.Kind == GameEventKind.Refused) Set(BodyPose.Refuse, e.Tick, 60);
                }
                if (Role == TravelerRole.Bram)
                {
                    if (e.Kind == GameEventKind.BramStarted) Set(BodyPose.TestPlank, e.Tick, 20);
                    if (e.Kind == GameEventKind.BramFinished) Set(BodyPose.Celebrate, e.Tick, 60);
                    if (e.Kind == GameEventKind.Refused) Set(BodyPose.Refuse, e.Tick, 60);
                }
            }
        }
        private void Set(BodyPose pose, long tick, int duration) { transient = pose; until = tick + duration; }
        public void Present(WorldPresentation frame)
        {
            Vec3 position = Role == TravelerRole.Bram ? frame.Bram : frame.Pip;
            Vec3 delta = position - previous;
            bool moving = hasPrevious && delta.Length > 0.00001;
            if (moving && Role != TravelerRole.Apprentice)
            {
                var forward = new Vector3((float)delta.X, 0, (float)delta.Z);
                if (forward.sqrMagnitude > 1e-10f) transform.localRotation = Quaternion.LookRotation(forward, Vector3.up);
            }
            previous = position; hasPrevious = true;
            BodyPose pose;
            if (frame.GhostRoute != null && Role == TravelerRole.Pip) pose = BodyPose.Grip;
            else if (transient.HasValue && frame.Tick < until) pose = transient.Value;
            else if (frame.BuildPhase == Core.BuildPhase.Building && Role == TravelerRole.Pip) pose = BodyPose.Hammer;
            else if (frame.BuildPhase == Core.BuildPhase.Paused && Role == TravelerRole.Pip) pose = BodyPose.Hesitate;
            else if (moving && Role != TravelerRole.Apprentice) pose = BodyPose.Walk;
            else if (frame.Phase == M1Phase.Complete) pose = BodyPose.Celebrate;
            else pose = Role == TravelerRole.Bram ? BodyPose.Hesitate : BodyPose.Wave;
            Show(pose, frame.Tick);
        }

        public void Show(BodyPose pose, long tick)
        {
            if (body == null) return;
            CurrentPose = pose;
            float wave = ReducedMotion ? 0 : Mathf.Sin((float)tick * 0.3f);
            body.localPosition = Vector3.up * 0.012f; body.localRotation = Quaternion.identity;
            body.localScale = new Vector3(0.012f, 0.009f, 0.009f);
            head.localPosition = Vector3.up * 0.026f; head.localRotation = Quaternion.identity;
            leftArm.localRotation = rightArm.localRotation = Quaternion.identity;
            leftFoot.localPosition = new Vector3(-0.003f, 0.001f, 0); rightFoot.localPosition = new Vector3(0.003f, 0.001f, 0);
            switch (pose)
            {
                case BodyPose.Wave: rightArm.localRotation = Quaternion.Euler(0, 0, 140 + wave * 20); break;
                case BodyPose.Hesitate: body.localRotation = Quaternion.Euler(-12, 0, 0); leftArm.localRotation = Quaternion.Euler(-35, 0, -15); break;
                case BodyPose.TestPlank: body.localRotation = Quaternion.Euler(14, 0, 0); rightFoot.localPosition += Vector3.forward * 0.006f; break;
                case BodyPose.Grip:
                    body.localScale = new Vector3(0.012f, 0.007f, 0.009f); body.localPosition = Vector3.up * 0.009f;
                    head.localPosition = Vector3.up * 0.022f; leftArm.localRotation = Quaternion.Euler(55, 0, -35); rightArm.localRotation = Quaternion.Euler(55, 0, 35); break;
                case BodyPose.Hammer: rightArm.localRotation = Quaternion.Euler(70 + wave * 35, 0, 0); head.localRotation = Quaternion.Euler(20, 0, 0); break;
                case BodyPose.Celebrate: leftArm.localRotation = Quaternion.Euler(0, 0, -150); rightArm.localRotation = Quaternion.Euler(0, 0, 150); break;
                case BodyPose.LookBack: head.localRotation = Quaternion.Euler(0, 125, 0); break;
                case BodyPose.Refuse: head.localRotation = Quaternion.Euler(0, ReducedMotion ? 35 : wave * 40, 0); leftArm.localRotation = Quaternion.Euler(0, 0, -70); break;
                case BodyPose.Walk:
                    leftFoot.localPosition += new Vector3(0, Mathf.Max(0, wave) * 0.002f, wave * 0.003f);
                    rightFoot.localPosition += new Vector3(0, Mathf.Max(0, -wave) * 0.002f, -wave * 0.003f); break;
            }
        }
        private void OnDestroy() { if (shadowMaterial != null) DestroyImmediate(shadowMaterial); }
    }
}
#endif
