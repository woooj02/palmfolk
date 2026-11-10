using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

namespace Palmfolk.Platform.Editor
{
    // D6 setup: XR Hands needs its OpenXR feature on, and the scene needs a PalmfolkRunner. Run after
    // Palmfolk > Slice 0 > Configure Android and Create Scene.
    public static class LiveInputSetup
    {
        public const string HandTrackingFeature = "UnityEngine.XR.Hands.OpenXR.HandTracking";
        public const string RecordingType = "Palmfolk.Platform.Developer.SessionRecording, Palmfolk.Platform.Developer";
        public const string ProbeType = "Palmfolk.Platform.Developer.CoreProbe, Palmfolk.Platform.Developer";

        [MenuItem("Palmfolk/Platform/1. Enable XR Hands")]
        public static void EnableHands()
        {
            foreach (var group in new[] { BuildTargetGroup.Android, BuildTargetGroup.Standalone })
            {
                FeatureHelpers.RefreshFeatures(group);
                var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
                if (settings == null) { if (group == BuildTargetGroup.Android) throw new BuildFailedException("No Android OpenXR settings. Run Palmfolk > Slice 0 > 1. Configure Android first."); continue; }
                var feature = Feature(settings) ?? throw new BuildFailedException("The XR Hands OpenXR feature is missing. Check that com.unity.xr.hands finished importing.");
                feature.enabled = true; EditorUtility.SetDirty(feature); EditorUtility.SetDirty(settings);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("XR Hands hand tracking is enabled for Android and the editor.");
        }

        public static bool HandsEnabled(BuildTargetGroup group)
        {
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
            var feature = settings == null ? null : Feature(settings);
            return feature != null && feature.enabled;
        }

        private static OpenXRFeature Feature(OpenXRSettings settings) =>
            settings.GetFeatures<OpenXRFeature>().FirstOrDefault(f => f != null && f.GetType().FullName == HandTrackingFeature);

        // Adds the runner (and, in developer builds, session recording) to the open scene, wired to the camera rig's
        // TrackingSpace and a new Valley Origin. Never replaces an existing runner.
        [MenuItem("Palmfolk/Platform/2. Add Runner To Open Scene")]
        public static PalmfolkRunner AddRunner()
        {
            var existing = UnityEngine.Object.FindAnyObjectByType<PalmfolkRunner>();
            if (existing != null) { Selection.activeObject = existing; Debug.Log("The scene already has a PalmfolkRunner."); return existing; }
            var tracking = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == "TrackingSpace");
            if (tracking == null) Debug.LogWarning("No TrackingSpace found: hand poses will be read as world space. Add the OVRCameraRig first (Palmfolk > Slice 0 > 2. Create Scene).");
            var origin = new GameObject("Valley Origin").transform;
            var go = new GameObject("Palmfolk Runner");
            var runner = go.AddComponent<PalmfolkRunner>();
            var serialized = new SerializedObject(runner);
            serialized.FindProperty("trackingSpace").objectReferenceValue = tracking;
            serialized.FindProperty("valleyRoot").objectReferenceValue = origin;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            // Developer builds: session recording (consent off until ticked) and the core probe.
            foreach (var name in new[] { RecordingType, ProbeType })
            {
                var type = Type.GetType(name); if (type == null) continue;
                var component = go.AddComponent(type);
                var s = new SerializedObject(component); s.FindProperty("runner").objectReferenceValue = runner; s.ApplyModifiedPropertiesWithoutUndo();
            }
            Undo.RegisterCreatedObjectUndo(go, "Add Palmfolk Runner"); Undo.RegisterCreatedObjectUndo(origin.gameObject, "Add Valley Origin");
            EditorSceneManager.MarkSceneDirty(go.scene);
            Selection.activeObject = go;
            return runner;
        }

        // D9: Table mode needs passthrough (Supported, not Required, so VR still runs without it) and spatial
        // anchors in the Android manifest, and MetaTableMode next to the runner. Run after 2. Add Runner.
        [MenuItem("Palmfolk/Platform/3. Enable Table Mode")]
        public static Palmfolk.Platform.Meta.MetaTableMode EnableTableMode()
        {
            var config = OVRProjectConfig.CachedProjectConfig;
            config.insightPassthroughSupport = OVRProjectConfig.FeatureSupport.Supported;
            config.anchorSupport = OVRProjectConfig.AnchorSupport.Enabled;
            OVRProjectConfig.CommitProjectConfig(config);
            AssetDatabase.SaveAssets();
            return AddTableMode();
        }

        // Adds MetaTableMode to the scene's runner (once).
        public static Palmfolk.Platform.Meta.MetaTableMode AddTableMode()
        {
            var runner = UnityEngine.Object.FindAnyObjectByType<PalmfolkRunner>();
            if (runner == null) { Debug.LogWarning("Passthrough and anchors are on. Add the runner (2. Add Runner To Open Scene), then run this again."); return null; }
            var mode = runner.GetComponent<Palmfolk.Platform.Meta.MetaTableMode>();
            if (mode == null)
            {
                mode = Undo.AddComponent<Palmfolk.Platform.Meta.MetaTableMode>(runner.gameObject);
                var s = new SerializedObject(mode); s.FindProperty("runner").objectReferenceValue = runner; s.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(runner.gameObject.scene);
            }
            Selection.activeObject = runner.gameObject;
            Debug.Log("Table mode: passthrough supported, spatial anchors on, MetaTableMode on the runner.");
            return mode;
        }

        public static bool TableModeEnabled()
        {
            var config = OVRProjectConfig.CachedProjectConfig;
            return config.insightPassthroughSupport != OVRProjectConfig.FeatureSupport.None && config.anchorSupport == OVRProjectConfig.AnchorSupport.Enabled;
        }
    }

    // An Android build without XR Hands would run with no hands at all: stop it with the fix.
    public sealed class LiveInputBuildCheck : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android) return;
            if (!LiveInputSetup.HandsEnabled(BuildTargetGroup.Android))
                throw new BuildFailedException("XR Hands hand tracking is off for Android. Run Palmfolk > Platform > 1. Enable XR Hands, then build again.");
        }
    }
}
