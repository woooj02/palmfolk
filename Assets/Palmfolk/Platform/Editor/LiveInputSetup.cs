using System;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEditor.Android;
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
        // Slice 0's two grey boxes sat in the M1 scene beside the real valley and read as the islands on the
        // Quest 2 (Nov 13). The valley view draws every island itself, so the boxes go.
        [MenuItem("Palmfolk/Platform/4. Remove Slice 0 Greybox Islands")]
        public static void RemoveGreyboxIslands()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Palmfolk/Scenes/M1.unity");
            int removed = 0;
            var boxes = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .Where(t => t.name == "Island S" || t.name == "Island A").Select(t => t.gameObject).ToList();
            foreach (var box in boxes) { UnityEngine.Object.DestroyImmediate(box); removed++; }
            if (removed > 0) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
            Debug.Log("Palmfolk: removed " + removed + " greybox islands from M1.");
        }

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

    // Palmfolk is hands-only, so the store must offer it only where hand tracking exists. XR Hands (order 10) writes
    // oculus.software.handtracking as optional; this puts it back to required before Meta's pass (99999) and
    // Slice0BuildChecks (100000) inspect the manifest. Unity adds microphone, Bluetooth and network-state permissions
    // because Meta's voice and platform packages reference those APIs, and Meta's dev-agent library brings a screen
    // capture service with foreground-service permissions. Palmfolk uses none of them, so they are marked removed for
    // the manifest merge (Test-Slice0Apk.ps1 checks the merged APK against its allowlist). Meta's Platform SDK (order 1)
    // also demands the hzplatformclientcore Horizon OS supplement, which the Quest 2 on v74 lacks, so the install is
    // refused; Palmfolk makes no Platform SDK calls, so that requirement is dropped too.
    public sealed class HandsOnlyManifest : IPostGenerateGradleAndroidProject
    {
        private const string AndroidNamespace = "http://schemas.android.com/apk/res/android";
        private const string ToolsNamespace = "http://schemas.android.com/tools";
        private const string HorizonNamespace = "http://schemas.horizonos/sdk";
        private const string PlatformSupplement = "horizonos-supplement-hzplatformclientcore";
        private static readonly string[] Unused =
        {
            "android.permission.RECORD_AUDIO", "android.permission.MODIFY_AUDIO_SETTINGS",
            "android.permission.BLUETOOTH", "android.permission.ACCESS_NETWORK_STATE",
            "android.permission.FOREGROUND_SERVICE", "android.permission.FOREGROUND_SERVICE_MEDIA_PROJECTION"
        };
        private static readonly string[] UnusedServices = { "com.meta.agenticxr.AgenticMediaProjectionService" };
        public int callbackOrder => 50000;
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var manifestPath = Path.Combine(path, "src/main/AndroidManifest.xml");
            if (!File.Exists(manifestPath)) return;
            var doc = new XmlDocument(); doc.Load(manifestPath);
            bool changed = false;
            foreach (XmlElement feature in doc.SelectNodes("/manifest/uses-feature").OfType<XmlElement>())
            {
                if (feature.GetAttribute("name", AndroidNamespace) != "oculus.software.handtracking" || feature.GetAttribute("required", AndroidNamespace) == "true") continue;
                feature.SetAttribute("required", AndroidNamespace, "true"); changed = true;
            }
            var root = doc.DocumentElement; var application = (XmlElement)doc.SelectSingleNode("/manifest/application");
            foreach (var name in Unused) changed |= Remove(doc, root, "uses-permission", name);
            if (application != null) foreach (var name in UnusedServices) changed |= Remove(doc, application, "service", name);
            foreach (var supplement in root.ChildNodes.OfType<XmlElement>().Where(e => e.LocalName == "uses-horizonos-supplement" && e.NamespaceURI == HorizonNamespace
                && e.GetAttribute("name") == PlatformSupplement).ToList())
            { root.RemoveChild(supplement); changed = true; }
            if (changed) doc.Save(manifestPath);
        }

        // Marks the entry removed, adding a removal marker if only a library declares it.
        private static bool Remove(XmlDocument doc, XmlElement parent, string tag, string name)
        {
            var entry = parent.ChildNodes.OfType<XmlElement>().FirstOrDefault(e => e.Name == tag && e.GetAttribute("name", AndroidNamespace) == name);
            if (entry != null && entry.GetAttribute("node", ToolsNamespace) == "remove") return false;
            if (entry == null)
            {
                entry = doc.CreateElement(tag); entry.SetAttribute("name", AndroidNamespace, name);
                if (tag == "uses-permission") parent.PrependChild(entry); else parent.AppendChild(entry);
            }
            entry.SetAttribute("node", ToolsNamespace, "remove");
            return true;
        }
    }

    // Meta's SDK packs its dev-agent OpenXR layer (XR_APILAYER_METAX_operator) into every Development build. On the
    // Quest 2 it sits between the game and the runtime and OpenXR fails to start (xrPollEvent unsupported, no display),
    // and it brings a screen capture service. Palmfolk never uses it: leave the AAR out of every Android build.
    // Runs after Meta's own processor (order 0), which includes it for Development builds.
    public sealed class ExcludeMetaOperatorLayer : IPreprocessBuildWithReport
    {
        private const string AarFileName = "XrApiLayer_METAX_operator_unity_android";
        public int callbackOrder => 1;
        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android) return;
            foreach (var importer in PluginImporter.GetAllImporters())
            {
                if (!importer.assetPath.Contains(AarFileName)) continue;
                importer.SetIncludeInBuildDelegate(_ => false);
                Debug.Log("Palmfolk: leaving Meta's operator OpenXR layer out of the APK.");
            }
        }
    }
}
