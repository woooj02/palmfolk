using System;
using Palmfolk.Core;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace Palmfolk.Platform
{
    // One XR Hands joint as read from the runtime, in tracking space.
    public struct RawJoint
    {
        public Pose Pose;
        public float Radius;
        public XRHandJointTrackingState State;
        public RawJoint(Pose pose, float radius, XRHandJointTrackingState state) { Pose = pose; Radius = radius; State = state; }
    }

    // Which joints count as confidently tracked (H3). XR Hands marks a joint HighFidelityPose when the runtime tracks
    // it rather than infers it. Not every runtime reports that flag, so until a hand has reported it once, a valid
    // pose counts as confident; from then on, only high-fidelity joints do. `ReportsFidelity` goes in the H3 log.
    public sealed class ConfidencePolicy
    {
        private readonly bool[] seen = new bool[2];
        public bool ReportsFidelity(HandSide side) => seen[(int)side];
        public void Reset() { seen[0] = seen[1] = false; }
        public JointFlags Flags(HandSide side, XRHandJointTrackingState state)
        {
            if ((state & XRHandJointTrackingState.HighFidelityPose) != 0) seen[(int)side] = true;
            if ((state & XRHandJointTrackingState.Pose) == 0) return JointFlags.None;
            var flags = JointFlags.PositionValid | JointFlags.RotationValid;
            if (!seen[(int)side] || (state & XRHandJointTrackingState.HighFidelityPose) != 0) flags |= JointFlags.HighConfidence;
            if ((state & XRHandJointTrackingState.Radius) != 0) flags |= JointFlags.RadiusValid;
            return flags;
        }
    }

    // The explicit mapping from XR Hands joints to Core's (T1). Core's JointId order is Palm, Wrist, then each
    // finger metacarpal to tip; XR Hands orders Wrist before Palm, so the table is written out, not cast.
    public static class HandJointMap
    {
        public static readonly XRHandJointID[] ByCoreJoint =
        {
            XRHandJointID.Palm, XRHandJointID.Wrist,
            XRHandJointID.ThumbMetacarpal, XRHandJointID.ThumbProximal, XRHandJointID.ThumbDistal, XRHandJointID.ThumbTip,
            XRHandJointID.IndexMetacarpal, XRHandJointID.IndexProximal, XRHandJointID.IndexIntermediate, XRHandJointID.IndexDistal, XRHandJointID.IndexTip,
            XRHandJointID.MiddleMetacarpal, XRHandJointID.MiddleProximal, XRHandJointID.MiddleIntermediate, XRHandJointID.MiddleDistal, XRHandJointID.MiddleTip,
            XRHandJointID.RingMetacarpal, XRHandJointID.RingProximal, XRHandJointID.RingIntermediate, XRHandJointID.RingDistal, XRHandJointID.RingTip,
            XRHandJointID.LittleMetacarpal, XRHandJointID.LittleProximal, XRHandJointID.LittleIntermediate, XRHandJointID.LittleDistal, XRHandJointID.LittleTip,
        };

        public const int JointCount = 26;

        // Reads a hand's joints into `joints` (Core order). Returns false if the hand isn't tracked.
        public static bool Read(XRHand hand, RawJoint[] joints)
        {
            if (joints == null || joints.Length != JointCount) throw new ArgumentException("Need one slot per Core joint.");
            if (!hand.isTracked) return false;
            for (int i = 0; i < JointCount; i++)
            {
                var joint = hand.GetJoint(ByCoreJoint[i]);
                joint.TryGetPose(out var pose); joint.TryGetRadius(out float radius);
                joints[i] = new RawJoint(pose, radius, joint.trackingState);
            }
            return true;
        }

        // Converts tracking-space joints to a world-space HandSample. Joints with a non-finite pose or radius are
        // marked invalid rather than passed on. Radii are scaled with the tracking space.
        public static HandSample Convert(HandSide side, RawJoint[] joints, Matrix4x4 trackingToWorld, ConfidencePolicy policy) =>
            Convert(joints, trackingToWorld, policy, new HandSampleBuffer(side)).Copy();

        // D11: converts into a buffer the reader keeps; the returned sample is refilled by the next call.
        public static HandSample Convert(RawJoint[] joints, Matrix4x4 trackingToWorld, ConfidencePolicy policy, HandSampleBuffer into)
        {
            if (joints == null || joints.Length != JointCount || policy == null || into == null) throw new ArgumentException("Need one joint per Core joint, a policy and a buffer.");
            var side = into.Side;
            float scale = trackingToWorld.lossyScale.x; var turn = trackingToWorld.rotation;
            for (int i = 0; i < JointCount; i++)
            {
                var raw = joints[i];
                var flags = policy.Flags(side, raw.State);
                var p = trackingToWorld.MultiplyPoint3x4(raw.Pose.position); var q = turn * raw.Pose.rotation;
                bool finite = Finite(p.x) && Finite(p.y) && Finite(p.z) && Finite(q.x) && Finite(q.y) && Finite(q.z) && Finite(q.w);
                double radius = raw.Radius * scale;
                if (!Finite((float)radius) || radius <= 0) { radius = 0; flags &= ~JointFlags.RadiusValid; }
                if (!finite) { into.Set(i, new JointSample(Vec3.Zero, Rotation.Identity, 0, JointFlags.None)); continue; }
                into.Set(i, new JointSample(new Vec3(p.x, p.y, p.z), new Rotation(q.x, q.y, q.z, q.w), radius, flags));
            }
            return into.Sample;
        }

        private static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
    }
}
