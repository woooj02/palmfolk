using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Palmfolk.Editor
{
    public static class OwnerBuild
    {
        public static void Build()
        {
            Slice0Setup.RequireWindowsEditor();
            string revision = Environment.GetEnvironmentVariable("PALMFOLK_SOURCE_REVISION");
            if (revision == null || revision.Length != 40 || revision.Any(c => !Uri.IsHexDigit(c)))
                throw new BuildFailedException("Use tools/Build-OwnerApk.ps1 to provide the source revision.");
            string configuration = Environment.GetEnvironmentVariable("PALMFOLK_BUILD_CONFIGURATION");
            if (configuration != "Release" && configuration != "Developer") throw new BuildFailedException("Unknown build configuration.");
            bool development = configuration == "Developer";
            var symbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android).Split(';');
            if (symbols.Contains(Slice0Setup.DeveloperDefine) != development)
                throw new BuildFailedException("PALMFOLK_DEVELOPMENT must be present only for a Developer APK. Change Android scripting defines, wait for compilation, then retry.");
            string previousVersion = PlayerSettings.bundleVersion;
            string version = "0.1.0-" + revision.Substring(0, 12);
            try
            {
                PlayerSettings.bundleVersion = version;
                Directory.CreateDirectory("Builds");
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                    target = BuildTarget.Android,
                    locationPathName = "Builds/Palmfolk-" + configuration + ".apk",
                    options = development ? BuildOptions.Development : BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException("APK build failed.");
                File.WriteAllText("Builds/Palmfolk-" + configuration + ".json", JsonUtility.ToJson(new BuildEvidence {
                    revision = revision, version = version, configuration = configuration,
                    defines = symbols, development = development, unity = Application.unityVersion
                }, true));
            }
            finally { PlayerSettings.bundleVersion = previousVersion; }
        }

        [Serializable] private sealed class BuildEvidence
        {
            public string revision, version, configuration, unity;
            public string[] defines;
            public bool development;
        }
    }

    // Covers both the owner script and Unity's ordinary Build button.
    public sealed class ReleaseSymbolChecks : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;
        public void OnPreprocessBuild(BuildReport report)
        {
            if ((report.summary.options & BuildOptions.Development) != 0) return;
            var defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android).Split(';');
            if (defines.Contains(Slice0Setup.DeveloperDefine))
                throw new BuildFailedException("Release APK cannot contain PALMFOLK_DEVELOPMENT or recording code.");
        }
    }
}
