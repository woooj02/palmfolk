using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Palmfolk.Editor
{
    public static class ValidationDefines
    {
        public static void Configure()
        {
            var target = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
            var defines = PlayerSettings.GetScriptingDefineSymbols(target).Split(';')
                .Where(s => !string.IsNullOrWhiteSpace(s) && s != Slice0Setup.DeveloperDefine).ToList();
            if (Environment.GetEnvironmentVariable("PALMFOLK_CHECK_DEVELOPMENT") == "true") defines.Add(Slice0Setup.DeveloperDefine);
            PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", defines));
            AssetDatabase.SaveAssets();
        }
        public static void Verify()
        {
            bool expected = Environment.GetEnvironmentVariable("PALMFOLK_CHECK_DEVELOPMENT") == "true";
            var stats = Type.GetType("Palmfolk.Developer.StatsPanel, Palmfolk.Stats");
            if ((stats != null) != expected) throw new BuildFailedException("Compiled stats assembly does not match requested development gate.");
            Debug.Log("Verified compiled stats type " + (expected ? "present" : "absent") + ".");
        }
    }
}
