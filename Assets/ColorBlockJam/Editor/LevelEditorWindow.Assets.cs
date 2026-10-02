using System;
using System.Collections.Generic;
using System.IO;
using ColorBlockJam.Board;
using ColorBlockJam.Gameplay;
using ColorBlockJam.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ColorBlockJam.Editor
{
    public sealed partial class LevelEditorWindow
    {
        private void LoadOrCreateVisualSettings()
        {
            visualSettings = LoadOrCreateDefaultVisualSettings();
            Selection.activeObject = visualSettings;
        }

        private static BoardVisualSettings LoadOrCreateDefaultVisualSettings()
        {
            BoardVisualSettings settings = AssetDatabase.LoadAssetAtPath<BoardVisualSettings>(VisualSettingsPath);
            if (settings == null)
            {
                EnsureFolder("Assets/ColorBlockJam/Settings");
                settings = CreateInstance<BoardVisualSettings>();
                AssetDatabase.CreateAsset(settings, VisualSettingsPath);
            }

            settings.ConfigurePrefabs(
                FindPrefab("GroundGrid", "Tile"),
                FindPrefab("Wall", "WallMiddle", "StraightWall"),
                FindPrefab("Corner", "WallCorner", "CornerWall"),
                FindPrefab("Gate", "Door_1x1"),
                FindPrefab("Block"),
                FindPrefab("GateArrow"));
            settings.ConfigureDirectionArrowPrefab(FindPrefab("BlockDirectionArrow"));
            settings.ConfigureMaterials(
                AssetDatabase.LoadAssetAtPath<Material>(BlockMaterialPath),
                AssetDatabase.LoadAssetAtPath<Material>(GateMaterialPath),
                AssetDatabase.LoadAssetAtPath<Material>(WallMaterialPath));
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            return settings;
        }

        private static GameObject FindPrefab(params string[] candidateNames)
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/ColorBlockJam/Prefabs" });
            foreach (string candidateName in candidateNames)
            {
                foreach (string guid in prefabGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null && string.Equals(prefab.name, candidateName, StringComparison.OrdinalIgnoreCase))
                        return prefab;
                }
            }
            return null;
        }

        private void RebuildScenePreview()
        {
            if (selectedLevel == null || visualSettings == null) return;

            GameObject legacyPreview = GameObject.Find("[CBJ] Board Preview");
            if (legacyPreview != null) Undo.DestroyObjectImmediate(legacyPreview);

            GameObject previousPreview = GameObject.Find(ScenePreviewRootName);
            if (previousPreview != null) Undo.DestroyObjectImmediate(previousPreview);

            GameObject runtimePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LevelRuntimePrefabPath);
            if (runtimePrefab == null)
            {
                EditorUtility.DisplayDialog("Level Runtime Prefab is missing",
                    "Create or assign Assets/ColorBlockJam/Prefabs/Level/LevelRuntime.prefab before rebuilding the preview.",
                    "OK");
                return;
            }

            GameObject previewRoot = (GameObject)PrefabUtility.InstantiatePrefab(runtimePrefab);
            previewRoot.name = ScenePreviewRootName;
            Undo.RegisterCreatedObjectUndo(previewRoot, "Create Level Preview");

            BoardPreviewGenerator generator = previewRoot.GetComponentInChildren<BoardPreviewGenerator>(true);
            Camera previewCamera = previewRoot.GetComponentInChildren<Camera>(true);
            Light previewDirectionalLight = null;
            foreach (Light light in previewRoot.GetComponentsInChildren<Light>(true))
                if (light.type == LightType.Directional) { previewDirectionalLight = light; break; }
            if (generator == null || previewCamera == null || previewDirectionalLight == null)
            {
                Undo.DestroyObjectImmediate(previewRoot);
                EditorUtility.DisplayDialog("Level Runtime Prefab is incomplete",
                    "The prefab needs a BoardPreviewGenerator, a Camera, and a directional Light.", "OK");
                return;
            }

            Undo.RecordObject(generator, "Configure Board Preview");
            generator.Configure(selectedLevel, visualSettings);
            generator.Rebuild();
            LevelCameraSettingsApplicator.Apply(previewCamera, selectedLevel.CameraSettings);
            LevelLightingSettingsApplicator.Apply(previewDirectionalLight, selectedLevel.LightingSettings);
            EditorUtility.SetDirty(generator);
            EditorSceneManager.MarkSceneDirty(previewRoot.scene);
            Selection.activeGameObject = previewRoot;
            SceneView.lastActiveSceneView?.FrameSelected();
            SceneView.RepaintAll();
        }

        private void PlaySelectedLevel()
        {
            SaveLevel();
            if (!File.Exists(GameplayScenePath))
            {
                EditorUtility.DisplayDialog("Gameplay scene is not ready",
                    "The level is saved and ready for runtime use. The Gameplay scene will be connected in the next development step.",
                    "OK");
                return;
            }

            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(selectedLevel));
            EditorPrefs.SetString("ColorBlockJam.PlaytestLevelGuid", guid);
            EditorSceneManager.OpenScene(GameplayScenePath);
            EditorApplication.isPlaying = true;
        }

        private void RefreshLevels()
        {
            LevelDefinition previous = selectedLevel;
            levels.Clear();
            string[] guids = AssetDatabase.FindAssets("t:LevelDefinition");
            foreach (string guid in guids)
            {
                LevelDefinition level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (level != null) levels.Add(level);
            }
            levels.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
            SyncLevelCatalog();
            if (previous != null && levels.Contains(previous)) selectedLevel = previous;
            else if (levels.Count > 0) selectedLevel = levels[0];
            if (selectedLevel != null && selectedLevel.EnsureBlockGroupIds())
                EditorUtility.SetDirty(selectedLevel);
            ValidateLevel();
        }

        private void SyncLevelCatalog()
        {
            LevelCatalog catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(LevelCatalogPath);
            if (catalog == null)
            {
                catalog = CreateInstance<LevelCatalog>();
                AssetDatabase.CreateAsset(catalog, LevelCatalogPath);
            }

            if (!catalog.SetLevels(levels)) return;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

    }
}
