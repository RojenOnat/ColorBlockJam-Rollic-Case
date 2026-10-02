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
    public sealed class LevelEditorWindow : EditorWindow
    {
        private const string LevelsFolder = "Assets/ColorBlockJam/Levels";
        private const string LevelCatalogPath = "Assets/ColorBlockJam/Levels/LevelCatalog.asset";
        private const string GameplayScenePath = "Assets/ColorBlockJam/Scenes/Gameplay.unity";
        private const string LevelRuntimePrefabPath = "Assets/ColorBlockJam/Prefab/Level/Level_Runtime.prefab";
        private const string ScenePreviewRootName = "[CBJ] Level Preview";
        private const string VisualSettingsPath = "Assets/ColorBlockJam/Settings/BoardVisualSettings.asset";
        private const string BlockMaterialPath = "Assets/ColorBlockJam/Materials/Blocks/M_Block.mat";
        private const string GateMaterialPath = "Assets/ColorBlockJam/Materials/Gates/M_Gate.mat";
        private const string WallMaterialPath = "Assets/ColorBlockJam/Materials/Walls/M_Wall.mat";
        private const float SidebarWidth = 210f;
        private const float InspectorWidth = 250f;
        private const float DoorBand = 22f;

        private enum Tool
        {
            Select,
            Wall,
            Door,
            Block,
            Feature,
            Eraser
        }

        private enum BlockPartType
        {
            Corner,
            Edge,
            Middle
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
        private DoorDefinition selectedDoor;
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

                if (activeTool == Tool.Door && !corner)
                {
                    // A gate can only replace a straight wall. Make every valid click target obvious.
                    Rect candidate = Shrink(cell, 7f);
                    EditorGUI.DrawRect(candidate, GetColor(paintColor));
                    GUI.Label(candidate, "+", CenteredLabelStyle());
                }
            }

            foreach (DoorDefinition door in selectedLevel.Doors)
                DrawDoor(boardRect, cellSize, door);

            DrawOutline(boardRect, new Color(0.7f, 0.7f, 0.75f), 2f);
        }

        private void DrawDoor(Rect boardRect, float cellSize, DoorDefinition door)
        {
            Rect doorRect = door.GridPlaced
                ? Shrink(GetCellRect(boardRect, cellSize, door.Position), 3f)
                : GetDoorRect(boardRect, cellSize, door.Edge, door.EdgePosition, door.Width);
            EditorGUI.DrawRect(doorRect, GetColor(door.Color));
            if (door.GridPlaced) GUI.Label(doorRect, "G", CenteredLabelStyle());
            if (door == selectedDoor)
                DrawOutline(doorRect, Color.white, 2f);
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
                case Tool.Door: hint = "Click a colored + to place a Gate. Right-click a Gate to remove it."; break;
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

        private void HandleBoardInput(Rect boardRect, float cellSize)
        {
            Event current = Event.current;

            if (current.type == EventType.MouseDown && current.button == 1 && activeTool == Tool.Door &&
                boardRect.Contains(current.mousePosition))
            {
                DoorDefinition gate = FindDoorAt(current.mousePosition, boardRect, cellSize);
                if (gate != null) RemoveDoor(gate);
                current.Use();
                return;
            }

            if (current.button != 0) return;

            if (current.type == EventType.MouseDown)
            {
                // The canvas is drawn before the inspector. Do not consume clicks that
                // belong to the side panels, otherwise their buttons never receive them.
                if (!boardRect.Contains(current.mousePosition)) return;

                if (activeTool == Tool.Door && boardRect.Contains(current.mousePosition))
                {
                    ReplaceWallWithGate(GetCellAt(current.mousePosition, boardRect, cellSize));
                    current.Use();
                    return;
                }

                BlockDefinition hitBlock = FindBlockAt(current.mousePosition, boardRect, cellSize);
                WallDefinition hitWall = FindWallAt(current.mousePosition, boardRect, cellSize);
                DoorDefinition hitDoor = FindDoorAt(current.mousePosition, boardRect, cellSize);

                if (activeTool == Tool.Eraser)
                {
                    if (hitBlock != null) RemoveBlock(hitBlock);
                    else if (hitWall != null) RemoveWall(hitWall);
                    else if (hitDoor != null) RemoveDoor(hitDoor);
                    current.Use();
                    return;
                }

                if (activeTool == Tool.Select)
                {
                    selectedBlock = hitBlock;
                    selectedWall = hitBlock == null && hitWall != null ? hitWall : null;
                    selectedDoor = hitBlock == null && hitWall == null ? hitDoor : null;
                    draggingBlock = selectedBlock != null;
                    Repaint();
                    current.Use();
                    return;
                }

                if (activeTool == Tool.Feature)
                {
                    selectedBlock = hitBlock;
                    selectedWall = null;
                    selectedDoor = null;
                    draggingBlock = false;
                    current.Use();
                    return;
                }

                if (activeTool == Tool.Wall && boardRect.Contains(current.mousePosition))
                {
                    ToggleWall(GetCellAt(current.mousePosition, boardRect, cellSize));
                    current.Use();
                    return;
                }

                if (activeTool == Tool.Block && boardRect.Contains(current.mousePosition))
                {
                    ToggleBlock(GetCellAt(current.mousePosition, boardRect, cellSize));
                    current.Use();
                }
            }
            else if (current.type == EventType.MouseDrag && draggingBlock && selectedBlock != null)
            {
                Vector2Int cell = GetCellAt(current.mousePosition, boardRect, cellSize);
                MoveBlock(selectedBlock, cell);
                current.Use();
            }
            else if (current.type == EventType.MouseUp)
            {
                draggingBlock = false;
            }
        }

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
            EditorGUILayout.Space(8f);
            GUILayout.Label("CAMERA", EditorStyles.boldLabel);
            LevelCameraSettings cameraSettings = selectedLevel.CameraSettings;
            Vector3 cameraPosition = EditorGUILayout.Vector3Field("Position", cameraSettings.Position);
            Vector3 cameraRotation = EditorGUILayout.Vector3Field("Rotation", cameraSettings.Rotation);
            float cameraFieldOfView = EditorGUILayout.Slider("Field of View", cameraSettings.FieldOfView, 20f, 100f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(selectedLevel, "Edit Level Settings");
                selectedLevel.SetDisplayName(displayName);
                selectedLevel.SetBoardSize(width, height);
                selectedLevel.SetTimer(timer);
                selectedLevel.SetCameraSettings(cameraPosition, cameraRotation, cameraFieldOfView);
                MarkChanged();
            }

            if (activeTool == Tool.Block || activeTool == Tool.Door)
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
                    selectedDoor = null;
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
            else if (selectedDoor != null)
            {
                EditorGUILayout.LabelField("Door", selectedDoor.Id);
                if (selectedDoor.GridPlaced)
                {
                    EditorGUILayout.Vector2IntField("Position", selectedDoor.Position);
                    EditorGUILayout.LabelField("Rotation", $"{selectedDoor.QuarterTurns * 90}° (Automatic)");
                }
                else
                {
                    EditorGUILayout.LabelField("Edge", selectedDoor.Edge.ToString());
                    EditorGUILayout.LabelField("Position", selectedDoor.EdgePosition.ToString());
                }
                EditorGUI.BeginChangeCheck();
                BlockColor color = (BlockColor)EditorGUILayout.EnumPopup("Color", selectedDoor.Color);
                int doorWidth = selectedDoor.GridPlaced
                    ? selectedDoor.Width
                    : EditorGUILayout.IntField("Width", selectedDoor.Width);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(selectedLevel, "Edit Door");
                    selectedDoor.SetColor(color);
                    selectedDoor.SetWidth(doorWidth);
                    MarkChanged();
                }
                if (GUILayout.Button("Delete Door")) RemoveDoor(selectedDoor);
            }
            else
            {
                EditorGUILayout.HelpBox("Select a block or door to edit it.", MessageType.None);
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

        private void CreateLevel()
        {
            EnsureFolder(LevelsFolder);
            string path = AssetDatabase.GenerateUniqueAssetPath($"{LevelsFolder}/Level_New.asset");
            var level = CreateInstance<LevelDefinition>();
            level.SetDisplayName(Path.GetFileNameWithoutExtension(path).Replace('_', ' '));
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
                $"{Path.GetDirectoryName(sourcePath)?.Replace('\\', '/')}/{selectedLevel.name}_Copy.asset");
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
            selectedDoor = null;
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
            selectedDoor = null;
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
                selectedDoor = null;
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
            selectedDoor = null;
            MarkChanged();
        }

        private BlockDefinition GetBlockAt(Vector2Int positionValue)
        {
            foreach (BlockDefinition block in selectedLevel.Blocks)
            {
                foreach (Vector2Int occupied in BlockShapeLibrary.GetBoardCells(block))
                    if (occupied == positionValue) return block;
            }
            return null;
        }

        private bool HasMatchingBlock(Vector2Int positionValue, BlockDefinition source)
        {
            BlockDefinition block = GetBlockAt(positionValue);
            return block != null && block.GroupId == source.GroupId;
        }

        private BlockPartType GetBlockPartType(BlockDefinition block)
        {
            Vector2Int positionValue = block.Position;
            bool left = HasMatchingBlock(positionValue + Vector2Int.left, block);
            bool right = HasMatchingBlock(positionValue + Vector2Int.right, block);
            bool bottom = HasMatchingBlock(positionValue + Vector2Int.down, block);
            bool top = HasMatchingBlock(positionValue + Vector2Int.up, block);
            int count = (left ? 1 : 0) + (right ? 1 : 0) + (bottom ? 1 : 0) + (top ? 1 : 0);

            if (count == 4) return BlockPartType.Middle;
            if (count == 2 && (left || right) && (bottom || top)) return BlockPartType.Corner;
            return BlockPartType.Edge;
        }

        private int GetBlockRotation(BlockDefinition block)
        {
            Vector2Int positionValue = block.Position;
            bool left = HasMatchingBlock(positionValue + Vector2Int.left, block);
            bool right = HasMatchingBlock(positionValue + Vector2Int.right, block);
            bool bottom = HasMatchingBlock(positionValue + Vector2Int.down, block);
            bool top = HasMatchingBlock(positionValue + Vector2Int.up, block);

            if (GetBlockPartType(block) == BlockPartType.Corner)
            {
                if (right && bottom) return 0;
                if (left && bottom) return 90;
                if (left && top) return 180;
                if (right && top) return 270;
            }

            if (!top) return 0;
            if (!right) return 90;
            if (!bottom) return 180;
            if (!left) return 270;
            return 0;
        }

        private void AddDoor(BoardEdge edge, int edgePosition)
        {
            Undo.RecordObject(selectedLevel, "Add Door");
            var door = new DoorDefinition(CreateId("Door"), paintColor, edge, edgePosition);
            selectedLevel.AddDoor(door);
            selectedDoor = door;
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
            var gate = new DoorDefinition(CreateId("Gate"), paintColor, positionValue, quarterTurns);
            selectedLevel.AddDoor(gate);
            selectedWall = null;
            selectedBlock = null;
            selectedDoor = gate;
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
            selectedDoor = null;
            MarkChanged();
        }

        private bool IsWallCell(Vector2Int positionValue)
        {
            foreach (WallDefinition wall in selectedLevel.Walls)
                if (wall.Position == positionValue) return true;
            return false;
        }

        private bool IsGateCell(Vector2Int positionValue)
        {
            foreach (DoorDefinition door in selectedLevel.Doors)
                if (door.GridPlaced && door.Position == positionValue) return true;
            return false;
        }

        private bool IsBoundaryCell(Vector2Int positionValue)
        {
            return IsWallCell(positionValue) || IsGateCell(positionValue);
        }

        private bool IsCornerCell(Vector2Int positionValue)
        {
            if (!IsWallCell(positionValue)) return false;
            bool left = IsBoundaryCell(positionValue + Vector2Int.left);
            bool right = IsBoundaryCell(positionValue + Vector2Int.right);
            bool bottom = IsBoundaryCell(positionValue + Vector2Int.down);
            bool top = IsBoundaryCell(positionValue + Vector2Int.up);
            int neighbourCount = (left ? 1 : 0) + (right ? 1 : 0) + (bottom ? 1 : 0) + (top ? 1 : 0);
            return neighbourCount == 2 && (left || right) && (bottom || top);
        }

        private int GetAutomaticRotation(Vector2Int positionValue)
        {
            bool left = IsBoundaryCell(positionValue + Vector2Int.left);
            bool right = IsBoundaryCell(positionValue + Vector2Int.right);
            bool bottom = IsBoundaryCell(positionValue + Vector2Int.down);
            bool top = IsBoundaryCell(positionValue + Vector2Int.up);

            if (!IsCornerCell(positionValue)) return left || right ? 0 : 90;
            if (right && bottom) return 0;
            if (left && bottom) return 90;
            if (left && top) return 180;
            if (right && top) return 270;
            return 0;
        }

        private HashSet<Vector2Int> FindEnclosedCells()
        {
            var exterior = new HashSet<Vector2Int>();
            var pending = new Queue<Vector2Int>();

            for (int x = 0; x < selectedLevel.BoardWidth; x++)
            {
                AddExteriorCell(new Vector2Int(x, 0), exterior, pending);
                AddExteriorCell(new Vector2Int(x, selectedLevel.BoardHeight - 1), exterior, pending);
            }

            for (int y = 0; y < selectedLevel.BoardHeight; y++)
            {
                AddExteriorCell(new Vector2Int(0, y), exterior, pending);
                AddExteriorCell(new Vector2Int(selectedLevel.BoardWidth - 1, y), exterior, pending);
            }

            Vector2Int[] directions = { Vector2Int.left, Vector2Int.right, Vector2Int.down, Vector2Int.up };
            while (pending.Count > 0)
            {
                Vector2Int current = pending.Dequeue();
                foreach (Vector2Int direction in directions)
                    AddExteriorCell(current + direction, exterior, pending);
            }

            var enclosed = new HashSet<Vector2Int>();
            for (int y = 0; y < selectedLevel.BoardHeight; y++)
            for (int x = 0; x < selectedLevel.BoardWidth; x++)
            {
                Vector2Int coordinate = new Vector2Int(x, y);
                if (!IsBoundaryCell(coordinate) && !exterior.Contains(coordinate)) enclosed.Add(coordinate);
            }
            return enclosed;
        }

        private void AddExteriorCell(Vector2Int coordinate, HashSet<Vector2Int> exterior,
            Queue<Vector2Int> pending)
        {
            if (!IsInsideBoard(coordinate) || IsBoundaryCell(coordinate) || !exterior.Add(coordinate)) return;
            pending.Enqueue(coordinate);
        }

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

        private void RemoveDoor(DoorDefinition door)
        {
            Undo.RecordObject(selectedLevel, "Delete Door");
            selectedLevel.RemoveDoor(door);
            if (selectedDoor == door) selectedDoor = null;
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
                FindPrefab("Gate", "Door", "Door_1x1"),
                FindPrefab("Block"),
                FindPrefab("Door_Arrow"));
            settings.ConfigureDirectionArrowPrefab(FindPrefab("Block_Direction_Arrow"));
            settings.ConfigureMaterials(
                AssetDatabase.LoadAssetAtPath<Material>(BlockMaterialPath),
                AssetDatabase.LoadAssetAtPath<Material>(GateMaterialPath),
                AssetDatabase.LoadAssetAtPath<Material>(WallMaterialPath));
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            return settings;
        }

        private static GameObject FindModelObject(string assetPath, string objectName)
        {
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (asset is GameObject gameObject && gameObject.name == objectName)
                    return gameObject;
            }
            return null;
        }

        private static GameObject FindPrefab(params string[] candidateNames)
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/ColorBlockJam/Prefab" });
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
                    "Create or assign Assets/ColorBlockJam/Prefab/Level/Level_Runtime.prefab before rebuilding the preview.",
                    "OK");
                return;
            }

            GameObject previewRoot = (GameObject)PrefabUtility.InstantiatePrefab(runtimePrefab);
            previewRoot.name = ScenePreviewRootName;
            Undo.RegisterCreatedObjectUndo(previewRoot, "Create Level Preview");

            BoardPreviewGenerator generator = previewRoot.GetComponentInChildren<BoardPreviewGenerator>(true);
            Camera previewCamera = previewRoot.GetComponentInChildren<Camera>(true);
            if (generator == null || previewCamera == null)
            {
                Undo.DestroyObjectImmediate(previewRoot);
                EditorUtility.DisplayDialog("Level Runtime Prefab is incomplete",
                    "The prefab needs both a BoardPreviewGenerator and a Camera.", "OK");
                return;
            }

            Undo.RecordObject(generator, "Configure Board Preview");
            generator.Configure(selectedLevel, visualSettings);
            generator.Rebuild();
            LevelCameraSettingsApplicator.Apply(previewCamera, selectedLevel.CameraSettings);
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

        private BlockDefinition FindBlockAt(Vector2 mousePosition, Rect boardRect, float cellSize)
        {
            if (!boardRect.Contains(mousePosition)) return null;
            Vector2Int cell = GetCellAt(mousePosition, boardRect, cellSize);
            foreach (BlockDefinition block in selectedLevel.Blocks)
            {
                foreach (Vector2Int occupied in BlockShapeLibrary.GetBoardCells(block))
                {
                    if (occupied == cell) return block;
                }
            }
            return null;
        }

        private WallDefinition FindWallAt(Vector2 mousePosition, Rect boardRect, float cellSize)
        {
            if (!boardRect.Contains(mousePosition)) return null;
            Vector2Int cell = GetCellAt(mousePosition, boardRect, cellSize);
            foreach (WallDefinition wall in selectedLevel.Walls)
                if (wall.Position == cell) return wall;
            return null;
        }

        private DoorDefinition FindDoorAt(Vector2 mousePosition, Rect boardRect, float cellSize)
        {
            foreach (DoorDefinition door in selectedLevel.Doors)
            {
                Rect doorRect = door.GridPlaced
                    ? GetCellRect(boardRect, cellSize, door.Position)
                    : GetDoorRect(boardRect, cellSize, door.Edge, door.EdgePosition, door.Width);
                if (doorRect.Contains(mousePosition))
                    return door;
            }
            return null;
        }

        private bool TryGetEdgeHit(Vector2 mousePosition, Rect boardRect, float cellSize,
            out BoardEdge edge, out int edgePosition)
        {
            Rect top = new Rect(boardRect.x, boardRect.y - DoorBand, boardRect.width, DoorBand);
            Rect bottom = new Rect(boardRect.x, boardRect.yMax, boardRect.width, DoorBand);
            Rect left = new Rect(boardRect.x - DoorBand, boardRect.y, DoorBand, boardRect.height);
            Rect right = new Rect(boardRect.xMax, boardRect.y, DoorBand, boardRect.height);

            if (top.Contains(mousePosition))
            {
                edge = BoardEdge.Top;
                edgePosition = Mathf.Clamp(Mathf.FloorToInt((mousePosition.x - boardRect.x) / cellSize), 0, selectedLevel.BoardWidth - 1);
                return true;
            }
            if (bottom.Contains(mousePosition))
            {
                edge = BoardEdge.Bottom;
                edgePosition = Mathf.Clamp(Mathf.FloorToInt((mousePosition.x - boardRect.x) / cellSize), 0, selectedLevel.BoardWidth - 1);
                return true;
            }
            if (left.Contains(mousePosition))
            {
                edge = BoardEdge.Left;
                edgePosition = Mathf.Clamp(selectedLevel.BoardHeight - 1 - Mathf.FloorToInt((mousePosition.y - boardRect.y) / cellSize), 0, selectedLevel.BoardHeight - 1);
                return true;
            }
            if (right.Contains(mousePosition))
            {
                edge = BoardEdge.Right;
                edgePosition = Mathf.Clamp(selectedLevel.BoardHeight - 1 - Mathf.FloorToInt((mousePosition.y - boardRect.y) / cellSize), 0, selectedLevel.BoardHeight - 1);
                return true;
            }

            edge = default;
            edgePosition = 0;
            return false;
        }

        private Rect GetDoorRect(Rect boardRect, float cellSize, BoardEdge edge, int edgePosition, int width)
        {
            switch (edge)
            {
                case BoardEdge.Top:
                    return new Rect(boardRect.x + edgePosition * cellSize + 2f, boardRect.y - DoorBand,
                        width * cellSize - 4f, DoorBand - 2f);
                case BoardEdge.Bottom:
                    return new Rect(boardRect.x + edgePosition * cellSize + 2f, boardRect.yMax + 2f,
                        width * cellSize - 4f, DoorBand - 2f);
                case BoardEdge.Left:
                    return new Rect(boardRect.x - DoorBand, boardRect.yMax - (edgePosition + width) * cellSize + 2f,
                        DoorBand - 2f, width * cellSize - 4f);
                default:
                    return new Rect(boardRect.xMax + 2f, boardRect.yMax - (edgePosition + width) * cellSize + 2f,
                        DoorBand - 2f, width * cellSize - 4f);
            }
        }

        private Vector2Int GetCellAt(Vector2 mousePosition, Rect boardRect, float cellSize)
        {
            int x = Mathf.FloorToInt((mousePosition.x - boardRect.x) / cellSize);
            int screenY = Mathf.FloorToInt((mousePosition.y - boardRect.y) / cellSize);
            int y = selectedLevel.BoardHeight - 1 - screenY;
            return new Vector2Int(x, y);
        }

        private Rect GetCellRect(Rect boardRect, float cellSize, Vector2Int cell)
        {
            return new Rect(boardRect.x + cell.x * cellSize,
                boardRect.y + (selectedLevel.BoardHeight - 1 - cell.y) * cellSize,
                cellSize, cellSize);
        }

        private bool IsInsideBoard(Vector2Int cell)
        {
            return cell.x >= 0 && cell.y >= 0 &&
                   cell.x < selectedLevel.BoardWidth && cell.y < selectedLevel.BoardHeight;
        }

        private string CreateId(string prefix) => $"{prefix}_{Guid.NewGuid():N}";

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static Rect Shrink(Rect rect, float amount)
        {
            return new Rect(rect.x + amount, rect.y + amount,
                Mathf.Max(0f, rect.width - amount * 2f), Mathf.Max(0f, rect.height - amount * 2f));
        }

        private static void DrawOutline(Rect rect, Color color, float thickness)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private static Color GetColor(BlockColor color)
        {
            switch (color)
            {
                case BlockColor.Red: return new Color(0.93f, 0.22f, 0.28f);
                case BlockColor.Blue: return new Color(0.18f, 0.48f, 0.95f);
                case BlockColor.Green: return new Color(0.24f, 0.76f, 0.36f);
                case BlockColor.Yellow: return new Color(1f, 0.78f, 0.12f);
                case BlockColor.Purple: return new Color(0.64f, 0.31f, 0.88f);
                default: return new Color(1f, 0.46f, 0.12f);
            }
        }

        private static GUIStyle CenteredLabelStyle()
        {
            return new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.8f, 0.8f, 0.84f) }
            };
        }

        private static GUIStyle CenteredMiniLabelStyle()
        {
            return new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.62f, 0.62f, 0.67f) }
            };
        }
    }
}
