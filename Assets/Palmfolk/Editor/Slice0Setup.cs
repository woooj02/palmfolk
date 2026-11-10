using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

namespace Palmfolk.Editor
{
    // Generates Palmfolk-owned scene objects; SDK prefabs stay in their package.
    public static class Slice0Setup
    {
        public const string ScenePath = "Assets/Palmfolk/Scenes/M1.unity";
        public const string DeveloperDefine = "PALMFOLK_DEVELOPMENT";
        public const string BuildVersion = "0.1.0-slice0";
        private const string CorePackage = "Packages/com.meta.xr.sdk.core/";
        private const string OpenXRLoaderType = "UnityEngine.XR.OpenXR.OpenXRLoader";
        private static readonly NamedBuildTarget Android = NamedBuildTarget.Android;

        [MenuItem("Palmfolk/Slice 0/1. Configure Android")]
        public static void ConfigureAndroid()
        {
            RequireWindowsEditor();
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new BuildFailedException("Switch to Android in File > Build Profiles, wait for import, then run Configure Android again.");

            PlayerSettings.companyName = "Keepsrunning";
            PlayerSettings.productName = "Palmfolk";
            PlayerSettings.bundleVersion = BuildVersion;
            PlayerSettings.SetApplicationIdentifier(Android, "com.keepsrunning.palmfolk");
            PlayerSettings.SetScriptingBackend(Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;
            PlayerSettings.Android.forceInternetPermission = false;
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            EditorSettings.serializationMode = SerializationMode.ForceText;

            ConfigureOpenXR();
            var config = OVRProjectConfig.CachedProjectConfig;
            config.handTrackingSupport = OVRProjectConfig.HandTrackingSupport.HandsOnly;
            config.handTrackingFrequency = OVRProjectConfig.HandTrackingFrequency.HIGH;
            config.targetDeviceTypes.Clear();
            config.targetDeviceTypes.Add(OVRProjectConfig.DeviceType.Quest2);
            config.targetDeviceTypes.Add(OVRProjectConfig.DeviceType.Quest3);
            config.targetDeviceTypes.Add(OVRProjectConfig.DeviceType.Quest3S);
            OVRProjectConfig.CommitProjectConfig(config);

            var runtime = OVRRuntimeSettings.Instance;
            runtime.HandSkeletonVersion = OVRHandSkeletonVersion.OpenXR;
            OVRRuntimeSettings.CommitRuntimeSettings(runtime);
            AssetDatabase.SaveAssets();
            // Set defines last: changing them can trigger a domain reload.
            // This is the single gate for future recording and diagnostics (T10).
            var defines = PlayerSettings.GetScriptingDefineSymbols(Android).Split(';')
                .Where(value => !string.IsNullOrWhiteSpace(value)).ToList();
            if (!defines.Contains(DeveloperDefine))
            {
                defines.Add(DeveloperDefine);
                PlayerSettings.SetScriptingDefineSymbols(Android, string.Join(";", defines));
            }
            Debug.Log("Palmfolk slice 0 configured. Wait for compilation, then create the scene. No signing key or Meta app was created.");
        }

        private static void ConfigureOpenXR()
        {
            const string settingsPath = "Assets/XR/XRGeneralSettingsPerBuildTarget.asset";
            Directory.CreateDirectory("Assets/XR");
            AssetDatabase.Refresh();
            if (!EditorBuildSettings.TryGetConfigObject<XRGeneralSettingsPerBuildTarget>(XRGeneralSettings.k_SettingsKey, out var perTarget) || perTarget == null)
            {
                perTarget = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(settingsPath);
                if (perTarget == null)
                {
                    perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                    AssetDatabase.CreateAsset(perTarget, settingsPath);
                }
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);
            }
            if (!perTarget.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))
                perTarget.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);

            var general = perTarget.SettingsForBuildTarget(BuildTargetGroup.Android);
            general.InitManagerOnStart = true;
            var manager = general.Manager;
            // Fail rather than silently keep another provider alongside OpenXR.
            if (manager.activeLoaders.Any(loader => loader.GetType().FullName != OpenXRLoaderType))
                throw new BuildFailedException("Remove other Android XR loaders in XR Plug-in Management before configuring Palmfolk.");
            if (!XRPackageMetadataStore.AssignLoader(manager, OpenXRLoaderType, BuildTargetGroup.Android))
                throw new BuildFailedException("Could not enable the Android OpenXR loader.");

            FeatureHelpers.RefreshFeatures(BuildTargetGroup.Android);
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (settings == null) throw new BuildFailedException("Could not create Android OpenXR settings. Check the first Unity Console error.");
            settings.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
            settings.depthSubmissionMode = OpenXRSettings.DepthSubmissionMode.None;
            EnableFeature(settings, typeof(MetaQuestFeature).FullName);
            settings.GetFeature<MetaQuestFeature>().ForceRemoveInternetPermission = true;
            EnableFeature(settings, "Meta.XR.MetaXRFeature");
            EnableFeature(settings, typeof(HandInteractionProfile).FullName);
            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(general);
            EditorUtility.SetDirty(manager);
            EditorUtility.SetDirty(perTarget);
        }

        private static void EnableFeature(OpenXRSettings settings, string typeName)
        {
            var feature = settings.GetFeatures<OpenXRFeature>().FirstOrDefault(item => item != null && item.GetType().FullName == typeName);
            if (feature == null)
                throw new BuildFailedException($"Missing OpenXR feature {typeName}. Verify the pinned packages finished importing.");
            feature.enabled = true;
            EditorUtility.SetDirty(feature);
        }

        [MenuItem("Palmfolk/Slice 0/2. Create Scene")]
        public static void CreateScene()
        {
            RequireWindowsEditor();
            // Never overwrite scene work when this command is rerun.
            if (File.Exists(ScenePath))
                throw new BuildFailedException($"{ScenePath} already exists. Open it instead; scene generation does not overwrite it.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory("Assets/Palmfolk/Scenes");
            Directory.CreateDirectory("Assets/Palmfolk/Materials");
            AssetDatabase.Refresh();

            var rigPrefab = LoadPrefab("Prefabs/OVRCameraRig.prefab");
            var handPrefab = LoadPrefab("Prefabs/OVRHandPrefab.prefab");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var rigObject = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
            var rig = rigObject.GetComponent<OVRCameraRig>();
            rig.EnsureGameObjectIntegrity();
            var manager = rigObject.GetComponent<OVRManager>();
            manager.trackingOriginType = OVRManager.TrackingOrigin.EyeLevel;
            PrefabUtility.RecordPrefabInstancePropertyModifications(manager);
            AddHand(handPrefab, rig.leftHandAnchor, "Left Hand", OVRHand.Hand.HandLeft, OVRSkeleton.SkeletonType.XRHandLeft, OVRMesh.MeshType.XRHandLeft);
            AddHand(handPrefab, rig.rightHandAnchor, "Right Hand", OVRHand.Hand.HandRight, OVRSkeleton.SkeletonType.XRHandRight, OVRMesh.MeshType.XRHandRight);

            foreach (var camera in rigObject.GetComponentsInChildren<Camera>(true))
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.055f, 0.065f, 0.08f);
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 20f;
                PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
            }
            var light = new GameObject("Scene Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.9f;
            light.shadows = LightShadows.None;
            light.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.4f, 0.4f, 0.4f);
            RenderSettings.skybox = null;

            const string materialPath = "Assets/Palmfolk/Materials/Greybox.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard")) { color = new Color(0.45f, 0.48f, 0.5f) };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            // Metres: inner edges at x=-0.06 and +0.06 give the M1 12 cm gap.
            AddIsland("Island S", new Vector3(-0.11f, -0.22f, 0.4f), material);
            AddIsland("Island A", new Vector3(0.11f, -0.22f, 0.4f), material);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new BuildFailedException("Could not save the M1 scene.");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Palmfolk M1 scene created: SDK hand visuals and two islands. Next run project validation, then Build APK. Quest 2 behavior is unverified.");
        }

        private static GameObject LoadPrefab(string relativePath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CorePackage + relativePath);
            if (prefab == null) throw new BuildFailedException($"Missing SDK prefab: {relativePath}. Verify Core SDK 207.0.0 finished importing.");
            return prefab;
        }

        private static void AddHand(GameObject prefab, Transform anchor, string name, OVRHand.Hand handType,
            OVRSkeleton.SkeletonType skeletonType, OVRMesh.MeshType meshType)
        {
            var hand = (GameObject)PrefabUtility.InstantiatePrefab(prefab, anchor);
            hand.name = name;
            hand.transform.localPosition = Vector3.zero;
            hand.transform.localRotation = Quaternion.identity;
            hand.transform.localScale = Vector3.one;
            PrefabUtility.RecordPrefabInstancePropertyModifications(hand);
            PrefabUtility.RecordPrefabInstancePropertyModifications(hand.transform);
            // These SDK fields are serialized but not publicly settable in v207.
            SetSerializedInt(hand.GetComponent<OVRHand>(), "HandType", (int)handType);
            var skeleton = hand.GetComponent<OVRSkeleton>();
            SetSerializedInt(skeleton, "_skeletonType", (int)skeletonType);
            var serialized = new SerializedObject(skeleton);
            serialized.FindProperty("_enablePhysicsCapsules").boolValue = false;
            serialized.FindProperty("_updateRootScale").boolValue = true;
            serialized.FindProperty("_applyBoneTranslations").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SetSerializedInt(hand.GetComponent<OVRMesh>(), "_meshType", (int)meshType);
        }

        private static void SetSerializedInt(UnityEngine.Object target, string field, int value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            if (property == null) throw new BuildFailedException($"SDK field changed: {target.GetType().Name}.{field}");
            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddIsland(string name, Vector3 position, Material material)
        {
            var island = GameObject.CreatePrimitive(PrimitiveType.Cube);
            island.name = name;
            island.transform.position = position;
            island.transform.localScale = new Vector3(0.1f, 0.04f, 0.16f);
            UnityEngine.Object.DestroyImmediate(island.GetComponent<Collider>());
            island.GetComponent<Renderer>().sharedMaterial = material;
        }

        [MenuItem("Palmfolk/Slice 0/3. Build APK")]
        public static void BuildApk()
        {
            RequireWindowsEditor();
            if (!File.Exists(ScenePath)) throw new BuildFailedException("Run Create Scene first.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory("Builds");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Palmfolk-slice0.apk",
                target = BuildTarget.Android,
                // No network profiler or script debugging: this slice is offline.
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("Slice 0 APK failed. Fix the first build error before testing.");
        }

        internal static void RequireWindowsEditor()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
                throw new BuildFailedException("Palmfolk's Meta SDK setup and builds run on Windows (T11). Linux is for source preparation only.");
            if (Application.unityVersion != "6000.0.66f2")
                throw new BuildFailedException("Open this checkout with the pinned Unity version 6000.0.66f2.");
        }
    }
}
