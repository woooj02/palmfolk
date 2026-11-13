#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using Palmfolk.Core;
using UnityEngine;

namespace Palmfolk.Views
{
    // Core supplies the observed recovery hand and a passing hint tree. These original stylised meshes are
    // display-only: a hint is a shape illustration along that tree, never a fabricated XR hand sample.
    public sealed class HandGuidanceView : MonoBehaviour, IValleyView
    {
        private Transform content, ghostRoot, hintRoot;
        private PalmfolkView.ShapePool ghost, hint, routeDots, motion;
        private Material carved, translucent, accent;
        private HandSample drawnGhost;
        private SolutionHint drawnHint;
        private HandSide drawnSide;
        private bool requested;
        private bool? contrasted;
        public bool ReducedMotion { get; set; }
        public bool HighContrast { get; set; }
        public HandSide PreferredHand { get; set; } = HandSide.Left;
        public int GhostJoints { get; private set; }
        public HintShape? ShowingHint { get; private set; }
        public Transform GhostRoot => ghostRoot;
        public Transform HintRoot => hintRoot;
        public void Consume(IReadOnlyList<GameEvent> events)
        {
            foreach (var e in events)
            {
                if (e.Kind == GameEventKind.HintDue) requested = true;
                else if (e.Kind == GameEventKind.Boarded || e.Kind == GameEventKind.CheckpointReached || e.Kind == GameEventKind.BuildStarted ||
                    e.Kind == GameEventKind.BridgeBaked || e.Kind == GameEventKind.PuzzleStarted || e.Kind == GameEventKind.VisitorCalled || e.Kind == GameEventKind.ReplantStarted)
                    requested = false;
            }
        }
        public void Present(ValleyPresentation frame)
        {
            Ensure(); content.gameObject.SetActive(frame.Pose != null); if (frame.Pose == null) return;
            content.SetPositionAndRotation(PalmfolkView.V(frame.Pose.Origin), Quaternion.Euler(0, (float)(frame.Pose.YawRadians * 180 / Math.PI), 0));
            content.localScale = Vector3.one * (float)frame.Pose.Scale;
            if (contrasted != HighContrast)
            {
                contrasted = HighContrast; carved.shader = Resources.Load<Material>("Palmfolk/" + (HighContrast ? "HighContrast" : "StandardOpaque")).shader;
                carved.color = HighContrast ? Color.white : new Color(0.72f, 0.67f, 0.53f);
                translucent.color = HighContrast ? new Color(1, 1, 1, 0.7f) : new Color(0.65f, 0.9f, 1, 0.35f);
            }
            bool showGhost = frame.Recovering && frame.GhostHand != null && frame.PlantingStage == ValleyStage.Playing;
            ghostRoot.gameObject.SetActive(showGhost); GhostJoints = 0;
            if (showGhost)
            {
                if (!ReferenceEquals(drawnGhost, frame.GhostHand)) { drawnGhost = frame.GhostHand; DrawGhost(drawnGhost); }
                foreach (var joint in drawnGhost.Joints) if (joint.Usable) GhostJoints++;
            }
            else drawnGhost = null;
            var side = frame.HandOwner ?? PreferredHand;
            if (!ReferenceEquals(drawnHint, frame.Hint) || drawnSide != side)
            {
                if (drawnHint != null && !ReferenceEquals(drawnHint, frame.Hint)) requested = false;
                drawnHint = frame.Hint; drawnSide = side;
                if (drawnHint != null) DrawHint(drawnHint, side);
            }
            bool showHint = requested && drawnHint != null && !frame.Recovering && frame.PlantingStage == ValleyStage.Playing && !frame.Paused;
            hintRoot.gameObject.SetActive(showHint); ShowingHint = showHint ? (HintShape?)drawnHint.Shape : null;
            hintRoot.localPosition = Vector3.up * (ReducedMotion ? 0 : Mathf.Sin(frame.Tick / 30f) * 0.0025f);
            motion.Begin();
            if (showHint)
                foreach (var curve in drawnHint.Tree.Curves)
                { double phase = ReducedMotion ? 0.5 : (frame.Tick % 120) / 120d; motion.Sphere(PalmfolkView.V(Sample(curve, phase)), 0.004f, accent); }
            motion.End();
        }
        private void DrawGhost(HandSample hand)
        {
            ghost.Begin();
            for (int i = 0; i < hand.Joints.Count; i++)
            {
                var joint = hand.Joints[i]; if (!joint.Usable) continue;
                float diameter = joint.HasRadius ? Mathf.Clamp((float)joint.Radius * 2, 0.003f, 0.012f) : 0.006f;
                ghost.Sphere(PalmfolkView.V(joint.Position), diameter, carved);
            }
            int[] starts = { 2, 6, 11, 16, 21 }, ends = { 5, 10, 15, 20, 25 };
            for (int finger = 0; finger < starts.Length; finger++)
            {
                Join(1, starts[finger]);
                for (int j = starts[finger]; j < ends[finger]; j++) Join(j, j + 1);
            }
            Join(0, 1);
            if (hand[JointId.Wrist].Usable && hand[JointId.IndexProximal].Usable && hand[JointId.LittleProximal].Usable)
            {
                var wrist = hand[JointId.Wrist].Position; var knuckles = (hand[JointId.IndexProximal].Position + hand[JointId.LittleProximal].Position) / 2;
                Vec3 forward = knuckles - wrist; float width = (float)Vec3.Distance(hand[JointId.IndexProximal].Position, hand[JointId.LittleProximal].Position) + 0.006f;
                var normal = HandRoutes.PalmNormal(hand);
                if (forward.Length > 1e-6 && normal.HasValue) ghost.Box(PalmfolkView.V((wrist + knuckles) / 2), new Vector3(width, 0.006f, (float)forward.Length), Facing(forward, normal.Value), carved);
            }
            ghost.End();
            void Join(int a, int b) { if (hand.Joints[a].Usable && hand.Joints[b].Usable) Bone(ghost, hand.Joints[a].Position, hand.Joints[b].Position, 0.005f, carved, false); }
        }
        private void DrawHint(SolutionHint solution, HandSide side)
        {
            hint.Begin(); routeDots.Begin();
            var trunk = solution.Shape == HintShape.CarryFinger ? solution.Tree.Curves[0].Reverse() : solution.Tree.Curves[0]; Vec3 start = trunk.Start;
            Vec3 forward = (trunk.End - trunk.Start).Normalized, across = Vec3.Cross(Vec3.Up, forward).Normalized;
            if (across.Length < 1e-6) across = new Vec3(1, 0, 0);
            double width = Math.Max(0.025, Math.Min(0.06, trunk.MinimumWidth));
            bool spread = solution.Shape == HintShape.Spread;
            Vec3 palmEnd = spread ? trunk.End : Sample(trunk, 0.3);
            hint.Box(PalmfolkView.V((start + palmEnd) / 2), new Vector3((float)width, 0.003f, (float)Vec3.Distance(start, palmEnd)), Facing(palmEnd - start, Vec3.Up), translucent);
            // A broken outline and separate beads distinguish the hint from the solid carved recovery hand.
            Bone(hint, start - across * width / 2, palmEnd - across * width / 2, 0.0025f, accent, true);
            Bone(hint, start + across * width / 2, palmEnd + across * width / 2, 0.0025f, accent, true);
            bool single = solution.Shape == HintShape.OneFinger || solution.Shape == HintShape.CarryFinger;
            for (int digit = 0; digit < 4; digit++)
            {
                if (single && digit > 0)
                {
                    Vec3 a = Sample(trunk, 0.15) + across * ((digit - 1) * 0.007 + 0.007);
                    Bone(hint, a, a + forward * 0.015 + Vec3.Up * 0.008, 0.003f, translucent, true);
                    Bone(hint, a + forward * 0.015 + Vec3.Up * 0.008, a + forward * 0.008, 0.003f, translucent, true);
                    continue;
                }
                Route curve = trunk; double lane = single ? 0 : (digit - 1.5) * width / 4;
                double from = 0.3;
                if (spread && solution.Tree.Curves.Count > 1)
                { int branch = Math.Min(solution.Tree.Curves.Count - 2, digit * (solution.Tree.Curves.Count - 1) / 4); curve = solution.Tree.Curves[branch + 1]; lane = (digit % 2 == 0 ? -1 : 1) * 0.003; from = 0; }
                Vec3 previous = Point(curve, from, lane);
                for (int joint = 1; joint <= 4; joint++)
                { var next = Point(curve, from + (1 - from) * joint / 4, lane); Bone(hint, previous, next, 0.003f, translucent, true); hint.Sphere(PalmfolkView.V(next), 0.004f, accent); previous = next; }
            }
            double thumbSide = side == HandSide.Left ? -1 : 1;
            Vec3 thumbBase = Sample(trunk, 0.12) + across * thumbSide * width / 2;
            Vec3 thumbKnuckle = thumbBase + across * thumbSide * 0.014 + forward * 0.01;
            Bone(hint, thumbBase, thumbKnuckle, 0.003f, translucent, true); Bone(hint, thumbKnuckle, thumbKnuckle + forward * 0.015, 0.003f, translucent, true);
            foreach (var curve in solution.Tree.Curves) foreach (var point in curve.Points) routeDots.Sphere(PalmfolkView.V(point), 0.0018f, accent);
            hint.End(); routeDots.End();
        }
        private static Vec3 Point(Route route, double t, double lane)
        {
            double p = Vec3.Clamp(t, 0, 1) * (route.Points.Count - 1); int i = Math.Min(route.Points.Count - 2, (int)p);
            return Sample(route, t) + Vec3.Cross(Vec3.Up, route.Points[i + 1] - route.Points[i]).Normalized * lane;
        }
        private static Vec3 Sample(Route route, double t)
        { double p = Vec3.Clamp(t, 0, 1) * (route.Points.Count - 1); int i = Math.Min(route.Points.Count - 2, (int)p); return route.Evaluate(new RouteCursor(i, p - i)); }
        private static Quaternion Facing(Vec3 direction, Vec3 normal)
        { var d = PalmfolkView.V(direction).normalized; var up = PalmfolkView.V(normal).normalized; if (up.sqrMagnitude < 1e-6f || Mathf.Abs(Vector3.Dot(d, up)) > 0.99f) up = Mathf.Abs(Vector3.Dot(d, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up; return Quaternion.LookRotation(d, up); }
        private static void Bone(PalmfolkView.ShapePool pool, Vec3 a, Vec3 b, float width, Material material, bool dotted)
        {
            double length = Vec3.Distance(a, b); if (length < 1e-6) return;
            if (dotted) { int count = Math.Max(2, Math.Min(40, (int)Math.Ceiling(length / 0.004))); for (int i = 0; i <= count; i++) pool.Sphere(PalmfolkView.V(Vec3.Lerp(a, b, i / (double)count)), width, material); }
            else pool.Box(PalmfolkView.V((a + b) / 2), new Vector3(width, width, (float)length), Facing(b - a, Vec3.Up), material);
        }
        private void Ensure()
        {
            if (content != null) return;
            content = new GameObject("Hand guidance").transform; content.SetParent(transform, false);
            ghostRoot = new GameObject("Carved recovery hand").transform; ghostRoot.SetParent(content, false);
            hintRoot = new GameObject("Moving solution hand").transform; hintRoot.SetParent(content, false);
            ghost = new PalmfolkView.ShapePool(ghostRoot, "Observed joints and bones"); hint = new PalmfolkView.ShapePool(hintRoot, "Illustrated hand shape");
            routeDots = new PalmfolkView.ShapePool(hintRoot, "Passing route dots"); motion = new PalmfolkView.ShapePool(hintRoot, "Travelling hint beads");
            carved = PalmfolkView.CreateMaterial(new Color(0.72f, 0.67f, 0.53f)); translucent = PalmfolkView.CreateMaterial(new Color(0.65f, 0.9f, 1, 0.35f));
            accent = new Material(Resources.Load<Material>("Palmfolk/Lines")) { color = new Color(0.9f, 0.98f, 1, 0.85f) };
        }
        private void OnDestroy() { if (carved != null) DestroyImmediate(carved); if (translucent != null) DestroyImmediate(translucent); if (accent != null) DestroyImmediate(accent); }
    }
}
#endif
