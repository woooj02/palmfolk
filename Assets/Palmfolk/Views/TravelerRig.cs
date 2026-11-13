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
        private bool hasContacts;
        private readonly FootPlanting contacts = new FootPlanting();
        private Transform leftShadow, rightShadow;
        private ValleyPose contactPose = ValleyPose.Identity;
        public event System.Action<Vec3> FootContact;
        public TravelerRole Role { get; set; }
        public BodyPose CurrentPose { get; private set; }
        public bool ReducedMotion { get; set; }
        public Route ContactSupport { get; private set; }
        public RouteCursor? ContactCursor { get; private set; }
        public Vec3 LeftContact => contacts.Left;
        public Vec3 RightContact => contacts.Right;

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
                if (foot == leftFoot) leftShadow = shadow; else rightShadow = shadow;
            }
            contacts.Landed += point => FootContact?.Invoke(contactPose.ToWorld(point));
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
                    if (e.Kind == GameEventKind.BridgeBaked || e.Kind == GameEventKind.LedgeWidened) Set(BodyPose.Celebrate, e.Tick, 45);
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
            PresentAt(position, frame.Tick, frame.GhostRoute, frame.BuildPhase, frame.Phase == M1Phase.Complete);
            Route support = Role == TravelerRole.Pip ? frame.GhostRoute ?? frame.FilteredHandRoute : null;
            if (support == null && frame.Bridge != null) support = Nearest(frame.Bridge.Curves, position);
            contactPose = frame.Pose ?? ValleyPose.Identity; PlantFeet(support, position, frame.Tick);
        }
        public void Present(ValleyPresentation frame)
        {
            bool bram = Role == TravelerRole.Bram;
            PresentAt(bram ? frame.Bram : frame.Pip, frame.Tick, bram || frame.RecoveryPhase == RecoveryPhase.Returning ? null : frame.GhostRoute,
                frame.Stage == DirectorStage.Widening ? Core.BuildPhase.Building : frame.BuildPhase, frame.Complete);
            contactPose = frame.Pose ?? ValleyPose.Identity;
            if (bram) { PlantFeet(frame.BramWalk?.Support, frame.Bram, frame.Tick, frame.BramWalk?.Support == null ? null : (RouteCursor?)frame.BramWalk.SupportCursor); return; }
            if (frame.PipCursor.HasValue) { PlantFeet(frame.FilteredHandRoute, frame.Pip, frame.Tick, frame.PipCursor, true); return; }
            // Recovery freezes the last stance with Pip. No new landing is emitted while the owning hand is lost.
            if (frame.Recovering && frame.RecoveryPhase != RecoveryPhase.Returning && hasContacts) { ApplyFeet(frame.Pip, ContactSupport != null); return; }
            // The builder's committed scaffold and the old M1 port have no traveler cursor. Only those legacy
            // presentation paths retain projection; valley travelers never choose support by proximity.
            var scaffold = frame.Scaffold ?? frame.GhostRoute;
            PlantFeet(scaffold, frame.Pip, frame.Tick);
        }
        public void PresentTraveler(TravelerView traveler, long tick, ValleyPose pose, double lane = 0)
        {
            Vec3 side = new Vec3(1, 0, 0);
            if (traveler.Support != null)
            { int segment = traveler.SupportCursor.Segment; side = Vec3.Cross(Vec3.Up, traveler.Support.Points[segment + 1] - traveler.Support.Points[segment]).Normalized; }
            Vec3 position = traveler.Position + side * lane;
            transform.localPosition = PalmfolkView.V(position); PresentAt(position, tick, null, null, traveler.Arrived);
            contactPose = pose ?? ValleyPose.Identity;
            PlantFeet(traveler.Support, position, tick, traveler.Support == null ? null : (RouteCursor?)traveler.SupportCursor, false, lane);
        }
        public void PresentTraveler(Vec3 position, long tick, bool arrived, ValleyPose pose, IReadOnlyDictionary<string, CurveTree> bridges)
        {
            PresentAt(position, tick, null, null, arrived); Route support = null;
            foreach (var tree in bridges.Values)
            {
                var candidate = Nearest(tree.Curves, position);
                if (candidate != null && (support == null || FootPlanting.Closest(candidate, position, out _) < FootPlanting.Closest(support, position, out _))) support = candidate;
            }
            contactPose = pose ?? ValleyPose.Identity; PlantFeet(support, position, tick);
        }
        private static Route Nearest(IReadOnlyList<Route> routes, Vec3 position)
        {
            Route best = null; double distance = 0.015;
            foreach (var route in routes) { double d = FootPlanting.Closest(route, position, out _); if (d < distance) { distance = d; best = route; } }
            return best;
        }
        private void PlantFeet(Route support, Vec3 position, long tick, RouteCursor? cursor = null, bool movingHand = false, double lane = 0)
        {
            ContactSupport = support; ContactCursor = cursor; hasContacts = true;
            Vector3 facing = transform.localRotation * Vector3.forward;
            if (support != null && cursor.HasValue) contacts.UpdateSupported(support, cursor.Value, position, new Vec3(facing.x, facing.y, facing.z), tick,
                CurrentPose == BodyPose.Walk || CurrentPose == BodyPose.TestPlank, movingHand, lane);
            else contacts.Update(support, position, new Vec3(facing.x, facing.y, facing.z), tick, CurrentPose == BodyPose.Walk || CurrentPose == BodyPose.TestPlank);
            if (support != null)
            {
                Vector3 forward = PalmfolkView.V(contacts.Forward); forward.y = 0;
                if (forward.sqrMagnitude > 1e-10f) transform.localRotation = Quaternion.LookRotation(forward, Vector3.up);
            }
            ApplyFeet(position, support != null);
        }
        private void ApplyFeet(Vec3 position, bool supported)
        {
            Quaternion inverse = Quaternion.Inverse(transform.localRotation);
            Vector3 surfaceForward = PalmfolkView.V(contacts.Forward).normalized;
            Vector3 up = !supported ? Vector3.up : Vector3.Cross(surfaceForward, Vector3.Cross(Vector3.up, surfaceForward)).normalized;
            Quaternion tilt = inverse * Quaternion.LookRotation(surfaceForward.sqrMagnitude > 1e-10f ? surfaceForward : Vector3.forward, up);
            leftFoot.localRotation = rightFoot.localRotation = leftShadow.localRotation = rightShadow.localRotation = tilt;
            leftFoot.localPosition = inverse * (PalmfolkView.V(contacts.Left - position) + up * 0.001f);
            rightFoot.localPosition = inverse * (PalmfolkView.V(contacts.Right - position) + up * 0.001f);
            leftShadow.localPosition = inverse * (PalmfolkView.V(contacts.Left - position) + up * 0.0001f);
            rightShadow.localPosition = inverse * (PalmfolkView.V(contacts.Right - position) + up * 0.0001f);
        }
        public void PresentAt(Vec3 position, long tick, Route ghost, BuildPhase? buildPhase, bool complete)
        {
            Vec3 delta = position - previous;
            bool moving = hasPrevious && delta.Length > 0.00001;
            if (moving && Role != TravelerRole.Apprentice)
            {
                var forward = new Vector3((float)delta.X, 0, (float)delta.Z);
                if (forward.sqrMagnitude > 1e-10f) transform.localRotation = Quaternion.LookRotation(forward, Vector3.up);
            }
            previous = position; hasPrevious = true;
            BodyPose pose;
            if (ghost != null && Role == TravelerRole.Pip) pose = BodyPose.Grip;
            else if (transient.HasValue && tick < until) pose = transient.Value;
            else if (buildPhase == Core.BuildPhase.Building && Role == TravelerRole.Pip) pose = BodyPose.Hammer;
            else if (buildPhase == Core.BuildPhase.Paused && Role == TravelerRole.Pip) pose = BodyPose.Hesitate;
            else if (moving && Role != TravelerRole.Apprentice) pose = BodyPose.Walk;
            else if (complete) pose = BodyPose.Celebrate;
            else pose = Role == TravelerRole.Bram ? BodyPose.Hesitate : BodyPose.Wave;
            Show(pose, tick);
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
