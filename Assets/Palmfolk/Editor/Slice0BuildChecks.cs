using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

namespace Palmfolk.Editor
{
    // Runs for both the Palmfolk menu and Unity's normal Build button.
    public sealed class Slice0BuildChecks : IPreprocessBuildWithReport, IPostGenerateGradleAndroidProject
    {
        // Meta's manifest callback is 99999; inspect its output afterwards.
        public int callbackOrder => 100000;
        private const string AndroidNamespace = "http://schemas.android.com/apk/res/android";

        public void OnPreprocessBuild(BuildReport report)
        {
            Slice0Setup.RequireWindowsEditor();
#if !ENABLE_INPUT_SYSTEM
            throw new BuildFailedException("Palmfolk: set Active Input Handling to Input System Package (New) in Player settings and restart Unity.");
#else
            Require(report.summary.platform == BuildTarget.Android, "Build target must be Android.");
            Require(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) == "com.keepsrunning.palmfolk", "Unexpected package identifier.");
            Require(PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) == ScriptingImplementation.IL2CPP, "Use IL2CPP.");
            Require(PlayerSettings.Android.targetArchitectures == AndroidArchitecture.ARM64, "Use ARM64 only.");
            Require(!PlayerSettings.Android.forceInternetPermission, "Disable Force Internet Permission.");
            Require(PlayerSettings.Android.applicationEntry == AndroidApplicationEntry.Activity, "Use the Activity entry point.");
            Require(File.Exists(Slice0Setup.ScenePath), "Create the M1 scene before building.");
            var enabledScenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();
            Require(enabledScenes.Length == 1 && enabledScenes[0].path == Slice0Setup.ScenePath, "M1 must be the only enabled build scene.");

            var general = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            Require(general != null && general.InitManagerOnStart && general.Manager != null, "Enable Android XR at startup.");
            Require(general.Manager.activeLoaders.Count == 1 && general.Manager.activeLoaders[0] is OpenXRLoader, "Enable only the Android OpenXR loader.");
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            Require(settings != null && settings.GetFeature<MetaQuestFeature>() != null && settings.GetFeature<MetaQuestFeature>().enabled,
                "Enable Meta Quest Support in Android OpenXR settings.");
            Require(settings.GetFeatures<OpenXRFeature>().Any(feature => feature.GetType().FullName == "Meta.XR.MetaXRFeature" && feature.enabled),
                "Enable Meta XR Feature in Android OpenXR settings.");
            Require(settings.GetFeature<HandInteractionProfile>() != null && settings.GetFeature<HandInteractionProfile>().enabled,
                "Enable Hand Interaction Profile in Android OpenXR settings.");
            var config = OVRProjectConfig.CachedProjectConfig;
            Require(config.handTrackingSupport == OVRProjectConfig.HandTrackingSupport.HandsOnly, "Set Meta hand tracking to Hands Only.");
            Require(config.handTrackingFrequency == OVRProjectConfig.HandTrackingFrequency.HIGH, "Set hand tracking frequency to High.");
            Require(OVRRuntimeSettings.Instance.HandSkeletonVersion == OVRHandSkeletonVersion.OpenXR, "Use the OpenXR hand skeleton.");
#endif
        }

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            // This is the generated library manifest, not yet Gradle's merged APK
            // manifest. Test-Slice0Apk.ps1 separately checks the actual APK.
            var manifestPath = Path.Combine(path, "src/main/AndroidManifest.xml");
            Require(File.Exists(manifestPath), "Generated Android manifest is missing.");
            var doc = new XmlDocument();
            doc.Load(manifestPath);
            var handFeature = Find(doc, "uses-feature", "oculus.software.handtracking");
            Require(handFeature != null && handFeature.GetAttribute("required", AndroidNamespace) == "true", "Manifest must require hand tracking.");
            Require(Find(doc, "uses-permission", "com.oculus.permission.HAND_TRACKING") != null, "Manifest must declare hand tracking permission.");
            var frequency = Find(doc, "application/meta-data", "com.oculus.handtracking.frequency");
            Require(frequency != null && frequency.GetAttribute("value", AndroidNamespace) == "HIGH", "Manifest must request High hand tracking frequency.");
            var internet = Find(doc, "uses-permission", "android.permission.INTERNET");
            Require(internet == null || internet.GetAttribute("node", "http://schemas.android.com/tools") == "remove", "Manifest must not grant Internet access.");
        }

        private static XmlElement Find(XmlDocument document, string path, string name)
        {
            return document.SelectNodes("/manifest/" + path).OfType<XmlElement>()
                .FirstOrDefault(element => element.GetAttribute("name", AndroidNamespace) == name);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new BuildFailedException("Palmfolk: " + message);
        }
    }
}
