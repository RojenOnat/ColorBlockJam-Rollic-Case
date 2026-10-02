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
    /// <summary>
    /// Designer-facing level authoring window. Drawing, input, inspector, mutation, asset, and geometry concerns
    /// are separated into focused partial files while Unity retains a single EditorWindow entry point.
    /// </summary>
    public sealed partial class LevelEditorWindow : EditorWindow
    {
        private const string LevelsFolder = "Assets/ColorBlockJam/Levels";
        private const string LevelCatalogPath = "Assets/ColorBlockJam/Levels/LevelCatalog.asset";
        private const string GameplayScenePath = "Assets/ColorBlockJam/Scenes/Gameplay.unity";
        private const string LevelRuntimePrefabPath = "Assets/ColorBlockJam/Prefabs/Level/LevelRuntime.prefab";
        private const string ScenePreviewRootName = "[CBJ] Level Preview";
        private const string VisualSettingsPath = "Assets/ColorBlockJam/Settings/BoardVisualSettings.asset";
        private const string BlockMaterialPath = "Assets/ColorBlockJam/Materials/Blocks/M_Block.mat";
        private const string GateMaterialPath = "Assets/ColorBlockJam/Materials/Gates/M_Gate.mat";
        private const string WallMaterialPath = "Assets/ColorBlockJam/Materials/Walls/M_Wall.mat";
        private const float SidebarWidth = 210f;
        private const float InspectorWidth = 250f;
        private const float GateBand = 22f;

        private enum Tool
        {
            Select,
            Wall,
            Gate,
            Block,
            Feature,
            Eraser
        }

        private enum FeaturePage
        {
            None,
            Direction,
            IceLock
        }

        private readonly List<LevelDefinition> levels = new List<LevelDefinition>();
        private readonly List<ValidationIssue> validationIssues = new List<ValidationIssue>();

        private LevelDefinition selectedLevel;
        private BlockDefinition selectedBlock;
        private GateDefinition selectedGate;
        private WallDefinition selectedWall;
        private Tool activeTool = Tool.Select;
        private BlockColor paintColor = BlockColor.Red;
        private Vector2 leftScroll;
        private Vector2 inspectorScroll;
        private Vector2 validationScroll;
        private bool draggingBlock;
        private BoardVisualSettings visualSettings;
        private string activeBlockGroupId;
        private BlockColor activeBlockColor;
        private FeaturePage activeFeaturePage;
        private bool showIceVisualSettings;
        private static LevelEditorWindow activeWindow;

        [MenuItem("Color Block Jam/Level Editor", priority = 1)]
        public static void Open()
        {
            var window = GetWindow<LevelEditorWindow>();
            window.titleContent = new GUIContent("CBJ Level Editor");
            window.minSize = new Vector2(920f, 600f);
            window.Show();
        }

        [InitializeOnLoadMethod]
        private static void ConfigureDefaultVisualSettingsAfterReload()
        {
            EditorApplication.delayCall += () => LoadOrCreateDefaultVisualSettings();
        }

        private void OnEnable()
        {
            activeWindow = this;
            LoadOrCreateVisualSettings();
            RefreshLevels();
            Undo.undoRedoPerformed += HandleUndoRedo;
        }

        private void OnDisable()
        {
            if (activeWindow == this) activeWindow = null;
            Undo.undoRedoPerformed -= HandleUndoRedo;
        }

        private void HandleUndoRedo()
        {
            ValidateLevel();
            Repaint();
        }

        private void OnGUI()
        {
            DrawHeader();

            Rect content = new Rect(0f, 42f, position.width, position.height - 42f);
            Rect left = new Rect(content.x, content.y, SidebarWidth, content.height);
            Rect right = new Rect(content.xMax - InspectorWidth, content.y, InspectorWidth, content.height);
            Rect center = new Rect(left.xMax, content.y, content.width - SidebarWidth - InspectorWidth, content.height);

            DrawLevelList(left);
            DrawCanvas(center);
            DrawInspector(right);
        }

        private void DrawHeader()
        {
            EditorGUI.DrawRect(new Rect(0f, 0f, position.width, 42f), new Color(0.12f, 0.12f, 0.14f));
            GUILayout.BeginArea(new Rect(8f, 7f, position.width - 16f, 32f));
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Color Block Jam", EditorStyles.boldLabel, GUILayout.Width(120f));
                GUI.enabled = selectedLevel != null;
                EditorGUI.BeginChangeCheck();
                activeTool = (Tool)GUILayout.Toolbar((int)activeTool,
                    new[] { "Select", "Wall", "Gate", "Block", "Features", "Eraser" }, GUILayout.Height(26f));
                if (EditorGUI.EndChangeCheck() && activeTool == Tool.Feature)
                    activeFeaturePage = FeaturePage.None;
                GUILayout.Space(8f);
                GUI.enabled = selectedLevel != null;
                if (GUILayout.Button("Rebuild Preview", GUILayout.Width(110f), GUILayout.Height(26f)))
                    RebuildScenePreview();
                if (GUILayout.Button("Validate", GUILayout.Width(80f), GUILayout.Height(26f)))
                    ValidateLevel();
                if (GUILayout.Button("Save", GUILayout.Width(70f), GUILayout.Height(26f)))
                    SaveLevel();
                if (GUILayout.Button("Play", GUILayout.Width(70f), GUILayout.Height(26f)))
                    PlaySelectedLevel();
                GUI.enabled = true;
            }
            GUILayout.EndArea();
        }

        private void DrawLevelList(Rect rect)
        {
            EditorGUI.DrawRect(rect, new Color(0.16f, 0.16f, 0.18f));
            GUILayout.BeginArea(new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, rect.height - 16f));
            GUILayout.Label("LEVELS", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("New")) CreateLevel();
                GUI.enabled = selectedLevel != null;
                if (GUILayout.Button("Duplicate")) DuplicateLevel();
                GUI.enabled = true;
            }

            EditorGUILayout.Space(4f);
            leftScroll = EditorGUILayout.BeginScrollView(leftScroll);
            foreach (LevelDefinition level in levels)
            {
                GUIStyle style = level == selectedLevel ? "SelectionRect" : "Label";
                Rect row = EditorGUILayout.GetControlRect(false, 25f);
                if (GUI.Button(row, level.DisplayName, style)) SelectLevel(level);
            }
            EditorGUILayout.EndScrollView();

            GUI.enabled = selectedLevel != null;
            if (GUILayout.Button("Delete Level")) DeleteLevel();
            GUI.enabled = true;
            GUILayout.EndArea();
        }

        private void DrawCanvas(Rect rect)
        {
            EditorGUI.DrawRect(rect, new Color(0.105f, 0.105f, 0.12f));
            if (selectedLevel == null)
            {
                GUI.Label(rect, "Create or select a level to begin.", CenteredLabelStyle());
                return;
            }

            if (activeTool == Tool.Feature && activeFeaturePage == FeaturePage.None)
            {
                DrawFeatureLanding(rect);
                return;
            }

            float availableWidth = Mathf.Max(100f, rect.width - 70f);
            float availableHeight = Mathf.Max(100f, rect.height - 120f);
            float cellSize = Mathf.Floor(Mathf.Min(availableWidth / selectedLevel.BoardWidth,
                availableHeight / selectedLevel.BoardHeight));
            cellSize = Mathf.Clamp(cellSize, 22f, 64f);

            Vector2 boardSize = new Vector2(selectedLevel.BoardWidth * cellSize,
                selectedLevel.BoardHeight * cellSize);
            Rect boardRect = new Rect(
                rect.center.x - boardSize.x * 0.5f,
                rect.center.y - boardSize.y * 0.5f - 18f,
                boardSize.x,
                boardSize.y);

            DrawBoard(boardRect, cellSize);
            DrawCanvasHelp(rect, boardRect);
            HandleBoardInput(boardRect, cellSize);
        }

        private void DrawFeatureLanding(Rect rect)
        {
            GUI.Label(new Rect(rect.x, rect.y + 30f, rect.width, 24f), "FEATURES", CenteredLabelStyle());
            GUI.Label(new Rect(rect.x, rect.y + 54f, rect.width, 20f),
                "Choose a feature to configure.", CenteredMiniLabelStyle());

            float cardWidth = 220f;
            float cardHeight = 118f;
            float gap = 18f;
            float startX = rect.center.x - (cardWidth * 2f + gap) * 0.5f;
            Rect directionCard = new Rect(startX, rect.center.y - cardHeight * 0.5f, cardWidth, cardHeight);
            Rect iceCard = new Rect(directionCard.xMax + gap, directionCard.y, cardWidth, cardHeight);
            DrawFeatureCard(directionCard, "DIRECTION", "Horizontal or vertical movement", "Open Direction",
                FeaturePage.Direction);
            DrawFeatureCard(iceCard, "ICE LOCK", "Lock a block until the counter ends", "Open Ice Lock",
                FeaturePage.IceLock);
        }

        private void DrawFeatureCard(Rect rect, string title, string description, string action, FeaturePage page)
        {
            EditorGUI.DrawRect(rect, new Color(0.18f, 0.19f, 0.22f));
            DrawOutline(rect, new Color(0.35f, 0.38f, 0.44f), 1f);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 12f, rect.width - 24f, 20f), title, EditorStyles.boldLabel);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 38f, rect.width - 24f, 30f), description,
                EditorStyles.wordWrappedMiniLabel);
            if (GUI.Button(new Rect(rect.x + 12f, rect.yMax - 36f, rect.width - 24f, 24f), action))
            {
                activeFeaturePage = page;
                Repaint();
            }
        }

        private void DrawBoard(Rect boardRect, float cellSize)
        {
            EditorGUI.DrawRect(boardRect, new Color(0.2f, 0.21f, 0.24f));
            HashSet<Vector2Int> enclosedCells = FindEnclosedCells();
            for (int y = 0; y < selectedLevel.BoardHeight; y++)
            {
                for (int x = 0; x < selectedLevel.BoardWidth; x++)
                {
                    Rect cell = GetCellRect(boardRect, cellSize, new Vector2Int(x, y));
                    bool enclosed = enclosedCells.Contains(new Vector2Int(x, y));
                    Color shade = enclosed
                        ? ((x + y) % 2 == 0
                            ? new Color(0.29f, 0.30f, 0.34f)
                            : new Color(0.255f, 0.265f, 0.30f))
                        : new Color(0.18f, 0.19f, 0.22f);
                    EditorGUI.DrawRect(Shrink(cell, 1f), shade);
                }
            }

            foreach (BlockDefinition block in selectedLevel.Blocks)
            {
                Color color = GetColor(block.Color);
                bool isSelected = activeTool == Tool.Feature && selectedBlock != null
                    ? block.GroupId == selectedBlock.GroupId
                    : block == selectedBlock;
                foreach (Vector2Int cellPosition in BlockShapeLibrary.GetBoardCells(block))
                {
                    if (!IsInsideBoard(cellPosition)) continue;
                    Rect cell = GetCellRect(boardRect, cellSize, cellPosition);
                    EditorGUI.DrawRect(Shrink(cell, isSelected ? 2f : 4f), color);
                    if (isSelected && activeTool != Tool.Feature)
                        DrawOutline(Shrink(cell, 1f), Color.white, 2f);
                }
            }
            if (activeTool == Tool.Feature) DrawSelectedFeatureGroupOutline(boardRect, cellSize);
            DrawMovementFeatureBadges(boardRect, cellSize);
            DrawIceLockBadges(boardRect, cellSize);

            foreach (WallDefinition wall in selectedLevel.Walls)
            {
                if (!IsInsideBoard(wall.Position)) continue;
                Rect cell = GetCellRect(boardRect, cellSize, wall.Position);
                bool corner = IsCornerCell(wall.Position);
                EditorGUI.DrawRect(Shrink(cell, 3f), corner
                    ? new Color(0.75f, 0.38f, 0.18f)
                    : new Color(0.32f, 0.48f, 0.72f));
                GUI.Label(cell, corner ? "C" : "W", CenteredLabelStyle());
                if (wall == selectedWall) DrawOutline(Shrink(cell, 2f), Color.white, 2f);

                if (activeTool == Tool.Gate && !corner)
                {
                    // A gate can only replace a straight wall. Make every valid click target obvious.
                    Rect candidate = Shrink(cell, 7f);
                    EditorGUI.DrawRect(candidate, GetColor(paintColor));
                    GUI.Label(candidate, "+", CenteredLabelStyle());
                }
            }

            foreach (GateDefinition gate in selectedLevel.Gates)
                DrawGate(boardRect, cellSize, gate);

            DrawOutline(boardRect, new Color(0.7f, 0.7f, 0.75f), 2f);
        }

        private void DrawGate(Rect boardRect, float cellSize, GateDefinition gate)
        {
            Rect gateRect = gate.GridPlaced
                ? Shrink(GetCellRect(boardRect, cellSize, gate.Position), 3f)
                : GetGateRect(boardRect, cellSize, gate.Edge, gate.EdgePosition, gate.Width);
            EditorGUI.DrawRect(gateRect, GetColor(gate.Color));
            if (gate.GridPlaced) GUI.Label(gateRect, "G", CenteredLabelStyle());
            if (gate == selectedGate)
                DrawOutline(gateRect, Color.white, 2f);
        }

        private void DrawMovementFeatureBadges(Rect boardRect, float cellSize)
        {
            var remaining = new HashSet<BlockDefinition>(selectedLevel.Blocks);
            Vector2Int[] directions =
                { Vector2Int.left, Vector2Int.right, Vector2Int.down, Vector2Int.up };
            while (remaining.Count > 0)
            {
                BlockDefinition seed = null;
                foreach (BlockDefinition candidate in remaining)
                {
                    seed = candidate;
                    break;
                }

                var group = new List<BlockDefinition>();
                var pending = new Queue<BlockDefinition>();
                pending.Enqueue(seed);
                remaining.Remove(seed);
                while (pending.Count > 0)
                {
                    BlockDefinition current = pending.Dequeue();
                    group.Add(current);
                    foreach (Vector2Int direction in directions)
                    {
                        Vector2Int neighbourPosition = current.Position + direction;
                        BlockDefinition neighbour = null;
                        foreach (BlockDefinition candidate in remaining)
                        {
                            if (candidate.GroupId != seed.GroupId || candidate.Position != neighbourPosition) continue;
                            neighbour = candidate;
                            break;
                        }

                        if (neighbour == null) continue;
                        remaining.Remove(neighbour);
                        pending.Enqueue(neighbour);
                    }
                }

                BlockMovementMode mode = seed.Features.MovementMode;
                if (mode == BlockMovementMode.Free) continue;
                DrawMovementFeatureBadge(boardRect, cellSize, group, mode);
            }
        }

        private void DrawSelectedFeatureGroupOutline(Rect boardRect, float cellSize)
        {
            if (selectedBlock == null) return;
            List<BlockDefinition> group = GetBlockGroup(selectedBlock);
            var positions = new HashSet<Vector2Int>();
            foreach (BlockDefinition block in group) positions.Add(block.Position);

            foreach (Vector2Int positionValue in positions)
            {
                Rect cell = GetCellRect(boardRect, cellSize, positionValue);
                const float thickness = 2f;
                if (!positions.Contains(positionValue + Vector2Int.up))
                    EditorGUI.DrawRect(new Rect(cell.x, cell.y, cell.width, thickness), Color.white);
                if (!positions.Contains(positionValue + Vector2Int.down))
                    EditorGUI.DrawRect(new Rect(cell.x, cell.yMax - thickness, cell.width, thickness), Color.white);
                if (!positions.Contains(positionValue + Vector2Int.left))
                    EditorGUI.DrawRect(new Rect(cell.x, cell.y, thickness, cell.height), Color.white);
                if (!positions.Contains(positionValue + Vector2Int.right))
                    EditorGUI.DrawRect(new Rect(cell.xMax - thickness, cell.y, thickness, cell.height), Color.white);
            }
        }

        private void DrawMovementFeatureBadge(Rect boardRect, float cellSize, List<BlockDefinition> group,
            BlockMovementMode mode)
        {
            Rect bounds = GetCellRect(boardRect, cellSize, group[0].Position);
            for (int i = 1; i < group.Count; i++)
                bounds = Encapsulate(bounds, GetCellRect(boardRect, cellSize, group[i].Position));

            Rect badge = new Rect(bounds.center.x - 15f, bounds.center.y - 11f, 30f, 22f);
            EditorGUI.DrawRect(badge, new Color(0.08f, 0.1f, 0.14f, 0.9f));
            GUI.Label(badge, mode == BlockMovementMode.HorizontalOnly ? "↔" : "↕", CenteredLabelStyle());
        }

        private void DrawIceLockBadges(Rect boardRect, float cellSize)
        {
            foreach (List<BlockDefinition> group in GetAllBlockGroups())
            {
                if (group.Count == 0 || !group[0].Features.IceLockEnabled) continue;

                Rect bounds = GetCellRect(boardRect, cellSize, group[0].Position);
                for (int i = 1; i < group.Count; i++)
                    bounds = Encapsulate(bounds, GetCellRect(boardRect, cellSize, group[i].Position));

                Rect badge = new Rect(bounds.center.x - 22f, bounds.center.y - 11f, 44f, 22f);
                EditorGUI.DrawRect(badge, new Color(0.2f, 0.72f, 0.95f, 0.95f));
                GUI.Label(badge, $"ICE {group[0].Features.IceUnlockAfter}", CenteredLabelStyle());
            }
        }

        private static Rect Encapsulate(Rect first, Rect second)
        {
            float xMin = Mathf.Min(first.xMin, second.xMin);
            float yMin = Mathf.Min(first.yMin, second.yMin);
            float xMax = Mathf.Max(first.xMax, second.xMax);
            float yMax = Mathf.Max(first.yMax, second.yMax);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private void DrawCanvasHelp(Rect canvasRect, Rect boardRect)
        {
            string hint;
            switch (activeTool)
            {
                case Tool.Wall: hint = "Click cells to draw the map boundary. Wall, Corner and rotation are automatic."; break;
                case Tool.Gate: hint = "Click a colored + to place a Gate. Right-click a Gate to remove it."; break;
                case Tool.Block: hint = "Click an interior Tile to add or remove a Block cell. Part and rotation are automatic."; break;
                case Tool.Feature: hint = "Click a block to edit its features in the Block Features window."; break;
                case Tool.Eraser: hint = "Click a block, wall or gate to remove it."; break;
                default: hint = "Click to select. Grid changes rebuild the 3D board preview."; break;
            }

            GUI.Label(new Rect(canvasRect.x + 10f, canvasRect.yMax - 34f, canvasRect.width - 20f, 22f),
                hint, CenteredMiniLabelStyle());
            GUI.Label(new Rect(boardRect.x, boardRect.y - 46f, boardRect.width, 20f),
                $"{selectedLevel.DisplayName}  •  {selectedLevel.BoardWidth} × {selectedLevel.BoardHeight}  •  {selectedLevel.TimerSeconds}s",
                CenteredLabelStyle());
        }

    }
}
