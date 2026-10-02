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
        private void CreateLevel()
        {
            EnsureFolder(LevelsFolder);
            string path = GetNextLevelAssetPath();
            var level = CreateInstance<LevelDefinition>();
            int levelNumber = levels.Count + 1;
            level.SetDisplayName($"Level_{levelNumber}");
            AssetDatabase.CreateAsset(level, path);
            AssetDatabase.SaveAssets();
            RefreshLevels();
            SelectLevel(level);
            Selection.activeObject = level;
        }

        private void DuplicateLevel()
        {
            string sourcePath = AssetDatabase.GetAssetPath(selectedLevel);
            string copyPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{Path.GetDirectoryName(sourcePath)?.Replace('\\', '/')}/{selectedLevel.name}_Variant.asset");
            if (AssetDatabase.CopyAsset(sourcePath, copyPath))
            {
                AssetDatabase.SaveAssets();
                RefreshLevels();
                SelectLevel(AssetDatabase.LoadAssetAtPath<LevelDefinition>(copyPath));
            }
        }

        private void DeleteLevel()
        {
            if (!EditorUtility.DisplayDialog("Delete level?",
                    $"Delete '{selectedLevel.DisplayName}'? This cannot be undone.", "Delete", "Cancel")) return;

            string path = AssetDatabase.GetAssetPath(selectedLevel);
            selectedLevel = null;
            selectedBlock = null;
            selectedGate = null;
            selectedWall = null;
            AssetDatabase.DeleteAsset(path);
            RefreshLevels();
        }

        private void SelectLevel(LevelDefinition level)
        {
            selectedLevel = level;
            if (selectedLevel != null && selectedLevel.EnsureBlockGroupIds())
                EditorUtility.SetDirty(selectedLevel);
            selectedBlock = null;
            selectedGate = null;
            activeBlockGroupId = null;
            ValidateLevel();
            Repaint();
        }

        private void ToggleBlock(Vector2Int positionValue)
        {
            if (!FindEnclosedCells().Contains(positionValue))
            {
                ShowNotification(new GUIContent("Block yalnızca kapalı alan içindeki Tile hücresine yerleştirilebilir."));
                return;
            }

            BlockDefinition existing = GetBlockAt(positionValue);
            if (existing != null)
            {
                selectedBlock = existing;
                selectedGate = null;
                selectedWall = null;
                ShowNotification(new GUIContent("Block silmek için Eraser aracını kullanın."));
                return;
            }

            if (string.IsNullOrEmpty(activeBlockGroupId))
            {
                ShowNotification(new GUIContent("Create a block from Block Builder first."));
                return;
            }

            Undo.RecordObject(selectedLevel, "Add Block");
            var block = new BlockDefinition(CreateId("Block"), activeBlockColor, BlockShape.Single, positionValue);
            block.SetGroupId(activeBlockGroupId);
            selectedLevel.AddBlock(block);
            selectedBlock = block;
            selectedGate = null;
            MarkChanged();
        }

        private BlockDefinition GetBlockAt(Vector2Int positionValue)
            => BlockTopology.GetBlockAt(selectedLevel, positionValue);

        private BlockVisualPart GetBlockPartType(BlockDefinition block)
            => BlockTopology.GetVisualPart(selectedLevel, block);

        private int GetBlockRotation(BlockDefinition block)
            => BlockTopology.GetVisualRotation(selectedLevel, block);

        private void AddGate(BoardEdge edge, int edgePosition)
        {
            Undo.RecordObject(selectedLevel, "Add Gate");
            var gate = new GateDefinition(CreateId("Gate"), paintColor, edge, edgePosition);
            selectedLevel.AddGate(gate);
            selectedGate = gate;
            selectedBlock = null;
            MarkChanged();
        }

        private void ReplaceWallWithGate(Vector2Int positionValue)
        {
            WallDefinition wall = null;
            foreach (WallDefinition candidate in selectedLevel.Walls)
            {
                if (candidate.Position == positionValue)
                {
                    wall = candidate;
                    break;
                }
            }

            if (wall == null)
            {
                ShowNotification(new GUIContent("Gate yalnızca bir Wall hücresine yerleştirilebilir."));
                return;
            }

            if (IsCornerCell(positionValue))
            {
                ShowNotification(new GUIContent("Corner hücresi Gate ile değiştirilemez."));
                return;
            }

            int quarterTurns = GetAutomaticRotation(positionValue) / 90;
            Undo.RecordObject(selectedLevel, "Replace Wall With Gate");
            selectedLevel.RemoveWall(wall);
            var gate = new GateDefinition(CreateId("Gate"), paintColor, positionValue, quarterTurns);
            selectedLevel.AddGate(gate);
            selectedWall = null;
            selectedBlock = null;
            selectedGate = gate;
            MarkChanged();
        }

        private void ToggleWall(Vector2Int positionValue)
        {
            if (!IsInsideBoard(positionValue)) return;
            if (IsGateCell(positionValue))
            {
                ShowNotification(new GUIContent("Bu hücrede Gate var. Önce Gate'i silin."));
                return;
            }
            foreach (WallDefinition wall in selectedLevel.Walls)
            {
                if (wall.Position != positionValue) continue;
                RemoveWall(wall);
                return;
            }

            Undo.RecordObject(selectedLevel, "Add Wall");
            selectedWall = new WallDefinition(CreateId("Wall"), positionValue);
            selectedLevel.AddWall(selectedWall);
            selectedBlock = null;
            selectedGate = null;
            MarkChanged();
        }

        private bool IsGateCell(Vector2Int positionValue)
            => BoardTopology.IsGate(selectedLevel, positionValue);

        private bool IsCornerCell(Vector2Int positionValue)
            => BoardTopology.IsCorner(selectedLevel, positionValue);

        private int GetAutomaticRotation(Vector2Int positionValue)
            => BoardTopology.GetAutomaticRotation(selectedLevel, positionValue);

        private HashSet<Vector2Int> FindEnclosedCells()
            => BoardTopology.FindEnclosedCells(selectedLevel);

        private void MoveBlock(BlockDefinition block, Vector2Int positionValue)
        {
            if (block.Position == positionValue) return;
            Undo.RecordObject(selectedLevel, "Move Block");
            block.SetPosition(positionValue);
            MarkChanged();
        }

        internal static void ApplyMovementFeature(LevelDefinition level, BlockDefinition seed,
            BlockMovementMode movementMode)
        {
            if (level == null || seed == null) return;
            if (activeWindow != null && activeWindow.selectedLevel == level)
            {
                Undo.RecordObject(level, "Edit Block Direction Feature");
                activeWindow.SetConnectedBlockMovementMode(seed, movementMode);
                activeWindow.MarkChanged();
                return;
            }

            Undo.RecordObject(level, "Edit Block Direction Feature");
            SetConnectedBlockMovementMode(level, seed, movementMode);
            EditorUtility.SetDirty(level);
        }

        private static void ApplyIceLockFeature(LevelDefinition level, BlockDefinition seed, bool enabled,
            int unlockAfter)
        {
            if (level == null || seed == null) return;
            Undo.RecordObject(level, enabled ? "Add Ice Lock Feature" : "Remove Ice Lock Feature");
            foreach (BlockDefinition candidate in level.Blocks)
                if (candidate.GroupId == seed.GroupId) candidate.Features.SetIceLock(enabled, unlockAfter);

            EditorUtility.SetDirty(level);
            if (activeWindow != null && activeWindow.selectedLevel == level)
            {
                activeWindow.RebuildScenePreview();
                activeWindow.Repaint();
            }
        }

        private void SetConnectedBlockMovementMode(BlockDefinition seed, BlockMovementMode movementMode)
        {
            SetConnectedBlockMovementMode(selectedLevel, seed, movementMode);
        }

        private static void SetConnectedBlockMovementMode(LevelDefinition level, BlockDefinition seed,
            BlockMovementMode movementMode)
        {
            foreach (BlockDefinition candidate in level.Blocks)
                if (candidate.GroupId == seed.GroupId) candidate.SetMovementMode(movementMode);
        }

        private void RemoveBlock(BlockDefinition block)
        {
            Undo.RecordObject(selectedLevel, "Delete Block");
            selectedLevel.RemoveBlock(block);
            if (selectedBlock == block) selectedBlock = null;
            MarkChanged();
        }

        private void RemoveGate(GateDefinition gate)
        {
            Undo.RecordObject(selectedLevel, "Delete Gate");
            selectedLevel.RemoveGate(gate);
            if (selectedGate == gate) selectedGate = null;
            MarkChanged();
        }

        private void RemoveWall(WallDefinition wall)
        {
            Undo.RecordObject(selectedLevel, "Delete Wall");
            selectedLevel.RemoveWall(wall);
            if (selectedWall == wall) selectedWall = null;
            MarkChanged();
        }

        private void MarkChanged()
        {
            EditorUtility.SetDirty(selectedLevel);
            ValidateLevel();
            RebuildScenePreview();
            Repaint();
        }

        private void SaveLevel()
        {
            EditorUtility.SetDirty(selectedLevel);
            AssetDatabase.SaveAssetIfDirty(selectedLevel);
            ValidateLevel();
            ShowNotification(new GUIContent("Level saved"));
        }

        private void ValidateLevel()
        {
            validationIssues.Clear();
            validationIssues.AddRange(LevelValidator.Validate(selectedLevel));
        }

    }
}
