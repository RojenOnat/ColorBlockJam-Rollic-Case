using System.Collections.Generic;
using ColorBlockJam.Gameplay;
using ColorBlockJam.Levels;
using UnityEngine;

namespace ColorBlockJam.Board
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class BoardPreviewGenerator : MonoBehaviour
    {
        public const string GeneratedRootName = "Generated Board";

        [SerializeField] private LevelDefinition level;
        [SerializeField] private BoardVisualSettings visualSettings;

        public LevelDefinition Level => level;
        public BoardVisualSettings VisualSettings => visualSettings;

        private void OnEnable()
        {
            RestoreGeneratedBlockColors();
        }

        public void Configure(LevelDefinition levelDefinition, BoardVisualSettings settings)
        {
            level = levelDefinition;
            visualSettings = settings;
        }

        [ContextMenu("Rebuild Board")]
        public void Rebuild()
        {
            ClearGenerated();
            if (level == null || visualSettings == null) return;
            level.EnsureBlockGroupIds();

            Transform generatedRoot = new GameObject(GeneratedRootName).transform;
            generatedRoot.SetParent(transform, false);

            HashSet<Vector2Int> enclosedCells = BoardTopology.FindEnclosedCells(level);
            BoardGridState board = generatedRoot.gameObject.AddComponent<BoardGridState>();
            board.Configure(level.BoardWidth, level.BoardHeight, visualSettings.CellSize, enclosedCells);

            BuildTiles(generatedRoot, enclosedCells);
            BuildBlocks(generatedRoot, board);
            BuildPlacedWalls(generatedRoot);
            BuildGates(generatedRoot, enclosedCells, board);
        }

        [ContextMenu("Clear Generated Board")]
        public void ClearGenerated()
        {
            Transform existing = transform.Find(GeneratedRootName);
            if (existing == null) return;

            if (Application.isPlaying) Destroy(existing.gameObject);
            else DestroyImmediate(existing.gameObject);
        }

        private void BuildTiles(Transform root, HashSet<Vector2Int> enclosedCells)
        {
            GameObject prefab = visualSettings.TilePrefab;
            if (prefab == null) return;

            Transform tileRoot = CreateGroup("Tiles", root);
            foreach (Vector2Int cell in enclosedCells)
            {
                Vector3 position = CellCenter(cell.x, cell.y) + visualSettings.TileOffset;
                Create(prefab, position, visualSettings.TileRotation, tileRoot, $"Tile_{cell.x}_{cell.y}");
            }
        }

        private void BuildGates(Transform root, HashSet<Vector2Int> enclosedCells, BoardGridState board)
        {
            if (visualSettings.GatePrefab == null) return;
            Transform gateRoot = CreateGroup("Gates", root);

            var remainingGridGates = new HashSet<DoorDefinition>();
            foreach (DoorDefinition gate in level.Doors)
            {
                if (gate.GridPlaced) remainingGridGates.Add(gate);
            }

            int groupIndex = 0;
            while (remainingGridGates.Count > 0)
            {
                DoorDefinition seed = null;
                foreach (DoorDefinition candidate in remainingGridGates)
                {
                    seed = candidate;
                    break;
                }

                HashSet<DoorDefinition> component = CollectConnectedGates(seed, remainingGridGates);
                Vector3 groupCenter = GetGateGroupCenter(component);
                Transform group = CreateGroup($"GateGroup_{seed.Color}_{groupIndex++}", gateRoot);
                group.localPosition = groupCenter;
                group.gameObject.AddComponent<ColorIdentity>().Configure(seed.Color);
                GateGroup gateGroup = group.gameObject.AddComponent<GateGroup>();
                var gateCells = new List<Vector2Int>();
                foreach (DoorDefinition gate in component) gateCells.Add(gate.Position);
                gateGroup.Configure(gateCells, GetGateExitDirection(component, enclosedCells));
                board.Register(gateGroup);

                foreach (DoorDefinition gate in component)
                {
                    Quaternion gridRotation = Quaternion.Euler(0f, gate.QuarterTurns * 90f, 0f) *
                                              visualSettings.GateBaseRotation;
                    Vector3 gridPosition = CellCenter(gate.Position.x, gate.Position.y) - groupCenter +
                                           gridRotation * visualSettings.GateOffset;
                    Create(visualSettings.GatePrefab, gridPosition, gridRotation, group,
                        $"GatePart_{gate.Position.x}_{gate.Position.y}");
                }

                ApplyMaterial(group.gameObject, visualSettings.GateMaterial);
                ApplyColor(group.gameObject, seed.Color);
                CreateGateArrow(group, seed.Color, gateGroup.ExitDirection);
            }

            foreach (DoorDefinition gate in level.Doors)
            {
                if (gate.GridPlaced) continue;
                int edgeLength = GetEdgeLength(gate.Edge);
                int gateWidth = Mathf.Clamp(gate.Width, 1, edgeLength);
                int start = Mathf.Clamp(gate.EdgePosition, 0, Mathf.Max(0, edgeLength - gateWidth));
                Vector3 gatePosition = EdgeSlotCenter(gate.Edge, start, gateWidth);
                Quaternion rotation = EdgeRotation(gate.Edge) * visualSettings.GateBaseRotation;
                Transform group = CreateGroup($"GateGroup_{gate.Color}_{groupIndex++}", gateRoot);
                group.localPosition = gatePosition;
                group.gameObject.AddComponent<ColorIdentity>().Configure(gate.Color);
                GateGroup gateGroup = group.gameObject.AddComponent<GateGroup>();
                gateGroup.Configure(System.Array.Empty<Vector2Int>(), EdgeDirection(gate.Edge));
                board.Register(gateGroup);
                Create(visualSettings.GatePrefab, rotation * visualSettings.GateOffset,
                    rotation, group, $"GatePart_{gate.Edge}_{start}");
                ApplyMaterial(group.gameObject, visualSettings.GateMaterial);
                ApplyColor(group.gameObject, gate.Color);
                CreateGateArrow(group, gate.Color, gateGroup.ExitDirection);
            }
        }

        private void CreateGateArrow(Transform gateGroup, BlockColor color, Vector2Int exitDirection)
        {
            if (visualSettings.DoorArrowPrefab == null || exitDirection == Vector2Int.zero) return;

            Quaternion rotation = DirectionRotation(exitDirection) * visualSettings.DoorArrowBaseRotation;
            Vector3 arrowPosition = visualSettings.DoorArrowOffset;
            arrowPosition.y = 2f;
            GameObject arrow = Create(visualSettings.DoorArrowPrefab, arrowPosition,
                rotation, gateGroup, "DoorArrow");
            if (arrow.transform.childCount > 0)
            {
                Transform visual = arrow.transform.GetChild(0);
                Vector3 visualPosition = visual.localPosition;
                visualPosition.z = 0f;
                visual.localPosition = visualPosition;
            }
            ApplyMaterial(arrow, visualSettings.GateMaterial);
            ApplyColor(arrow, color, 0.85f);
        }

        private static Quaternion DirectionRotation(Vector2Int direction)
        {
            if (direction == Vector2Int.right) return Quaternion.Euler(0f, 90f, 0f);
            if (direction == Vector2Int.down) return Quaternion.Euler(0f, 180f, 0f);
            if (direction == Vector2Int.left) return Quaternion.Euler(0f, 270f, 0f);
            return Quaternion.identity;
        }

        private HashSet<DoorDefinition> CollectConnectedGates(DoorDefinition seed,
            HashSet<DoorDefinition> remaining)
        {
            var component = new HashSet<DoorDefinition>();
            var pending = new Queue<DoorDefinition>();
            remaining.Remove(seed);
            component.Add(seed);
            pending.Enqueue(seed);

            Vector2Int[] directions = { Vector2Int.left, Vector2Int.right, Vector2Int.down, Vector2Int.up };
            while (pending.Count > 0)
            {
                DoorDefinition current = pending.Dequeue();
                foreach (Vector2Int direction in directions)
                {
                    DoorDefinition neighbour = GetGridGateAt(current.Position + direction);
                    if (neighbour == null || neighbour.Color != seed.Color || !remaining.Remove(neighbour))
                        continue;
                    component.Add(neighbour);
                    pending.Enqueue(neighbour);
                }
            }
            return component;
        }

        private DoorDefinition GetGridGateAt(Vector2Int coordinate)
        {
            foreach (DoorDefinition gate in level.Doors)
                if (gate.GridPlaced && gate.Position == coordinate) return gate;
            return null;
        }

        private Vector3 GetGateGroupCenter(HashSet<DoorDefinition> component)
        {
            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;
            foreach (DoorDefinition gate in component)
            {
                minX = Mathf.Min(minX, gate.Position.x);
                maxX = Mathf.Max(maxX, gate.Position.x);
                minY = Mathf.Min(minY, gate.Position.y);
                maxY = Mathf.Max(maxY, gate.Position.y);
            }
            return (CellCenter(minX, minY) + CellCenter(maxX, maxY)) * 0.5f;
        }

        private static Vector2Int GetGateExitDirection(HashSet<DoorDefinition> component,
            HashSet<Vector2Int> enclosedCells)
        {
            Vector2Int[] directions = { Vector2Int.left, Vector2Int.right, Vector2Int.down, Vector2Int.up };
            Vector2Int inward = Vector2Int.zero;
            int bestScore = 0;
            foreach (Vector2Int direction in directions)
            {
                int score = 0;
                foreach (DoorDefinition gate in component)
                    if (enclosedCells.Contains(gate.Position + direction)) score++;
                if (score <= bestScore) continue;
                bestScore = score;
                inward = direction;
            }
            return -inward;
        }

        private static Vector2Int EdgeDirection(BoardEdge edge)
        {
            switch (edge)
            {
                case BoardEdge.Right: return Vector2Int.right;
                case BoardEdge.Bottom: return Vector2Int.down;
                case BoardEdge.Left: return Vector2Int.left;
                default: return Vector2Int.up;
            }
        }

        private void BuildBlocks(Transform root, BoardGridState board)
        {
            Transform blockRoot = CreateGroup("Blocks", root);
            GameObject prefab = visualSettings.BlockPrefab;
            if (prefab == null) return;

            var remaining = new HashSet<BlockDefinition>(level.Blocks);
            int groupIndex = 0;
            while (remaining.Count > 0)
            {
                BlockDefinition seed = null;
                foreach (BlockDefinition candidate in remaining)
                {
                    seed = candidate;
                    break;
                }

                HashSet<BlockDefinition> component = CollectConnectedBlock(seed, remaining);
                Vector3 groupCenter = GetBlockGroupCenter(component);
                Transform group = CreateGroup($"BlockGroup_{seed.Color}_{groupIndex++}", blockRoot);
                group.localPosition = groupCenter;
                group.gameObject.layer = prefab.layer;
                group.gameObject.AddComponent<ColorIdentity>().Configure(seed.Color);

                HashSet<BlockDefinition> innerCornerCells =
                    BuildInnerCorners(group, prefab, component, groupCenter);
                foreach (BlockDefinition block in component)
                {
                    if (innerCornerCells.Contains(block)) continue;
                    BlockVisualPart partType = BlockTopology.GetVisualPart(level, block);

                    float angle = BlockTopology.GetVisualRotation(level, block);
                    Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
                    Vector3 position = CellCenter(block.Position.x, block.Position.y) - groupCenter +
                                       rotation * visualSettings.BlockOffset;
                    GameObject instance = Create(prefab, position, Quaternion.identity, group,
                        $"Block_{block.Color}_{partType}_{block.Position.x}_{block.Position.y}");
                    SetBlockRotation(instance.transform, rotation);
                    Transform activePart = SetActiveBlockPart(instance, partType);
                    CenterBlockPartOnCell(instance.transform, activePart);
                    ApplyMaterial(instance, visualSettings.BlockMaterial);
                    ClearMaterialOverrides(instance);
                    ApplyColor(instance, block.Color);
                }

                var occupiedCells = new List<Vector2Int>();
                foreach (BlockDefinition block in component) occupiedCells.Add(block.Position);
                GridMovableBlock movableBlock = group.gameObject.AddComponent<GridMovableBlock>();
                IceLockFeature iceLock = group.gameObject.AddComponent<IceLockFeature>();
                iceLock.Configure(board, seed.Features, visualSettings, seed.Color);
                movableBlock.Configure(board, occupiedCells, seed.Features);
                CreateDirectionFeatureVisual(group, component, seed.Features.MovementMode);

            }
        }

        private void CreateDirectionFeatureVisual(Transform blockGroup, HashSet<BlockDefinition> component,
            BlockMovementMode movementMode)
        {
            if (movementMode == BlockMovementMode.Free || visualSettings.DirectionArrowPrefab == null) return;

            bool horizontal = movementMode == BlockMovementMode.HorizontalOnly;
            // A physical modular block occupies a 2 x 2 editor-grid area.
            // The arrow variants represent physical block units, not individual grid cells.
            int span = Mathf.CeilToInt(GetBlockSpan(component, horizontal) / 2f);
            Quaternion rotation = horizontal ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f);
            GameObject arrow = Create(visualSettings.DirectionArrowPrefab, Vector3.up * -0.25f, rotation,
                blockGroup, $"DirectionFeature_{movementMode}");
            SetDirectionArrowVariant(arrow.transform, span);
        }

        private static void ClearMaterialOverrides(GameObject instance)
        {
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                renderer.SetPropertyBlock(new MaterialPropertyBlock());
        }

        private static int GetBlockSpan(IEnumerable<BlockDefinition> component, bool horizontal)
        {
            int minimum = int.MaxValue;
            int maximum = int.MinValue;
            foreach (BlockDefinition block in component)
            {
                int value = horizontal ? block.Position.x : block.Position.y;
                minimum = Mathf.Min(minimum, value);
                maximum = Mathf.Max(maximum, value);
            }

            return maximum - minimum + 1;
        }

        private static void SetDirectionArrowVariant(Transform arrow, int span)
        {
            Transform variants = arrow.Find("Visual");
            if (variants == null) variants = arrow;
            if (variants.childCount == 0) return;

            int activeIndex = Mathf.Clamp(span - 1, 0, variants.childCount - 1);
            for (int i = 0; i < variants.childCount; i++)
                variants.GetChild(i).gameObject.SetActive(i == activeIndex);
        }

        private HashSet<BlockDefinition> CollectConnectedBlock(BlockDefinition seed,
            HashSet<BlockDefinition> remaining)
        {
            var component = new HashSet<BlockDefinition>();
            var pending = new Queue<BlockDefinition>();
            remaining.Remove(seed);
            component.Add(seed);
            pending.Enqueue(seed);

            Vector2Int[] directions = { Vector2Int.left, Vector2Int.right, Vector2Int.down, Vector2Int.up };
            while (pending.Count > 0)
            {
                BlockDefinition current = pending.Dequeue();
                foreach (Vector2Int direction in directions)
                {
                    BlockDefinition neighbour = BlockTopology.GetBlockAt(level, current.Position + direction);
                    if (neighbour == null || neighbour.GroupId != seed.GroupId || !remaining.Remove(neighbour))
                        continue;
                    component.Add(neighbour);
                    pending.Enqueue(neighbour);
                }
            }
            return component;
        }

        private Vector3 GetBlockGroupCenter(HashSet<BlockDefinition> component)
        {
            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;
            foreach (BlockDefinition block in component)
            {
                minX = Mathf.Min(minX, block.Position.x);
                maxX = Mathf.Max(maxX, block.Position.x);
                minY = Mathf.Min(minY, block.Position.y);
                maxY = Mathf.Max(maxY, block.Position.y);
            }

            return (CellCenter(minX, minY) + CellCenter(maxX, maxY)) * 0.5f;
        }

        private HashSet<BlockDefinition> BuildInnerCorners(Transform blockRoot, GameObject prefab,
            HashSet<BlockDefinition> component, Vector3 groupCenter)
        {
            var consumed = new HashSet<BlockDefinition>();
            for (int y = 0; y < level.BoardHeight - 1; y++)
            {
                for (int x = 0; x < level.BoardWidth - 1; x++)
                {
                    BlockDefinition bottomLeft = BlockTopology.GetBlockAt(level, new Vector2Int(x, y));
                    BlockDefinition bottomRight = BlockTopology.GetBlockAt(level, new Vector2Int(x + 1, y));
                    BlockDefinition topLeft = BlockTopology.GetBlockAt(level, new Vector2Int(x, y + 1));
                    BlockDefinition topRight = BlockTopology.GetBlockAt(level, new Vector2Int(x + 1, y + 1));

                    BlockDefinition anchor;
                    BlockDefinition first;
                    BlockDefinition second;
                    BlockDefinition third;
                    float angle;

                    if (topRight == null && HaveSameGroup(bottomLeft, bottomRight, topLeft) &&
                        HasMatchingBlock(topLeft.Position + Vector2Int.up, topLeft) &&
                        HasMatchingBlock(bottomRight.Position + Vector2Int.right, bottomRight))
                    {
                        anchor = topLeft;
                        first = bottomLeft;
                        second = bottomRight;
                        third = topLeft;
                        angle = 0f;
                    }
                    else if (bottomRight == null && HaveSameGroup(bottomLeft, topLeft, topRight) &&
                             HasMatchingBlock(topRight.Position + Vector2Int.right, topRight) &&
                             HasMatchingBlock(bottomLeft.Position + Vector2Int.down, bottomLeft))
                    {
                        anchor = topRight;
                        first = bottomLeft;
                        second = topLeft;
                        third = topRight;
                        angle = 90f;
                    }
                    else if (bottomLeft == null && HaveSameGroup(bottomRight, topLeft, topRight) &&
                             HasMatchingBlock(bottomRight.Position + Vector2Int.down, bottomRight) &&
                             HasMatchingBlock(topLeft.Position + Vector2Int.left, topLeft))
                    {
                        anchor = bottomRight;
                        first = bottomRight;
                        second = topLeft;
                        third = topRight;
                        angle = 180f;
                    }
                    else if (topLeft == null && HaveSameGroup(bottomLeft, bottomRight, topRight) &&
                             HasMatchingBlock(bottomLeft.Position + Vector2Int.left, bottomLeft) &&
                             HasMatchingBlock(topRight.Position + Vector2Int.up, topRight))
                    {
                        anchor = bottomLeft;
                        first = bottomLeft;
                        second = bottomRight;
                        third = topRight;
                        angle = 270f;
                    }
                    else
                    {
                        continue;
                    }

                    if (!component.Contains(first) || !component.Contains(second) || !component.Contains(third) ||
                        consumed.Contains(first) || consumed.Contains(second) || consumed.Contains(third))
                        continue;

                    Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
                    Vector3 position = CellCenter(anchor.Position.x, anchor.Position.y) - groupCenter +
                                       rotation * visualSettings.BlockOffset;
                    GameObject instance = Create(prefab, position, Quaternion.identity, blockRoot,
                        $"Block_{anchor.Color}_InnerCorner_{anchor.Position.x}_{anchor.Position.y}");
                    SetBlockRotation(instance.transform, rotation);
                    SetActiveBlockPart(instance, BlockVisualPart.InnerCorner);
                    ApplyMaterial(instance, visualSettings.BlockMaterial);
                    ClearMaterialOverrides(instance);
                    ApplyColor(instance, anchor.Color);

                    consumed.Add(first);
                    consumed.Add(second);
                    consumed.Add(third);
                }
            }
            return consumed;
        }

        private static bool HaveSameGroup(BlockDefinition first, BlockDefinition second, BlockDefinition third)
        {
            return first != null && second != null && third != null &&
                   first.GroupId == second.GroupId && second.GroupId == third.GroupId;
        }

        private static void SetBlockRotation(Transform blockRoot, Quaternion rotation)
        {
            Transform rotationRoot = blockRoot.childCount > 0
                ? blockRoot.GetChild(0)
                : blockRoot;
            rotationRoot.localRotation = rotation;
        }

        private static Transform SetActiveBlockPart(GameObject instance, BlockVisualPart activePart)
        {
            string activeName = activePart == BlockVisualPart.Corner
                ? "Block_Corner"
                : activePart == BlockVisualPart.Middle
                    ? "Block_Center"
                    : activePart == BlockVisualPart.InnerCorner
                        ? "ModularBlock_InnerCorner"
                        : "Block_Edge";

            Transform activeTransform = null;
            foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "Block_Corner" || child.name == "Block_Edge" ||
                    child.name == "Block_Center" || child.name == "ModularBlock_InnerCorner")
                {
                    bool active = child.name == activeName;
                    child.gameObject.SetActive(active);
                    if (active) activeTransform = child;
                }
            }
            return activeTransform;
        }

        private static void CenterBlockPartOnCell(Transform blockRoot, Transform activePart)
        {
            if (activePart == null) return;

            Renderer[] renderers = activePart.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            Vector3 correction = blockRoot.position - bounds.center;
            correction.y = 0f;
            activePart.position += correction;
        }

        private bool HasMatchingBlock(Vector2Int coordinate, BlockDefinition source)
        {
            return BlockTopology.IsSameGroupAt(level, source, coordinate);
        }

        private static void ApplyColor(GameObject instance, BlockColor color, float brightness = 1f)
        {
            Color value;
            switch (color)
            {
                case BlockColor.Red: value = new Color(0.93f, 0.22f, 0.28f); break;
                case BlockColor.Blue: value = new Color(0.18f, 0.48f, 0.95f); break;
                case BlockColor.Green: value = new Color(0.24f, 0.76f, 0.36f); break;
                case BlockColor.Yellow: value = new Color(1f, 0.78f, 0.12f); break;
                case BlockColor.Purple: value = new Color(0.64f, 0.31f, 0.88f); break;
                default: value = new Color(1f, 0.46f, 0.12f); break;
            }

            value = new Color(value.r * brightness, value.g * brightness, value.b * brightness, value.a);

            var properties = new MaterialPropertyBlock();
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>())
            {
                renderer.GetPropertyBlock(properties);
                properties.SetColor("_BaseColor", value);
                properties.SetColor("_Color", value);
                renderer.SetPropertyBlock(properties);
            }
        }

        private static void ApplyMaterial(GameObject instance, Material material)
        {
            if (material == null) return;

            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++) materials[i] = material;
                renderer.sharedMaterials = materials;
            }
        }

        private void RestoreGeneratedBlockColors()
        {
            Transform generatedRoot = transform.Find(GeneratedRootName);
            if (generatedRoot == null) return;

            Transform blocks = generatedRoot.Find("Blocks");
            if (blocks == null) return;

            foreach (Transform group in blocks)
            {
                foreach (BlockColor color in System.Enum.GetValues(typeof(BlockColor)))
                {
                    if (!group.name.StartsWith($"BlockGroup_{color}_", System.StringComparison.Ordinal))
                        continue;

                    if (group.GetComponent<IceLockFeature>() == null ||
                        !group.GetComponent<IceLockFeature>().IsLocked)
                        ApplyColor(group.gameObject, color);
                    RestoreDirectionFeatureMaterialColor(group);
                    break;
                }
            }

            Transform gates = generatedRoot.Find("Gates");
            if (gates == null) return;

            foreach (Transform gate in gates)
            {
                foreach (BlockColor color in System.Enum.GetValues(typeof(BlockColor)))
                {
                    if (!gate.name.StartsWith($"GateGroup_{color}_", System.StringComparison.Ordinal)) continue;
                    ApplyColor(gate.gameObject, color);
                    Transform arrow = gate.Find("DoorArrow");
                    if (arrow != null) ApplyColor(arrow.gameObject, color, 0.85f);
                    break;
                }
            }
        }

        private static void RestoreDirectionFeatureMaterialColor(Transform blockGroup)
        {
            foreach (Transform child in blockGroup)
            {
                if (!child.name.StartsWith("DirectionFeature_", System.StringComparison.Ordinal)) continue;
                foreach (Renderer renderer in child.GetComponentsInChildren<Renderer>(true))
                    renderer.SetPropertyBlock(new MaterialPropertyBlock());
            }
        }

        private void BuildPerimeter(Transform root)
        {
            Transform wallRoot = CreateGroup("Walls", root);
            var occupiedGateSlots = new HashSet<GateSlot>();

            foreach (DoorDefinition gate in level.Doors)
            {
                int edgeLength = GetEdgeLength(gate.Edge);
                int gateWidth = Mathf.Clamp(gate.Width, 1, edgeLength);
                int start = Mathf.Clamp(gate.EdgePosition, 0, Mathf.Max(0, edgeLength - gateWidth));
                for (int offset = 0; offset < gateWidth; offset++)
                    occupiedGateSlots.Add(new GateSlot(gate.Edge, start + offset));

                if (visualSettings.GatePrefab != null)
                {
                    Vector3 gatePosition = EdgeSlotCenter(gate.Edge, start, gateWidth);
                    Quaternion rotation = EdgeRotation(gate.Edge) * visualSettings.GateBaseRotation;
                    Vector3 offset = rotation * visualSettings.GateOffset;
                    Create(visualSettings.GatePrefab, gatePosition + offset, rotation, wallRoot,
                        $"Gate_{gate.Edge}_{start}");
                }
            }

            BuildStraightEdge(BoardEdge.Top, level.BoardWidth, occupiedGateSlots, wallRoot);
            BuildStraightEdge(BoardEdge.Right, level.BoardHeight, occupiedGateSlots, wallRoot);
            BuildStraightEdge(BoardEdge.Bottom, level.BoardWidth, occupiedGateSlots, wallRoot);
            BuildStraightEdge(BoardEdge.Left, level.BoardHeight, occupiedGateSlots, wallRoot);
            BuildCorners(wallRoot);
        }

        private void BuildPlacedWalls(Transform root)
        {
            Transform wallRoot = CreateGroup("Walls", root);
            foreach (WallDefinition wall in level.Walls)
            {
                bool corner = IsCornerCell(wall.Position);
                GameObject prefab = corner
                    ? visualSettings.CornerWallPrefab
                    : visualSettings.StraightWallPrefab;
                if (prefab == null) continue;

                float angle = GetAutomaticRotation(wall.Position);
                Quaternion baseRotation = corner
                    ? visualSettings.CornerBaseRotation
                    : visualSettings.StraightWallBaseRotation;
                Vector3 offset = corner ? visualSettings.CornerOffset : visualSettings.WallOffset;
                Quaternion rotation = Quaternion.Euler(0f, angle, 0f) * baseRotation;
                Vector3 position = CellCenter(wall.Position.x, wall.Position.y) + rotation * offset;
                GameObject instance = Create(prefab, position, rotation, wallRoot,
                    $"{(corner ? "Corner" : "Wall")}_{wall.Position.x}_{wall.Position.y}");
                ApplyMaterial(instance, visualSettings.WallMaterial);
            }
        }

        private bool IsCornerCell(Vector2Int coordinate)
            => BoardTopology.IsCorner(level, coordinate);

        private float GetAutomaticRotation(Vector2Int coordinate)
            => BoardTopology.GetAutomaticRotation(level, coordinate);

        private void BuildStraightEdge(BoardEdge edge, int length, HashSet<GateSlot> gateSlots, Transform parent)
        {
            GameObject prefab = visualSettings.StraightWallPrefab;
            if (prefab == null) return;

            Quaternion rotation = EdgeRotation(edge) * visualSettings.StraightWallBaseRotation;
            Vector3 rotatedOffset = rotation * visualSettings.WallOffset;
            for (int position = 0; position < length; position++)
            {
                if (gateSlots.Contains(new GateSlot(edge, position))) continue;
                GameObject instance = Create(prefab, EdgeSlotCenter(edge, position, 1) + rotatedOffset,
                    rotation, parent, $"Wall_{edge}_{position}");
                ApplyMaterial(instance, visualSettings.WallMaterial);
            }
        }

        private void BuildCorners(Transform parent)
        {
            GameObject prefab = visualSettings.CornerWallPrefab;
            if (prefab == null) return;

            float halfWidth = level.BoardWidth * visualSettings.CellSize * 0.5f;
            float halfHeight = level.BoardHeight * visualSettings.CellSize * 0.5f;
            CreateCorner(prefab, new Vector3(-halfWidth, 0f, halfHeight), 0f, "Corner_TopLeft", parent);
            CreateCorner(prefab, new Vector3(halfWidth, 0f, halfHeight), 90f, "Corner_TopRight", parent);
            CreateCorner(prefab, new Vector3(halfWidth, 0f, -halfHeight), 180f, "Corner_BottomRight", parent);
            CreateCorner(prefab, new Vector3(-halfWidth, 0f, -halfHeight), 270f, "Corner_BottomLeft", parent);
        }

        private void CreateCorner(GameObject prefab, Vector3 position, float yRotation, string objectName, Transform parent)
        {
            Quaternion rotation = Quaternion.Euler(0f, yRotation, 0f) * visualSettings.CornerBaseRotation;
            GameObject instance = Create(prefab, position + rotation * visualSettings.CornerOffset,
                rotation, parent, objectName);
            ApplyMaterial(instance, visualSettings.WallMaterial);
        }

        private Vector3 CellCenter(int x, int y)
        {
            float xPosition = (x - (level.BoardWidth - 1) * 0.5f) * visualSettings.CellSize;
            float zPosition = (y - (level.BoardHeight - 1) * 0.5f) * visualSettings.CellSize;
            return new Vector3(xPosition, 0f, zPosition);
        }

        private Vector3 EdgeSlotCenter(BoardEdge edge, int start, int width)
        {
            float cellSize = visualSettings.CellSize;
            float halfWidth = level.BoardWidth * cellSize * 0.5f;
            float halfHeight = level.BoardHeight * cellSize * 0.5f;
            float center = (start + (width - 1) * 0.5f);

            switch (edge)
            {
                case BoardEdge.Top:
                    return new Vector3((center - (level.BoardWidth - 1) * 0.5f) * cellSize, 0f, halfHeight);
                case BoardEdge.Right:
                    return new Vector3(halfWidth, 0f, (center - (level.BoardHeight - 1) * 0.5f) * cellSize);
                case BoardEdge.Bottom:
                    return new Vector3((center - (level.BoardWidth - 1) * 0.5f) * cellSize, 0f, -halfHeight);
                default:
                    return new Vector3(-halfWidth, 0f, (center - (level.BoardHeight - 1) * 0.5f) * cellSize);
            }
        }

        private int GetEdgeLength(BoardEdge edge)
        {
            return edge == BoardEdge.Top || edge == BoardEdge.Bottom
                ? level.BoardWidth
                : level.BoardHeight;
        }

        private static Quaternion EdgeRotation(BoardEdge edge)
        {
            switch (edge)
            {
                case BoardEdge.Right: return Quaternion.Euler(0f, 90f, 0f);
                case BoardEdge.Bottom: return Quaternion.Euler(0f, 180f, 0f);
                case BoardEdge.Left: return Quaternion.Euler(0f, 270f, 0f);
                default: return Quaternion.identity;
            }
        }

        private static Transform CreateGroup(string groupName, Transform parent)
        {
            Transform group = new GameObject(groupName).transform;
            group.SetParent(parent, false);
            return group;
        }

        private static GameObject Create(GameObject prefab, Vector3 localPosition, Quaternion localRotation,
            Transform parent, string objectName)
        {
            GameObject instance = Instantiate(prefab, parent);
            instance.name = objectName;
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;
            return instance;
        }

        private readonly struct GateSlot
        {
            private readonly BoardEdge edge;
            private readonly int position;

            public GateSlot(BoardEdge edge, int position)
            {
                this.edge = edge;
                this.position = position;
            }

            public override bool Equals(object obj)
            {
                return obj is GateSlot other && other.edge == edge && other.position == position;
            }

            public override int GetHashCode() => ((int)edge * 397) ^ position;
        }
    }
}
