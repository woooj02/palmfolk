using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Palmfolk.Editor
{
    // Inspect the scene Unity is actually packaging, after prefab deserialization.
    public sealed class Slice0SceneChecks : IProcessSceneWithReport
    {
        public int callbackOrder => 100000;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (report == null || scene.path != Slice0Setup.ScenePath) return;
            var roots = scene.GetRootGameObjects();
            var rigs = roots.SelectMany(root => root.GetComponentsInChildren<OVRCameraRig>(true)).ToArray();
            var managers = roots.SelectMany(root => root.GetComponentsInChildren<OVRManager>(true)).ToArray();
            Require(rigs.Length == 1 && managers.Length == 1, "M1 needs exactly one SDK camera rig and one OVRManager.");
            var rig = rigs[0];
            Require(rig.gameObject.activeInHierarchy && !rig.disableEyeAnchorCameras && !rig.useFixedUpdateForTracking,
                "Enable the SDK camera rig with frame-based tracking.");
            Require(managers[0].gameObject == rig.gameObject, "OVRManager must belong to the SDK rig.");
            var origin = new SerializedObject(managers[0]).FindProperty("_trackingOriginType");
            Require(origin != null && origin.intValue == (int)OVRManager.TrackingOrigin.EyeLevel, "Use Eye Level tracking for the M1 island placement.");

            var hands = roots.SelectMany(root => root.GetComponentsInChildren<OVRHand>(true)).ToArray();
            Require(hands.Length == 2, "M1 needs exactly two SDK hand visuals.");
            CheckHand(hands, rig, OVRHand.Hand.HandLeft, "LeftHandAnchor", OVRSkeleton.SkeletonType.XRHandLeft, OVRMesh.MeshType.XRHandLeft);
            CheckHand(hands, rig, OVRHand.Hand.HandRight, "RightHandAnchor", OVRSkeleton.SkeletonType.XRHandRight, OVRMesh.MeshType.XRHandRight);
        }

        private static void CheckHand(OVRHand[] hands, OVRCameraRig rig, OVRHand.Hand side, string anchorName,
            OVRSkeleton.SkeletonType skeletonType, OVRMesh.MeshType meshType)
        {
            var matches = hands.Where(hand => new SerializedObject(hand).FindProperty("HandType").intValue == (int)side).ToArray();
            Require(matches.Length == 1, $"M1 needs one {side} hand, assigned to the correct side.");
            var hand = matches[0];
            var anchor = rig.transform.Find("TrackingSpace/" + anchorName);
            Require(anchor != null && hand.transform.parent == anchor, $"Parent {side} to its SDK hand anchor.");
            Require(hand.enabled && hand.gameObject.activeInHierarchy, $"Enable {side}.");
            var skeleton = hand.GetComponent<OVRSkeleton>();
            Require(skeleton != null && skeleton.enabled && skeleton.GetSkeletonType() == skeletonType, $"Use the correct OpenXR skeleton for {side}.");
            Require(!new SerializedObject(skeleton).FindProperty("_enablePhysicsCapsules").boolValue, "Hand physics capsules must remain off.");
            var mesh = hand.GetComponent<OVRMesh>();
            Require(mesh != null && mesh.enabled && new SerializedObject(mesh).FindProperty("_meshType").intValue == (int)meshType,
                $"Use the correct SDK mesh for {side}.");
            var visual = hand.GetComponent<OVRMeshRenderer>();
            var renderer = hand.GetComponent<SkinnedMeshRenderer>();
            Require(visual != null && visual.enabled && renderer != null && renderer.sharedMaterial != null, $"Enable the SDK hand visual and material for {side}.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new BuildFailedException("Palmfolk: " + message);
        }
    }
}
