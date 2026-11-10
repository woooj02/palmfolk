using Palmfolk.Core;
using UnityEditor;
using UnityEngine;
using Palmfolk.Views;
using Palmfolk.Fixtures;

namespace Palmfolk.Editor
{
    // An unsaved editor-only preview. Does not modify the shared M1 scene.
    public sealed class PresentationPreview : EditorWindow
    {
        private M1Simulation game;
        private PalmfolkView view;
        private GameObject root;
        private HandSequence sequence;
        private double last;
        private double accumulator;
        [MenuItem("Palmfolk/Presentation/Replay greybox")]
        public static void Open() { GetWindow<PresentationPreview>("Presentation replay"); }
        private void OnEnable() { EditorApplication.update += Advance; }
        private void OnDisable() { EditorApplication.update -= Advance; Clear(); }
        private void Clear() { if (root != null) DestroyImmediate(root); game = null; }
        private void OnGUI()
        {
            GUILayout.Label("Unity editor synthetic replay; no headset behavior verified.");
            EditorGUILayout.HelpBox("Select a fixture through the test runner for automated replay. This window shows Core state without changing the M1 scene. Close it before saving a scene.", MessageType.Info);
            if (GUILayout.Button("Replay synthetic build and withdrawal"))
            {
                Clear(); var level = new LevelGeometry(new Vec3(-0.06, 0.006, 0), new Vec3(0.06, 0.006, 0));
                game = new M1Simulation(level); root = new GameObject("Temporary presentation preview") { hideFlags = HideFlags.DontSave };
                game = new M1Simulation(level, new GameSettings { TravelerSpeed = 0.3 });
                sequence = new HandSequence(ticks: 900, withdrawStart: 400, withdrawEnd: 430, withdrawal: new Vec3(0, 0, 0.2));
                view = root.AddComponent<PalmfolkView>(); view.Configure(level); accumulator = 0; last = EditorApplication.timeSinceStartup;
            }
        }
        private void Advance()
        {
            if (game == null || view == null) return;
            double now = EditorApplication.timeSinceStartup; accumulator += now - last; last = now;
            while (accumulator >= GameSettings.Delta)
            {
                if (!sequence.TryRead(game.Tick + 1, out var frame)) { accumulator = 0; break; }
                game.Step(frame); view.Consume(game.Events); accumulator -= GameSettings.Delta;
            }
            view.Present(new WorldPresentation(game, accumulator / GameSettings.Delta)); SceneView.RepaintAll();
        }
    }
}
