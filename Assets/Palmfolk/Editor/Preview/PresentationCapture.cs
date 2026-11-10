using System.IO;
using Palmfolk.Core;
using Palmfolk.Fixtures;
using Palmfolk.Views;
using UnityEngine;

namespace Palmfolk.Editor
{
    // Editor-only QA capture from synthetic input; never submission footage.
    public static class PresentationCapture
    {
        public static void Capture()
        {
            Directory.CreateDirectory("Logs");
            Render(HandShape.OneFinger, "Logs/rope-greybox.png");
            Render(HandShape.Flat, "Logs/plank-greybox.png");
        }
        private static void Render(HandShape shape, string path)
        {
            var root = new GameObject("Capture QA");
            RenderTexture target = null; Texture2D image = null;
            var previousTarget = RenderTexture.active;
            try
            {
                var level = new LevelGeometry(new Vec3(-0.06, 0.006, 0), new Vec3(0.06, 0.006, 0));
                var simulation = new M1Simulation(level, new GameSettings { TravelerSpeed = 0.3 });
                foreach (var frame in new HandSequence(shape, ticks: 500, withdrawStart: 400, withdrawEnd: 430, withdrawal: new Vec3(0, 0, 0.2)).Frames()) simulation.Step(frame);
                var view = root.AddComponent<PalmfolkView>(); view.Configure(level); view.Present(new WorldPresentation(simulation, 1));
                foreach (var renderer in root.GetComponentsInChildren<Renderer>())
                {
                    Color c = renderer.sharedMaterial.color;
                    float gray = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
                    renderer.sharedMaterial.color = new Color(gray, gray, gray, c.a);
                }
                var camera = new GameObject("QA camera").AddComponent<Camera>(); camera.transform.SetParent(root.transform);
                camera.transform.position = new Vector3(0.17f, 0.19f, -0.26f); camera.transform.LookAt(new Vector3(0, 0.005f, 0));
                camera.orthographic = true; camera.orthographicSize = 0.13f; camera.nearClipPlane = 0.01f;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.12f, 0.12f, 0.12f);
                var light = new GameObject("QA light").AddComponent<Light>(); light.transform.SetParent(root.transform);
                light.type = LightType.Directional; light.intensity = 1.3f; light.transform.rotation = Quaternion.Euler(45, -30, 0);
                target = new RenderTexture(1280, 720, 24); camera.targetTexture = target; camera.Render();
                RenderTexture.active = target; image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                var pixels = image.GetPixels();
                for (int i = 0; i < pixels.Length; i++)
                {
                    float gray = pixels[i].grayscale;
                    pixels[i] = new Color(gray, gray, gray, 1);
                }
                image.SetPixels(pixels); image.Apply(); File.WriteAllBytes(path, image.EncodeToPNG());
                Debug.Log("Unity editor synthetic greyscale QA capture: " + path + ". Not headset evidence.");
            }
            finally
            {
                RenderTexture.active = previousTarget;
                Object.DestroyImmediate(root);
                if (image != null) Object.DestroyImmediate(image);
                if (target != null) { target.Release(); Object.DestroyImmediate(target); }
            }
        }
    }
}
