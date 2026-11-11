using System.Linq;
using Palmfolk.Platform;
using Palmfolk.UI;
using Palmfolk.UI.Meta;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Palmfolk.Editor
{
    public static class PresentationSetup
    {
        [MenuItem("Palmfolk/Presentation/1. Add Live Views To Open Scene")]
        public static void AddViews()
        {
            var runner = Object.FindAnyObjectByType<PalmfolkRunner>();
            if (runner == null) { Debug.LogWarning("Add the platform runner to this scene first (Palmfolk > Platform > 2. Add Runner To Open Scene)."); return; }
            var view = Object.FindAnyObjectByType<GamePresentation>();
            if (view == null)
            {
                var root = new GameObject("Palmfolk Presentation"); Undo.RegisterCreatedObjectUndo(root, "Add live presentation");
                view = Undo.AddComponent<GamePresentation>(root);
                Undo.AddComponent<TableDimmingBinding>(root);
            }
            var head = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).FirstOrDefault(c => c.transform.name == "CenterEyeAnchor") ?? Camera.main;
            var serialized = new SerializedObject(view); serialized.FindProperty("runner").objectReferenceValue = runner;
            serialized.FindProperty("head").objectReferenceValue = head == null ? null : head.transform; serialized.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(view.gameObject.scene); Selection.activeObject = view;
            Debug.Log("Live views connected. Save the reviewed scene. Table dimming remains zero until the owner verifies a table-edge visibility limit.");
        }

        [MenuItem("Palmfolk/Presentation/2. Add Measured Developer Stats")]
        public static void AddStats()
        {
            var type = System.Type.GetType("Palmfolk.Developer.LiveStatsBinding, Palmfolk.Stats");
            if (type == null) { Debug.LogWarning("Enable PALMFOLK_DEVELOPMENT for the developer build first. Stats are compiled out of release."); return; }
            var runner = Object.FindAnyObjectByType<PalmfolkRunner>(); if (runner == null) { Debug.LogWarning("Add the platform runner first."); return; }
            var component = runner.GetComponent(type); if (component == null) component = Undo.AddComponent(runner.gameObject, type);
            var serialized = new SerializedObject(component); serialized.FindProperty("runner").objectReferenceValue = runner;
            serialized.FindProperty("head").objectReferenceValue = Camera.main == null ? null : Camera.main.transform; serialized.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(runner.gameObject.scene); Selection.activeObject = component;
            Debug.Log("Measured developer stats connected. This component does not record or export anything.");
        }
    }
}
