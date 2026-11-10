using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace Palmfolk.Editor
{
    public static class PresentationMaterials
    {
        private const string Folder = "Assets/Palmfolk/Art/Resources/Palmfolk";
        [MenuItem("Palmfolk/Presentation/Create material templates")]
        public static void Create()
        {
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            Add("StandardOpaque", "Standard", false, false);
            Add("StandardTransparent", "Standard", true, false);
            Add("Glow", "Standard", false, true);
            Add("HighContrast", "Unlit/Color", false, false);
            Add("Lines", "Sprites/Default", false, false);
            AssetDatabase.SaveAssets(); Verify();
        }
        private static void Add(string name, string shaderName, bool transparent, bool glow)
        {
            string path = Folder + "/" + name + ".mat";
            if (File.Exists(path)) return; // Never overwrite reviewed art.
            var shader = Shader.Find(shaderName); if (shader == null) throw new BuildFailedException("Missing built-in shader " + shaderName);
            var material = new Material(shader) { color = transparent ? new Color(1, 1, 1, 0.3f) : Color.white };
            if (transparent)
            {
                material.SetFloat("_Mode", 3); material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha); material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0); material.EnableKeyword("_ALPHABLEND_ON"); material.renderQueue = 3000;
            }
            if (glow) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", new Color(1, 0.5f, 0.1f)); }
            AssetDatabase.CreateAsset(material, path);
        }
        public static void Verify()
        {
            foreach (string name in new[] { "StandardOpaque", "StandardTransparent", "Glow", "HighContrast", "Lines" })
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/" + name + ".mat");
                if (material == null || material.shader == null) throw new BuildFailedException("Missing presentation resource " + name);
            }
        }
    }
    public sealed class PresentationMaterialChecks : IPreprocessBuildWithReport
    {
        public int callbackOrder => -900;
        public void OnPreprocessBuild(BuildReport report) { PresentationMaterials.Verify(); }
    }
}
