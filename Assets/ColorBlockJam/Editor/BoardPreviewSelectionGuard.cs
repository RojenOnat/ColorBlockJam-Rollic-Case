using ColorBlockJam.Board;
using UnityEditor;
using UnityEngine;

namespace ColorBlockJam.Editor
{
    /// <summary>Keeps generated preview objects out of Unity's play-mode selection restore.</summary>
    internal static class BoardPreviewSelectionGuard
    {
        [InitializeOnLoadMethod]
        private static void Register()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;

            foreach (Object selected in Selection.objects)
            {
                GameObject selectedObject = selected as GameObject;
                if (selectedObject == null && selected is Component component)
                    selectedObject = component.gameObject;
                if (selectedObject == null) continue;

                Transform generatedRoot = selectedObject.transform;
                while (generatedRoot != null && generatedRoot.name != BoardPreviewGenerator.GeneratedRootName)
                    generatedRoot = generatedRoot.parent;
                if (generatedRoot == null) continue;

                BoardPreviewGenerator generator = generatedRoot.GetComponentInParent<BoardPreviewGenerator>();
                Selection.activeGameObject = generator != null ? generator.gameObject : null;
                break;
            }
        }
    }
}
