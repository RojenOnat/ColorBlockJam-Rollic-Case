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
        private void DrawInspector(Rect rect)
        {
            EditorGUI.DrawRect(rect, new Color(0.16f, 0.16f, 0.18f));
            GUILayout.BeginArea(new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, rect.height - 16f));
            inspectorScroll = EditorGUILayout.BeginScrollView(inspectorScroll);
            GUILayout.Label("LEVEL SETTINGS", EditorStyles.boldLabel);

            if (selectedLevel == null)
            {
                EditorGUILayout.HelpBox("Select a level from the list.", MessageType.Info);
                EditorGUILayout.EndScrollView();
                GUILayout.EndArea();
                return;
            }

            if (activeTool == Tool.Feature)
            {
                DrawFeatureInspector();
                DrawValidationPanel();
                EditorGUILayout.EndScrollView();
                GUILayout.EndArea();
                return;
            }

            EditorGUI.BeginChangeCheck();
            string displayName = EditorGUILayout.TextField("Name", selectedLevel.DisplayName);
            int width = EditorGUILayout.IntSlider("Width", selectedLevel.BoardWidth,
                LevelDefinition.MinimumBoardSize, LevelDefinition.MaximumBoardSize);
            int height = EditorGUILayout.IntSlider("Height", selectedLevel.BoardHeight,
                LevelDefinition.MinimumBoardSize, LevelDefinition.MaximumBoardSize);
            int timer = EditorGUILayout.IntField("Timer (sec)", selectedLevel.TimerSeconds);
            int rewardGold = EditorGUILayout.IntField("Reward Gold", selectedLevel.RewardGold);
            EditorGUILayout.Space(8f);
            GUILayout.Label("CAMERA", EditorStyles.boldLabel);
            LevelCameraSettings cameraSettings = selectedLevel.CameraSettings;
            Vector3 cameraPosition = EditorGUILayout.Vector3Field("Position", cameraSettings.Position);
            Vector3 cameraRotation = EditorGUILayout.Vector3Field("Rotation", cameraSettings.Rotation);
            float cameraFieldOfView = EditorGUILayout.Slider("Field of View", cameraSettings.FieldOfView, 20f, 100f);
            EditorGUILayout.Space(8f);
            GUILayout.Label("LIGHTING", EditorStyles.boldLabel);
            Vector3 directionalLightRotation = EditorGUILayout.Vector3Field(
                "Directional Light Rotation", selectedLevel.LightingSettings.DirectionalLightRotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(selectedLevel, "Edit Level Settings");
                selectedLevel.SetDisplayName(displayName);
                selectedLevel.SetBoardSize(width, height);
                selectedLevel.SetTimer(timer);
                selectedLevel.SetRewardGold(rewardGold);
                selectedLevel.SetCameraSettings(cameraPosition, cameraRotation, cameraFieldOfView);
                selectedLevel.SetDirectionalLightRotation(directionalLightRotation);
                MarkChanged();
            }

            if (activeTool == Tool.Block || activeTool == Tool.Gate)
            {
                EditorGUILayout.Space(12f);
                GUILayout.Label("PLACEMENT", EditorStyles.boldLabel);
                if (activeTool == Tool.Block)
                {
                    DrawBlockBuilder();
                }
                else
                {
                    DrawGatePlacementControls();
                }
            }

            if (activeTool == Tool.Select)
            {
                EditorGUILayout.Space(12f);
                GUILayout.Label("3D BOARD PREVIEW", EditorStyles.boldLabel);
                visualSettings = (BoardVisualSettings)EditorGUILayout.ObjectField(
                    "Visual Settings", visualSettings, typeof(BoardVisualSettings), false);
                if (visualSettings == null)
                {
                    EditorGUILayout.HelpBox("Create or assign Board Visual Settings.", MessageType.Warning);
                    if (GUILayout.Button("Create Settings")) LoadOrCreateVisualSettings();
                }
                else
                {
                    if (!visualSettings.HasRequiredPrefabs(out string prefabMessage))
                        EditorGUILayout.HelpBox(prefabMessage, MessageType.Warning);
                    if (GUILayout.Button("Edit Prefab Settings")) Selection.activeObject = visualSettings;
                    if (GUILayout.Button("Rebuild Scene Preview")) RebuildScenePreview();
                }
            }

            if (activeTool == Tool.Select) DrawSelectedItemInspector();
            DrawValidationPanel();
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawBlockBuilder()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("BLOCK BUILDER", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "How to use:\n" +
                    "1. Choose a color.\n" +
                    "2. Create a block, then click empty grey tiles to paint its shape.\n" +
                    "3. Finish Block when the shape is complete.\n\n" +
                    "Each Create Block starts a new group. Use it again to make a separate block, even when it has the same color.",
                    MessageType.Info);
                EditorGUILayout.LabelField("1. Choose a color", EditorStyles.miniLabel);
                DrawBlockColorPalette();
                EditorGUILayout.Space(4f);

                if (string.IsNullOrEmpty(activeBlockGroupId))
                {
                    EditorGUILayout.LabelField("2. Start a block, then paint its tiles.", EditorStyles.miniLabel);
                    Color previous = GUI.backgroundColor;
                    GUI.backgroundColor = GetColor(paintColor);
                    if (GUILayout.Button($"Create {paintColor} Block", GUILayout.Height(28f)))
                    {
                        activeBlockGroupId = CreateId("BlockGroup");
                        activeBlockColor = paintColor;
                    }
                    GUI.backgroundColor = previous;
                }
                else
                {
                    EditorGUILayout.LabelField($"Painting {activeBlockColor} block", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField("3. Click empty tiles, then finish the block.", EditorStyles.miniLabel);
                    if (GUILayout.Button("Finish Block", GUILayout.Height(28f))) activeBlockGroupId = null;
                }
            }
        }

        private void DrawGatePlacementControls()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("GATE PLACE MODE", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("1. Choose the gate color", EditorStyles.miniLabel);
                DrawBlockColorPalette();
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("2. Click a colored + on a straight wall.", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("Corners cannot become gates. Rotation is automatic.",
                    EditorStyles.miniLabel);
                EditorGUILayout.LabelField("Right-click an existing gate to remove it.", EditorStyles.miniLabel);
            }
        }

        private void DrawFeatureInspector()
        {
            EditorGUILayout.Space(12f);
            GUILayout.Label("FEATURES", EditorStyles.boldLabel);
            if (GUILayout.Button("Back to Feature List"))
            {
                activeFeaturePage = FeaturePage.None;
                Repaint();
                return;
            }

            EditorGUILayout.Space(6f);
            if (activeFeaturePage == FeaturePage.Direction)
            {
                DrawFeatureGroupPicker();
                DrawDirectionFeatureInspector();
                return;
            }

            if (activeFeaturePage == FeaturePage.IceLock)
            {
                DrawFeatureGroupPicker();
                DrawIceLockFeatureInspector();
                return;
            }

            EditorGUILayout.HelpBox("Choose a feature from the main panel.", MessageType.Info);
        }

        private void DrawFeatureGroupPicker()
        {
            GUILayout.Label("BLOCK GROUPS", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Select a whole block to edit.", EditorStyles.miniLabel);
            List<List<BlockDefinition>> groups = GetAllBlockGroups();
            for (int index = 0; index < groups.Count; index++)
            {
                List<BlockDefinition> group = groups[index];
                BlockDefinition seed = group[0];
                bool selected = selectedBlock != null && selectedBlock.GroupId == seed.GroupId;
                Color previous = GUI.backgroundColor;
                GUI.backgroundColor = selected ? Color.white : GetColor(seed.Color);
                if (GUILayout.Button(GetFeatureGroupLabel(group, index + 1), GUILayout.Height(26f)))
                {
                    selectedBlock = seed;
                    selectedWall = null;
                    selectedGate = null;
                    Repaint();
                }
                GUI.backgroundColor = previous;
            }
            EditorGUILayout.Space(8f);
        }

        private List<List<BlockDefinition>> GetAllBlockGroups()
        {
            var lookup = new Dictionary<string, List<BlockDefinition>>();
            foreach (BlockDefinition block in selectedLevel.Blocks)
            {
                if (!lookup.TryGetValue(block.GroupId, out List<BlockDefinition> group))
                {
                    group = new List<BlockDefinition>();
                    lookup.Add(block.GroupId, group);
                }
                group.Add(block);
            }
            return new List<List<BlockDefinition>>(lookup.Values);
        }

        private static string GetFeatureGroupLabel(List<BlockDefinition> group, int number)
        {
            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;
            foreach (BlockDefinition block in group)
            {
                minX = Mathf.Min(minX, block.Position.x);
                maxX = Mathf.Max(maxX, block.Position.x);
                minY = Mathf.Min(minY, block.Position.y);
                maxY = Mathf.Max(maxY, block.Position.y);
            }
            string ice = group[0].Features.IceLockEnabled ? "  •  ICE" : string.Empty;
            return $"{group[0].Color} Block {number}  •  {maxX - minX + 1}×{maxY - minY + 1}{ice}";
        }

        private void DrawDirectionFeatureInspector()
        {
            GUILayout.Label("DIRECTION", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Click a block group on the grid to edit it.", EditorStyles.miniLabel);
            if (selectedBlock == null)
            {
                EditorGUILayout.HelpBox("Select a block on the grid.", MessageType.Info);
                return;
            }

            List<BlockDefinition> group = GetBlockGroup(selectedBlock);
            EditorGUILayout.LabelField("Selected", $"{selectedBlock.Color} • {group.Count} cells");
            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawDirectionFeatureButton("HORIZONTAL\n←  →", BlockMovementMode.HorizontalOnly);
                DrawDirectionFeatureButton("VERTICAL\n↑  ↓", BlockMovementMode.VerticalOnly);
            }

            if (selectedBlock.Features.MovementMode != BlockMovementMode.Free &&
                GUILayout.Button("Remove Direction Feature"))
                SetDirectionFeature(BlockMovementMode.Free);
        }

        private void DrawDirectionFeatureButton(string label, BlockMovementMode movementMode)
        {
            Color previous = GUI.backgroundColor;
            if (selectedBlock.Features.MovementMode == movementMode)
                GUI.backgroundColor = new Color(0.35f, 0.63f, 0.95f);
            if (GUILayout.Button(label, GUILayout.Height(52f)))
                SetDirectionFeature(movementMode);
            GUI.backgroundColor = previous;
        }

        private void SetDirectionFeature(BlockMovementMode movementMode)
        {
            ApplyMovementFeature(selectedLevel, selectedBlock, movementMode);
            string message = movementMode == BlockMovementMode.Free
                ? "Direction feature removed."
                : movementMode == BlockMovementMode.HorizontalOnly
                    ? "Horizontal direction added."
                    : "Vertical direction added.";
            ShowNotification(new GUIContent(message));
            Repaint();
        }

        private List<BlockDefinition> GetBlockGroup(BlockDefinition seed)
        {
            var group = new List<BlockDefinition>();
            if (seed == null) return group;
            foreach (BlockDefinition candidate in selectedLevel.Blocks)
                if (candidate.GroupId == seed.GroupId) group.Add(candidate);
            return group;
        }

        private void DrawIceLockFeatureInspector()
        {
            GUILayout.Label("ICE LOCK", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Click a block group to add or edit Ice Lock.", EditorStyles.miniLabel);
            if (selectedBlock == null)
            {
                EditorGUILayout.HelpBox("Select a block group first.", MessageType.Info);
                return;
            }

            BlockFeatureData feature = selectedBlock.Features;
            EditorGUILayout.LabelField("Selected", $"{selectedBlock.Color} • {GetBlockGroup(selectedBlock).Count} cells");
            if (!feature.IceLockEnabled)
            {
                if (GUILayout.Button("Add Ice Lock", GUILayout.Height(32f)))
                    ApplyIceLockFeature(selectedLevel, selectedBlock, true, 1);
            }
            else
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Unlock after this many block exits", EditorStyles.miniLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("−", GUILayout.Width(30f)))
                        ApplyIceLockFeature(selectedLevel, selectedBlock, true, feature.IceUnlockAfter - 1);
                    GUILayout.Label(feature.IceUnlockAfter.ToString(), CenteredLabelStyle(), GUILayout.Width(36f));
                    if (GUILayout.Button("+", GUILayout.Width(30f)))
                        ApplyIceLockFeature(selectedLevel, selectedBlock, true, feature.IceUnlockAfter + 1);
                }
                if (GUILayout.Button("Remove Ice Lock", GUILayout.Height(28f)))
                    ApplyIceLockFeature(selectedLevel, selectedBlock, false, feature.IceUnlockAfter);
            }

            EditorGUILayout.Space(10f);
            showIceVisualSettings = EditorGUILayout.Foldout(showIceVisualSettings, "Ice Visual Settings", true);
            if (!showIceVisualSettings) return;

            EditorGUILayout.LabelField("Shared visual settings for every Ice Lock block.", EditorStyles.miniLabel);
            if (visualSettings == null)
            {
                EditorGUILayout.HelpBox("Assign Board Visual Settings first.", MessageType.Info);
                return;
            }
            var settingsObject = new SerializedObject(visualSettings);
            settingsObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(settingsObject.FindProperty("iceTexture"), new GUIContent("Ice Texture"));
            EditorGUILayout.PropertyField(settingsObject.FindProperty("iceMaterial"), new GUIContent("Ice Material"));
            EditorGUILayout.PropertyField(settingsObject.FindProperty("iceCounterFont"), new GUIContent("Counter Font"));
            EditorGUILayout.PropertyField(settingsObject.FindProperty("iceCounterFontSize"), new GUIContent("Counter Font Size"));
            EditorGUILayout.PropertyField(settingsObject.FindProperty("iceCounterColor"), new GUIContent("Counter Color"));
            EditorGUILayout.PropertyField(settingsObject.FindProperty("iceCounterOffset"), new GUIContent("Counter Offset"));
            if (EditorGUI.EndChangeCheck())
            {
                settingsObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(visualSettings);
            }
        }

        private void DrawBlockColorPalette()
        {
            BlockColor[] colors = (BlockColor[])Enum.GetValues(typeof(BlockColor));
            const int columns = 3;
            for (int start = 0; start < colors.Length; start += columns)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int offset = 0; offset < columns && start + offset < colors.Length; offset++)
                    {
                        BlockColor color = colors[start + offset];
                        Color previous = GUI.backgroundColor;
                        GUI.backgroundColor = GetColor(color);
                        if (GUILayout.Button(color.ToString(), GUILayout.Height(22f))) paintColor = color;
                        GUI.backgroundColor = previous;
                    }
                }
            }
        }

        private void DrawSelectedItemInspector()
        {
            EditorGUILayout.Space(12f);
            GUILayout.Label("SELECTION", EditorStyles.boldLabel);
            if (selectedBlock != null)
            {
                EditorGUILayout.LabelField("Block", selectedBlock.Id);
                EditorGUI.BeginChangeCheck();
                BlockColor color = (BlockColor)EditorGUILayout.EnumPopup("Color", selectedBlock.Color);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(selectedLevel, "Edit Block");
                    selectedBlock.SetColor(color);
                    MarkChanged();
                }

                EditorGUILayout.Vector2IntField("Position", selectedBlock.Position);
                EditorGUILayout.LabelField("Part", GetBlockPartType(selectedBlock).ToString());
                EditorGUILayout.LabelField("Rotation", $"{GetBlockRotation(selectedBlock)}° (Automatic)");
                if (GUILayout.Button("Open Direction Feature"))
                {
                    activeTool = Tool.Feature;
                    activeFeaturePage = FeaturePage.Direction;
                }
                if (GUILayout.Button("Delete")) RemoveBlock(selectedBlock);
            }
            else if (selectedWall != null)
            {
                bool corner = IsCornerCell(selectedWall.Position);
                EditorGUILayout.LabelField(corner ? "Corner" : "Wall", selectedWall.Id);
                EditorGUILayout.Vector2IntField("Position", selectedWall.Position);
                EditorGUILayout.LabelField("Rotation", $"{GetAutomaticRotation(selectedWall.Position)}° (Automatic)");
                if (GUILayout.Button("Delete")) RemoveWall(selectedWall);
            }
            else if (selectedGate != null)
            {
                EditorGUILayout.LabelField("Gate", selectedGate.Id);
                if (selectedGate.GridPlaced)
                {
                    EditorGUILayout.Vector2IntField("Position", selectedGate.Position);
                    EditorGUILayout.LabelField("Rotation", $"{selectedGate.QuarterTurns * 90}° (Automatic)");
                }
                else
                {
                    EditorGUILayout.LabelField("Edge", selectedGate.Edge.ToString());
                    EditorGUILayout.LabelField("Position", selectedGate.EdgePosition.ToString());
                }
                EditorGUI.BeginChangeCheck();
                BlockColor color = (BlockColor)EditorGUILayout.EnumPopup("Color", selectedGate.Color);
                int gateWidth = selectedGate.GridPlaced
                    ? selectedGate.Width
                    : EditorGUILayout.IntField("Width", selectedGate.Width);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(selectedLevel, "Edit Gate");
                    selectedGate.SetColor(color);
                    selectedGate.SetWidth(gateWidth);
                    MarkChanged();
                }
                if (GUILayout.Button("Delete Gate")) RemoveGate(selectedGate);
            }
            else
            {
                EditorGUILayout.HelpBox("Select a block or gate to edit it.", MessageType.None);
            }
        }

        private void DrawValidationPanel()
        {
            EditorGUILayout.Space(14f);
            GUILayout.Label("VALIDATION", EditorStyles.boldLabel);
            if (validationIssues.Count == 0)
            {
                EditorGUILayout.HelpBox("No issues found.", MessageType.Info);
                return;
            }

            validationScroll = EditorGUILayout.BeginScrollView(validationScroll, GUILayout.MinHeight(90f));
            foreach (ValidationIssue issue in validationIssues)
            {
                MessageType type = issue.Severity == ValidationSeverity.Error
                    ? MessageType.Error
                    : MessageType.Warning;
                EditorGUILayout.HelpBox(issue.Message, type);
            }
            EditorGUILayout.EndScrollView();
        }

    }
}
